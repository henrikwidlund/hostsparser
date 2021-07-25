// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Linq;

namespace HostsParser
{
    internal static class CollectionUtilities
    {
        internal static List<string> SortDnsList(IEnumerable<string> dnsList, bool distinct)
        {
            return (distinct ? dnsList.Distinct() : dnsList)
                .OrderBy(l => GetTopMostDns(l).ToString())
                .ThenBy(l => l.Length)
                .ToList();
        }
        
        internal static IEnumerable<IGrouping<string, string>> GroupDnsList(IEnumerable<string> dnsList)
        {
            return dnsList
                .GroupBy(l => GetTopMostDns(l).ToString());
        }

        private static ReadOnlySpan<char> GetTopMostDns(ReadOnlySpan<char> item)
        {
            var indexes = GetIndexes(item);
            return indexes.Count <= 1 ? item : ProcessItem(indexes, item);
        }

        private static List<int> GetIndexes(ReadOnlySpan<char> item)
        {
            var foundIndexes = new List<int>();
            for (var i = item.IndexOf(Constants.DotSign); i > -1; i = item.IndexOf(Constants.DotSign, i +1))
                foundIndexes.Add(i);

            return foundIndexes;
        }
        
        private static ReadOnlySpan<char> ProcessItem(List<int> indexes, ReadOnlySpan<char> l)
        {
            if (indexes.Count != 2)
            {
                ReadOnlySpan<char> dns;
                var secondTop = l[(indexes[^2] + 1)..indexes[^1]];
                if (secondTop.Equals(Constants.TopDomains.Co, StringComparison.Ordinal)
                    || secondTop.Equals(Constants.TopDomains.Com, StringComparison.Ordinal)
                    || secondTop.Equals(Constants.TopDomains.Org, StringComparison.Ordinal))
                    dns = l[(indexes[^3] + 1)..];
                else
                    dns = l[(indexes[^2] + 1)..];
                
                return dns.Length > 3 ? dns : l[(indexes[^3] + 1)..];
            }
            
            var item = l[(indexes[0] + 1)..indexes[1]];
            return item.Length <= 3 ? l : l[(indexes[0] + 1)..];
        }

        internal static (List<string> withPrefix, List<string> withoutPrefix) GetWwwOnly(IEnumerable<string> hosts)
        {
            var withPrefix = new List<string>();
            var withoutPrefix = new List<string>();
            foreach (var host in hosts)
            {
                if (!host.AsSpan().StartsWith(Constants.WwwPrefix))
                    continue;
                
                withPrefix.Add(host);
                withoutPrefix.Add(host[4..]);
            }

            return (withPrefix, withoutPrefix);
        }

        private static int IndexOf(this ReadOnlySpan<char> aSpan, char aChar, int startIndex)
        {
            var indexInSlice = aSpan[startIndex..].IndexOf(aChar);

            if (indexInSlice == -1)
                return -1;

            return startIndex + indexInSlice;
        }
    }
}
