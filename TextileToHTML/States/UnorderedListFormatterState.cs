using System.Text.RegularExpressions;

namespace TextileToHTML.States
{
    /// <summary>
    /// Formatting state for a bulleted list.
    /// </summary>
    [FormatterState(ListFormatterState.PatternBegin + @"\*+" + ListFormatterState.PatternEnd)]
    public class UnorderedListFormatterState : ListFormatterState
    {
        public UnorderedListFormatterState()
        {
        }

        protected override void WriteIndent()
        {
            Formatter.Output.WriteLine("<ul" + FormattedStylesAndAlignment() + ">");
        }

        protected override void WriteOutdent()
        {
            Formatter.Output.WriteLine("</ul>");
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