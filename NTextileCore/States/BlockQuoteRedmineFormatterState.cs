using System;
using System.Text.RegularExpressions;

namespace NTextileCore.States
{
    [FormatterState(Pattern)]
    public class BlockQuoteRedmineFormatterState : FormatterState
    {
        #region Fields

        /// <summary>
        /// Паттерн регулярного выражения, отвечающего за блок цитат.
        /// </summary>
        private const string Pattern = @"^\s*(?<lvl>>+)(?:\s+)?(?<content>.*)$";
        /// <summary>
        /// Регулярное выражение, отвечающего за блок цитат.
        /// </summary>
        private static readonly Regex Guard = new Regex(Pattern, RegexOptions.Compiled);

        /// <summary>
        /// Уровень вложенности.
        /// </summary>
        private int _level = 0;
        
        #endregion

        #region Overrides

        public override void Enter()
        {
            Formatter.Output.Write("<blockquote><p>");
        }

        public override string Consume(FormatterStateConsumeContext context)
        {
            _level = context.Match.Groups["lvl"].Value.Length;

            var oldState = this.CurrentState as BlockQuoteRedmineFormatterState;
            var oldLevel = oldState?._level ?? 0;

            if (oldLevel < this._level)
            {
                for (var i = oldLevel + 1; i < this._level; ++i)
                {
                    var newState = new BlockQuoteRedmineFormatterState
                    {
                        _level = i
                    };
                    this.ChangeState(newState);
                }
                this.ChangeState(this);
            }
            
            return context.Match.Groups["content"].Value;
        }

        public override void Exit()
        {
            Formatter.Output.WriteLine("</p></blockquote>");
        }

        public override void FormatLine(string input)
        {
            Formatter.Output.Write(input);
        }
        
        public override bool ShouldNestState(FormatterState other)
        {
            if (other is not BlockQuoteRedmineFormatterState otherState)
            {
                return true;
            }
            
            return this._level < otherState._level;
        }

        public override bool ShouldExit(string input, string inputLookAhead)
        {
            // проверяем, что строка парсится регуляркой.
            var match = Guard.Match(input);
            if (!match.Success)
            {
                return true;
            }

            // если в строке уровень вложенности меньше чем текущий - нужно удалить уровень вложенности.
            if (match.Groups["lvl"].Value.Length < this._level)
            {
                return true;
            }

            Formatter.Output.WriteLine("<br />");
            return false;
        }

        public override Type FallbackFormattingState
        {
            get { return null; }
        }

        #endregion
    }
}