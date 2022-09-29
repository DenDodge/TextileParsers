namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var testString = "*-жирный шрифт-*";
            var textileToTessaHtml = TextileToTessaHtml.TextileFormatter.FormatString(testString);
        }
    }
}