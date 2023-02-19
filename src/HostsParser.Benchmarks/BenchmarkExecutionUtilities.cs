// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace HostsParser.Benchmarks;

[MemoryDiagnoser]
public class BenchmarkExecutionUtilities : BenchmarkStreamBase
{
    [GlobalSetup]
    public void Setup()
    {
        _streamHttpMessageHandler = new StreamHttpMessageHandler();
        _httpClient = new HttpClient(_streamHttpMessageHandler);
        _loggerFactory = new NullLoggerFactory();
        _logger = _loggerFactory.CreateLogger(nameof(Benchmarks));
    }
    
    private StreamHttpMessageHandler? _streamHttpMessageHandler;
    private HttpClient? _httpClient;
    private ILoggerFactory? _loggerFactory;
    private ILogger? _logger;

    [Benchmark]
    [BenchmarkCategory(nameof(Execute), nameof(BenchmarkExecutionUtilities))]
    public async Task Execute() => await ExecutionUtilities.Execute(_httpClient!, _logger!);

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
        _streamHttpMessageHandler?.Dispose();
        _loggerFactory?.Dispose();
    }
    
    private sealed class StreamHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(PrepareStream())});
    }
}
