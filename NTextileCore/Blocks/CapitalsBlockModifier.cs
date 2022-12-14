using System.Text.RegularExpressions;

namespace NTextileCore.Blocks
{
    public class CapitalsBlockModifier : BlockModifier
    {
        public override string ModifyLine(string line)
        {
            var me = new MatchEvaluator(CapitalsFormatMatchEvaluator);
            line = Regex.Replace(line, @"(?<=^|\s|" + TextileGlobals.PunctuationPattern + @")(?<caps>[A-Z][A-Z0-9]+)(?=$|\s|" + TextileGlobals.PunctuationPattern + @")", me);
            return line;
        }

        static private string CapitalsFormatMatchEvaluator(Match m)
        {
            return $@"<span class=""caps"">{m.Groups["caps"].Value}</span>";
        }
    }
}