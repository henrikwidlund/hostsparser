// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
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
        
        // public BenchmarkHostUtilities()
        // {
        //     MemoryStream = new MemoryStream(null, 0, 0, true, );
        //     var pipeWriter = PipeWriter.Create(MemoryStream);
        //     pipeWriter.Write(BenchmarkTestData.SourceTestBytes);
        // }
        
        [Benchmark]
        public async Task<List<string>> ProcessSource()
        {
            // MemoryStream.Position = 0;
            // await using var stream = new MemoryStream();
            // var pipeWriter = PipeWriter.Create(stream);
            // pipeWriter.Write(BenchmarkTestData.SourceTestBytes);
            return await HostUtilities.ProcessSource(_stream,
                BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);
        }

        // private MemoryStream MemoryStream { get; }
        
        public IEnumerable<MemoryStream> Source()
        {
            var stream = new MemoryStream();
            var pipeWriter = PipeWriter.Create(stream);
            pipeWriter.Write(BenchmarkTestData.SourceTestBytes);
            yield return stream;
            // yield return HostUtilities
            //     .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
            //     .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();
        }

        // [Benchmark]
        // public HashSet<string> ProcessAdGuard()
        //     => HostUtilities.ProcessAdGuard(BenchmarkTestData.AdGuardTestBytes, BenchmarkTestData.Decoder);
        //
        // [Benchmark]
        // [ArgumentsSource(nameof(Source))]
        // public void RemoveKnownBadHosts(List<string> data)
        //     => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);
        //
        // public IEnumerable<List<string>> Source()
        // {
        //     yield return HostUtilities
        //         .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
        //         .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();
        // }
    }
}