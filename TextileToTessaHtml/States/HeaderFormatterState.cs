using System.Text.RegularExpressions;

namespace TextileToTessaHtml.States
{
    [FormatterState(TextilePatternBegin + @"h[0-9]+" + TextilePatternEnd)]
    public class HeaderFormatterState : SimpleBlockFormatterState
    {
        private static readonly Regex HeaderRegex = new Regex(@"^h(?<lvl>[0-9]+)");

        private int _headerLevel;

        public override void Enter()
        {
            Write($"<h{_headerLevel}{FormattedStylesAndAlignment()}>");
        }

        public override void Exit()
        {
            WriteLine($"</h{_headerLevel}>");
        }

        protected override void OnContextAcquired()
        {
            var m = HeaderRegex.Match(Tag);
            _headerLevel = int.Parse(m.Groups["lvl"].Value);
        }

        public override void FormatLine(string input)
        {
            Write(input);
        }

        public override bool ShouldExit(string intput, string inputLookAhead) => true;

        public override bool ShouldNestState(FormatterState other) => false;
    }
}