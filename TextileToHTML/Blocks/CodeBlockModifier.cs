using System.Text.RegularExpressions;

namespace TextileToHTML.Blocks
{
    public class CodeBlockModifier : BlockModifier
    {
        public override string ModifyLine(string line)
        {
            // Replace "@...@" zones with "<code>" tags.
            MatchEvaluator me = new MatchEvaluator(CodeFormatMatchEvaluator);
            line = Regex.Replace(line,
                @"(?<before>^|([\s\([{]))" +    // before
                "@" +
                @"(\|(?<lang>\w+)\|)?" +        // lang
                "(?<code>[^@]+)" +              // code
                "@" +
                @"(?<after>$|([\]}])|(?=" + TextileGlobals.PunctuationPattern + @"{1,2}|\s|$))",  // after
                me);
            // Encode the contents of the "<code>" tags so that we don't
            // generate formatting out of it.
            line = NoTextileEncoder.EncodeNoTextileZones(line,
                @"(?<=(^|\s)<code(" + TextileGlobals.HtmlAttributesPattern + @")>)",
                @"(?=</code>)");
            return line;
        }

        public override string Conclude(string line)
        {
            // Recode everything except "<" and ">";
            line = NoTextileEncoder.DecodeNoTextileZones(line,
                @"(?<=(^|\s)<code(" + TextileGlobals.HtmlAttributesPattern + @")>)",
                @"(?=</code>)",
                new string[] { "<", ">" });
            return line;
        }

        static public string CodeFormatMatchEvaluator(Match m)
        {
            string res = m.Groups["before"].Value + "<inlinecode";
            if (m.Groups["lang"].Length > 0)
                res += " language=\"" + m.Groups["lang"].Value + "\"";
            res += ">" + m.Groups["code"].Value + "</inlinecode>" + m.Groups["after"].Value;
            return res;
        }
    }
}