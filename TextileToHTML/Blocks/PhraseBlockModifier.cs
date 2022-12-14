using System.Text.RegularExpressions;

namespace TextileToHTML.Blocks
{
    public abstract class PhraseBlockModifier : BlockModifier
    {
        public static string GetPhraseModifierPattern(string modifier)
        {
            // All phrase modifiers are one character, or a double character. Sometimes,
            // there's an additional escape character for the regex ('\').
            string compressedModifier = modifier;
            if (modifier.Length == 4)
            {
                compressedModifier = modifier.Substring(0, 2);
            }
            else if (modifier.Length == 2)
            {
                if (modifier[0] != '\\')
                    compressedModifier = modifier.Substring(0, 1);
                //else: compressedModifier = modifier;
            }
            //else: compressedModifier = modifier;

            // We try to remove the Textile tag used for the formatting from
            // the punctuation pattern, so that we match the end of the formatted
            // zone correctly.
            string punctuationPattern = TextileGlobals.PunctuationPattern.Replace(compressedModifier, "");

            string pattern = @"(?<=\s|" + punctuationPattern + @"|[{\(\[]|^)" +
                                modifier +
                                TextileGlobals.BlockModifiersPattern +
                                @"(:(?<cite>(\S+)))?" +
                                @"(?<content>[^" + compressedModifier + "]*)" +
                                @"(?<end>" + punctuationPattern + @"*)" +
                                modifier +
                                @"(?=[\]\)}]|" + punctuationPattern + @"+|\s|$)";
            return pattern;
        }

        protected PhraseBlockModifier()
        {
        }

        protected string PhraseModifierFormat(string input, Regex regex, string tag)
        {
            PhraseModifierMatchEvaluator pmme = new PhraseModifierMatchEvaluator(tag, UseRestrictedMode);
            string res = regex.Replace(input, new MatchEvaluator(pmme.MatchEvaluator));
            return res;
        }

        private class PhraseModifierMatchEvaluator
        {
            string m_tag;
            bool m_restrictedMode;

            public PhraseModifierMatchEvaluator(string tag, bool restrictedMode)
            {
                m_tag = tag;
                m_restrictedMode = restrictedMode;
            }

            public string MatchEvaluator(Match m)
            {
                if (m.Groups["content"].Length == 0)
                {
                    // It's possible that the "atts" match groups eats the contents
                    // when the user didn't want to give block attributes, but the content
                    // happens to match the syntax. For example: "*(blah)*".
                    if (m.Groups["atts"].Length == 0)
                        return m.ToString();
                    return "<" + m_tag + ">" + m.Groups["atts"].Value + m.Groups["end"].Value + "</" + m_tag + ">";
                }

                string atts = BlockAttributesParser.ParseBlockAttributes(m.Groups["atts"].Value, "", m_restrictedMode);
                if (m.Groups["cite"].Length > 0)
                    atts += " cite=\"" + m.Groups["cite"] + "\"";

                string res = "<" + m_tag + atts + ">" +
                             m.Groups["content"].Value + m.Groups["end"].Value +
                             "</" + m_tag + ">";
                return res;
            }
        }
    }
}