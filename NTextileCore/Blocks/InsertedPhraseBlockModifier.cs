﻿using System.Text.RegularExpressions;

namespace NTextileCore.Blocks
{
    public class InsertedPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(GetPhraseModifierPattern(@"\+"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "ins");
        }
    }
}