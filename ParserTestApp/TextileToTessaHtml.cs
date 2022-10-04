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
        private static bool IsTopicText;
        
        /// <summary>
        /// Пустое описание инцидента.
        /// Применяется, если входящая строка пустая или с пробелами.
        /// </summary>
        private const string EmptyString = "{\"Text\":\"<div class=\\\"forum-div\\\"><p><span> </span></p></div>\"}";

        /// <summary>
        /// Шаблон регулярного выражения для блоков "&lt;pre&gt;&lt;code&gt;".
        /// </summary>
        private static readonly string preCodeTagsTemplate = "<pre><code.*?>";
        
        /// <summary>
        /// Регулярное выражение для секции кода с "pre".
        /// </summary>
        private static readonly Regex _preCodeTag = new (preCodeTagsTemplate, RegexOptions.Singleline | RegexOptions.Compiled);

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
            ("//precode", HandlePreCode),
            ("//pre", HandlePre),
            ("//code", HandleCode),
            ("//br", HandleBreakLine),
        };

        #endregion

        public static (string ResultString, HashSet<Guid> AttachedFileIds) Parse(string mainString, bool isTopicText)
        {
            // если пришла пустая строка - вернем заглушку.
            if (string.IsNullOrWhiteSpace(mainString))
            {
                return (EmptyString, new HashSet<Guid>());
            }
            
            IsTopicText = isTopicText;
            
            // получаю HTML строку со стандартной разметкой.
            var parseString = TextileFormatter.FormatString(mainString);
            
            // парсер возвращает результат обернутый в <p>. Для корректной разметки - нужно еще обернуть все в <span>.
            // TODO: возможно, нужно удалить или поправить форматирование.
            //parseString = parseString.Replace("<p>", "<p><span>", StringComparison.CurrentCulture);
            //parseString = parseString.Replace("</p>", "</span></p>", StringComparison.CurrentCulture);

            // делаем отдельный тег <precode></precode> для моноширинного БЛОКА.
            // если блоки <pre> и <code> идут друг за другом.
            while (Regex.IsMatch(parseString, preCodeTagsTemplate, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled))
            {
                parseString = _preCodeTag.Replace(parseString, "<precode>");
            }
            parseString = parseString.Replace("</code></pre>", "</precode>");
            
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
            // HtmlAgilityPack - в разметке не экранирует кавычки. А в разметке они нужны.
            parseString = doc.DocumentNode.InnerHtml.Replace("\"", "\\\"", StringComparison.CurrentCulture);
            // устанавливаем разметку в начало и конец строки.
            parseString = PostParseProcess(parseString);

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
        /// Обработчик тега &lt;pre&gt;&lt;code&gt;.
        /// </summary>
        /// <param name="preCodeTag">Узел &lt;pre&gt;&lt;code&gt; дерева HTML.</param>
        private static void HandlePreCode(HtmlNode preCodeTag)
        {
            var tessaPreTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
            // преобразуем перенос на новую строку в <br/> для того, чтобы моноширинный блок красиво отображался.
            tessaPreTag.InnerHtml = preCodeTag.InnerHtml.Replace("\n", "<br/>");;
            preCodeTag.ParentNode.ReplaceChild(tessaPreTag, preCodeTag);
        }
        
        /// <summary>
        /// Обработчик тега &lt;pre&gt;.
        /// </summary>
        /// <param name="preTag">Узел &lt;pre&gt; дерева HTML.</param>
        private static void HandlePre(HtmlNode preTag)
        {
            // в <pre> могут быть теги <code>.
            var codeTags = preTag.SelectNodes("//code");
            // если тегов <code> нет - обрабатываем как моноширинную строку.
            if (codeTags is null)
            {
                var tessaPreTag = HtmlNode.CreateNode($"<span style=\"{TessaMarkup.Styles.Pre}\" data-custom-style=\"{TessaMarkup.DataCustomStyles.Pre}\" class=\"{TessaMarkup.Classes.Pre}\"/>");
                tessaPreTag.InnerHtml = preTag.InnerHtml;
                preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
            }
            // иначе обрабатываем как моноширинный блок.
            else
            {
                var tessaPreTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
                tessaPreTag.InnerHtml = preTag.InnerHtml;
                preTag.ParentNode.ReplaceChild(tessaPreTag, preTag);
            }
        }

        /// <summary>
        /// Обработчик тега &lt;code&gt;.
        /// </summary>
        /// <param name="codeTag">Узел &lt;code&gt; дерева HTML.</param>
        private static void HandleCode(HtmlNode codeTag)
        {
            var tessaCodeTag = HtmlNode.CreateNode($"<div class=\"{TessaMarkup.Classes.PreCode}\"/>");
            // преобразуем перенос на новую строку в <br/> для того, чтобы моноширинный блок красиво отображался.
            tessaCodeTag.InnerHtml = codeTag.InnerHtml.Replace("\n", "<br/>");
            codeTag.ParentNode.ReplaceChild(tessaCodeTag, codeTag);
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
        
        #endregion

        /// <summary>
        /// Установка начала и конца строки.
        /// </summary>
        /// <param name="mainString">Строка для преобразования.</param>
        /// <returns>Преобразованная строка.</returns>
        private static string PostParseProcess(string mainString)
        {
            var preString = "{\"Text\":\"<div class=\\\"forum-div\\\">";
            var postString = "</div>\"}";
            if (IsTopicText)
            {
                preString = "<div class=\\\"forum-div\\\">";
                postString = "</div>";
            }

            //TODO: Нужно для тестирования.
            mainString = mainString.Replace("\n", "", StringComparison.CurrentCulture);
            
            return $"{preString}{mainString}{postString}";
        }

        #endregion
    }
}