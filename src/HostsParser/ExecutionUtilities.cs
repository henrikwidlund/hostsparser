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

namespace HostsParser;

/// <summary>
/// Utilities for creating AdBlock files.
/// </summary>
public static class ExecutionUtilities
{
    /// <summary>
    /// Reads settings, fetches defined sources and generates a AdBlock file.
    /// </summary>
    /// <param name="httpClient">The <see cref="HttpClient"/> that will be used to read from external sources.</param>
    /// <param name="logger">The <see cref="ILogger"/> that will be used for logging.</param>
    public static async Task Execute(HttpClient httpClient, ILogger logger)
    {
        logger.Running();
        var stopWatch = new Stopwatch();
        stopWatch.Start();

        await using var fileStream = File.OpenRead("appsettings.json");
        var settings = await JsonSerializer.DeserializeAsync(fileStream, SourceGenerationContext.Default.Settings);
        if (settings is null)
        {
            logger.UnableToRun();
            return;
        }

        var (combineLines, externalCoverageLines) = await ReadSources(settings, httpClient);

        combineLines.ExceptWith(externalCoverageLines);
        combineLines = HostUtilities.RemoveKnownBadHosts(settings.KnownBadHosts, combineLines);
        combineLines.UnionWith(settings.KnownBadHosts);
        combineLines.UnionWith(externalCoverageLines);
        CollectionUtilities.FilterGrouped(combineLines);

        var sortedDnsList = CollectionUtilities.SortDnsList(combineLines);
        HashSet<string> filteredCache = new(combineLines.Count);
        sortedDnsList = settings.MultiPassFilter
            ? ProcessingUtilities.ProcessCombinedWithMultipleRounds(sortedDnsList, externalCoverageLines, filteredCache)
            : ProcessingUtilities.ProcessCombined(sortedDnsList, externalCoverageLines, filteredCache);

        if (settings.ExtraFiltering)
        {
            logger.StartExtraFiltering();
            sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList,
                externalCoverageLines,
                filteredCache);
            logger.DoneExtraFiltering();
        }

        await using StreamWriter streamWriter = new(settings.OutputFileName, false);
        foreach (var headerLine in settings.HeaderLines)
            streamWriter.WriteLine(headerLine);

        streamWriter.WriteLine($"! Last Modified: {DateTime.UtcNow:u}");

        foreach (var s in sortedDnsList)
        {
            streamWriter.WriteLine();
            streamWriter.WriteLine();
            streamWriter.Write((char) Constants.PipeSign);
            streamWriter.Write((char) Constants.PipeSign);
            streamWriter.Write(s);
            streamWriter.Write((char) Constants.HatSign);
        }

        stopWatch.Stop();
        logger.Finalized(stopWatch.Elapsed, sortedDnsList.Count);
    }

    private static async Task<(HashSet<string> CombinedLines, HashSet<string> ExternalCoverageLines)> ReadSources(
        Settings settings,
        HttpClient httpClient)
    {
        var decoder = Encoding.UTF8.GetDecoder();

        // Assumed length to reduce allocations
        var combineLines = new HashSet<string>(170_000);
        var externalCoverageLines = new HashSet<string>(50_000);
        var tasks = new List<Task>(settings.Filters.Sources.Length);
        foreach (var sourceItem in settings.Filters.Sources)
        {
            tasks.Add(Task.Run(async () =>
            {
                await using var stream = await httpClient.GetStreamAsync(sourceItem.Uri);
                if (sourceItem.Format == SourceFormat.Hosts)
                {
                    await HostUtilities.ProcessHostsBased(
                        sourceItem.SourceAction == SourceAction.Combine ? combineLines : externalCoverageLines,
                        stream,
                        settings.Filters.SkipLinesBytes,
                        sourceItem.SourcePrefix,
                        decoder);
                }
                else
                {
                    await HostUtilities.ProcessAdBlockBased(
                        sourceItem.SourceAction == SourceAction.Combine ? combineLines : externalCoverageLines,
                        stream,
                        decoder);
                }
            }));
        }

        await Task.WhenAll(tasks);

        return (combineLines, externalCoverageLines);
    }
}
