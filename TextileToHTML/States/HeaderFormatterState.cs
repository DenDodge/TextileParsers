using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace TextileToHTML.States
{
    /// <summary>
    /// Formatting state for headers and titles.
    /// </summary>
    [FormatterState(SimpleBlockFormatterState.TextilePatternBegin + @"h[0-9]+" + SimpleBlockFormatterState.TextilePatternEnd)]
    public class HeaderFormatterState : SimpleBlockFormatterState
    {
        private static readonly Regex HeaderRegex = new Regex(@"^h(?<lvl>[0-9]+)");

        private int m_headerLevel = 0;
        public int HeaderLevel
        {
            get { return m_headerLevel; }
        }

        public HeaderFormatterState()
        {
        }

        public override void Enter()
        {
            Formatter.Output.Write(string.Format("<h{0}{1}>", HeaderLevel, FormattedStylesAndAlignment()));
        }

        public override void Exit()
        {
            Formatter.Output.WriteLine(string.Format("</h{0}>", HeaderLevel.ToString()));
        }

        protected override void OnContextAcquired()
        {
            Match m = HeaderRegex.Match(Tag);
            m_headerLevel = Int32.Parse(m.Groups["lvl"].Value);
        }

        public override void FormatLine(string input)
        {
            Formatter.Output.Write(input);
        }

        public override bool ShouldExit(string intput, string inputLookAhead)
        {
            return true;
        }

        public override bool ShouldNestState(FormatterState other)
        {
            return false;
        }
    }
}