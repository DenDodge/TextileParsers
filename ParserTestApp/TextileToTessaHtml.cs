using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using ColorCode;
using HtmlAgilityPack;
using NTextileCore;
using Tessa.Extensions.Console.Helpers.TextConversion.Models;

namespace TestApp
{
    public class TextileToTessaHtml
    {
        #region Fiedls

        /// <summary>
        /// Приложенные к тексту файлы (копия основного словаря).
        /// Копия необходима из-за того, что словарь модифицируется при работе алгоритма данного класса.
        /// Key - Идентификатор приложенного файла.
        /// Value Name - Имя приложенного файла.
        /// Value Path - Путь к приложенному файлу.
        /// </summary>
        private Dictionary<Guid, (string Name, string Path)> textAttachments;

        /// <summary>
        /// Обработанные ссылки.
        /// Key - идентификатор ссылки.
        /// Value - значение ссылки (Uri).
        /// </summary>
        private Dictionary<Guid, string> uriAttachments;

        /// <summary>
        /// Модель текста для описания инцидента.
        /// </summary>
        private JsonDescription description;

        /// <summary>
        /// Объект для логирования.
        /// </summary>
        //private IConsoleLogger logger;

        /// <summary>
        /// Идентификатор текущего инцидента.
        /// </summary>
        private readonly int issueId;

        /// <summary>
        /// Класс, обеспечивающий подсветку синтаксиса в блоках кода.
        /// </summary>
        private HtmlFormatter codeHighlighter;

        /// <summary>
        /// Список обработчиков тегов разметки.
        /// </summary>
        private readonly List<(string XPath, Action<HtmlNode> Handler)> documentTransitions;
        
        /// <summary>
        /// Максимальная длина описания ссылки.
        /// </summary>
        private const int MaxLinkCaptionLength = 50;
        
        #endregion
        
        #region Static Fields

        /// <summary>
        /// Пустое описание инцидента.
        /// Применяется, если входящая строка пустая или с пробелами.
        /// </summary>
        private static readonly string EmptyString = "{\"Text\":\"<div class=\"forum-div\"><p><span> </span></p></div>\"}";

        /// <summary>
        /// Флаг, определяющий использование раскраски кода.
        /// </summary>
        public static bool UseHighlightCode;

        #endregion

        #region Constructors

        /// <summary>
        /// Преобразователь из Textile в Tessa HTML.
        /// </summary>
        /// <param name="logger">Объект для логирования.</param>
        /// <param name="currentIssueId">Идентификатор текущего инцидента.</param>
        // public TextileToTessaHtml(IConsoleLogger logger, int currentIssueId)
        // {
        //     this.logger = logger;
        //     this.issueId = currentIssueId;
        //     // Список определяет порядок и применяемые трансформации для результирующего `HTML` сообщения.
        //     // Изменение порядка следования элементов в данном списке может привести к нечитаемому или искажённому тексту, т.к. последовательность применения трансформаций ОЧЕНЬ ВАЖНА!
        //     // НЕ МЕНЯЙТЕ ПОСЛЕДОВАТЕЛЬНОСТЬ ЕСЛИ ВЫ НЕ УВЕРЕНЫ В ПРАВИЛЬНОСТИ СВОИХ ДЕЙСТВИЙ!
        //     this.documentTransitions = new List<(string XPath, Action<HtmlNode> Handler)>
        //     {
        //         ("//table", this.HandleTable),
        //         ("//*[self::strong or self::b]", HandleBold),
        //         ("//*[self::em or self::i]", HandleItalic),
        //         ("//ins", HandleUnderline),
        //         ("//del", HandleCrossedOut),
        //         ("//*[self::h1 or self::h2 or self::h3 or self::h4 or self::h5 or self::h6]", HandleHeader),
        //         ("//ul", HandleUnorderedList),
        //         ("//ol", HandleOrderedList),
        //         ("//li", HandleListItem),
        //         ("//blockquote", HandleBlockquote),
        //         ("//code", this.HandleCode),
        //         ("//pre", HandlePre),
        //         ("//a", this.HandleLink),
        //         ("//img", this.HandleImg),
        //         ("//p", HandleParagraph),
        //     };
        // }

        #endregion

