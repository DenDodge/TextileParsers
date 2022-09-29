using System.Text;

namespace TextileToHTML
{
    public class StringBuilderOutputter : IOutputter
    {
        private string m_newLine;
        private StringBuilder m_stringBuilder = null;

        public StringBuilderOutputter()
            : this("\n")
        {
        }

        public StringBuilderOutputter(string newLine)
        {
            m_newLine = newLine;
        }

        public string GetFormattedText()
        {
            return m_stringBuilder.ToString();
        }

        #region IOutputter Members

        public void Begin()
        {
            m_stringBuilder = new StringBuilder();
        }

        public void End()
        {
        }

        public void Write(string text)
        {
            m_stringBuilder.Append(text);
        }

        public void WriteLine(string line)
        {
            m_stringBuilder.Append(line);
            m_stringBuilder.Append(m_newLine);
        }

        #endregion
    }
}