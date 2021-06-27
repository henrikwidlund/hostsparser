using System.Collections.Generic;
using System.Linq;

namespace HostsParser
{
    internal class CollectionUtilities
    {
        internal static List<string> SortDnsList(IEnumerable<string> dnsList)
        {
            return dnsList
                .Distinct()
                .OrderBy(l =>
                {
                    var indexes = GetIndexes(l);
                    if (indexes.Count <= 1)
                        return l;

                    if (indexes.Count == 2)
                    {
                        var item = l[(indexes[0] + 1)..indexes[1]];
                        if (item.Length == 3)
                            return l;
                        return l[(indexes[0] + 1)..];
                    }
                    else
                        return l[(indexes[^3] + 1)..];
                })
                .ThenBy(l => l.Length)
                .ToList();

            static List<int> GetIndexes(string item)
            {
                var foundIndexes = new List<int>();
                for (var i = item.IndexOf(Constants.DotSign); i > -1; i = item.IndexOf(Constants.DotSign, i + 1))
                    foundIndexes.Add(i);

                return foundIndexes;
            }
        }

        internal static IEnumerable<WwwOnly> GetWwwOnly(List<string> hosts) =>
            hosts.Where(l => l.StartsWith(Constants.WwwPrefix))
                .Select(l => new WwwOnly(l, l[4..]));
    }
}
