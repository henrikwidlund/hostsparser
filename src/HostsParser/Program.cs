// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HostsParser
{
    public static class Program
    {
        public static async Task Main()
        {
            using var loggerFactory = LoggerFactory.Create(options =>
            {
                options.AddDebug();
                options.AddSimpleConsole(consoleOptions =>
                {
                    consoleOptions.SingleLine = true;
                    consoleOptions.TimestampFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
                                                     CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern + " ";
                });
            });
            var logger = loggerFactory.CreateLogger("HostsParser");

            logger.LogInformation("Running...");
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

            // Assumed length to reduce allocations
            var combineLines = new HashSet<string>(170_000);
            var externalCoverageLines = new HashSet<string>(50_000);
            for (var i = 0; i < settings.Filters.Sources.Length; i++)
            {
                var sourceItem = settings.Filters.Sources[i];
                await using var stream = await httpClient.GetStreamAsync(sourceItem.Uri);
                if (sourceItem.Format == SourceFormat.Hosts)
                {
                    await HostUtilities.ProcessHostsBased(
                        sourceItem.SourceAction == SourceAction.Combine ? combineLines : externalCoverageLines,
                        stream,
                        settings.Filters.SkipLinesBytes,
                        sourceItem.SourcePrefixes,
                        decoder);
                }
                else
                {
                    await HostUtilities.ProcessAdBlockBased(
                        sourceItem.SourceAction == SourceAction.Combine ? combineLines : externalCoverageLines,
                        stream,
                        decoder);
                }
            }

            var combined = combineLines;
            combined.ExceptWith(externalCoverageLines);
            combined = HostUtilities.RemoveKnownBadHosts(settings.KnownBadHosts, combined);
            combined.UnionWith(settings.KnownBadHosts);
            combined.UnionWith(externalCoverageLines);
            CollectionUtilities.FilterGrouped(combined);

            var sortedDnsList = CollectionUtilities.SortDnsList(combined);
            HashSet<string> filteredCache = new(combined.Count);
            sortedDnsList = settings.MultiPassFilter
                ? ProcessingUtilities.ProcessCombinedWithMultipleRounds(sortedDnsList, externalCoverageLines, filteredCache)
                : ProcessingUtilities.ProcessCombined(sortedDnsList, externalCoverageLines, filteredCache);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation("Start extra filtering of duplicates");
                sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList, externalCoverageLines, filteredCache);
                logger.LogInformation("Done extra filtering of duplicates");
            }

            await using StreamWriter streamWriter = new(settings.OutputFileName, false);
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
            logger.LogInformation("Execution duration - {elapsed} | Produced {count} hosts", stopWatch.Elapsed, sortedDnsList.Count);
        }
    }
}
