// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            HashSet<string> filteredCache = new(combined.Count);
            sortedDnsList = settings.MultiPassFilter
                ? ProcessingUtilities.ProcessCombinedWithMultipleRounds(sortedDnsList, adBlockBasedLines, filteredCache)
                : ProcessingUtilities.ProcessCombined(sortedDnsList, adBlockBasedLines, filteredCache);

            if (settings.ExtraFiltering)
            {
                logger.LogInformation(WithTimeStamp("Start extra filtering of duplicates"));
                sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList, adBlockBasedLines, filteredCache);
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
    }
}
