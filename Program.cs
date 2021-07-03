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

logger.LogInformation(WithTimeStamp("Start checking if external last run should be used"));
if (settings.LastRunExternalUri != null
    && bool.TryParse(Environment.GetEnvironmentVariable(Constants.UseExternalLastRun), out var useExternalLastRun)
    && useExternalLastRun)
{
    var httpResponseMessage = await httpClient.GetAsync(settings.LastRunExternalUri);
    if (httpResponseMessage.IsSuccessStatusCode)
    {
        var lastRunString = await httpResponseMessage.Content.ReadAsStringAsync();
        if (long.TryParse(lastRunString, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out var lastRun))
        {
            settings = settings with { SourcePreviousUpdateEpoch = lastRun };
            logger.LogInformation(WithTimeStamp($"Using external last run: {lastRunString}"));
        }
    }
}
logger.LogInformation(WithTimeStamp("Done checking if external last run should be used"));

logger.LogInformation(WithTimeStamp("Start get source hosts"));
var sourceUris = (await httpClient.GetStringAsync(settings.SourceUri))
    .Split(Constants.NewLine);
logger.LogInformation(WithTimeStamp("Done get source hosts"));

var modifiedDateString = sourceUris[..14]
    .Single(l => l.StartsWith("# Date: "))
    .Replace("# Date: ", null)
    .Replace(" (UTC)", null);

var modifiedDate = DateTime.Parse(modifiedDateString, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal);

sourceUris = sourceUris
    .Where(l => !l.StartsWith(Constants.HashSign))
    .ToArray();

logger.LogInformation(WithTimeStamp("Start get AdGuard hosts"));
var adGuardLines = (await httpClient.GetStringAsync(settings.AdGuardUri))
    .Split(Constants.NewLine);
logger.LogInformation(WithTimeStamp("Done get AdGuard hosts"));

modifiedDateString = adGuardLines[..6]
    .Single(l => l.StartsWith("! Last modified: "))
    .Replace("! Last modified: ", null);
var adGuardModified = DateTime.Parse(modifiedDateString, DateTimeFormatInfo.InvariantInfo);

var epoch = new DateTimeOffset(adGuardModified > modifiedDate ? adGuardModified : modifiedDate).ToUnixTimeSeconds();
await File.WriteAllTextAsync(Constants.ModifiedFile, epoch.ToString(NumberFormatInfo.InvariantInfo));
if (epoch <= settings.SourcePreviousUpdateEpoch)
{
    logger.LogInformation(WithTimeStamp("Source not modified since previous run. Terminating..."));
    return;
}

adGuardLines = adGuardLines
    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(Constants.ExclamationMark) && !l.StartsWith(Constants.AtSign))
    .Select(l => DnsUtilities.ReplaceAdGuard(l))
    .Where(l => !string.IsNullOrWhiteSpace(l))
    .ToArray();

logger.LogInformation(WithTimeStamp("Start combining host sources"));
var combined = sourceUris
    .Except(settings.SkipLines)
    .Where(l => !l.Equals(Constants.LoopbackEntry) && l.StartsWith(Constants.IpFilter))
    .Select(l => DnsUtilities.ReplaceSource(l, Constants.IpFilterLength))
    .OrderBy(l => l)
    .Except(adGuardLines)
    .ToList();
sourceUris = null;
combined.AddRange(adGuardLines);
combined.AddRange(settings.KnownBadHosts);
combined = CollectionUtilities.SortDnsList(combined);
logger.LogInformation(WithTimeStamp("Done combining host sources"));

logger.LogInformation(WithTimeStamp("Start removing www duplicates"));
var wwwOnly = CollectionUtilities.GetWwwOnly(combined);
var wwwToRemove = new List<string>();
Parallel.ForEach(wwwOnly, item =>
{
    if (combined.Contains(item.WithoutPrefix))
        wwwToRemove.Add(item.WithPrefix);
});
wwwOnly = null;
combined = CollectionUtilities.SortDnsList(combined.Except(wwwToRemove));
wwwToRemove = null;
logger.LogInformation(WithTimeStamp("Done removing www duplicates"));

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
            if (item.Equals(combined[j])) continue;
            if (!item.EndsWith($".{combined[j]}")) continue;
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

var newLines = new HashSet<string>(settings.HeaderLines);
foreach (var item in newLinesList)
    newLines.Add(item);

logger.LogInformation(WithTimeStamp("Done building hosts results"));

logger.LogInformation(WithTimeStamp("Start writing hosts file"));
await File.WriteAllLinesAsync("hosts", newLines);
logger.LogInformation(WithTimeStamp("Done writing hosts file"));

logger.LogInformation(WithTimeStamp("Start updating settings"));
var newSettings = settings with { SourcePreviousUpdateEpoch = epoch };
await File.WriteAllTextAsync("appsettings.json", JsonSerializer.Serialize(newSettings, options: new()
{
    WriteIndented = true
}));
logger.LogInformation(WithTimeStamp("Done updating settings"));

stopWatch.Stop();
logger.LogInformation(WithTimeStamp($"Execution duration: {stopWatch.Elapsed}"));

static string WithTimeStamp(string message)
{
    return $"{DateTime.Now:yyyy-MM-dd hh:mm:ss} - {message}";
}