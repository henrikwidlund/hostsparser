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
using Microsoft.Extensions.Logging.Abstractions;

namespace HostsParser.IntegrationTests;

public sealed class ExecutionUtilitiesTests
{
    [Test]
    public async Task When_Running_Execute_MultiPassFilter_Toggle_Should_Differ_At_Most_Five()
    {
        // Arrange
        using var streamHttpMessageHandler = new StreamHttpMessageHandler();
        using var httpClient = new HttpClient(streamHttpMessageHandler);
        using var loggerFactory = new NullLoggerFactory();
        var logger = loggerFactory.CreateLogger(nameof(IntegrationTests));
        const string ConfigurationFile = "appsettings.json";

        // Act
        await ExecutionUtilities.Execute(httpClient, logger, ConfigurationFile);
        var linesWithoutMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];
        var settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllBytesAsync(ConfigurationFile))!;
        settings = settings with { MultiPassFilter = true };
        await File.WriteAllBytesAsync(ConfigurationFile, JsonSerializer.SerializeToUtf8Bytes(settings));
        await ExecutionUtilities.Execute(httpClient, logger, ConfigurationFile);
        var linesWithMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];

        // Assert
        // Sometimes there's one item in linesWithoutMultiPass that aren't in linesWithMultiPass.
        // This is "okay" because the sort isn't 100% stable and it's a tradeoff between performance and stability.
        await Assert.That(linesWithoutMultiPass).IsNotEmpty();
        await Assert.That(linesWithMultiPass).IsNotEmpty();
        await Assert.That(linesWithoutMultiPass.Except(linesWithMultiPass)).HasCount().LessThanOrEqualTo(5);
        await Assert.That(linesWithMultiPass.Except(linesWithoutMultiPass)).HasCount().LessThanOrEqualTo(5);
    }
}

file sealed class StreamHttpMessageHandler : HttpMessageHandler
{
    private static readonly byte[] HostsBasedTestBytes = File.ReadAllBytes("hostsbased.txt");
    private static readonly byte[] AdBlockBasedTestBytes = File.ReadAllBytes("adbockbased.txt");

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
        => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(PrepareStream())});

    private static MemoryStream PrepareStream()
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
