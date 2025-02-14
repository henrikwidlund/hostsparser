// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace HostsParser.Tests;

public sealed class HostsParserLoggerTests
{
    [Test]
    public async Task Running_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.Running();

        // Assert
        await Assert.That(store).HasSingleItem().And
            .ContainsOnly(static s => s == "Information-1-Running-Running...");
    }

    [Test]
    public async Task UnableToRun_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.UnableToRun();

        // Assert
        await Assert.That(store).HasSingleItem().And
            .ContainsOnly(static s => s == "Error-2-UnableToRun-Couldn't load settings. Terminating...");
    }

    [Test]
    public async Task StartExtraFiltering_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.StartExtraFiltering();

        // Assert
        await Assert.That(store).HasSingleItem().And
            .ContainsOnly(static s => s == "Information-3-StartExtraFiltering-Start extra filtering of duplicates.");
    }

    [Test]
    public async Task DoneExtraFiltering_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.DoneExtraFiltering();

        // Assert
        await Assert.That(store).HasSingleItem().And
            .ContainsOnly(static s => s == "Information-4-DoneExtraFiltering-Done extra filtering of duplicates.");
    }

    [Test]
    public async Task Finalized_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);
        var timeSpan = TimeSpan.FromSeconds(10);
        const int Count = 1;

        // Act
        logger.Finalized(timeSpan, Count);

        // Assert
        await Assert.That(store).HasSingleItem().And
            .ContainsOnly( s => s == $"Information-5-Finalized-Execution duration - {timeSpan} | Produced {Count} hosts.");
    }
}

file sealed class TestLogger(List<string> store) : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => store.Add($"{logLevel}-{eventId.Id}-{eventId.Name}-{state}");

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
