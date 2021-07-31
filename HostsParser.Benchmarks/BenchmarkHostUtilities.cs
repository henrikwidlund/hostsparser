// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkProcessSource : BenchmarkStreamBase
    {
        private Stream _stream;

        [IterationSetup]
        public void IterationSetup() => _stream = PrepareStream();
        
        [IterationCleanup]
        public void IterationCleanup() => _stream?.Dispose();
        
        [Benchmark]
        [BenchmarkCategory(nameof(ProcessSource), nameof(HostUtilities))]
        public async Task<List<string>> ProcessSource()
        {
            return await HostUtilities.ProcessSource(_stream,
                BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);
        }
    }
    
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkProcessAdGuard : BenchmarkStreamBase
    {
        private Stream _stream;

        [IterationSetup]
        public void IterationSetup() => _stream = PrepareStream();
        
        [IterationCleanup]
        public void IterationCleanup() => _stream?.Dispose();
        
        [Benchmark]
        [BenchmarkCategory(nameof(ProcessAdGuard), nameof(HostUtilities))]
        public async Task<HashSet<string>> ProcessAdGuard()
            => await HostUtilities.ProcessAdGuard(_stream, BenchmarkTestData.Decoder);
    }
    
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkRemoveKnownBadHosts : BenchmarkStreamBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(RemoveKnownBadHosts), nameof(HostUtilities))]
        [ArgumentsSource(nameof(Source))]
        public void RemoveKnownBadHosts(List<string> data)
            => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);

        public IEnumerable<List<string>> Source()
        {
            var stream = PrepareStream();
            var source = HostUtilities
                .ProcessSource(stream, BenchmarkTestData.Settings.SkipLinesBytes,
                    BenchmarkTestData.Decoder).GetAwaiter().GetResult();

            stream = PrepareStream();
            var adGuard = HostUtilities.ProcessAdGuard(stream, BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();
            
            stream.Dispose();

            yield return source.Concat(adGuard).ToList();
        }
    }
}