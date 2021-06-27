// Copyright Henrik Widlund
// Apache License 2.0

using HostsParser;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using var loggerFactory = LoggerFactory.Create(options =>
{
    options.AddDebug();
    options.AddConsole();
});
var logger = loggerFactory.CreateLogger("HostsParser");

logger.LogInformation("Running...");
var stopWatch = new Stopwatch();
stopWatch.Start();

var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllBytes("appsettings.json"));

using var httpClient = new HttpClient();

logger.LogInformation("Start get source hosts");
var sourceUris = (await httpClient.GetStringAsync(settings.SourceUri))
    .Split(Constants.NewLine);
logger.LogInformation("Done get source hosts");

var modifiedDateString = sourceUris[..14]
    .Single(l => l.StartsWith("# Date: "))
    .Replace("# Date: ", null)
    .Replace(" (UTC)", null);

var modifiedDate = DateTime.Parse(modifiedDateString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);
var epoch = new DateTimeOffset(modifiedDate).ToUnixTimeSeconds();
if (epoch <= settings.SourcePreviousUpdateEpoch)
{
    logger.LogInformation("Source not modified since previous run. Terminating...");
    return;
}

logger.LogInformation("Start get AdGuard hosts");
var adGuardLines = (await httpClient.GetStringAsync(settings.AdGuardUri))
    .Split(Constants.NewLine)
    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(Constants.ExclamationMark) && !l.StartsWith(Constants.AtSign))
    .Select(l => DnsUtilities.ReplaceAdGuard(l))
    .Where(l => !string.IsNullOrWhiteSpace(l))
    .ToArray();
logger.LogInformation("Done get AdGuard hosts");

logger.LogInformation("Start combining host sources");
var combined = sourceUris
    .Except(settings.SkipLines)
    .Where(l => !l.Equals(Constants.LoopbackEntry) && l.StartsWith(Constants.IpFilter))
    .Select(l => DnsUtilities.ReplaceSource(l, Constants.IpFilterLength))
    .OrderBy(l => l)
    .Except(adGuardLines)
    .ToList();
combined.AddRange(adGuardLines);
logger.LogInformation("Done combining host sources");

logger.LogInformation("Start removing www duplicates");
var wwwOnly = CollectionUtilities.GetWwwOnly(combined);
var wwwToRemove = new List<string>();
Parallel.ForEach(wwwOnly, item =>
{
    if (combined.Contains(item.WithoutPrefix))
        wwwToRemove.Add(item.WithPrefix);
});
logger.LogInformation("Done removing www duplicates");

combined = CollectionUtilities.SortDnsList(combined.Except(wwwToRemove));

logger.LogInformation("Start filtering duplicates");
var superFiltered = new List<string>(combined.Count);
var round = 0;
do
{
    superFiltered.Clear();
    var lookBack = ++round * 250;
    Parallel.For(0, combined.Count, (i) =>
    {
        for (int j = (i < lookBack ? 0 : i - lookBack); j < i; j++)
        {
            var item = combined[i];
            if (item.EndsWith($".{combined[j]}"))
            {
                superFiltered.Add(item);
                break;
            }
        }
    });

    combined = CollectionUtilities.SortDnsList(combined.Except(superFiltered).Except(adGuardLines));
} while (superFiltered.Any());
logger.LogInformation("Done filtering duplicates");

logger.LogInformation("Start formatting hosts");
var newLinesList = combined
    .Select(l => $"||{l}^")
    .OrderBy(l => l);
logger.LogInformation("Done formatting hosts");

logger.LogInformation("Start building hosts results");

var newLines = new HashSet<string>(settings.HeaderLines);
foreach (var item in newLinesList)
    newLines.Add(item);

logger.LogInformation("Done building hosts results");

logger.LogInformation("Start writing hosts file");
await File.WriteAllLinesAsync("hosts", newLines);
logger.LogInformation("Done writing hosts file");

logger.LogInformation("Start updating settings");
var newSettings = settings with { SourcePreviousUpdateEpoch = epoch };
await File.WriteAllTextAsync("appsettings.json", JsonSerializer.Serialize(newSettings, options: new()
{
    WriteIndented = true
}));
logger.LogInformation("Done updating settings");

stopWatch.Stop();
logger.LogInformation($"Execution duration: {stopWatch.Elapsed}");