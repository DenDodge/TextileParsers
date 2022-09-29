using System.Text.RegularExpressions;

namespace TextileToHTML.Blocks
{
    public class ImageBlockModifier : BlockModifier
    {
        private static readonly Regex HrefRegex = new Regex(@"(.*)(?<end>\.|,|;|\))$");

        public override string ModifyLine(string line)
        {
            line = Regex.Replace(line,
                                    @"\!" +                   // opening !
                                    @"(?<algn>\<|\=|\>)?" +   // optional alignment atts
                                    TextileGlobals.BlockModifiersPattern + // optional style, public class atts
                                    @"(?:\. )?" +             // optional dot-space
                                    @"(?<url>[^\s(!]+)" +     // presume this is the src
                                    @"\s?" +                  // optional space
                                    @"(?:\((?<title>([^\)]+))\))?" +// optional title
                                    @"\!" +                   // closing
                                    @"(?::(?<href>(\S+)))?" +     // optional href
                                    @"(?=\s|\.|,|;|\)|\||$)",               // lookahead: space or simple punctuation or end of string
                                new MatchEvaluator(ImageFormatMatchEvaluator)
                                );
            return line;
        }

        private string ImageFormatMatchEvaluator(Match m)
        {
            string atts = BlockAttributesParser.ParseBlockAttributes(m.Groups["atts"].Value, "", UseRestrictedMode);
            if (m.Groups["algn"].Length > 0)
                atts += " align=\"" + TextileGlobals.ImageAlign[m.Groups["algn"].Value] + "\"";
            if (m.Groups["title"].Length > 0)
            {
                atts += " title=\"" + m.Groups["title"].Value + "\"";
                atts += " alt=\"" + m.Groups["title"].Value + "\"";
            }
            else
            {
                atts += " alt=\"\"";
            }
            // Get Image Size?

            // Validate the URL.
            string url = m.Groups["url"].Value;
            if (url.Contains("\"")) // Disable the URL if someone's trying a cheap hack.
                url = "#";

            string res = "<img src=\"" + url + "\"" + atts + " />";

            if (m.Groups["href"].Length > 0)
            {
                string href = m.Groups["href"].Value;
                string end = string.Empty;
                Match endMatch = HrefRegex.Match(href);
                if (m.Success && !string.IsNullOrEmpty(endMatch.Groups["end"].Value))
                {
                    href = href.Substring(0, href.Length - 1);
                    end = endMatch.Groups["end"].Value;
                }
                res = "<a href=\"" + TextileGlobals.EncodeHTMLLink(href) + "\">" + res + "</a>" + end;
            }

            return res;
        }
    }
}