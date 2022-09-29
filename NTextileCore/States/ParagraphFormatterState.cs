using System.Text.RegularExpressions;

namespace NTextileCore.States
{
    [FormatterState(TextilePatternBegin + @"p" + TextilePatternEnd)]
    public class ParagraphFormatterState : SimpleBlockFormatterState
    {
        public override void Enter()
        {
            Write("<p" + FormattedStylesAndAlignment() + ">");
        }

        public override void Exit()
        {
            WriteLine("</p>");
        }

        public override void FormatLine(string input)
        {
            Write(input);
        }

        public override bool ShouldExit(string input, string inputLookAhead)
        {
            if (Regex.IsMatch(input, @"^\s*$"))
            {
                return true;
            }

            WriteLine("<br />");

            return false;
        }

        public override bool ShouldNestState(FormatterState other) => false;
    }
}