using System;
using System.Linq;
using HtmlAgilityPack;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var testString = ">>sample\r\n>>another\r\n>new\r\n>>new new\r\n>new new new";
            //var testString = "# Item 1\r\n# Item 2\r\n## Item 21\r\n## Item 22\r\n# Item 3";
            //var testString = "Спасибо, про разрешения поняла.\r\n\r\nПроцесс упал с такой ошибкой:\r\n!bp_region.JPG!\r\n\r\n\"Wikipedia\":https://en.wikipedia.org/";
            //var testString = "p)))>. Привет";
            var testString = "есть замечательный метод CardHelper.CopyFiles: https://mytessa.ru/docs/api/html/M_Tessa_Cards_CardHelper_CopyFiles.htm\r\n\r\nиспользовать его очень просто:\r\n\r\n<pre><code class=\"java\">\r\n// копируем файлы из card в targetCard\r\nValidationResult result = CardHelper.CopyFiles(card, targetCard, this.unityContainer);\r\nlogger.LogResult(result);\r\n\r\nif (!result.IsSuccessful) { return; }\r\n</code></pre>\r\n\r\nПосле этого в targetCard будут файлы, причём контент будет копироваться по факту сохранения (а не сразу во временную папку).\r\n\r\nА вот подписи проще перенести не супер-красивым способом выше, а напрямую копируя секции\r\n\r\n<pre><code class=\"java\">\r\nforeach (CardFile file in targetCard.Files)\r\n{\r\n    Guid originalFileID = file.ExternalSource.FileID;\r\n    CardFile originalFile = card.Files.First(x => x.RowID == originalFileID);\r\n\r\n    if (originalFile.Card.Sections.TryGetValue(CardSignatureHelper.SectionName, out CardSection originalSignatures))\r\n    {\r\n        var signatures = file.Card.Sections.GetOrAddTable(CardSignatureHelper.SectionName).Rows;\r\n        foreach (CardRow originalSignature in originalSignatures.Rows)\r\n        {\r\n            CardRow signature = signatures.Add(originalSignature);\r\n            signature.RowID = Guid.NewGuid();\r\n            signature.State = CardRowState.Inserted;\r\n        }\r\n    }\r\n}\r\n</code></pre>\r\n\r\nИ теперь сохраняем карточку targetCard, для этого обязательно понадобится fileContainer:\r\n\r\n<pre><code class=\"java\">\r\nusing (var container = this.fileManager.CreateContainer(targetCard))\r\n{\r\n    var storeResponse = container.Store((c, storeRequest) =>\r\n    {\r\n        storeRequest.DoesNotAffectVersion = true;\r\n        token.Set(storeRequest.Card.Info);\r\n    });\r\n\r\n    logger.LogResult(storeResponse.ValidationResult.Build());\r\n\r\n    if (!storeResponse.ValidationResult.IsSuccessful())\r\n    { return; }\r\n}\r\n</code></pre>";
                //var testString = "<pre>*Your text won't become bold*</pre>";
            var isTopicText = true;

            var parseResult = TextileToTessaHtml.Parse(testString, isTopicText);
            var parseString = parseResult.ResultString;
        }
    }
}