using System.Text.RegularExpressions;

namespace NTextileCore.States
{
    [FormatterState(PatternBegin + @"#+" + PatternEnd)]
    public class OrderedListFormatterState : ListFormatterState
    {
        protected override void WriteIndent()
        {
            WriteLine("<ol" + FormattedStylesAndAlignment() + ">");
        }

        protected override void WriteOutdent()
        {
            WriteLine("</ol>");
        }

        protected override bool IsMatchForMe(string input, int minNestingDepth, int maxNestingDepth)
        {
            return Regex.IsMatch(input, @"^\s*([\*#]{" + (minNestingDepth - 1) + @"," + (maxNestingDepth - 1) + @"})#" + TextileGlobals.BlockModifiersPattern + @"\s");
        }

        protected override bool IsMatchForOthers(string input, int minNestingDepth, int maxNestingDepth)
        {
            return Regex.IsMatch(input, @"^\s*([\*#]{" + (minNestingDepth - 1) + @"," + (maxNestingDepth - 1) + @"})\*" + TextileGlobals.BlockModifiersPattern + @"\s");
        }
    }
}