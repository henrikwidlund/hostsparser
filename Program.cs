// Copyright Henrik Widlund
// GNU General Public License v3.0

using HostsParser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

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

var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllBytes("appsettings.json"));
if (settings == null)
{
    logger.LogError("Couldn't load settings. Terminating...");
    return;
}

using var httpClient = new HttpClient();

logger.LogInformation(WithTimeStamp("Start get source hosts"));
var sourceUris = (await httpClient.GetStringAsync(settings.SourceUri))
    .Split(Constants.NewLine)
    .Where(l => !l.StartsWith(Constants.HashSign))
    .ToArray();
logger.LogInformation(WithTimeStamp("Done get source hosts"));

logger.LogInformation(WithTimeStamp("Start get AdGuard hosts"));
var adGuardLines = (await httpClient.GetStringAsync(settings.AdGuardUri))
    .Split(Constants.NewLine)
    .Where(l => l.StartsWith(Constants.PipeSign))
    .Select(l => DnsUtilities.ReplaceAdGuard(l))
    .Where(l => !string.IsNullOrWhiteSpace(l))
    .ToArray();
logger.LogInformation(WithTimeStamp("Done get AdGuard hosts"));

logger.LogInformation(WithTimeStamp("Start combining host sources"));
var combined = sourceUris
    .Except(settings.SkipLines)
    .Where(l => !l.Equals(Constants.LoopbackEntry) && l.StartsWith(Constants.IpFilter))
    .Select(l => DnsUtilities.ReplaceSource(l, Constants.IpFilterLength))
    .Concat(settings.KnownBadHosts)
    .Except(adGuardLines)
    .ToList();
sourceUris = null;

var (withPrefix, withoutPrefix) = CollectionUtilities.GetWwwOnly(combined);
combined = CollectionUtilities.SortDnsList(combined.Except(withPrefix).Concat(withoutPrefix)
    .Concat(adGuardLines));

logger.LogInformation(WithTimeStamp("Done combining host sources"));

logger.LogInformation(WithTimeStamp("Start filtering duplicates - Part 1"));
var superFiltered = new List<string>(combined.Count);

var round = 0;
do
{
    superFiltered.Clear();
    var lookBack = ++round * 250;
    Parallel.For(0, combined.Count, (i) =>
    {
        for (var j = (i < lookBack ? 0 : i - lookBack); j < i; j++)
        {
            var item = combined[i];
            var otherItem = combined[j];
            if (otherItem.Length + 1 > item.Length) continue;
            if (item == otherItem) continue;

            if (!item.EndsWith(string.Concat(Constants.DotSignString, otherItem))) continue;
            superFiltered.Add(item);
            break;
        }
    });

    combined = CollectionUtilities.SortDnsList(combined.Except(superFiltered).Except(adGuardLines));
} while (superFiltered.Any());
logger.LogInformation(WithTimeStamp("Done filtering duplicates - Part 1"));

#if EXTRA_FILTERING
logger.LogInformation(WithTimeStamp("Start filtering duplicates - Part 2"));
Parallel.ForEach(CollectionUtilities.SortDnsList(adGuardLines), item =>
{
    for (var i = 0; i < combined.Count; i++)
    {
        var localItem = combined[i];
        if (localItem.EndsWith($".{item}"))
            superFiltered.Add(localItem);
    }
});
combined = CollectionUtilities.SortDnsList(combined.Except(superFiltered).Except(adGuardLines));
logger.LogInformation(WithTimeStamp("Done filtering duplicates - Part 2"));
#endif

logger.LogInformation(WithTimeStamp("Start formatting hosts"));
var newLinesList = combined
    .Select(l => $"||{l}^")
    .OrderBy(l => l);
logger.LogInformation(WithTimeStamp("Done formatting hosts"));

logger.LogInformation(WithTimeStamp("Start building hosts results"));

var newLines = new HashSet<string>(settings.HeaderLines) { $"! Lst Modified: {DateTime.UtcNow:u}", string.Empty };
foreach (var item in newLinesList)
    newLines.Add(item);

logger.LogInformation(WithTimeStamp("Done building hosts results"));

logger.LogInformation(WithTimeStamp("Start writing hosts file"));
await File.WriteAllLinesAsync("hosts", newLines);
logger.LogInformation(WithTimeStamp("Done writing hosts file"));

stopWatch.Stop();
logger.LogInformation(WithTimeStamp($"Execution duration: {stopWatch.Elapsed}"));

static string WithTimeStamp(string message)
{
    return $"{DateTime.Now:yyyy-MM-dd hh:mm:ss} - {message}";
}