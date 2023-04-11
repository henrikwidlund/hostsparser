// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace HostsParser.Tests;

public class HostsParserLoggerTests
{
    [Fact]
    public void Running_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.Running();

        // Assert
        store.Should().HaveCount(1).And.ContainSingle(s => s == "Information-1-Running-Running...");
    }

    [Fact]
    public void UnableToRun_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.UnableToRun();

        // Assert
        store.Should().HaveCount(1).And
            .ContainSingle(s => s == "Error-2-UnableToRun-Couldn't load settings. Terminating...");
    }

    [Fact]
    public void StartExtraFiltering_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.StartExtraFiltering();

        // Assert
        store.Should().HaveCount(1).And.ContainSingle(s =>
            s == "Information-3-StartExtraFiltering-Start extra filtering of duplicates.");
    }

    [Fact]
    public void DoneExtraFiltering_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);

        // Act
        logger.DoneExtraFiltering();

        // Assert
        store.Should().HaveCount(1).And
            .ContainSingle(s => s == "Information-4-DoneExtraFiltering-Done extra filtering of duplicates.");
    }

    [Fact]
    public void Finalized_Should_Log_Text()
    {
        // Arrange
        var store = new List<string>();
        var logger = new TestLogger(store);
        var timeSpan = TimeSpan.FromSeconds(10);
        const int Count = 1;

        // Act
        logger.Finalized(timeSpan, Count);

        // Assert
        store.Should().HaveCount(1).And.ContainSingle(s =>
            s == $"Information-5-Finalized-Execution duration - {timeSpan} | Produced {Count} hosts.");
    }
}

file class TestLogger : ILogger
{
    private readonly List<string> _store;

    public TestLogger(List<string> store) => _store = store;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
        => _store.Add($"{logLevel}-{eventId.Id}-{eventId.Name}-{state}");

    public bool IsEnabled(LogLevel logLevel) => true;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
