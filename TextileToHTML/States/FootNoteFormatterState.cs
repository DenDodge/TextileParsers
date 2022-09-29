using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace TextileToHTML.States
{
    [FormatterState(SimpleBlockFormatterState.TextilePatternBegin + @"fn[0-9]+" + SimpleBlockFormatterState.TextilePatternEnd)]
    public class FootNoteFormatterState : SimpleBlockFormatterState
    {
        private static readonly Regex FootNoteRegex = new Regex(@"^fn(?<id>[0-9]+)");

        private int m_noteID = 0;

        public FootNoteFormatterState()
        {
        }

        public override void Enter()
        {
            Formatter.Output.Write(
                string.Format("<p id=\"fn{0}\"{1}><sup>{2}</sup> ",
                    m_noteID,
                    FormattedStylesAndAlignment(),
                    m_noteID));
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
            return true;
        }
        protected override void OnContextAcquired()
        {
            Match m = FootNoteRegex.Match(Tag);
            m_noteID = Int32.Parse(m.Groups["id"].Value);
        }

        public override bool ShouldNestState(FormatterState other)
        {
            return false;
        }
    }
}