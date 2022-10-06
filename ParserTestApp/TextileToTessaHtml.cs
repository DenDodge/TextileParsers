using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using NTextileCore;

namespace TestApp
{
   public class TextileToTessaHtml
    {
        #region Fiedls

        /// <summary>
        /// Флаг, что строка из сообщения топика.
        /// </summary>
        private bool IsTopicText;

        #region Static Fields

        /// <summary>
        /// Пустое описание инцидента.
        /// Применяется, если входящая строка пустая или с пробелами.
        /// </summary>
        private static readonly string EmptyString = "{\"Text\":\"<div class=\\\"forum-div\\\"><p><span> </span></p></div>\"}";

        /// <summary>
        /// Список обработчиков тегов разметки.
        /// </summary>
        private static readonly List<(string XPath, Action<HtmlNode> Handler)> DocumentTransitions = new ()
        {
            ("//*[self::strong or self::b]", HandleBold),
            ("//*[self::em or self::i]", HandleItalic),
            ("//ins", HandleUnderline),
            ("//del", HandleCrossedOut),
            ("//*[self::h1 or self::h2 or self::h3 or self::h4]", HandleHeader),
            ("//ul", HandleUnorderedList),
            ("//ol", HandleOrderedList),
            ("//li", HandleListItem),
            ("//blockquote", HandleBlockquote),
            ("//code", HandleCode),
            ("//pre", HandlePre),
            ("//a", HandleLink),
            ("//br", HandleBreakLine),
            ("//p", HandleParagraph)
        };

        #endregion

        #endregion

        public (string ResultString, HashSet<Guid> AttachedFileIds) Parse(string mainString, bool isTopicText)
        {
            // если пришла пустая строка - вернем заглушку.
            if (string.IsNullOrWhiteSpace(mainString))
            {
                return (EmptyString, new HashSet<Guid>());
            }
            
            this.IsTopicText = isTopicText;
            
            // получаю HTML строку со стандартной разметкой.
            var parseString = TextileFormatter.FormatString(mainString);

            // получаю DOM дерево полученного HTML.
            var doc = new HtmlDocument();
            doc.LoadHtml(parseString);
            // обрабатываем переходы документа.
            foreach (var transition in DocumentTransitions)
            {
                // получаем теги по XPath, который нужно обработать.
                var tags = doc.DocumentNode.SelectNodes(transition.XPath) as IList<HtmlNode> ?? new List<HtmlNode>();
                // обрабатываем полученные теги.
                for (var i = tags.Count - 1; i >= 0; i--)
                {
                    var tag = tags[i];
                    transition.Handler(tag);
                }
            }

            // получаем преобразованный с помощью HtmlAgilityPack текст.
            parseString = doc.DocumentNode.InnerHtml;
            // устанавливаем разметку в начало и конец строки.
            parseString = this.PostParseProcess(parseString);

            return (parseString, new HashSet<Guid>());
        }

        #region Private Methods

        #region TagHenlers
        
