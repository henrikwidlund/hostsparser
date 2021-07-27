using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkHostUtilities
    {
        private readonly List<string> _combined;
        public BenchmarkHostUtilities() =>
            _combined = HostUtilities
                .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
                .Except(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();

        [Benchmark]
        public List<string> ProcessSource()
            => HostUtilities.ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);

        [Benchmark]
        public List<string> ProcessAdGuard()
            => HostUtilities.ProcessAdGuard(BenchmarkTestData.AdGuardTestBytes, BenchmarkTestData.Decoder);

        [Benchmark]
        public void RemoveKnownBadHosts()
            => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, _combined);
    }
}