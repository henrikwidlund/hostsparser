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
                    var indexes = GetIndexes(l);
                    if (indexes.Count <= 1)
                        return l;

                    if (indexes.Count == 2)
                    {
                        var item = l[(indexes[0] + 1)..indexes[1]];
                        return item.Length <= 3 ? l : l[(indexes[0] + 1)..];
                    }

                    return l[(indexes[^3] + 1)..];
                })
                .ThenBy(l => l.Length)
                .ToList();
        }

        private static List<int> GetIndexes(string item)
        {
            var foundIndexes = new List<int>();
            for (var i = item.IndexOf(Constants.DotSign); i > -1; i = item.IndexOf(Constants.DotSign, i + 1))
                foundIndexes.Add(i);

            return foundIndexes;
        }

        internal static IEnumerable<WwwOnly> GetWwwOnly(List<string> hosts) =>
            hosts.Where(l => l.StartsWith(Constants.WwwPrefix))
                .Select(l => new WwwOnly(l, l[4..]));
    }
}
