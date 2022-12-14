using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TextileToTessaHtml
{
    public class TextileGlobals
    {
        public static readonly Regex EmptyLineRegex = new Regex(@"^\s*$", RegexOptions.CultureInvariant);

        public const string HorizontalAlignPattern = @"(?:[()]*(\<(?!>)|(?<!<)\>|\<\>|=)[()]*)";
        public const string VerticalAlignPattern = @"[\-^~]";
		public const string CssClassPattern = @"(?:\([^)]+\))";
		public const string LanguagePattern = @"(?:\[[^]]+\])";
		public const string CssStylePattern = @"(?:\{[^}]+\})";
		public const string ColumnSpanPattern = @"(?:\\\d+)";
		public const string RowSpanPattern = @"(?:/\d+)";
        public const string PunctuationPattern = @"[\!""#\$%&'()\*\+,\-\./:;<=>\?@\[\\\]\^_`{}~]";
        public const string HtmlAttributesPattern = @"(\s+\w+=((""[^""]+"")|('[^']+')))*";

        public const string AlignPattern = "(?<align>" + HorizontalAlignPattern + "?" + VerticalAlignPattern + "?|" + VerticalAlignPattern + "?" + HorizontalAlignPattern + "?)";
        public const string SpanPattern = @"(?<span>" + ColumnSpanPattern + "?" + RowSpanPattern + "?|" + RowSpanPattern + "?" + ColumnSpanPattern + "?)";
        public const string BlockModifiersPattern = @"(?<atts>" + CssClassPattern + "?" + CssStylePattern + "?" + LanguagePattern + "?|" +
                                                        CssStylePattern + "?" + LanguagePattern + "?" + CssClassPattern + "?|" +
                                                        LanguagePattern + "?" + CssStylePattern + "?" + CssClassPattern + "?)";

        public static readonly Dictionary<string, string> ImageAlign = new Dictionary<string, string>
        {
            ["<"] = "left",
            ["="] = "center",
            [">"] = "right"
        };

        public static readonly Dictionary<string, string> HorizontalAlign = new Dictionary<string, string>
        {
            ["<"] = "left",
            ["="] = "center",
            [">"] = "right",
            ["<>"] = "justify"
        };

        public static readonly Dictionary<string, string> VerticalAlign = new Dictionary<string, string>
        {
            ["^"] = "top",
            ["-"] = "middle",
            ["~"] = "bottom"
        };


        public const RegexOptions BlockModifierRegexOptions = RegexOptions.CultureInvariant;

        public static string EncodeHTMLLink(string url)
        {
            url = url.Replace("&amp;", "&#38;");
            url = Regex.Replace(url, "&(?=[^#])", "&#38;");
            return url;
        }
    }
}