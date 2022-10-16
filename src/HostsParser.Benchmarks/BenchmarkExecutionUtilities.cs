// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks;

[MemoryDiagnoser]
public class BenchmarkExecutionUtilities : BenchmarkStreamBase
{
    [GlobalSetup]
    public void Setup()
    {
        _streamHttpMessageHandler = new StreamHttpMessageHandler();
        _httpClient = new HttpClient(_streamHttpMessageHandler);
    }
    
    private StreamHttpMessageHandler? _streamHttpMessageHandler;
    private HttpClient? _httpClient;

    [Benchmark]
    [BenchmarkCategory(nameof(Execute), nameof(BenchmarkExecutionUtilities))]
    public async Task Execute() => await ExecutionUtilities.Execute(_httpClient!);

    [GlobalCleanup]
    public void Cleanup()
    {
        _httpClient?.Dispose();
        _streamHttpMessageHandler?.Dispose();
    }
    
    private sealed class StreamHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {Content = new StreamContent(PrepareStream())});
    }
}
