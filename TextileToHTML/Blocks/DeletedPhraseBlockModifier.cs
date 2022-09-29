using System.Text.RegularExpressions;

namespace TextileToHTML.Blocks
{
    public class DeletedPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(PhraseBlockModifier.GetPhraseModifierPattern(@"\-"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "del");
        }
    }
}