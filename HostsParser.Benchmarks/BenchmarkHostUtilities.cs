// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkHostUtilities
    {
        [Benchmark]
        public List<string> ProcessSource()
            => HostUtilities.ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);

        [Benchmark]
        public HashSet<string> ProcessAdGuard()
            => HostUtilities.ProcessAdGuard(BenchmarkTestData.AdGuardTestBytes, BenchmarkTestData.Decoder);

        [Benchmark]
        [ArgumentsSource(nameof(Source))]
        public void RemoveKnownBadHosts(List<string> data)
            => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);
        
        public IEnumerable<List<string>> Source()
        {
            yield return HostUtilities
                .ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes, BenchmarkTestData.Decoder)
                .Concat(HostUtilities.ProcessAdGuard(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Decoder)).ToList();
        }
    }
}