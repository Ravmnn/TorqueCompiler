using System;
using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public static class CollectionExtensions
{
    public static IReadOnlyList<string> ItemsToString<T>(this IEnumerable<T> collection, Func<T, string>? processor = null)
        where T : notnull
    {
        processor ??= item => item.ToString() ?? string.Empty;

        return collection.Select(processor).ToArray();
    }


    public static string ItemsToStringThenJoin<T>(this IEnumerable<T> collection, string separator, Func<T, string>? processor = null)
        where T : notnull
    {
        var itemsAsString = collection.ItemsToString(processor);
        var processedExpressionsString = string.Join(separator, itemsAsString);

        return processedExpressionsString;
    }
}
