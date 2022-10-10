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
        /// Флаг, что строка из сообщения топика.
        /// </summary>
        private bool IsTopicText;

        /// <summary>
        /// Приложенные к тексту файлы.
        /// </summary>
        private Dictionary<Guid, (string Name, string Path)> TextAttachments;

        /// <summary>
        /// Обработанные ссылки.
        /// </summary>
        private Dictionary<Guid, string> UriAttachments = new();

        /// <summary>
        /// Модель описания инцидента.
        /// </summary>
        private JsonDescription JsonDescription;

        #region Static Fields

        /// <summary>
        /// Пустое описание инцидента.
        /// Применяется, если входящая строка пустая или с пробелами.
        /// </summary>
        private static readonly string EmptyString = "{\"Text\":\"<div class=\\\"forum-div\\\"><p><span> </span></p></div>\"}";

        /// <summary>
        /// Список обработчиков тегов разметки.
        /// </summary>
        private readonly List<(string XPath, Action<HtmlNode> Handler)> DocumentTransitions;

        public TextileToTessaHtml()
        {
            this.DocumentTransitions = new List<(string XPath, Action<HtmlNode> Handler)>
            {
                ("//table", HandleTable),
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
                ("//a", this.HandleLink),
                ("//img", this.HandleImg),
                ("//br", HandleBreakLine),
                ("//p", HandleParagraph),
            };
        }

        #endregion

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
            this.UriAttachments.Clear();
            // если пришла пустая строка - вернем заглушку.
            if (string.IsNullOrWhiteSpace(mainString))
            {
                return (EmptyString, this.UriAttachments);
            }
            
            this.IsTopicText = isTopicText;
            this.TextAttachments = textAttachments.ToDictionary(x => x.Key, x => x.Value);
            // if (!this.IsTopicText)
            // {
            //     this.JsonDescription = new JsonDescription();
            // }

            // получаю HTML строку со стандартной разметкой.
            var parseString = TextileFormatter.FormatString(mainString);

            // получаю DOM дерево полученного HTML.
            var doc = new HtmlDocument();
            doc.LoadHtml(parseString);
            // обрабатываем переходы документа.
            foreach (var transition in this.DocumentTransitions)
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
            
            var childNodes = HandleSpan(doc.DocumentNode, "");
            doc.DocumentNode.RemoveAllChildren();
            doc.DocumentNode.ChildNodes.AddRange(childNodes);
            
            // получаем преобразованный с помощью HtmlAgilityPack текст.
            parseString = doc.DocumentNode.InnerHtml;
            // устанавливаем разметку в начало и конец строки.
            parseString = this.PostParseProcess(parseString);

            return (parseString, this.UriAttachments);
        }

        #region Private Methods

        #region TagHenlers

        /// <summary>
        /// Обработчик таблицы.
        /// </summary>
        /// <param name="tableTag">Узел таблицы дерева HTML.</param>
        private static void HandleTable(HtmlNode tableTag)
        {
            // RichEdit не может обрабатывать таблицы.
            // Удаляем этот элемент из дерева и сообщаем о том, что не удается преобразовать тег.
            tableTag.ParentNode.RemoveChild(tableTag);
        }
        
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
            // если в li уже есть span.
            if (listItemTag.SelectNodes("span") is not null)
            {
                // удаляем добавленный нами span.
                tessaListItemTag.Descendants("span").First().Remove();
                tessaListItemTag.Descendants("p").First().InnerHtml = listItemTag.InnerHtml;
            }
            else
            {
                tessaListItemTag.Descendants("span").First().InnerHtml = listItemTag.InnerHtml;
            }
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
                return;
            }
            
            // если есть - тогда добавляем корректное форматирование в строку,
            // делим текст на строки.
            var codeInnerHtml = codeTag.InnerHtml;
            codeTag.RemoveAll();
            var lines = codeInnerHtml.Split('\n');
            foreach (var line in lines)
            {
                // каждую строку оборачиваем в <p/>
                var pTag = HtmlNode.CreateNode("<p/>");
                pTag.InnerHtml = line.Replace("\n", "", StringComparison.CurrentCulture);
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
                tessaPreTag.InnerHtml = preTag.InnerHtml;
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
            
            tessaPreTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
            tessaPreTag.InnerHtml = preTag.InnerHtml;
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
            // если url нет - нам ссылка не нужна.
            if (string.IsNullOrEmpty(linkHrefAttribute))
            {
                linkTag.ParentNode.RemoveChild(linkTag);
                return;
            }
            
            var tessaLinkTag = HtmlNode.CreateNode($"<a style=\"{TessaMarkup.Styles.Link}\" data-custom-href=\"{linkHrefAttribute}\" href=\"{linkHrefAttribute}\" class=\"{TessaMarkup.Classes.Link}\"><span/></a>");
            tessaLinkTag.Descendants("span").First().InnerHtml = linkTag.InnerHtml;
            linkTag.ParentNode.ReplaceChild(tessaLinkTag, linkTag);

            var uriId = Guid.NewGuid();
            this.UriAttachments.Add(uriId, linkHrefAttribute);
            // if (!this.IsTopicText)
            // {
            //     this.JsonDescription.Attachments.Add(GenerateItemModel(
            //         uriId, 
            //         linkHrefAttribute, 
            //         !string.IsNullOrEmpty(linkTag.InnerHtml) ? linkTag.InnerHtml : linkHrefAttribute, 
            //         AttachmentType.Link));
            // }
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
                imgTag.ParentNode.RemoveChild(imgTag);
                return;
            }

            // если проложенное изображение - это ссылка.
            if (Uri.IsWellFormedUriString(imgSrcAttributeValue, UriKind.Absolute))
            {
                var imgTitleAttributeValue = imgTag.Attributes.FirstOrDefault(a => a.Name == "title")?.Value;
                var tessaLinkTag = HtmlNode.CreateNode($"<a style=\"{TessaMarkup.Styles.Link}\" data-custom-href=\"{imgSrcAttributeValue}\" href=\"{imgSrcAttributeValue}\" class=\"{TessaMarkup.Classes.Link}\"><span/></a>");
                tessaLinkTag.Descendants("span").First().InnerHtml = !string.IsNullOrWhiteSpace(imgTitleAttributeValue) ? imgTitleAttributeValue : imgSrcAttributeValue;
                imgTag.ParentNode.ReplaceChild(tessaLinkTag, imgTag);
                
                var uriId = Guid.NewGuid();
                this.UriAttachments.Add(uriId, imgSrcAttributeValue);
                // if (!this.IsTopicText)
                // {
                //     this.JsonDescription.Attachments.Add(GenerateItemModel(
                //         uriId, 
                //         imgSrcAttributeValue, 
                //         !string.IsNullOrWhiteSpace(imgTitleAttributeValue) ? imgTitleAttributeValue : imgSrcAttributeValue, 
                //         AttachmentType.Link));
                // }
                
                return;
            }
            
