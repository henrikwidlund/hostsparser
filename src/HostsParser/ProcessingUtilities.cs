// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Threading.Tasks;

namespace HostsParser;

public static class ProcessingUtilities
{
    /// <summary>
    /// Attempts to remove all sub domain entries in <paramref name="sortedDnsList"/>
    /// that are otherwise covered by a main domain in the same collection.
    /// Any entries in <paramref name="externalCoverageLines"/> that also exist in <paramref name="sortedDnsList"/>
    /// will also be removed from the returned value.
    /// </summary>
    /// <param name="sortedDnsList">The collection for which sub domains will be removed from.</param>
    /// <param name="externalCoverageLines">Collection of domains considered to be covered by another source.</param>
    /// <param name="filteredCache">Cache used by the method to store items that should be removed.</param>
    public static List<string> ProcessCombined(List<string> sortedDnsList,
        HashSet<string> externalCoverageLines,
        HashSet<string> filteredCache)
    {
        filteredCache.Clear();

        Parallel.For(0, sortedDnsList.Count, i =>
        {
            var item = sortedDnsList[i];
            var lookUpCount = i + 250;
            if (lookUpCount > sortedDnsList.Count)
                lookUpCount = sortedDnsList.Count;

            for (var j = i + 1; j < lookUpCount; j++)
            {
                var sortedItem = sortedDnsList[j];
                AddIfSubDomain(filteredCache, sortedItem, item);
            }
        });

        // We only need to check for domains/sub domains covered by AdBlock based file
        // in the code above, after that sub domains covered by AdBlock based file will be gone
        // and the domains in the file can be discarded.
        sortedDnsList.RemoveAll(externalCoverageLines.Contains);

        sortedDnsList.RemoveAll(filteredCache.Contains);
        return CollectionUtilities.SortDnsList(sortedDnsList);
    }

    /// <summary>
    /// Attempts to remove all sub domain entries in <paramref name="sortedDnsList"/>
    /// that are otherwise covered by a main domain in the same collection.
    /// Any entries in <paramref name="externalCoverageLines"/> that also exist in <paramref name="sortedDnsList"/>
    /// will also be removed from the returned value.
    /// </summary>
    /// <param name="sortedDnsList">The collection for which sub domains will be removed from.</param>
    /// <param name="externalCoverageLines">Collection of domains considered to be covered by another source.</param>
    /// <param name="filteredCache">Cache used by the method to store items that should be removed.</param>
    public static List<string> ProcessCombinedWithMultipleRounds(
        List<string> sortedDnsList,
        HashSet<string> externalCoverageLines,
        HashSet<string> filteredCache)
    {
        var round = 0;
        do
        {
            filteredCache.Clear();
            // Increase the number of items processed in each run since we'll have fewer items to loop and they'll be further apart.
            var lookBack = ++round * 250;
            Parallel.For(0, sortedDnsList.Count, i =>
            {
                var item = sortedDnsList[i];
                for (var j = i < lookBack ? 0 : i - lookBack; j < i; j++)
                {
                    var otherItem = sortedDnsList[j];
                    AddIfSubDomain(filteredCache, item, otherItem);
                }
            });

            // We only need to check for domains/sub domains covered by AdBlock based file
            // on first run, after that sub domains covered by AdBlock based file will be gone
            // and we don't want to process unnecessary entries or produce a file containing
            // lines contained in the AdBlock based file
            if (round == 1)
                sortedDnsList.RemoveAll(externalCoverageLines.Contains);

            sortedDnsList.RemoveAll(filteredCache.Contains);
            sortedDnsList = CollectionUtilities.SortDnsList(sortedDnsList);
        } while (filteredCache.Count > 0);

        return CollectionUtilities.SortDnsList(sortedDnsList);
    }

    /// <summary>
    /// Removes sub domains in <paramref name="sortedDnsList"/> that are covered by a main domain
    /// in <paramref name="externalCoverageLines"/>.
    /// This is a slow process since it for each item in <paramref name="externalCoverageLines"/> has
    /// to loop over all items in <paramref name="sortedDnsList"/>.
    /// </summary>
    /// <param name="sortedDnsList">The collection for which sub domains will be removed from.</param>
    /// <param name="externalCoverageLines">Collection of domains considered to be covered by another source.</param>
    /// <param name="filteredCache">Cache used by the method to store items that should be removed.</param>
    public static List<string> ProcessWithExtraFiltering(List<string> sortedDnsList,
        HashSet<string> externalCoverageLines,
        HashSet<string> filteredCache)
    {
        Parallel.ForEach(CollectionUtilities.SortDnsList(externalCoverageLines), item =>
        {
            for (var i = 0; i < sortedDnsList.Count; i++)
            {
                var localItem = sortedDnsList[i];
                if (HostUtilities.IsSubDomainOf(localItem, item))
                    filteredCache.Add(localItem);
            }
        });
        sortedDnsList.RemoveAll(filteredCache.Contains);
        return CollectionUtilities.SortDnsList(sortedDnsList);
    }

    private static void AddIfSubDomain(ISet<string> filteredCache,
        string potentialSubDomain,
        string potentialDomain)
    {
        if (ShouldSkip(potentialDomain, potentialSubDomain)) return;
        if (HostUtilities.IsSubDomainOf(potentialSubDomain, potentialDomain))
            filteredCache.Add(potentialSubDomain);
    }

    private static bool ShouldSkip(string potentialDomain,
        string potentialSubDomain)
    {
        return potentialDomain.Length + 1 > potentialSubDomain.Length
               || potentialSubDomain == potentialDomain;
    }
}
