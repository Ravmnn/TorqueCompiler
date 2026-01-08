using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public static class CollectionExtensions
{
    public static IReadOnlyList<string> ItemsToString<T>(this IEnumerable<T> collection)
        => (from item in collection select item.ToString()).ToArray();
}