        /// <summary>
        /// Преобразовать строку из Textile в TessaHtml
        /// </summary>
        /// <param name="mainString">Строка для преобразования.</param>
        /// <param name="textAttachments">Приложенные к тексту файлы.</param>
        /// <param name="isTopicText">Флаг, что строка из сообщения топика.</param>
        /// <returns>Преобразованная строка и ссылки, приложенные к тексту.</returns>
        public (string ResultText, Dictionary<Guid, string> UriAttachments) Parse(
            string mainString,
            Dictionary<Guid, (string Name, string Path)> textAttachments,
            bool isTopicText)
        {
            this.uriAttachments = new Dictionary<Guid, string>();
            // если пришла пустая строка - вернем заглушку.
            if (string.IsNullOrWhiteSpace(mainString))
            {
                return (EmptyString, this.uriAttachments);
            }
            
            this.textAttachments = textAttachments.ToDictionary(x => x.Key, x => x.Value);
            this.description = isTopicText ? null : new JsonDescription();
            this.codeHighlighter = UseHighlightCode ? new HtmlFormatter() : null; 

            // получаю HTML строку со стандартной разметкой.
            var parseString = TextileFormatter.FormatString(mainString);

            // получаю DOM дерево полученного HTML.
            var doc = new HtmlDocument();
            doc.LoadHtml(parseString);
            // выполняем преобразования документа.
            foreach (var transition in this.documentTransitions)
            {
                // получаем теги по XPath, которые нужно обработать.
                var tags = doc.DocumentNode.SelectNodes(transition.XPath) as IList<HtmlNode> ?? new List<HtmlNode>();
                // обрабатываем полученные теги.
                foreach (var tag in tags)
                {
                    transition.Handler(tag);
                }
            }
            
            this.HandleSpans(doc.DocumentNode);
            
            // получаем преобразованный с помощью HtmlAgilityPack текст.
            parseString = doc.DocumentNode.InnerHtml;
            // устанавливаем разметку в начало и конец строки.
            parseString = this.PostParseProcess(parseString);

            return (parseString, this.uriAttachments);
        }

        #region Private Methods

        #region Tag Handlers
        
        /// <summary>
        /// Обработчик таблицы.
        /// </summary>
        /// <param name="tableTag">Узел таблицы дерева HTML.</param>
        private void HandleTable(HtmlNode tableTag)
        {
            // Rich edit не может обрабатывать таблицы.
            // Удаляем этот элемент из дерева и сообщаем о том, что не удается преобразовать тег.
            // this.logger.ErrorAsync(
            //     $"{this.issueId}: При преобразовании из Textile в HTML была удалена таблица. Rich edit не поддерживает таблицы.");
            tableTag.Remove();
        }
        
