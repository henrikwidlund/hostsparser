// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
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

            var decoder = Encoding.UTF8.GetDecoder();
            using var httpClient = new HttpClient();

            var bytes = await httpClient.GetByteArrayAsync(settings.SourceUri);
            var sourceLines = HostUtilities.ProcessSource(bytes, settings.SkipLinesBytes, decoder);

            bytes = await httpClient.GetByteArrayAsync(settings.AdGuardUri);
            var adGuardLines = HostUtilities.ProcessAdGuard(bytes, decoder);

            var combined = sourceLines;
            combined.RemoveAll(s => adGuardLines.Contains(s));
            combined = HostUtilities.RemoveKnownBadHosts(settings.KnownBadHosts, combined);
            combined = CollectionUtilities.SortDnsList(combined.Concat(settings.KnownBadHosts)
                .Concat(adGuardLines), true);

            var filtered = new HashSet<string>(combined.Count);
            CollectionUtilities.FilterGrouped(combined, ref filtered);
            combined.RemoveAll(s => filtered.Contains(s));
            combined = CollectionUtilities.SortDnsList(combined, false);
            combined = ProcessCombined(filtered, combined, adGuardLines);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation(WithTimeStamp("Start extra filtering of duplicates"));
                combined = ProcessWithExtraFiltering(adGuardLines, combined, filtered);
                logger.LogInformation(WithTimeStamp("Done extra filtering of duplicates"));
            }

            var newLinesList = combined
                .Select(l => $"||{l}^");

            var newLines = new HashSet<string>(settings.HeaderLines) { $"! Last Modified: {DateTime.UtcNow:u}", string.Empty };
            foreach (var item in newLinesList)
                newLines.Add(item);

            await File.WriteAllLinesAsync("hosts", newLines);

            stopWatch.Stop();
            logger.LogInformation(WithTimeStamp($"Execution duration - {stopWatch.Elapsed} | Produced {ProducedCount()} hosts"));

            int? ProducedCount() => newLines.Count - settings.HeaderLines.Length - 2;
            static string WithTimeStamp(string message) => $"{DateTime.Now:yyyy-MM-dd hh:mm:ss} - {message}";
        }

        private static List<string> ProcessWithExtraFiltering(HashSet<string> adGuardLines,
            List<string> combined,
            HashSet<string> filtered)
        {
            Parallel.ForEach(CollectionUtilities.SortDnsList(adGuardLines, true), item =>
            {
                for (var i = 0; i < combined.Count; i++)
                {
                    var localItem = combined[i];
                    if (HostUtilities.IsSubDomainOf(localItem, item))
                        filtered.Add(localItem);
                }
            });
            combined = CollectionUtilities.SortDnsList(combined.Except(filtered), false);
            return combined;
        }

        private static List<string> ProcessCombined(HashSet<string> filtered,
            List<string> combined,
            HashSet<string> adGuardLines)
        {
            var round = 0;
            do
            {
                filtered.Clear();
                var lookBack = ++round * 250;
                Parallel.For(0, combined.Count, i =>
                {
                    for (var j = (i < lookBack ? 0 : i - lookBack); j < i; j++)
                    {
                        var item = combined[i];
                        var otherItem = combined[j];
                        AddIfSubDomain(filtered, item, otherItem);
                    }
                });

                if (round == 1)
                    combined.RemoveAll(adGuardLines.Contains);

                combined.RemoveAll(filtered.Contains);
                combined = CollectionUtilities.SortDnsList(combined, false);
            } while (filtered.Count > 0);

            return combined;
        }

        private static void AddIfSubDomain(HashSet<string> filtered,
            string item,
            string otherItem)
        {
            if (ShouldSkip(otherItem, item)) return;
            if (HostUtilities.IsSubDomainOf(item, otherItem))
                filtered.Add(item);
        }

        private static bool ShouldSkip(string otherItem,
            string item)
        {
            return otherItem.Length + 1 > item.Length
                   || item == otherItem;
        }
    }
}
