using System.Text.RegularExpressions;

namespace NTextileCore.States
{
    [FormatterState(TextilePatternBegin + @"fn[0-9]+" + TextilePatternEnd)]
    public class FootNoteFormatterState : SimpleBlockFormatterState
    {
        private static readonly Regex FootNoteRegex = new Regex(@"^fn(?<id>[0-9]+)");

        private int _noteID = 0;

        public override void Enter()
        {
            Write($"<p id=\"fn{_noteID}\"{FormattedStylesAndAlignment()}><sup>{_noteID}</sup>");
        }

        public override void Exit()
        {
            WriteLine("</p>");
        }

        public override void FormatLine(string input)
        {
            Write(input);
        }

        public override bool ShouldExit(string input, string inputLookAhead) => true;

        protected override void OnContextAcquired()
        {
            var m = FootNoteRegex.Match(Tag);
            _noteID = int.Parse(m.Groups["id"].Value);
        }

        public override bool ShouldNestState(FormatterState other) => false;
    }
}