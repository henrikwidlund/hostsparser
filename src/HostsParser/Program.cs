// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Net.Http;
using HostsParser;

using var httpClient = new HttpClient();
using var loggerFactory = HostsParserLogger.Create();
var logger = loggerFactory.CreateLogger();
var configurationFile = args.Length > 0 ? args[0] : "appsettings.json";
await ExecutionUtilities.Execute(httpClient, logger, configurationFile);
