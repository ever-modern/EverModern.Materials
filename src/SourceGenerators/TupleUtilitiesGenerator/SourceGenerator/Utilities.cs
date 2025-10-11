using System;
using System.Collections.Generic;
using System.Linq;

namespace SourceGenerator
{
    public static class Utilities
    {
        public static string Merge(this IEnumerable<string> strings, string joiner = ", ")
                    => string.Join(joiner, strings);

        internal static IEnumerable<T> DistinctBy<T, TSelector>(this IEnumerable<T> items, Func<T, TSelector> selector)
            => items.GroupBy(selector).Select(x => x.First());
    }
}

