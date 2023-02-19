// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace HostsParser;

public static partial class HostsParserLogger
{
    [LoggerMessage(EventId = 1, EventName = nameof(Running), Level = LogLevel.Information, Message = "Running...")]
    public static partial void Running(this ILogger logger);
    
    [LoggerMessage(EventId = 2, EventName = nameof(UnableToRun), Level = LogLevel.Error, Message = "Couldn't load settings. Terminating...")]
    public static partial void UnableToRun(this ILogger logger);
    
    [LoggerMessage(EventId = 3, EventName = nameof(StartExtraFiltering), Level = LogLevel.Information, Message = "Start extra filtering of duplicates.")]
    public static partial void StartExtraFiltering(this ILogger logger);
    
    [LoggerMessage(EventId = 4, EventName = nameof(DoneExtraFiltering), Level = LogLevel.Information, Message = "Done extra filtering of duplicates.")]
    public static partial void DoneExtraFiltering(this ILogger logger);

    [LoggerMessage(EventId = 5, EventName = nameof(Finalized), Level = LogLevel.Information, Message = "Execution duration - {Elapsed} | Produced {Count} hosts.")]
    public static partial void Finalized(this ILogger logger, TimeSpan elapsed, int count);

    [ExcludeFromCodeCoverage(Justification = "Helper method, private type returned.")]
    public static ILoggerFactory Create() =>
        LoggerFactory.Create(options =>
        {
            options.AddDebug();
            options.AddSimpleConsole(consoleOptions =>
            {
                consoleOptions.SingleLine = true;
                consoleOptions.TimestampFormat = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " " +
                                                 CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern + " ";
            });
        });

    [ExcludeFromCodeCoverage(Justification = "Helper method, private type returned.")]
    public static ILogger CreateLogger(this ILoggerFactory loggerFactory) =>
        loggerFactory.CreateLogger(nameof(HostsParser));
}
