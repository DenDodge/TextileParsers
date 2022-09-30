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
        /// <summary>
        /// Флаг, что строка из сообщения топика.
        /// </summary>
        private static bool IsTopicText;
        
        /// <summary>
        /// Пустое описание инцидента.
        /// Применяется, если входящая строка пустая или с пробелами.
        /// </summary>
        private const string EmptyString = "{\"Text\":\"<div class=\\\"forum-div\\\"><p><span>Описание инцидента отсутствует.</span></p></div>\"}";

        /// <summary>
        /// Шаблон регулярного выражения для блоков "&lt;pre&gt;&lt;code&gt;".
        /// </summary>
        private static readonly string preCodeTagsTemplate = "<pre><code.*?>";
        
        /// <summary>
        /// Регулярное выражение для секции кода с "pre".
        /// </summary>
        private static readonly Regex _preCodeTag = new (preCodeTagsTemplate, RegexOptions.Singleline | RegexOptions.Compiled);
        
        /// <summary>
        /// Список стилей разметки TESSA.
        /// </summary>
        private static Dictionary<string, string> tessaTagStyles = new()
        {
            { "*[self::strong or self::b]", "<span style=\"font-weight:bold;\">" },
            { "*[self::em or self::i]", "<span style=\"font-style:italic;\">" },
            { "ins", "<span style=\"text-decoration:underline;\">" },
            { "del", "<span style=\"text-decoration:line-through;\">" },
            { "ul", "<ul class=\"forum-ul\">" },
            { "ol", "<ol class=\"forum-ol\">" },
            { "li", "<li><p><span>" },
            { "blockquote", "<div class=\"forum-quote\"><p><span>" },
            { "pre", "<p><span style=\"font-size: 14px\" data-custom-style=\"font-size:14;\" class=\"forum-block-inline\">" },
            { "precode", "<div class=\"forum-block-monospace\"><p><span>" },
            { "br", "<p>" },
            { "*[self::h1 or self::h2 or self::h3 or self::h4]", "<p>" },
        };
        
        public static (string ResultString, HashSet<Guid> AttachedFileIds) Parse(string mainString, bool isTopicText)
        {
            // если пришла пустая строка - вернем заглушку.
            if (string.IsNullOrWhiteSpace(mainString))
            {
                return (mainString, new HashSet<Guid>());
            }
            
            IsTopicText = isTopicText;
            
            // получаю HTML строку со стандартной разметкой.
            var parseString = TextileFormatter.FormatString(mainString);
            
            // парсер возвращает результат обернутый в <p>. Для корректной разметки - нужно еще обернуть все в <span>.
            parseString = parseString.Replace("<p>", "<p><span>", StringComparison.CurrentCulture);
            parseString = parseString.Replace("</p>", "</span></p>", StringComparison.CurrentCulture);
            
            // делаем отдельный тег <precode></precode> для моноширинного БЛОКА.
            while (Regex.IsMatch(parseString, preCodeTagsTemplate, RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.Compiled))
            {
                parseString = _preCodeTag.Replace(parseString, "<precode>");
            }
            parseString = parseString.Replace("</code></pre>", "</precode>");
            
            // получаю DOM дерево полученного HTML.
            var doc = new HtmlDocument();
            doc.LoadHtml(parseString);
            foreach (var tessaTag in tessaTagStyles)
            {
                ParseHtmlTag(doc.DocumentNode, tessaTag.Key);
            }

            // получаем преобразованный с помощью HtmlAgilityPack текст.
            // HtmlAgilityPack - в разметке не экранирует кавычки. А в разметке они нужны.
            parseString = doc.DocumentNode.InnerHtml.Replace("\"", "\\\"", StringComparison.CurrentCulture);
            // устанавливаем разметку в начало и конец строки.
            parseString = PostParseProcess(parseString);

            return (parseString, new HashSet<Guid>());
        }

        /// <summary>
        /// Преобразование стандартных тегов HTML.
        /// </summary>
        /// <param name="documentNode">Основной узел дерева HTML.</param>
        /// <param name="tagName">Наименование стандартного тега.</param>
        private static void ParseHtmlTag(HtmlNode documentNode, string tagName)
        {
            var tags = documentNode
                .SelectNodes($"//{tagName}");
            
            if (tags is null)
            {
                return;
            }

            for (var i = tags.Count - 1; i >= 0; i--)
            {
                var tag = tags[i];
                var tessaTag = HtmlNode.CreateNode(tessaTagStyles[tagName]);
                SetInnerHtml(tessaTag, tag.InnerHtml);
                //tessaTag.InnerHtml = tag.InnerHtml;
                tag.ParentNode.ReplaceChild(tessaTag, tag);
            }
        }

        /// <summary>
        /// Установить HTML код элемента.
        /// </summary>
        /// <param name="tag">Элемент.</param>
        /// <param name="innerHtml">HTML код.</param>
        /// <returns>HTML код, установленный в элемент.</returns>
        private static string SetInnerHtml(HtmlNode tag, string innerHtml)
        {
            // если у тега есть дочерние элементы - идем внутрь.
            if (tag.ChildNodes.Any())
            {
                innerHtml = SetInnerHtml(tag.FirstChild, innerHtml);
            }
            // если детей нет - устанавливает в текущий тег нужный нам innerHtml.
            tag.InnerHtml = innerHtml;
            // возвращаем полную разметку элемента.
            return tag.OuterHtml;
        }
        
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

            mainString = mainString.Replace("\n", "", StringComparison.CurrentCulture);
            
            return $"{preString}{mainString}{postString}";
        }
    }
}