using System.Collections.Generic;
using System.Linq;
//using Tessa.Forums.Models;

namespace Tessa.Extensions.Console.Helpers.TextConversion.Models
{
    /// <summary>
    /// Модель описания.
    /// </summary>
    public class JsonDescription
    {
        /// <summary>
        /// Приложенные к описанию файлы.
        /// </summary>
        //public List<ItemModel> Attachments { get; init; } = new();
        /// <summary>
        /// Текст описания.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Возвращает хранилище <c>Dictionary&lt;string, object&gt;</c>, декоратором для которого является текущий объект.
        /// </summary>
        /// <returns>Хранилище <c>Dictionary&lt;string, object&gt;</c>, декоратором для которого является текущий объект.</returns>
        // public IDictionary<string, object> GetStorage() =>
        //     new Dictionary<string, object>
        //     {
        //         { nameof(this.Attachments), this.Attachments.Select(x => (object)x.GetStorage()
        //             .Where(x => x.Value is not null)
        //             .ToDictionary(x => x.Key, x => x.Value)).ToList() },
        //         { nameof(this.Text), this.Text }
        //     };
    }
}