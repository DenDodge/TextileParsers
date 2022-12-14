using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace TextileToHTML.States
{
    [FormatterState(SimpleBlockFormatterState.TextilePatternBegin + @"bc" + SimpleBlockFormatterState.TextilePatternEnd)]
    public class PreCodeFormatterState : SimpleBlockFormatterState
    {
        public PreCodeFormatterState()
        {
        }

        public override void Enter()
        {
            Formatter.Output.Write("<pre><code>");
        }

        public override void Exit()
        {
            Formatter.Output.WriteLine("</code></pre>");
        }

        public override void FormatLine(string input)
        {
            Formatter.Output.WriteLine(FixEntities(input));
        }

        public override bool ShouldExit(string input, string inputLookAhead)
        {
            if (Regex.IsMatch(input, @"^\s*$"))
                return true;
            Formatter.Output.WriteLine("<br />");
            return false;
        }

        public override bool ShouldFormatBlocks(string input)
        {
            return false;
        }

        public override bool ShouldNestState(FormatterState other)
        {
            return false;
        }

        public override bool ShouldParseForNewFormatterState(string input)
        {
            return false;
        }

        private string FixEntities(string text)
        {
            // de-entify any remaining angle brackets or ampersands
            text = text.Replace("&", "&amp;");
            text = text.Replace(">", "&gt;");
            text = text.Replace("<", "&lt;");
            //Regex.Replace(text, @"\b&([#a-z0-9]+;)", "x%x%");
            return text;
        }
    }
}