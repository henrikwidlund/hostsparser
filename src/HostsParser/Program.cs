// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

[assembly:InternalsVisibleTo("HostsParser.Benchmarks")]

namespace HostsParser
{
    internal static class Program
    {
        internal static async Task Main()
        {
            using var loggerFactory = LoggerFactory.Create(options =>
            {
                options.AddDebug();
                options.AddSimpleConsole(consoleOptions =>
                {
                    consoleOptions.SingleLine = true;
                });
            });
            var logger = loggerFactory.CreateLogger("HostsParser");

            logger.LogInformation(WithTimeStamp("Running..."));
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            var settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllBytesAsync("appsettings.json"));
            if (settings == null)
            {
                logger.LogError("Couldn't load settings. Terminating...");
                return;
            }

            const string connectionString =
                "Persist Security Info=False;User ID=*****;Password=*****;Initial Catalog=AdventureWorks;Server=MySqlServer";
            await using var connection = new SqlConnection(connectionString);
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(settings.HostsBased.SkipLinesBytes![0]);
            logger.LogInformation(string.Concat(hash.Select(b => b.ToString("x2"))));

            var decoder = Encoding.UTF8.GetDecoder();
            using var httpClient = new HttpClient();

            var stream = await httpClient.GetStreamAsync(settings.HostsBased.SourceUri);
            var hostsBasedLines = await HostUtilities.ProcessHostsBased(stream, settings.HostsBased.SkipLinesBytes, decoder);
            await stream.DisposeAsync();

            stream = await httpClient.GetStreamAsync(settings.AdBlockBased.SourceUri);
            var adBlockBasedLines = await HostUtilities.ProcessAdBlockBased(stream, decoder);
            await stream.DisposeAsync();

            var combined = hostsBasedLines;
            combined.ExceptWith(adBlockBasedLines);
            combined = HostUtilities.RemoveKnownBadHosts(settings.KnownBadHosts, combined);
            combined.UnionWith(settings.KnownBadHosts);
            combined.UnionWith(adBlockBasedLines);
            CollectionUtilities.FilterGrouped(combined);

            var sortedDnsList = CollectionUtilities.SortDnsList(combined);
            HashSet<string> filteredCache = new(combined.Count);
            sortedDnsList = ProcessCombined(sortedDnsList, adBlockBasedLines, filteredCache);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation(WithTimeStamp("Start extra filtering of duplicates"));
                sortedDnsList = ProcessWithExtraFiltering(adBlockBasedLines, sortedDnsList, filteredCache);
                logger.LogInformation(WithTimeStamp("Done extra filtering of duplicates"));
            }

            await using StreamWriter streamWriter = new("filter.txt", false);
            for (var i = 0; i < settings.HeaderLines.Length; i++)
                await streamWriter.WriteLineAsync(settings.HeaderLines[i]);

            await streamWriter.WriteLineAsync($"! Last Modified: {DateTime.UtcNow:u}");

            foreach (var s in sortedDnsList)
            {
                await streamWriter.WriteLineAsync();
                await streamWriter.WriteAsync((char)Constants.PipeSign);
                await streamWriter.WriteAsync((char)Constants.PipeSign);
                await streamWriter.WriteAsync(s);
                await streamWriter.WriteAsync((char)Constants.HatSign);
            }

            stopWatch.Stop();
            logger.LogInformation(WithTimeStamp($"Execution duration - {stopWatch.Elapsed} | Produced {sortedDnsList.Count} hosts"));

            static string WithTimeStamp(string message) => $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        }
        
        private static List<string> ProcessCombined(
            List<string> sortedDnsList,
            HashSet<string> adBlockBasedLines,
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
                    for (var j = (i < lookBack ? 0 : i - lookBack); j < i; j++)
                    {
                        var item = sortedDnsList[i];
                        var otherItem = sortedDnsList[j];
                        AddIfSubDomain(filteredCache, item, otherItem);
                    }
                });

                // We only need to check for domains/sub domains covered by AdBlock based file
                // on first run, after that sub domains covered by AdBlock based file will be gone
                // and we don't want to process unnecessary entries or produce a file containing
                // lines contained in the AdBlock based file 
                if (round == 1)
                    sortedDnsList.RemoveAll(adBlockBasedLines.Contains);

                sortedDnsList.RemoveAll(filteredCache.Contains);
                sortedDnsList = CollectionUtilities.SortDnsList(sortedDnsList);
            } while (filteredCache.Count > 0);

            return sortedDnsList;
        }

        /// <summary>
        /// Removes sub domains covered by a main domain in <paramref name="sortedDnsList"/> by looping over
        /// all items in <paramref name="sortedDnsList"/> and check if any other item in
        /// <paramref name="sortedDnsList"/> is a sub domain of it.
        /// </summary>
        private static List<string> ProcessWithExtraFiltering(HashSet<string> adBlockBasedLines,
            List<string> sortedDnsList,
            HashSet<string> filteredCache)
        {
            Parallel.ForEach(CollectionUtilities.SortDnsList(adBlockBasedLines), item =>
            {
                for (var i = 0; i < sortedDnsList.Count; i++)
                {
                    var localItem = sortedDnsList[i];
                    if (HostUtilities.IsSubDomainOf(localItem, item))
                        filteredCache.Add(localItem);
                }
            });
            sortedDnsList.RemoveAll(filteredCache.Contains);
            sortedDnsList = CollectionUtilities.SortDnsList(sortedDnsList);
            return sortedDnsList;
        }

        private static void AddIfSubDomain(HashSet<string> filteredCache,
            string item,
            string otherItem)
        {
            if (ShouldSkip(otherItem, item)) return;
            if (HostUtilities.IsSubDomainOf(item, otherItem))
                filteredCache.Add(item);
        }

        private static bool ShouldSkip(string otherItem,
            string item)
        {
            return otherItem.Length + 1 > item.Length
                   || item == otherItem;
        }
    }
}
