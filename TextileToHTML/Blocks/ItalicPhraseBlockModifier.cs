using System.Text.RegularExpressions;

namespace TextileToHTML.Blocks
{
    public class ItalicPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(PhraseBlockModifier.GetPhraseModifierPattern(@"__"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "i");
        }
    }
}