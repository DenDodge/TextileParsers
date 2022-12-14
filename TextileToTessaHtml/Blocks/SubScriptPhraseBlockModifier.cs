using System.Text.RegularExpressions;

namespace TextileToTessaHtml.Blocks
{
    public class SubScriptPhraseBlockModifier : PhraseBlockModifier
    {
        private static readonly Regex BlockRegex = new Regex(GetPhraseModifierPattern(@"~"), TextileGlobals.BlockModifierRegexOptions);

        public override string ModifyLine(string line)
        {
            return PhraseModifierFormat(line, BlockRegex, "sub");
        }
    }
}