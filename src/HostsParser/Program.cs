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
            var hostsBasedLines = new HashSet<string>(140_000);
            for (var i = 0; i < settings.HostsBased.SourceUris.Length; i++)
            {
                await using var stream = await httpClient.GetStreamAsync(settings.HostsBased.SourceUris[i]);
                await HostUtilities.ProcessHostsBased(hostsBasedLines, stream, settings.HostsBased.SkipLinesBytes, decoder);
            }

            // Assumed length to reduce allocations
            var adBlockBasedLines = new HashSet<string>(50_000);
            for (var i = 0; i < settings.AdBlockBased.SourceUris.Length; i++)
            {
                await using var stream = await httpClient.GetStreamAsync(settings.AdBlockBased.SourceUris[i]);
                await HostUtilities.ProcessAdBlockBased(adBlockBasedLines, stream, decoder);
            }

            var combined = hostsBasedLines;
            combined.ExceptWith(adBlockBasedLines);
            combined = HostUtilities.RemoveKnownBadHosts(settings.KnownBadHosts, combined);
            combined.UnionWith(settings.KnownBadHosts);
            combined.UnionWith(adBlockBasedLines);
            CollectionUtilities.FilterGrouped(combined);

            var sortedDnsList = CollectionUtilities.SortDnsList(combined);
            HashSet<string> filteredCache = new(combined.Count);
            sortedDnsList = settings.MultiPassFilter
                ? ProcessingUtilities.ProcessCombinedWithMultipleRounds(sortedDnsList, adBlockBasedLines, filteredCache)
                : ProcessingUtilities.ProcessCombined(sortedDnsList, adBlockBasedLines, filteredCache);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation("Start extra filtering of duplicates");
                sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList, adBlockBasedLines, filteredCache);
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
