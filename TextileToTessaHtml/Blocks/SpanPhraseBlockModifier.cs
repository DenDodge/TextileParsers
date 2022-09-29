﻿using System.Text.RegularExpressions;

namespace TextileToTessaHtml.Blocks
{
    public class SpanPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(GetPhraseModifierPattern(@"%"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "span");
        }
    }
}