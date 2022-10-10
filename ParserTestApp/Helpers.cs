using System.Collections.Generic;

namespace TestApp
{
    public static class Helpers
    {
        /// <summary>
        /// Добавляет значения <paramref name="items" /> в коллекцию <paramref name="collection" />.
        /// </summary>
        /// <typeparam name="T">Тип элементов коллекции.</typeparam>
        /// <param name="collection">Коллекция, в которую должны быть добавлены значения.</param>
        /// <param name="items">Значения, добавляемые в коллекцию.</param>
        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
        {
            foreach (T obj in items)
                collection.Add(obj);
        }
    }
}