        /// <summary>
        /// Обработчик жирного текста.
        /// </summary>
        /// <param name="boldTag">Узел жирного текста дерева HTML.</param>
        private static void HandleBold(HtmlNode boldTag)
        {
            var tessaBoldTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Bold}\"/>");
            tessaBoldTag.ChildNodes.AddRange(boldTag.ChildNodes);
            boldTag.ParentNode.ReplaceChild(tessaBoldTag, boldTag);
        }

        /// <summary>
        /// Обработчик курсивного текста.
        /// </summary>
        /// <param name="italicTag">Узел курсивного текста дерева HTML.</param>
        private static void HandleItalic(HtmlNode italicTag)
        {
            var tessaItalicTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Italic}\"/>");
            tessaItalicTag.ChildNodes.AddRange(italicTag.ChildNodes);
            italicTag.ParentNode.ReplaceChild(tessaItalicTag, italicTag);
        }
        
        /// <summary>
        /// Обработчик подчеркнутого текста.
        /// </summary>
        /// <param name="underlineTag">Узел подчеркнутого текста дерева HTML.</param>
        private static void HandleUnderline(HtmlNode underlineTag)
        {
            var tessaUnderlineTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Underline}\"/>");
            tessaUnderlineTag.ChildNodes.AddRange(underlineTag.ChildNodes);
            underlineTag.ParentNode.ReplaceChild(tessaUnderlineTag, underlineTag);
        }
        
        /// <summary>
        /// Обработчик зачеркнутого текста.
        /// </summary>
        /// <param name="crossedOutTag">Узел зачеркнутого текста дерева HTML.</param>
        private static void HandleCrossedOut(HtmlNode crossedOutTag)
        {
            var tessaCrossedOutTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.CrossedOut}\"/>");
            tessaCrossedOutTag.ChildNodes.AddRange(crossedOutTag.ChildNodes);
            crossedOutTag.ParentNode.ReplaceChild(tessaCrossedOutTag, crossedOutTag);
        }
        
        /// <summary>
        /// Обработчик заголовка.
        /// </summary>
        /// <param name="headerTag">Узел заголовка дерева HTML.</param>
        private static void HandleHeader(HtmlNode headerTag)
        {
            var tessaTag = HtmlNode.CreateNode($"<p><span style=\"{TessaMarkup.Styles.Header}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Header}\"/></p>");
            tessaTag.Descendants("span").First().ChildNodes.AddRange(headerTag.ChildNodes);
            headerTag.ParentNode.ReplaceChild(tessaTag, headerTag);
        }
        
        /// <summary>
        /// Обработчик маркированного списка.
        /// </summary>
        /// <param name="unorderedListTag">Узел маркированного списка дерева HTML.</param>
        private static void HandleUnorderedList(HtmlNode unorderedListTag)
        {
            var tessaUnorderedListTag = HtmlNode.CreateNode($"<ul class=\"{TessaMarkup.Classes.UnorderedList}\"/>");
            tessaUnorderedListTag.ChildNodes.AddRange(unorderedListTag.ChildNodes);
            unorderedListTag.ParentNode.ReplaceChild(tessaUnorderedListTag, unorderedListTag);
        }
        
        /// <summary>
        /// Обработчик нумерованного списка.
        /// </summary>
        /// <param name="orderedListTag">Узел нумерованного списка дерева HTML.</param>
        private static void HandleOrderedList(HtmlNode orderedListTag)
        {
            var tessaOrderedListTag = HtmlNode.CreateNode($"<ol class=\"{TessaMarkup.Classes.OrderedList}\"/>");
            tessaOrderedListTag.ChildNodes.AddRange(orderedListTag.ChildNodes);
            orderedListTag.ParentNode.ReplaceChild(tessaOrderedListTag, orderedListTag);
        }
        
        /// <summary>
        /// Обработчик элемента списка.
        /// </summary>
        /// <param name="listItemTag">Узел элемента списка дерева HTML.</param>
        private static void HandleListItem(HtmlNode listItemTag)
        {
            var tessaListItemTag = HtmlNode.CreateNode("<li><p><span/></p></li>");
            var span = tessaListItemTag.Descendants("span").First();
            var listTags = listItemTag.SelectNodes("*[self::ol or self::ul]");
            if (listTags is not null)
            {
                tessaListItemTag.ChildNodes.AddRange(listTags);
                //listItemTag.ChildNodes.RemoveRange(listTags);
            }
            span.ChildNodes.AddRange(listItemTag.ChildNodes);
            listItemTag.ParentNode.ReplaceChild(tessaListItemTag, listItemTag);
        }
        
        /// <summary>
        /// Обработчик цитаты.
        /// </summary>
        /// <param name="blockquoteTag">Узел цитаты дерева HTML.</param>
        private static void HandleBlockquote(HtmlNode blockquoteTag)
        {
            var tessaBlockquoteTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.Blockquote}\"><p><span/></p></div>");
            tessaBlockquoteTag.Descendants("span").First().ChildNodes.AddRange(blockquoteTag.ChildNodes);
            blockquoteTag.ParentNode.ReplaceChild(tessaBlockquoteTag, blockquoteTag);
        }

        /// <summary>
        /// Обработчик тега &lt;code/&gt;.
        /// </summary>
        /// <param name="codeTag">Узел &lt;code/&gt; дерева HTML.</param>
        private void HandleCode(HtmlNode codeTag)
        {
            // проверяем - у тега <code/> есть родитель <pre/>?
            var preParenTag = codeTag.SelectNodes("ancestor::pre");
            // если нет - тогда это инлайн строка, где мы не рассматриваем форматирование строки.
            // <code/> => <span style="font-size: 14px" data-custom-style="font-size:14;" class="forum-block-inline"/>"
            if (preParenTag is null)
            {
                var tessaCodeTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Pre}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Pre}\" class=\"{TessaMarkup.Classes.Pre}\"/>");
                tessaCodeTag.ChildNodes.AddRange(codeTag.ChildNodes);
                codeTag.ParentNode.ReplaceChild(tessaCodeTag, codeTag);
                return;
            }

            if (this.codeHighlighter is not null)
            {
                var codeClassValue = codeTag.Attributes.FirstOrDefault(a => a.Name == "class")?.Value;
                var language = Languages.FindById(codeClassValue ?? "java");
                var newHtmlNode = HtmlNode.CreateNode(this.codeHighlighter.GetHtmlString(codeTag.InnerHtml, language));
                // делаем через "InnerHtml" т.к в документации к методу тоже есть теги, например <summary>, что может попортить разметку.
                codeTag.InnerHtml = newHtmlNode.Descendants("pre").First().InnerHtml;
            }
            // если есть - тогда добавляем корректное форматирование в строку,
            // делим текст на строки.
            
            // обработка тегов <code/>, которые вложены в <pre/>, проводится в "HandlePre".
            // обработчик тега <pre/> должен вызываться после обработки тегов <code/>.
            // тег <pre/> не изымается из разметки и модифицируется.
            var codeInnerHtml = codeTag.InnerHtml;
            codeTag.RemoveAll();
            var lines = codeInnerHtml.Split('\n');
            foreach (var line in lines.Where(a => a != "\r"))
            {
                // каждую строку оборачиваем в <p/>
                var pTag = HtmlNode.CreateNode("<p/>");
                if (!string.IsNullOrWhiteSpace(line))
                {
                    pTag.InnerHtml = UseHighlightCode 
                        ? line 
                        : line
                            // парсер не обрабатывает символы "<" и ">", если они  в <pre><code/></pre>.
                            .Replace("&", @"&amp;", StringComparison.InvariantCultureIgnoreCase)
                            .Replace("<", @"&lt;", StringComparison.InvariantCultureIgnoreCase)
                            .Replace(">", @"&gt;", StringComparison.InvariantCultureIgnoreCase);
                }
                codeTag.ChildNodes.Add(pTag);
            }
        }
        
        /// <summary>
        /// Обработчик тега &lt;pre/&gt;.
        /// </summary>
        /// <param name="preTag">Узел &lt;pre/&gt; дерева HTML.</param>
        private static void HandlePre(HtmlNode preTag)
        {
            HtmlNode tessaPreTag;
            // в <pre> могут быть теги <code>.
            var codeTags = preTag.SelectNodes("code");
            if (codeTags is null)
            {
                tessaPreTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Pre}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Pre}\" class=\"{TessaMarkup.Classes.Pre}\"/>");
                tessaPreTag.ChildNodes.AddRange(preTag.ChildNodes);
                preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
                return;
            }
            // если есть теги <code/>.
            // находим текст, как прямой наследник <pre>.
            var text = preTag.SelectNodes("text()");
            // если есть текст - тогда code отдельно в блок
            if (text is not null)
            {
                foreach (var codeTag in codeTags)
                {
                    var tessaCodeTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
                    tessaCodeTag.ChildNodes.AddRange(codeTag.ChildNodes);
                    codeTag.ParentNode.ReplaceChild(tessaCodeTag, codeTag);
                }
            }
            // если нет текста
            else
            {
                // удаляем все дочерние элементы.
                preTag.RemoveChildren(codeTags);
                // с помощью InnerHtml перемещаем контент из дочерних <code/> в родительский <pre/>
                // var preTagInnerHtml = StringBuilderHelper.Acquire();
                // preTagInnerHtml.Append(preTag.InnerHtml);
                // foreach (var codeTag in codeTags)
                // {
                //     preTagInnerHtml.Append(codeTag.InnerHtml);
                // }
                //
                // preTag.InnerHtml = preTagInnerHtml.ToStringAndRelease();
            }
            
            tessaPreTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
            tessaPreTag.ChildNodes.AddRange(preTag.ChildNodes);
            preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
        }

        /// <summary>
        /// Обработчик ссылки.
        /// </summary>
        /// <param name="linkTag">Узел ссылки дерева HTML.</param>
        private void HandleLink(HtmlNode linkTag)
        {
            // получаем url из атрибута 
            var linkHrefAttribute = linkTag.Attributes.FirstOrDefault(a => a.Name == "href")?.Value;
            // если url нет.
            if (string.IsNullOrEmpty(linkHrefAttribute))
            {
                // если в ссылке есть контент, то помещаем его вместо ссылки.
                // if (linkTag.ChildNodes.Any())
                // {
                //     linkTag.ParentNode.ChildNodes.InsertRange(linkTag.ParentNode.ParentNode.ChildNodes.IndexOf(linkTag.ParentNode), linkTag.ChildNodes.ToList());
                // }
                // если контента нет - просто удаляем ссылку.
                linkTag.Remove();
                return;
            }
            
            var tessaLinkTag = HtmlNode.CreateNode($"<a style=\"{TessaMarkup.Styles.Link}\" data-custom-href=\"{linkHrefAttribute}\" href=\"{linkHrefAttribute}\" class=\"{TessaMarkup.Classes.Link}\"><span/></a>");
            tessaLinkTag.Descendants("span").First().ChildNodes.AddRange(linkTag.ChildNodes);
            linkTag.ParentNode.ReplaceChild(tessaLinkTag, linkTag);

            var uriId = Guid.NewGuid();
            this.uriAttachments.Add(uriId, linkHrefAttribute);
            
            // this.description?.Attachments.Add(GenerateItemModel(
            //     uriId, 
            //     linkHrefAttribute, 
            //     GetLinkCaption(linkTag.InnerText, linkHrefAttribute), 
            //     AttachmentType.Link));
        }

        /// <summary>
        /// Обработчик приложенного изображения.
        /// </summary>
        /// <param name="imgTag">Узел изображения дерева HTML.</param>
        private void HandleImg(HtmlNode imgTag)
        {
            var imgSrcAttributeValue = imgTag.Attributes.FirstOrDefault(a => a.Name == "src")?.Value;
            // если атрибут "src" пустой, то что-то пошло не так.
            if (string.IsNullOrEmpty(imgSrcAttributeValue))
            {
                // удаляем это изображение из разметки.
                imgTag.Remove();
                return;
            }

            // если проложенное изображение - это ссылка.
            if (Uri.IsWellFormedUriString(imgSrcAttributeValue, UriKind.Absolute))
            {
                var imgTitleAttributeValue = imgTag.Attributes.FirstOrDefault(a => a.Name == "title")?.Value;
                var tessaLinkTag = HtmlNode.CreateNode($"<a style=\"{TessaMarkup.Styles.Link}\" data-custom-href=\"{imgSrcAttributeValue}\" href=\"{imgSrcAttributeValue}\" class=\"{TessaMarkup.Classes.Link}\"><span/></a>");
                tessaLinkTag.Descendants("span").First().InnerHtml = GetLinkCaption(imgTitleAttributeValue, imgSrcAttributeValue);
                imgTag.ParentNode.ReplaceChild(tessaLinkTag, imgTag);
                
                var uriId = Guid.NewGuid();
                this.uriAttachments.Add(uriId, imgSrcAttributeValue);
                
                // this.description?.Attachments.Add(GenerateItemModel(
                //     uriId, 
                //     imgSrcAttributeValue, 
                //     GetLinkCaption(imgTitleAttributeValue, imgSrcAttributeValue), 
                //     AttachmentType.Link));

                return;
            }
            
            var image = this.textAttachments.First(a => string.Equals(a.Value.Name, imgSrcAttributeValue, StringComparison.OrdinalIgnoreCase));

            // if (!image.Value.Name.IsImage())
            // {
            //     this.logger.ErrorAsync($"{this.issueId}: Файл {image.Value.Name}, помеченный как изображение не удается обработать, т.к. тип не соответствует.");
            //     imgTag.Remove();
            //     return;
            // }
            //var thumbnail = GenerateThumbnail(image.Value.Path);
            
            //var tessaImgTag = HtmlNode.CreateNode($"<span><img data-custom-style=\"{string.Format(TessaMarkup.DataCustomStyles.Img, thumbnail.Width, thumbnail.Height)}\" name=\"{image.Key:N}\" src=\"{thumbnail.Base64}\"/></span>");
            //imgTag.ParentNode.ReplaceChild(tessaImgTag, imgTag);
            // if (this.description is not null)
            // {
            //     this.description.Attachments.Add(GenerateItemModel(
            //         image.Key, 
            //         imgSrcAttributeValue, 
            //         image.Key.ToString("N"), 
            //         AttachmentType.InnerItem));
            //     this.textAttachments.Remove(image.Key);
            // }
        }

        /// <summary>
        /// Обработчик тега &lt;p/&gt;.
        /// </summary>
        /// <param name="paragraphTag">Узел &lt;p/&gt; дерева HTML.</param>
        private static void HandleParagraph(HtmlNode paragraphTag)
        {
            var firstBreakLine = paragraphTag.ChildNodes.FirstOrDefault(cn => cn.Name == "br");
            var breakLineIndex  = paragraphTag.ChildNodes.IndexOf(firstBreakLine);
            var paragraphStyleAttribute = paragraphTag.Attributes.FirstOrDefault(a => a?.Name == "style");
            // если <br> нет.
            if (breakLineIndex < 0)
            {
                if (paragraphTag.FirstChild?.Name == "span")
                {
                    return;
                }
                var tessaParagraphTag = HtmlNode.CreateNode("<p><span/></p>");
                if (paragraphStyleAttribute is not null)
                {
                    tessaParagraphTag.Attributes.Add(paragraphStyleAttribute);
                }
                tessaParagraphTag.Descendants("span").First().ChildNodes.AddRange(paragraphTag.ChildNodes);
                paragraphTag.ParentNode.ReplaceChild(tessaParagraphTag, paragraphTag);
                return;
            }

            var parentParagraphTag = paragraphTag.ParentNode;
            var parentParagraphTagIndex = parentParagraphTag.ChildNodes.IndexOf(paragraphTag);
            
            var nodes = paragraphTag.ChildNodes.Skip(breakLineIndex).ToList();
            //paragraphTag.ChildNodes.RemoveRange(nodes);
            nodes.Remove(firstBreakLine);
            var newParagraphs = new List<HtmlNode>();
            // while((breakLineIndex = nodes.IndexOf(n => n.Name == "br")) >= 0)
            // { 
            //     var newParagraphTag = HtmlNode.CreateNode("<p><span/></p>");
            //     if (paragraphStyleAttribute is not null)
            //     {
            //         newParagraphTag.Attributes.Add(paragraphStyleAttribute);
            //     }
            //     newParagraphTag.Descendants("span").First().ChildNodes.AddRange(nodes.Take(breakLineIndex));
            //     newParagraphs.Add(newParagraphTag);
            //     // удаляем <br> и следующий за ним элемент, т.к он уже обработан.
            //     nodes.RemoveRange(0, breakLineIndex + 1);
            // }

            // если еще остались теги в <p>.
            if (nodes.Any())
            {
                var newParagraphTag = HtmlNode.CreateNode("<p><span/></p>");
                if (paragraphStyleAttribute is not null)
                {
                    newParagraphTag.Attributes.Add(paragraphStyleAttribute);
                }
                newParagraphTag.Descendants("span").First().ChildNodes.AddRange(nodes);
                newParagraphs.Add(newParagraphTag);
            }
            
            //parentParagraphTag.ChildNodes.InsertRange(parentParagraphTagIndex + 1, newParagraphs);
        }

        /// <summary>
        /// Обработчик тегов &lt;span/&gt;
        /// </summary>
        /// <param name="root">Главный узел дерева HTML.</param>
        private void HandleSpans(HtmlNode root)
        {
            HashSet<HtmlNode> handled = new();
            var nodes = root.SelectNodes("//span");
            if (nodes is null)
            {
                return;
            }
            foreach (var node in nodes)
            {
                this.HandleSpan(node, handled);
            }
        }

        /// <summary>
        /// Обработчик тега &lt;span/&gt;
        /// </summary>
        /// <param name="span">Тег &lt;span/&gt;</param>
        /// <param name="handled">Обработанные теги &lt;span/&gt;</param>
        private void HandleSpan(HtmlNode span, HashSet<HtmlNode> handled)
        {
            if (handled.Contains(span))
            {
                return;
            }
            handled.Add(span);
            if (span.Descendants().All(x => x.Name != "span"))
            {
                return;
            }
            List<HtmlNode> linearized = new();
            var style = span.Attributes.FirstOrDefault(a => a.Name == "style")?.Value ?? string.Empty;
            this.HandleSpan(span, handled, linearized, style);
            var index = span.ParentNode.ChildNodes.IndexOf(span);
            var spanParentNode = span.ParentNode;
            //spanParentNode.ChildNodes.InsertRange(index, linearized);
            span.Remove();
        }

        /// <summary>
        /// Обработчик тега &lt;span/&gt;
        /// </summary>
        /// <param name="span">Тег &lt;span/&gt;</param>
        /// <param name="handled">Обработанные теги &lt;span/&gt;</param>
        /// <param name="linearized">Линейно результирующие теги &lt;span/&gt;.</param>
        /// <param name="style">Стиль.</param>
        private void HandleSpan(
            HtmlNode span, 
            HashSet<HtmlNode> handled, 
            List<HtmlNode> linearized, 
            string style)
        {
            foreach (var node in span.ChildNodes)
            {
                switch (node.Name)
                {
                    case "span":
                    {
                        handled.Add(node);
                        var spanAttribute = node.Attributes.FirstOrDefault(a => a.Name == "style");
                        var spanStyle = spanAttribute?.Value;
                        spanStyle = $"{style}{(style!.EndsWith(";") ? "" : ";")}{spanStyle}";
                        if (span.Descendants().Any(x => x.Name == "span"))
                        {
                            this.HandleSpan(node, handled, linearized, spanStyle);
                        }
                        else
                        {
                            if (spanAttribute is not null)
                            {
                                spanAttribute.Value = spanStyle;
                            }
                            else
                            {
                                node.Attributes.Add("style", style);
                            }
                            linearized.Add(node);
                        }
                        
                        continue;
                    }
                    case "#text" when !string.IsNullOrWhiteSpace(node.InnerText):
                    {
                        if (string.IsNullOrEmpty(style))
                        {
                            linearized.Add(node);
                            break;
                        }
                        var newSpan = HtmlNode.CreateNode("<span/>");
                        newSpan.Attributes.Add("style", style);
                        // делаем через "InnerHtml", т.к обрабатываем "#text".
                        newSpan.InnerHtml = node.InnerHtml;
                        linearized.Add(newSpan);
                        break;
                    }
                    default:
                    {
                        var newSpan = HtmlNode.CreateNode("<span/>");
                        if (string.IsNullOrEmpty(style))
                        {
                            newSpan.Attributes.Add("style", style);
                        }
                        newSpan.ChildNodes.AddRange(node.ChildNodes);
                        linearized.Add(node);
                        break;
                    }
                }
            }
        }
        
        #endregion

        /// <summary>
        /// Создать модель приложенного файла.
        /// </summary>
        /// <param name="id">Идентификатор приложенного файла.</param>
        /// <param name="uri">Uri приложенного файла.</param>
        /// <param name="caption">Описание приложенного файла.</param>
        /// <param name="attachmentType">Тип приложенного файла.</param>
        /// <param name="showInToolbar">Флаг отображать вложение под сообщением в режиме чтения.</param>
        /// <returns>Модель приложенного файла.</returns>
        // private static ItemModel GenerateItemModel(Guid id, string uri, string caption, AttachmentType attachmentType, bool showInToolbar = false) =>
        // new (id, uri, caption, attachmentType)
        //     {
        //         MessageID = Guid.Empty,
        //         StoreMode = AttachmentStoreMode.Insert,
        //         ShowInToolbar = showInToolbar
        //     };

        /// <summary>
        /// Получить заголовок ссылки.
        /// </summary>
        /// <param name="linkTitle">Текст в ссылке.</param>
        /// <param name="linkHref">Uri, указанное в ссылке.</param>
        /// <returns>Заголовок ссылки.</returns>
        private static string GetLinkCaption(string linkTitle, string linkHref)
        {
            if (string.IsNullOrWhiteSpace(linkTitle) || linkTitle.Length > MaxLinkCaptionLength)
            {
                //return linkHref.Limit(MaxLinkCaptionLength);
            }

            return linkTitle;
        }
        
        /// <summary>
        /// Получить миниатюру приложенного изображения и его размеры.
        /// </summary>
        /// <param name="pathToImage">Путь до изображения.</param>
        /// <returns>Миниатюра приложенного изображения и его размеры.</returns>
        // [SuppressMessage("Interoperability", "CA1416")]
        // private static (string Base64, int Width, int Height) GenerateThumbnail(string pathToImage)
        // {
        //     FileStream sourceStream = null;
        //     Bitmap bitmap = null;
        //     MemoryStream stream;
        //     int imageWidth;
        //     int imageHeight;
        //
        //     try
        //     {
        //         sourceStream = FileHelper.OpenRead(pathToImage);
        //         bitmap = new Bitmap(sourceStream);
        //         sourceStream.Dispose();
        //         sourceStream = null;
        //
        //         if (bitmap.Width >= ForumHelper.ImageSideSize && bitmap.Width > bitmap.Height)
        //         {
        //             double factor = bitmap.Width / (float) ForumHelper.ImageSideSize;
        //             var newHeight = (int) (bitmap.Height / factor);
        //             var resizedBitmap = new Bitmap(bitmap, ForumHelper.ImageSideSize, newHeight);
        //             bitmap.Dispose();
        //             bitmap = resizedBitmap;
        //         }
        //         else if (bitmap.Height >= ForumHelper.ImageSideSize)
        //         {
        //             double factor = bitmap.Height / (float) ForumHelper.ImageSideSize;
        //             var newWidth = (int) (bitmap.Width / factor);
        //             var resizedBitmap = new Bitmap(bitmap, newWidth, ForumHelper.ImageSideSize);
        //             bitmap.Dispose();
        //             bitmap = resizedBitmap;
        //         }
        //         
        //         var encoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
        //         var myEncoderParameters = new EncoderParameters(1)
        //         {
        //             Param =
        //             {
        //                 [0] = new EncoderParameter(Encoder.Quality, 75L)
        //             }
        //         };
        //         stream = new MemoryStream(80_000);
        //         bitmap.Save(stream, encoder, myEncoderParameters);
        //         imageWidth = bitmap.Width;
        //         imageHeight = bitmap.Height;
        //     }
        //     finally
        //     {
        //         bitmap?.Dispose();
        //         sourceStream?.Dispose();
        //     } 
        //     
        //     stream.Position = 0L;
        //     const string prefix = "data:image/png;base64,";
        //     using var stringStream = new MemoryStream(prefix.Length + (int) stream.Length);
        //     using (var stringWriter = new StreamWriter(stringStream, Encoding.UTF8, leaveOpen: true))
        //     {
        //         stringWriter.Write(prefix);
        //     }
        //     using (var base64Stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read))
        //     {
        //         base64Stream.CopyTo(stringStream);
        //     }
        //     string base64;
        //     stringStream.Position = 0L;
        //     using (var stringReader = new StreamReader(stringStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
        //     {
        //         base64 = stringReader.ReadToEnd();
        //     }
        //         
        //     return (base64, imageWidth, imageHeight);
        // }
        
        /// <summary>
        /// Установка начала и конца строки.
        /// </summary>
        /// <param name="mainString">Строка для преобразования.</param>
        /// <returns>Преобразованная строка.</returns>
        private string PostParseProcess(string mainString)
        {
            const string preString = "<div class=\"forum-div\">";
            const string postString = "</div>";
            mainString = mainString.Replace("\n", "", StringComparison.CurrentCulture);
            mainString = $"{preString}{mainString}{postString}";
            // if (this.description is not null)
            // {
            //     this.description.Text = mainString;
            //     // добавить остальные файлы.
            //     foreach (var textAttachment in this.textAttachments)
            //     {
            //         this.description.Attachments.Add(GenerateItemModel(
            //             textAttachment.Key,
            //             $"tessa://attachfile_{textAttachment.Key}", 
            //             textAttachment.Value.Name, 
            //             AttachmentType.File,
            //             true));
            //     }
            //     return StorageHelper.SerializeToTypedJson(this.description.GetStorage());
            // }

            return mainString;
        }

        #endregion
    }
}