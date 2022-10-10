using System;
using System.Collections.Generic;
using System.Linq;
using ColorCode;
using Ganss.XSS;
using HtmlAgilityPack;
using HtmlFormatter = ColorCode.HtmlFormatter;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            //var testString = ">>sample\r\n>>another\r\n>new\r\n>>new new\r\n>new new new";
            
            //var testString = "# Item 1\r\n# Item 2\r\n## Item 21\r\n## Item 22\r\n# Item 3";
            
            //var testString = "Коллеги, здравствуйте!\r\n\r\nПодскажите, пожалуйста, с какой целью было сделано следующее:\r\n# Возьмем любое строковое поле в карточке\r\n# Запишем в него значение и сохраним карточку\r\n# Сотрем это значение в этом поле и сохраним карточку\r\n# *В строковое поле запишется пустая строка \"\" (раньше в v.2.7 записывалось @NULL@)*\r\n\r\nПри таком решении возможны последствия.\r\nВыполним вышеперечисленные пункты, например, для поля \"Исходящий номер\" в карточке \"Входящий\".\r\nПроизведем поиск по данному полю с параметром фильтрации \"пусто\" в представлении \"Входящие\".\r\nКарточка не отобразится в представлении, поскольку в поле \"Исходящий номер\" записана пустая строка, а не NULL.\r\n\r\nДанный подход \"ломает\" старое решение, в котором проверялось только на NULL (теперь нужно проверять и на пустую строку).\r\n\r\nКонечно, возможно сделать небольшой костыль - добавить расширения очистки связанных полей для строковых полей в карточке, где\r\nи устанавливать значение отслеживаемой колонки равное значению очищаемой колонки, но как-то так себе.";
            
            //var testString = "Спасибо, про разрешения поняла.\r\n\r\nПроцесс упал с такой ошибкой:\r\n!bp_region.JPG!\r\n\r\n\"Wikipedia\":https://en.wikipedia.org/";
            
            //var testString = "p>. Привет";
            
            var testString = "| A | simple | table | row |\r\n| And | another | table | row |\r\n| With an | | empty | cell |";

            //var testString = "<pre> Вот тут текст после пре. \r\n <code class=\"java\">\r\nforeach (CardFile file in targetCard.Files)\r\n{\r\n    Guid originalFileID = file.ExternalSource.FileID;\r\n    CardFile originalFile = card.Files.First(x => x.RowID == originalFileID);\r\n\r\n    if (originalFile.Card.Sections.TryGetValue(CardSignatureHelper.SectionName, out CardSection originalSignatures))\r\n    {\r\n        var signatures = file.Card.Sections.GetOrAddTable(CardSignatureHelper.SectionName).Rows;\r\n        foreach (CardRow originalSignature in originalSignatures.Rows)\r\n        {\r\n            CardRow signature = signatures.Add(originalSignature);\r\n            signature.RowID = Guid.NewGuid();\r\n            signature.State = CardRowState.Inserted;\r\n        }\r\n    }\r\n}\r\n</code> Вот тут тест после первого кода. \r\n <code class=\"java\">\r\nforeach (CardFile file in targetCard.Files)\r\n{\r\n    Guid originalFileID = file.ExternalSource.FileID;\r\n    CardFile originalFile = card.Files.First(x => x.RowID == originalFileID);\r\n\r\n    if (originalFile.Card.Sections.TryGetValue(CardSignatureHelper.SectionName, out CardSection originalSignatures))\r\n    {\r\n        var signatures = file.Card.Sections.GetOrAddTable(CardSignatureHelper.SectionName).Rows;\r\n        foreach (CardRow originalSignature in originalSignatures.Rows)\r\n        {\r\n            CardRow signature = signatures.Add(originalSignature);\r\n            signature.RowID = Guid.NewGuid();\r\n            signature.State = CardRowState.Inserted;\r\n        }\r\n    }\r\n}\r\n</code> Вот тут текст после второго кода.\r\n </pre> \r\n Вот тут текст после пре.";
            
            //var testString = "есть замечательный метод CardHelper.CopyFiles: https://mytessa.ru/docs/api/html/M_Tessa_Cards_CardHelper_CopyFiles.htm\r\n\r\nиспользовать его очень просто:\r\n\r\nПосле этого в targetCard будут файлы, причём контент будет копироваться по факту сохранения (а не сразу во временную папку).\r\n\r\nА вот подписи проще перенести не супер-красивым способом выше, а напрямую копируя секции\r\n\r\n<pre><code class=\"java\">\r\nforeach (CardFile file in targetCard.Files)\r\n{\r\n    Guid originalFileID = file.ExternalSource.FileID;\r\n    CardFile originalFile = card.Files.First(x => x.RowID == originalFileID);\r\n\r\n    if (originalFile.Card.Sections.TryGetValue(CardSignatureHelper.SectionName, out CardSection originalSignatures))\r\n    {\r\n        var signatures = file.Card.Sections.GetOrAddTable(CardSignatureHelper.SectionName).Rows;\r\n        foreach (CardRow originalSignature in originalSignatures.Rows)\r\n        {\r\n            CardRow signature = signatures.Add(originalSignature);\r\n            signature.RowID = Guid.NewGuid();\r\n            signature.State = CardRowState.Inserted;\r\n        }\r\n    }\r\n}\r\n</code></pre>\r\n\r\nИ теперь сохраняем карточку targetCard, для этого обязательно понадобится fileContainer:\r\n\r\n<pre><code class=\"java\">\r\nusing (var container = this.fileManager.CreateContainer(targetCard))\r\n{\r\n    var storeResponse = container.Store((c, storeRequest) =>\r\n    {\r\n        storeRequest.DoesNotAffectVersion = true;\r\n        token.Set(storeRequest.Card.Info);\r\n    });\r\n\r\n    logger.LogResult(storeResponse.ValidationResult.Build());\r\n\r\n    if (!storeResponse.ValidationResult.IsSuccessful())\r\n    { return; }\r\n}\r\n</code></pre>";
            
            //var testString = "Добрый день!\r\n\r\nУ одного пользователя в Tessa Applications не работает сканирование в РМ Исполнение Запросов Sonic\r\nИз-за чего может появляться данная ошибка?\r\n    !61.jpg!\r\n\r\n\r\n\r\n Коллеги, добрый день!\r\n31.12.2016 у нас заканчивается срок действия лицензии.\r\n   Огромная просьба продлить!\r\n   !http://dl3.joxi.net/drive/2016/12/26/0020/1024/1319936/36/2f0fb00476.jpg(Image title)!";

            var textAttacments = new Dictionary<Guid, (string Name, string Path)>
            {
                { Guid.NewGuid(), ("bp_region.JPG", "D:\\WORK_SYNTELLECT\\OtherFiles\\Migration\\83\\61.jpg") },
            };

            var isTopicText = true;

            // var formatter = new HtmlFormatter();
            // testString = formatter.GetHtmlString(testString, Languages.CSharp);
            
            var parseResult = new TextileToTessaHtml().Parse(testString, textAttacments, isTopicText);
            // реплейсим для тестирования.
            var parseString = parseResult.ResultText
                // реплейсим для тестирования.
                .Replace("\"", "\\\"")
                .Replace("\n", "")
                .Replace("\r", "");

            
            // var formatter = new HtmlFormatter();
            // var html = formatter.GetHtmlString(testString, Languages.CSharp);
            // var sanitizer = new HtmlSanitizer();
            // sanitizer.AllowedAttributes.Add("class");
            
            // var sanitizer = new HtmlSanitizer(
            //     new[]
            //     {
            //         "p", "span", "a", "ol", "ul", "li", "div", "img", "table", "td", "th", "tr", "tbody", "thead", "br"
            //     },
            //     new[] { "http", "https", "mailto", "tessa" },
            //     new[] { "href", "data-custom-href", "style", "data-custom-style", "name", "src", "class" },
            //     new[] { "href", "data-custom-href" });
            //
            //
            // parseString = sanitizer.Sanitize(parseString);
        }
    }
}