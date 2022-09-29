using TextileToTessaHtml.Blocks;

namespace TextileToTessaHtml.States
{
    public abstract class ListFormatterState : FormatterState
    {
        internal const string PatternBegin = @"^\s*(?<tag>";
        internal const string PatternEnd = @")" + TextileGlobals.BlockModifiersPattern + @"(?:\s+)? (?<content>.*)$";

        private bool _firstItem = true;
        private bool _firstItemLine = true;
        private string _tag;
        private string _attsInfo;
        private string _alignInfo;

        protected int NestingDepth
        {
            get { return _tag.Length; }
        }

		public override string Consume(FormatterStateConsumeContext context)
        {
            _tag = context.Match.Groups["tag"].Value;
            _alignInfo = context.Match.Groups["align"].Value;
            _attsInfo = context.Match.Groups["atts"].Value;

            var input = context.Match.Groups["content"].Value;

            ChangeState(this);

            return input;
        }

        public sealed override void Enter()
        {
            _firstItem = true;
            _firstItemLine = true;
            WriteIndent();
        }

        public sealed override void Exit()
        {
            WriteLine("</li>");
            WriteOutdent();
        }

        public sealed override void FormatLine(string input)
        {
            if (_firstItemLine)
            {
                if (!_firstItem)
                {
                    WriteLine("</li>");
                }

                Write("<li>");

                _firstItemLine = false;
            }
            else
            {
                WriteLine("<br />");
            }

            Write(input);
            _firstItem = false;
        }

        public sealed override bool ShouldNestState(FormatterState other)
        {
            var listState = (ListFormatterState)other;
            return listState.NestingDepth > NestingDepth;
        }

		public sealed override bool ShouldExit(string input, string inputLookAhead)
        {
            // If we have an empty line, we can exit.
            if (string.IsNullOrEmpty(input))
            {
                return true;
            }

            // We exit this list if the next
            // list item is of the same type but less
            // deep as us, or of the other type of
            // list and as deep or less.
            if (NestingDepth > 1 && IsMatchForMe(input, 1, NestingDepth - 1))
            {
                return true;
            }

            if (IsMatchForOthers(input, 1, NestingDepth))
            {
                return true;
            }

            // As it seems we're going to continue taking
            // care of this line, we take the opportunity
            // to check whether it's the same list item as
            // previously (no "**" or "##" tags), or if it's
            // a new list item.
            if (IsMatchForMe(input, NestingDepth, NestingDepth))
            {
                _firstItemLine = true;
            }

            return false;
        }

        public sealed override bool ShouldParseForNewFormatterState(string input)
        {
            // We don't let anyone but ourselves mess with our stuff.
            return IsMatchForMe(input, 1, 100) || IsMatchForOthers(input, 1, 100);
        }

        protected abstract void WriteIndent();
        protected abstract void WriteOutdent();
        protected abstract bool IsMatchForMe(string input, int minNestingDepth, int maxNestingDepth);
        protected abstract bool IsMatchForOthers(string input, int minNestingDepth, int maxNestingDepth);

        protected string FormattedStylesAndAlignment()
        {
            return BlockAttributesParser.Parse(_alignInfo + _attsInfo, UseRestrictedMode);
        }
    }
}