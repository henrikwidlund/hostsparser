// Copyright Henrik Widlund
// Apache License 2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

using var httpClient = new HttpClient();

var lines = (await httpClient.GetStringAsync("https://raw.githubusercontent.com/StevenBlack/hosts/master/alternates/fakenews-gambling-porn/hosts"))
    .Split('\n');

var headerLines = new List<string>(16){"# Content below this line is based on StevenBlack/hosts and only modified to work with AdGuard Home"};
headerLines.AddRange(lines[..14]);
headerLines.Add(string.Empty);

var skipLines = new string[14]
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
    "0.0.0.0 0.0.0.0"
};

var newLines = new HashSet<string>(headerLines);
lines = lines.Except(newLines).Except(skipLines).ToArray();

const string ipFilter = "0.0.0.0 ";
var length = ipFilter.Length;
foreach (var item in lines)
{
    if (item.StartsWith('#') || !item.StartsWith(ipFilter))
        continue;
    
    newLines.Add(Replace(item, length));
}

await File.WriteAllLinesAsync("hosts", newLines);

static string Replace(ReadOnlySpan<char> item, int length)
    => $"||{item.Slice(length).ToString()}^";
