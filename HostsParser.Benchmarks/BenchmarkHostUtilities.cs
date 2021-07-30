// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class BenchmarkHostUtilities
    {
        private Stream _stream;
        
        [IterationSetup]
        public void IterationSetup()
        {
            _stream = PrepareStream();
        }

        [IterationCleanup]
        public void IterationCleanup()
        {
            _stream.Dispose();
        }

        public Stream PrepareStream()
        {
            var stream = new MemoryStream();
            var pipeWriter = PipeWriter.Create(stream);
            pipeWriter.Write(BenchmarkTestData.SourceTestBytes);

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }

        [Benchmark]
        [BenchmarkCategory(nameof(ProcessSource), nameof(BenchmarkHostUtilities))]
        public async Task<List<string>> ProcessSource()
        {
            return await HostUtilities.ProcessSource(_stream,
                BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);
        }

        [Benchmark]
        [BenchmarkCategory(nameof(ProcessAdGuard), nameof(BenchmarkHostUtilities))]
        public async Task<HashSet<string>> ProcessAdGuard()
            => await HostUtilities.ProcessAdGuard(_stream, BenchmarkTestData.Decoder);
        
        [Benchmark]
        [ArgumentsSource(nameof(Source))]
        public void RemoveKnownBadHosts(List<string> data)
            => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);

        public async IAsyncEnumerable<List<string>> Source()
        {
            var source = await HostUtilities
                .ProcessSource(_stream, BenchmarkTestData.Settings.SkipLinesBytes,
                    BenchmarkTestData.Decoder);
            var adGuard = await HostUtilities.ProcessAdGuard(_stream, BenchmarkTestData.Decoder);
            yield return source
                .Concat(adGuard).ToList();
        }
    }
}