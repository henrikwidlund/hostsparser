// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace HostsParser.IntegrationTests;

public sealed class ExecutionUtilitiesTests
{
    [Fact]
    public async Task When_Running_Execute_MultiPassFilter_Toggle_Should_Differ_At_Most_Five()
    {
        // Arrange
        using var streamHttpMessageHandler = new StreamHttpMessageHandler();
        using var httpClient = new HttpClient(streamHttpMessageHandler);
        using var loggerFactory = new NullLoggerFactory();
        var logger = loggerFactory.CreateLogger(nameof(IntegrationTests));

        // Act
        await ExecutionUtilities.Execute(httpClient, logger);
        var linesWithoutMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];
        var settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllBytesAsync("appsettings.json"))!;
        settings = settings with { MultiPassFilter = true };
        await File.WriteAllBytesAsync("appsettings.json", JsonSerializer.SerializeToUtf8Bytes(settings));
        await ExecutionUtilities.Execute(httpClient, logger);
        var linesWithMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];

        // Assert
        // Sometimes there's one item in linesWithoutMultiPass that aren't in linesWithMultiPass.
        // This is "okay" because the sort isn't 100% stable and it's a tradeoff between performance and stability.
        linesWithoutMultiPass.Should().NotBeEmpty();
        linesWithMultiPass.Should().NotBeEmpty();
        linesWithoutMultiPass.Except(linesWithMultiPass).Should().HaveCountLessOrEqualTo(5);
        linesWithMultiPass.Except(linesWithoutMultiPass).Should().HaveCountLessOrEqualTo(5);
    }
}

file sealed class StreamHttpMessageHandler : HttpMessageHandler
{
    private static readonly byte[] HostsBasedTestBytes = File.ReadAllBytes("hostsbased.txt");
    private static readonly byte[] AdBlockBasedTestBytes = File.ReadAllBytes("adbockbased.txt");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(PrepareStream())});

    private static Stream PrepareStream()
    {
        var stream = new MemoryStream();

        using var sw = new BinaryWriter(stream, Encoding.UTF8, true);
        sw.Write(HostsBasedTestBytes);
        sw.Write(AdBlockBasedTestBytes);
        sw.Flush();

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
}
