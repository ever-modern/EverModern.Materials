using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;

namespace DestallMaterials.WheelProtection.Copying
{
    public interface ICopied<TSelf>
        where TSelf : ICopied<TSelf>
    {
        TSelf Copy();
    }

    public static class CopiedExtensions
    {
        public static T CopyWith<T>(this T item, Action<T> with)
            where T : ICopied<T>
        {
            var result = item.Copy();
            with(result);
            return result;
        }

        public static IEnumerable<T> CopyWith<T>(this IEnumerable<T> items, Action<T> with)
            where T : ICopied<T> => items.Select(i => i.CopyWith(with));
    }
}
