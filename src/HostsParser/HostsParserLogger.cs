// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace HostsParser;

internal static class HostsParserLogger
{
    private static readonly Action<ILogger, Exception?> RunningAction =
        LoggerMessage.Define(LogLevel.Information,
            new EventId(1, nameof(Running)), 
            "Running...");

    private static readonly Action<ILogger, Exception?> UnableToRunAction =
        LoggerMessage.Define(LogLevel.Error,
            new EventId(2, nameof(UnableToRun)), 
            "Couldn't load settings. Terminating...");

    private static readonly Action<ILogger, Exception?> StartExtraFilteringAction =
        LoggerMessage.Define(LogLevel.Information,
            new EventId(3, nameof(StartExtraFiltering)), 
            "Start extra filtering of duplicates.");

    private static readonly Action<ILogger, Exception?> DoneExtraFilteringAction =
        LoggerMessage.Define(LogLevel.Information,
            new EventId(4, nameof(DoneExtraFiltering)), 
            "Done extra filtering of duplicates.");

    private static readonly Action<ILogger, TimeSpan, int, Exception?> FinalizedAction =
        LoggerMessage.Define<TimeSpan, int>(LogLevel.Information,
            new EventId(5, nameof(Finalized)), 
            "Execution duration - {Elapsed} | Produced {Count} hosts.");

    internal static void Running(this ILogger logger) => RunningAction(logger, null);
    internal static void UnableToRun(this ILogger logger) => UnableToRunAction(logger, null);
    internal static void StartExtraFiltering(this ILogger logger) => StartExtraFilteringAction(logger, null);
    internal static void DoneExtraFiltering(this ILogger logger) => DoneExtraFilteringAction(logger, null);

    internal static void Finalized(this ILogger logger, TimeSpan elapsed, int count)
        => FinalizedAction(logger, elapsed, count, null);

    internal static ILoggerFactory Create() =>
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

    internal static ILogger CreateLogger(this ILoggerFactory loggerFactory) =>
        loggerFactory.CreateLogger(nameof(HostsParser));
}
