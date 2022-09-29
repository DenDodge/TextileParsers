using System.Text.RegularExpressions;

namespace TextileToHTML.States
{
    /// <summary>
    /// Formatting state for a standard text (i.e. just paragraphs).
    /// </summary>
    [FormatterState(SimpleBlockFormatterState.TextilePatternBegin + @"p" + SimpleBlockFormatterState.TextilePatternEnd)]
    public class ParagraphFormatterState : SimpleBlockFormatterState
    {
        public ParagraphFormatterState()
        {
        }

        public override void Enter()
        {
            Formatter.Output.Write("<p" + FormattedStylesAndAlignment() + ">");
        }

        public override void Exit()
        {
            Formatter.Output.WriteLine("</p>");
        }

        public override void FormatLine(string input)
        {
            Formatter.Output.Write(input);
        }

        public override bool ShouldExit(string input, string inputLookAhead)
        {
            if (Regex.IsMatch(input, @"^\s*$"))
                return true;
            Formatter.Output.WriteLine("<br />");
            return false;
        }

        public override bool ShouldNestState(FormatterState other)
        {
            return false;
        }
    }
}