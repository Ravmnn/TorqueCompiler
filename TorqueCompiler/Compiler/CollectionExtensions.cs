using System;
using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public static class CollectionExtensions
{
    extension<T>(IEnumerable<T> collection) where T : notnull
    {
        public IReadOnlyList<string> ItemsToString(Func<T, string>? processor = null)
        {
            processor ??= item => item.ToString() ?? string.Empty;

            return collection.Select(processor).ToArray();
        }


        public string ItemsToStringThenJoin(string separator, Func<T, string>? processor = null)
        {
            var itemsAsString = collection.ItemsToString(processor);
            var processedExpressionsString = string.Join(separator, itemsAsString);

            return processedExpressionsString;
        }
    }
}
