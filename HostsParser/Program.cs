// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            HashSet<string> filtered = new(combined.Count);
            sortedDnsList = ProcessCombined(sortedDnsList, adBlockBasedLines, filtered);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation(WithTimeStamp("Start extra filtering of duplicates"));
                sortedDnsList = ProcessWithExtraFiltering(adBlockBasedLines, sortedDnsList, filtered);
                logger.LogInformation(WithTimeStamp("Done extra filtering of duplicates"));
            }

            await using StreamWriter streamWriter = new("hosts", false);
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

        private static List<string> ProcessWithExtraFiltering(HashSet<string> adBlockBasedLines,
            List<string> combined,
            HashSet<string> filtered)
        {
            Parallel.ForEach(CollectionUtilities.SortDnsList(adBlockBasedLines), item =>
            {
                for (var i = 0; i < combined.Count; i++)
                {
                    var localItem = combined[i];
                    if (HostUtilities.IsSubDomainOf(localItem, item))
                        filtered.Add(localItem);
                }
            });
            combined.RemoveAll(filtered.Contains);
            combined = CollectionUtilities.SortDnsList(combined);
            return combined;
        }

        private static List<string> ProcessCombined(
            List<string> combined,
            HashSet<string> adBlockBasedLines,
            HashSet<string> filtered)
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
                    combined.RemoveAll(adBlockBasedLines.Contains);

                combined.RemoveAll(filtered.Contains);
                combined = CollectionUtilities.SortDnsList(combined);
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
