// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HostsParser
{
    internal static class CollectionUtilities
    {
        internal static List<string> SortDnsList(ICollection<string> dnsList)
        {
            List<string> list = new(dnsList.Count);
            list.AddRange((dnsList
                .Select(d => new StringSortItem(d))
                .OrderBy(l => GetTopMostDns(l.RawMemory), ReadOnlyMemoryCharComparer.Default)
                .ThenBy(l => l.RawMemory.Length)
                .Select(l => l.Raw)));
            
            return list;
        }

        internal static void FilterGrouped(HashSet<string> dnsList)
        {
            var hashSet = new HashSet<int>(dnsList.Count);
            foreach (var s in dnsList)
            {
                hashSet.Add(s.GetHashCode());
            }
            
            var dnsGroups = GroupDnsList(dnsList);
            HashSet<string> filtered = new HashSet<string>(dnsList.Count);
            foreach (var (key, value) in dnsGroups)
            {
                if (!hashSet.Contains(key)
                    || value.Count < 2)
                    continue;

                for (var index = 0; index < value.Count; index++)
                {
                    if (key == value[index].GetHashCode())
                        continue;

                    filtered.Add(value[index]);
                }
            }

            dnsList.ExceptWith(filtered);
        }

        internal static Dictionary<int, List<string>> GroupDnsList(HashSet<string> dnsList)
        {
            var dict = new Dictionary<int, List<string>>(dnsList.Count);
            foreach (var s in dnsList)
            {
                var key = string.GetHashCode(GetTopMostDns(s));
                List<string> values;
                if (!dict.ContainsKey(key))
                {
                    values = new List<string>();
                    dict.Add(key, values);
                }
                else
                {
                    values = dict[key];
                }

                values.Add(s);
            }

            return dict;
        }

        private static ReadOnlySpan<char> GetTopMostDns(in ReadOnlySpan<char> item)
        {
            var indexes = GetIndexes(item);
            return indexes.Count <= 1 ? item : ProcessItem(indexes, item);
        }

        private static ReadOnlyMemory<char> GetTopMostDns(in ReadOnlyMemory<char> item)
        {
            var indexes = GetIndexes(item.Span);
            return indexes.Count <= 1 ? item : ProcessItem(indexes, item);
        }

        private static List<int> GetIndexes(in ReadOnlySpan<char> item)
        {
            var foundIndexes = new List<int>();
            for (var i = item.IndexOf(Constants.DotSign); i > -1; i = item.IndexOf(Constants.DotSign, i + 1))
                foundIndexes.Add(i);

            return foundIndexes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSecondLevelTopDomain(in ReadOnlySpan<char> secondTop)
        {
            return secondTop.Equals(Constants.TopDomains.Co, StringComparison.Ordinal)
                   || secondTop.Equals(Constants.TopDomains.Com, StringComparison.Ordinal)
                   || secondTop.Equals(Constants.TopDomains.Org, StringComparison.Ordinal)
                   || secondTop.Equals(Constants.TopDomains.Ne, StringComparison.Ordinal)
                   || secondTop.Equals(Constants.TopDomains.Edu, StringComparison.Ordinal)
                   || secondTop.Equals(Constants.TopDomains.Or, StringComparison.Ordinal);
        }

        private static ReadOnlySpan<char> ProcessItem(List<int> indexes,
            in ReadOnlySpan<char> item)
        {
            if (indexes.Count != 2)
            {
                var secondTop = item[(indexes[^2] + 1)..indexes[^1]];
                var dns = IsSecondLevelTopDomain(secondTop)
                    ? item[(indexes[^3] + 1)..]
                    : item[(indexes[^2] + 1)..];

                return dns.Length > 3 ? dns : item[(indexes[^3] + 1)..];
            }

            var slicedItem = item[(indexes[0] + 1)..indexes[1]];
            return slicedItem.Length <= 3 ? item : item[(indexes[0] + 1)..];
        }

        private static ReadOnlyMemory<char> ProcessItem(List<int> indexes,
            in ReadOnlyMemory<char> item)
        {
            if (indexes.Count != 2)
            {
                var secondTop = item[(indexes[^2] + 1)..indexes[^1]];
                var dns = IsSecondLevelTopDomain(secondTop.Span)
                    ? item[(indexes[^3] + 1)..]
                    : item[(indexes[^2] + 1)..];

                return dns.Length > 3 ? dns : item[(indexes[^3] + 1)..];
            }
            
            var slicedItem = item[(indexes[0] + 1)..indexes[1]];
            return slicedItem.Length <= 3 ? item : item[(indexes[0] + 1)..];
        }

        private static int IndexOf(in this ReadOnlySpan<char> aSpan,
            in char aChar,
            in int startIndex)
        {
            var indexInSlice = aSpan[startIndex..].IndexOf(aChar);

            if (indexInSlice == -1)
                return -1;

            return startIndex + indexInSlice;
        }

        private readonly struct StringSortItem
        {
            public readonly string Raw;
            public ReadOnlyMemory<char> RawMemory => Raw.AsMemory();

            public StringSortItem(string raw) => Raw = raw;
        }
    }
}
