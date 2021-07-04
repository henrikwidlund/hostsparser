using System;
using System.Collections.Generic;
using System.Linq;

namespace HostsParser
{
    internal static class CollectionUtilities
    {
        internal static List<string> SortDnsList(IEnumerable<string> dnsList)
        {
            return dnsList.Distinct()
                .OrderBy(l =>
                {
                    var span = l.AsSpan();
                    var indexes = GetIndexes(span);
                    return indexes.Count <= 1 ? l : ProcessItem(indexes, span).ToString();
                })
                .ThenBy(l => l.Length)
                .ToList();
            
            static ReadOnlySpan<char> ProcessItem(List<int> indexes, ReadOnlySpan<char> l)
            {
                if (indexes.Count != 2) return l[(indexes[^3] + 1)..];
            
                var item = l[(indexes[0] + 1)..indexes[1]];
                return item.Length <= 3 ? l : l[(indexes[0] + 1)..];
            }
        }

        private static List<int> GetIndexes(ReadOnlySpan<char> item)
        {
            var foundIndexes = new List<int>();
            for (var i = item.IndexOf(Constants.DotSign); i > -1; i = item.IndexOf(Constants.DotSign, i +1))
                foundIndexes.Add(i);

            return foundIndexes;
        }

        internal static IEnumerable<WwwOnly> GetWwwOnly(List<string> hosts) =>
            hosts.Where(l => l.AsSpan().StartsWith(Constants.WwwPrefix))
                .Select(l => new WwwOnly(l, l[4..]));

        private static int IndexOf(this ReadOnlySpan<char> aSpan, char aChar, int startIndex)
        {
            var indexInSlice = aSpan[startIndex..].IndexOf(aChar);

            if (indexInSlice == -1)
                return -1;

            return startIndex + indexInSlice;
        }
    }
}