#pragma warning disable CA1309
            var image = this.TextAttachments.First(a => string.Equals(a.Value.Name, imgSrcAttributeValue, StringComparison.CurrentCultureIgnoreCase));
#pragma warning restore CA1309
            
            var thumbnail = GenerateThumbnail(image.Value.Path);
            
            var tessaImgTag = HtmlNode.CreateNode($"<p><span><img data-custom-style=\"{string.Format(TessaMarkup.DataCustomStyles.Img, thumbnail.Width, thumbnail.Height)}\" name=\"{image.Key:N}\" src=\"{thumbnail.Base64}\"/></span></p>");
            imgTag.ParentNode.ReplaceChild(tessaImgTag, imgTag);
            // if (!this.IsTopicText)
            // {
            //     this.JsonDescription.Attachments.Add(GenerateItemModel(
            //         image.Key, 
            //         imgSrcAttributeValue, 
            //         image.Key.ToString("N"), 
            //         AttachmentType.InnerItem));
            //     this.TextAttachments.Remove(image.Key);
            // }
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
            var styleAttributes = paragraphTag.Attributes.Where(a => a.Name == "style").ToList();
            if (styleAttributes.Any())
            {
                tessaParagraphTag.Descendants("p").First().Attributes.AddRange(styleAttributes);
            }
            tessaParagraphTag.Attributes.AddRange(styleAttributes);
            tessaParagraphTag.Descendants("span").First().InnerHtml = paragraphTag.InnerHtml;
            paragraphTag.ParentNode.ReplaceChild(tessaParagraphTag, paragraphTag);
        }

        /// <summary>
        /// Обработчик тегов &lt;span/&gt;
        /// </summary>
        /// <param name="parentNode">Узел дерева HTML.</param>
        /// <param name="parentStyleValue">Значение стиля родительского элемента.</param>
        /// <returns>Список преобразованных тегов родительского <paramref name="parentNode"/>.</returns>
        private static List<HtmlNode> HandleSpan(HtmlNode parentNode, string parentStyleValue)
        {
            var resultTags = new List<HtmlNode>();
            
            if (!parentNode.ChildNodes.Any())
            {
                if (parentNode.Name == "#text" && !string.IsNullOrWhiteSpace(parentStyleValue) && !string.IsNullOrWhiteSpace(parentNode.InnerText))
                {
                    var newSpan = HtmlNode.CreateNode("<span/>");
                    newSpan.Attributes.Add("style", parentStyleValue);
                    newSpan.InnerHtml = parentNode.InnerHtml;
                    resultTags.Add(newSpan);
                    return resultTags;
                }
                resultTags.Add(parentNode.Clone());
                return resultTags;
            }
            
            foreach (var currentChild in parentNode.ChildNodes)
            {
                if (currentChild.Name == "span")
                {
                    var styleAttributeValue = currentChild.Attributes.FirstOrDefault(a => a.Name == "style")?.Value;
                    var attributeValue = $"{parentStyleValue}{styleAttributeValue}";
                    var parseResult = HandleSpan(currentChild, attributeValue);
                    if (!parseResult.Exists(ps => ps.Name == "span"))
                    {
                        resultTags.Add(currentChild);
                    }
                    else
                    {
                        resultTags.AddRange(parseResult);
                    }
                }
                else
                {
                    resultTags.AddRange(HandleSpan(currentChild, parentStyleValue));
                }
            }

            if (parentNode.Name != "span" && parentNode.Name != "#document")
            {
                var newNode = parentNode.Clone();
                newNode.RemoveAllChildren();
                newNode.ChildNodes.AddRange(resultTags);
                return new List<HtmlNode>
                {
                    newNode
                };
            }

            return resultTags;
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
        /// Получить миниатюру приложенного изображения и его размеры.
        /// </summary>
        /// <param name="pathToImage">Путь до изображения.</param>
        /// <returns>Миниатюра приложенного изображения и его размеры.</returns>
        [SuppressMessage("Interoperability", "CA1416")]
        private static (string Base64, int Width, int Height) GenerateThumbnail(string pathToImage)
        {
            // FileStream sourceStream = null;
            // Bitmap bitmap = null;
            // MemoryStream stream;
            // int imageWidth;
            // int imageHeight;
            //
            // try
            // {
            //     sourceStream = FileHelper.OpenRead(pathToImage);
            //     bitmap = new Bitmap(sourceStream);
            //     sourceStream.Dispose();
            //     sourceStream = null;
            //
            //     if (bitmap.Width >= ForumHelper.ImageSideSize && bitmap.Width > bitmap.Height)
            //     {
            //         double factor = bitmap.Width / (float) ForumHelper.ImageSideSize;
            //         var newHeight = (int) (bitmap.Height / factor);
            //         var resizedBitmap = new Bitmap(bitmap, ForumHelper.ImageSideSize, newHeight);
            //         bitmap.Dispose();
            //         bitmap = resizedBitmap;
            //     }
            //     else if (bitmap.Height >= ForumHelper.ImageSideSize)
            //     {
            //         double factor = bitmap.Height / (float) ForumHelper.ImageSideSize;
            //         var newWidth = (int) (bitmap.Width / factor);
            //         var resizedBitmap = new Bitmap(bitmap, newWidth, ForumHelper.ImageSideSize);
            //         bitmap.Dispose();
            //         bitmap = resizedBitmap;
            //     }
            //     
            //     var encoder = ImageCodecInfo.GetImageDecoders().First(codec => codec.FormatID == ImageFormat.Jpeg.Guid);
            //     var myEncoderParameters = new EncoderParameters(1)
            //     {
            //         Param =
            //         {
            //             [0] = new EncoderParameter(Encoder.Quality, 75L)
            //         }
            //     };
            //     stream = new MemoryStream(80_000);
            //     bitmap.Save(stream, encoder, myEncoderParameters);
            //     imageWidth = bitmap.Width;
            //     imageHeight = bitmap.Height;
            // }
            // finally
            // {
            //     bitmap?.Dispose();
            //     sourceStream?.Dispose();
            // } 
            //
            // stream.Position = 0L;
            // const string prefix = "data:image/png;base64,";
            // using var stringStream = new MemoryStream(prefix.Length + (int) stream.Length);
            // using (var stringWriter = new StreamWriter(stringStream, Encoding.UTF8, leaveOpen: true))
            // {
            //     stringWriter.Write(prefix);
            // }
            // using (var base64Stream = new CryptoStream(stream, new ToBase64Transform(), CryptoStreamMode.Read))
            // {
            //     base64Stream.CopyTo(stringStream);
            // }
            // string base64;
            // stringStream.Position = 0L;
            // using (var stringReader = new StreamReader(stringStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: false))
            // {
            //     base64 = stringReader.ReadToEnd();
            // }
            //     
            // return (base64, imageWidth, imageHeight);
            return ("", 0, 0);
        }
        
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
            // if (!this.IsTopicText)
            // {
            //     this.JsonDescription.Text = mainString;
            //     // добавить остальные файлы.
            //     foreach (var textAttachment in this.TextAttachments)
            //     {
            //         this.JsonDescription.Attachments.Add(GenerateItemModel(
            //             textAttachment.Key,
            //             $"tessa://attachfile_{textAttachment.Key}", 
            //             textAttachment.Value.Name, 
            //             AttachmentType.File,
            //             true));
            //         this.TextAttachments.Remove(textAttachment.Key);
            //     }
            //     return StorageHelper.SerializeToTypedJson(this.JsonDescription.GetStorage());
            // }

            return mainString;
        }

        #endregion
   }
}