using System.Text.RegularExpressions;

namespace NTextileCore.States
{
    [FormatterState(PatternBegin + @"\*+" + PatternEnd)]
    public class UnorderedListFormatterState : ListFormatterState
    {
        protected override void WriteIndent()
        {
            WriteLine($"<ul{FormattedStylesAndAlignment()}>");
        }

        protected override void WriteOutdent()
        {
            WriteLine("</ul>");
        }

        protected override bool IsMatchForMe(string input, int minNestingDepth, int maxNestingDepth)
        {
            return Regex.IsMatch(input, @"^\s*[\*]{" + minNestingDepth + @"," + maxNestingDepth + @"}" + TextileGlobals.BlockModifiersPattern + @"\s");
        }

        protected override bool IsMatchForOthers(string input, int minNestingDepth, int maxNestingDepth)
        {
            return Regex.IsMatch(input, @"^\s*[#]{" + minNestingDepth + @"," + maxNestingDepth + @"}" + TextileGlobals.BlockModifiersPattern + @"\s");
        }
    }
}