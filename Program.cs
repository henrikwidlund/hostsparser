// Copyright Henrik Widlund
// Apache License 2.0

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

using var loggerFactory = LoggerFactory.Create(options =>
{
    options.AddDebug();
    options.AddConsole();
});
var logger = loggerFactory.CreateLogger("HostsParser");

logger.LogInformation("Running...");
var stopWatch = new Stopwatch();
stopWatch.Start();

const string Pipe = "|";
const string Hat = "^";
const char NewLine = '\n';
const char ExclamationMark = '!';
const char AtSign = '@';
const string IpFilter = "0.0.0.0 ";
const string LoopbackEntry = "0.0.0.0 0.0.0.0";

var settings = JsonSerializer.Deserialize<Settings>(File.ReadAllBytes("appsettings.json"));

using var httpClient = new HttpClient();

logger.LogInformation("Start get source hosts");
var sourceUris = (await httpClient.GetStringAsync(settings.SourceUri))
    .Split(NewLine);
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
    .Split(NewLine)
    .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith(ExclamationMark) && !l.StartsWith(AtSign))
    .Select(l => ReplaceAdGuard(l))
    .Where(l => !string.IsNullOrWhiteSpace(l))
    .ToArray();
logger.LogInformation("Done get AdGuard hosts");

var length = IpFilter.Length;
var skipLines = new[]
{
    "127.0.0.1 localhost",
    "127.0.0.1 localhost.localdomain",
    "127.0.0.1 local",
    "255.255.255.255 broadcasthost",
    "::1 localhost",
    "::1 ip6-localhost",
    "::1 ip6-loopback",
    "fe80::1%lo0 localhost",
    "ff00::0 ip6-localnet",
    "ff00::0 ip6-mcastprefix",
    "ff02::1 ip6-allnodes",
    "ff02::2 ip6-allrouters",
    "ff02::3 ip6-allhosts",
    "0.0.0.0 0.0.0.0",
    "0.0.0.0 fe #00::0 ip6-localnet",
    "0.0.0.0 ff #00::0 ip6-mcastprefix"
};
var cleanedLinesArray = sourceUris
    .Except(skipLines)
    .Where(l => !l.Equals(LoopbackEntry) && l.StartsWith(IpFilter))
    .Select(l => ReplaceSource(l, length))
    .OrderBy(l => l)
    .ToArray();

var a = cleanedLinesArray.Where(l => l == "fe ").ToList();
var b = adGuardLines.Where(l => l == "fe ").ToList();

List<string> combined = cleanedLinesArray.Except(adGuardLines).ToList();
combined.AddRange(adGuardLines);
combined = combined.Distinct().OrderBy(l => l.Length).ToList();

var superFiltered = new List<string>();

using (var connection = new SqliteConnection("Data Source=hosts.db"))
{
    connection.Open();

    var delTableCmd = connection.CreateCommand();
    delTableCmd.CommandText = "DROP TABLE IF EXISTS tblHosts";
    await delTableCmd.ExecuteNonQueryAsync();

    var createTableCmd = connection.CreateCommand();
    createTableCmd.CommandText = @"CREATE TABLE tblHosts(host NVARCHAR(1000));
                                   CREATE UNIQUE INDEX idx_tblHosts ON tblHosts(host);";
    await createTableCmd.ExecuteNonQueryAsync();

    using (var transaction = await connection.BeginTransactionAsync())
    {
        foreach (var item in combined)
        {
            var command = connection.CreateCommand();
            command.CommandText =
            "INSERT INTO tblHosts VALUES($host)";
            command.Parameters.AddWithValue("$host", "." + item);
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    try
    {
        using var query = connection.CreateCommand();
        query.CommandText = @"SELECT t.host FROM tblHosts as t
                            WHERE (SELECT COUNT(1) FROM tblHosts AS t2 WHERE LENGTH(t2.host) < LENGTH(t.host) AND t2.host like '%t.host' LIMIT 1) > 0";
        await using var reader = await query.ExecuteReaderAsync();
        {
            while (await reader.ReadAsync())
            {
                var name = reader.GetString(0);
                superFiltered.Add(name[1..]);
            }
        }

    }
    catch (Exception ex)
    {
        logger.LogError(ex, "error");
        throw;
    }
}

combined = combined.Except(superFiltered).Except(adGuardLines).ToList();

var newLinesList = new List<string>();

logger.LogInformation("Start filtering hosts");
foreach (var item in combined)
{
    newLinesList.Add($"||{item}^");
}

logger.LogInformation("Done filtering hosts");

logger.LogInformation("Start sorting hosts");
newLinesList = newLinesList
    .OrderBy(l => l)
    .ToList();
logger.LogInformation("Done sorting hosts");

logger.LogInformation("Start building hosts results");
var headerLines = new[]
{
    "# Copyright Henrik Widlund https://github.com/henrikwidlund/HostsParser/blob/main/LICENSE",
    "# All content below commented lines are based on StevenBlack/hosts and AdGuard DNS filter.",
    "# It is only modified to work with AdGuard Home and remove duplicates.",
    $"# StevenBlack/hosts: https://github.com/StevenBlack/hosts/ ({settings.SourceUri})",
    $"# AdGuard DNS filter: https://github.com/AdguardTeam/AdguardSDNSFilter ({settings.AdGuardUri})",
    string.Empty
};

var newLines = new HashSet<string>(headerLines);

sourceUris = sourceUris.Except(newLines).Except(skipLines).ToArray();

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

static string ReplaceSource(ReadOnlySpan<char> item, in int length)
{
    var item2 = item[length..];
    if (item2.Contains("#", StringComparison.Ordinal))
        item2 = item2[..item2.IndexOf("#")];
    item2 = item2.Trim();
    
    return item2.ToString();
}

static string ReplaceAdGuard(ReadOnlySpan<char> item)
{
    var originalItem = item.ToString();
    if (item.StartsWith(Pipe))
    {
        var index = item.LastIndexOf(Pipe);
        item = item[++index..];
    }

    if (item.EndsWith(Hat))
    {
        var index = item.IndexOf(Hat);
        item = item[..index];
    }

    return item.ToString();
}

record Settings(Uri SourceUri, long SourcePreviousUpdateEpoch, Uri AdGuardUri);