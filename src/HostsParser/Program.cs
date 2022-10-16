// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Net.Http;
using HostsParser;

using var httpClient = new HttpClient();
await ExecutionUtilities.Execute(httpClient);