        /// <summary>
        /// Обработчик жирного текста.
        /// </summary>
        /// <param name="boldTag">Узел жирного текста дерева HTML.</param>
        private static void HandleBold(HtmlNode boldTag)
        {
            var tessaBoldTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Bold}\"/>");
            tessaBoldTag.InnerHtml = boldTag.InnerHtml;
            boldTag.ParentNode.ReplaceChild(tessaBoldTag, boldTag);
        }

        /// <summary>
        /// Обработчик курсивного текста.
        /// </summary>
        /// <param name="italicTag">Узел курсивного текста дерева HTML.</param>
        private static void HandleItalic(HtmlNode italicTag)
        {
            var tessaItalicTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Italic}\"/>");
            tessaItalicTag.InnerHtml = italicTag.InnerHtml;
            italicTag.ParentNode.ReplaceChild(tessaItalicTag, italicTag);
        }
        
        /// <summary>
        /// Обработчик подчеркнутого текста.
        /// </summary>
        /// <param name="underlineTag">Узел подчеркнутого текста дерева HTML.</param>
        private static void HandleUnderline(HtmlNode underlineTag)
        {
            var tessaUnderlineTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Underline}\"/>");
            tessaUnderlineTag.InnerHtml = underlineTag.InnerHtml;
            underlineTag.ParentNode.ReplaceChild(tessaUnderlineTag, underlineTag);
        }
        
        /// <summary>
        /// Обработчик зачеркнутого текста.
        /// </summary>
        /// <param name="crossedOutTag">Узел зачеркнутого текста дерева HTML.</param>
        private static void HandleCrossedOut(HtmlNode crossedOutTag)
        {
            var tessaCrossedOutTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.CrossedOut}\"/>");
            tessaCrossedOutTag.InnerHtml = crossedOutTag.InnerHtml;
            crossedOutTag.ParentNode.ReplaceChild(tessaCrossedOutTag, crossedOutTag);
        }
        
        /// <summary>
        /// Обработчик заголовка.
        /// </summary>
        /// <param name="headerTag">Узел заголовка дерева HTML.</param>
        private static void HandleHeader(HtmlNode headerTag)
        {
            var tessaTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Header}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Header}\"/>");
            tessaTag.InnerHtml = headerTag.InnerHtml;
            headerTag.ParentNode.ReplaceChild(tessaTag, headerTag);
        }
        
        /// <summary>
        /// Обработчик маркированного списка.
        /// </summary>
        /// <param name="unorderedListTag">Узел маркированного списка дерева HTML.</param>
        private static void HandleUnorderedList(HtmlNode unorderedListTag)
        {
            var tessaUnorderedListTag = HtmlNode.CreateNode($"<ul class=\"{TessaMarkup.Classes.UnorderedList}\"/>");
            tessaUnorderedListTag.InnerHtml = unorderedListTag.InnerHtml;
            unorderedListTag.ParentNode.ReplaceChild(tessaUnorderedListTag, unorderedListTag);
        }
        
        /// <summary>
        /// Обработчик нумерованного списка.
        /// </summary>
        /// <param name="orderedListTag">Узел нумерованного списка дерева HTML.</param>
        private static void HandleOrderedList(HtmlNode orderedListTag)
        {
            var tessaOrderedListTag = HtmlNode.CreateNode($"<ol class=\"{TessaMarkup.Classes.OrderedList}\"/>");
            tessaOrderedListTag.InnerHtml = orderedListTag.InnerHtml;
            orderedListTag.ParentNode.ReplaceChild(tessaOrderedListTag, orderedListTag);
        }
        
        /// <summary>
        /// Обработчик элемента списка.
        /// </summary>
        /// <param name="listItemTag">Узел элемента списка дерева HTML.</param>
        private static void HandleListItem(HtmlNode listItemTag)
        {
            var tessaListItemTag = HtmlNode.CreateNode("<li><p><span/></p></li>");
            tessaListItemTag.Descendants("span").First().InnerHtml = listItemTag.InnerHtml;
            listItemTag.ParentNode.ReplaceChild(tessaListItemTag, listItemTag);
        }
        
        /// <summary>
        /// Обработчик цитаты.
        /// </summary>
        /// <param name="blockquoteTag">Узел цитаты дерева HTML.</param>
        private static void HandleBlockquote(HtmlNode blockquoteTag)
        {
            var tessaBlockquoteTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.Blockquote}\"><p><span/></p></div>");
            tessaBlockquoteTag.Descendants("span").First().InnerHtml = blockquoteTag.InnerHtml;
            blockquoteTag.ParentNode.ReplaceChild(tessaBlockquoteTag, blockquoteTag);
        }

        /// <summary>
        /// Обработчик тега &lt;code/&gt;.
        /// </summary>
        /// <param name="codeTag">Узел &lt;code/&gt; дерева HTML.</param>
        private static void HandleCode(HtmlNode codeTag)
        {
            // проверяем - у тега <code/> есть родитель <pre/>?
            var preParenTag = codeTag.ParentNode.SelectNodes("/pre");
            // если нет - тогда это инлайн строка, где мы не рассматриваем форматирование строки.
            // <code/> => <span style="font-size: 14px" data-custom-style="font-size:14;" class="forum-block-inline"/>"
            if (preParenTag is null)
            {
                var tessaCodeTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Pre}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Pre}\" class=\"{TessaMarkup.Classes.Pre}\"/>");
                tessaCodeTag.InnerHtml = codeTag.InnerHtml;
                codeTag.ParentNode.ReplaceChild(tessaCodeTag, codeTag);
            }
            // если есть - тогда добавляем корректное форматирование в строку,
            else
            {
                // делим текст на строки.
                var codeInnerHtml = codeTag.InnerHtml;
                codeTag.RemoveAll();
                var lines = codeInnerHtml.Split('\n');
                foreach (var line in lines)
                {
                    // каждую строку оборачиваем в <p/>
                    var pTag = HtmlNode.CreateNode("<p/>");
                    pTag.InnerHtml = line.Replace("\n", "");
                    codeTag.ChildNodes.Add(pTag);
                }
            }
        }
        
        /// <summary>
        /// Обработчик тега &lt;pre/&gt;.
        /// </summary>
        /// <param name="preTag">Узел &lt;pre/&gt; дерева HTML.</param>
        private static void HandlePre(HtmlNode preTag)
        {
            // в <pre> могут быть теги <code>.
            var codeTags = preTag.SelectNodes("code");
            // если есть теги <code/>.
            if (codeTags is not null)
            {
                // находим текст, как прямой наследник <pre>.
                var text = preTag.SelectNodes("text()");
                // если есть текст - тогда code отдельно в блок
                if (text is not null)
                {
                    foreach (var codeTag in codeTags)
                    {
                        var tessaCodeTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
                        tessaCodeTag.InnerHtml = codeTag.InnerHtml;
                        codeTag.ParentNode.ReplaceChild(tessaCodeTag, codeTag);
                    }
                }
                // если нет текста
                else
                {
                    // удаляем все дочерние элементы.
                    preTag.RemoveChildren(codeTags);
                    // с помощью InnerHtml перемещаем контент из дочерних <code/> в родительский <pre/>
                    foreach (var codeTag in codeTags)
                    {
                        preTag.InnerHtml += codeTag.InnerHtml;
                    }
                }
                var tessaPreTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
                tessaPreTag.InnerHtml = preTag.InnerHtml;
                preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
                
            }
            // если тегов <code/> нет.
            else
            {
                var tessaPreTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Pre}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Pre}\" class=\"{TessaMarkup.Classes.Pre}\"/>");
                tessaPreTag.InnerHtml = preTag.InnerHtml;
                preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
            }
        }

        /// <summary>
        /// Обработчик ссылки.
        /// </summary>
        /// <param name="linkTag">Узел ссылки дерева HTML.</param>
        private static void HandleLink(HtmlNode linkTag)
        {
            // получаем url из атрибута 
            var linkHref = linkTag.Attributes.FirstOrDefault(a => a.Name == "href")?.Value;
            // если url нет - нам ссылка не нужна.
            if (string.IsNullOrEmpty(linkHref))
            {
                linkTag.ParentNode.RemoveChild(linkTag);
                return;
            }
            
            var tessaLinkTag = HtmlNode.CreateNode($"<a style=\"{TessaMarkup.Styles.Link}\" data-custom-href=\"{linkHref}\" href=\"{linkHref}\" class=\"{TessaMarkup.Classes.Link}\"><span/></a>");
            tessaLinkTag.Descendants("span").First().InnerHtml = linkTag.InnerHtml;
            linkTag.ParentNode.ReplaceChild(tessaLinkTag, linkTag);
        }
        
        /// <summary>
        /// Обработчик перехода на новую строку.
        /// </summary>
        /// <param name="breakLineTag">Узел перехода на новую строку дерева HTML.</param>
        private static void HandleBreakLine(HtmlNode breakLineTag)
        {
            var tessaBreakLineTag = HtmlNode.CreateNode("<p/>");
            tessaBreakLineTag.InnerHtml = breakLineTag.InnerHtml;
            breakLineTag.ParentNode.ReplaceChild(tessaBreakLineTag, breakLineTag);
        }
        
        /// <summary>
        /// Обработка параграфа.
        /// </summary>
        /// <param name="paragraphTag">Узел параграфа дерева HTML.</param>
        private static void HandleParagraph(HtmlNode paragraphTag)
        {
            // если параграф используется как перенос на новую строку.
            // если у параграфа первый наследник уже <span/>.
            if (!paragraphTag.ChildNodes.Any() || paragraphTag.FirstChild.Name == "span")
            {
                return;
            }

            var tessaParagraphTag = HtmlNode.CreateNode("<p><span/></p>");
            tessaParagraphTag.Descendants("span").First().InnerHtml = paragraphTag.InnerHtml;
            paragraphTag.ParentNode.ReplaceChild(tessaParagraphTag, paragraphTag);
        }
        
        #endregion

        /// <summary>
        /// Установка начала и конца строки.
        /// </summary>
        /// <param name="mainString">Строка для преобразования.</param>
        /// <returns>Преобразованная строка.</returns>
        private string PostParseProcess(string mainString)
        {
            var preString = "{\"Text\":\"<div class=\\\"forum-div\\\">";
            var postString = "</div>\"}";
            if (this.IsTopicText)
            {
                preString = "<div class=\"forum-div\">";
                postString = "</div>";
            }

            mainString = mainString.Replace("\n", "", StringComparison.CurrentCulture);
            
            return $"{preString}{mainString}{postString}";
        }

        #endregion
    }
}