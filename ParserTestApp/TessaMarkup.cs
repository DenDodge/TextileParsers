namespace TestApp
{
    /// <summary>
    /// Разметка Tessa для RichEdit.
    /// </summary>
    public static class TessaMarkup
    {
        /// <summary>
        /// Стили.
        /// </summary>
        public static class Styles
        {
            /// <summary>
            /// Стиль для жирного шрифта.
            /// </summary>
            public const string Bold = "font-weight:bold;";
            /// <summary>
            /// Стиль для курсивного шрифта.
            /// </summary>
            public const string Italic = "font-style:italic;";
            /// <summary>
            /// Стиль для подчеркнутого шрифта.
            /// </summary>
            public const string Underline = "text-decoration:underline;";
            /// <summary>
            /// Стиль для зачеркнутого шрифта.
            /// </summary>
            public const string CrossedOut = "text-decoration:line-through;";
            /// <summary>
            /// Стиль для моноширинного текста.
            /// </summary>
            public const string Pre = "font-size: 14px";
            /// <summary>
            /// Стиль для заголовка.
            /// </summary>
            public const string Header = "font-weight:bold;";
            /// <summary>
            /// Стиль для ссылки.
            /// </summary>
            public const string Link = "color: rgba(0, 102, 204, 1); text-decoration: underline";
        }
        
        /// <summary>
        /// Пользовательский стили данных.
        /// </summary>
        public static class DataCustomStyles
        {
            /// <summary>
            /// Пользовательский стиль для моноширинного текста.
            /// </summary>
            public const string Pre = "font-size:14;";
            /// <summary>
            /// Пользовательский стиль для заголовка.
            /// </summary>
            public const string Header = "font-size:18;";

            public const string Img = "width:{0};height:{1};";
        }
        
        /// <summary>
        /// Классы.
        /// </summary>
        public static class Classes
        {
            /// <summary>
            /// Класс для маркированного списка.
            /// </summary>
            public const string UnorderedList = "forum-ul";
            /// <summary>
            /// Класс для нумерованного списка.
            /// </summary>
            public const string OrderedList = "forum-ol";
            /// <summary>
            /// Класс для блока цитирования.
            /// </summary>
            public const string Blockquote = "forum-quote";
            /// <summary>
            /// Класс для моноширинного текста.
            /// </summary>
            public const string Pre = "forum-block-inline";
            /// <summary>
            /// Класс для моноширинного блока.
            /// </summary>
            public const string PreCode = "forum-block-monospace";
            /// <summary>
            /// Класс для ссылки.
            /// </summary>
            public const string Link = "forum-url";
        }
    }
}