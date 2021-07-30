// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    public class BenchmarkHostUtilities
    {
        [Benchmark]
        [BenchmarkCategory(nameof(ProcessSource), nameof(BenchmarkHostUtilities))]
        public List<string> ProcessSource()
            => HostUtilities.ProcessSource(BenchmarkTestData.SourceTestBytes, BenchmarkTestData.Settings.SkipLinesBytes,
                BenchmarkTestData.Decoder);

        [Benchmark]
        [BenchmarkCategory(nameof(ProcessAdGuard), nameof(BenchmarkHostUtilities))]
        public HashSet<string> ProcessAdGuard()
            => HostUtilities.ProcessAdGuard(BenchmarkTestData.AdGuardTestBytes, BenchmarkTestData.Decoder);

        [Benchmark]
        [BenchmarkCategory(nameof(RemoveKnownBadHosts), nameof(BenchmarkHostUtilities))]
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