using System.Text.RegularExpressions;

namespace TextileToTessaHtml.Blocks
{
    public class ItalicPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(GetPhraseModifierPattern(@"__"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "i");
        }
    }
}