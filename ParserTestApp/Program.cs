using System;
using System.Linq;
using HtmlAgilityPack;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var testString = "<pre><code class=\"ruby\">\r\nPlace your code here.\r\n</code></pre>\r\n";
            var isTopicText = true;

            var parseResult = TextileToTessaHtml.Parse(testString, isTopicText);
            var parseString = parseResult.ResultString;
        }
    }
}