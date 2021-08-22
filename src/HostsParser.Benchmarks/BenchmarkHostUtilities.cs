// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkProcessHostsBased : BenchmarkStreamBase
    {
        private Stream? _stream;

        [IterationSetup]
        public void IterationSetup() => _stream = PrepareStream();

        [IterationCleanup]
        public void IterationCleanup() => _stream?.Dispose();

        [Benchmark]
        [BenchmarkCategory(nameof(ProcessHostsBased), nameof(HostUtilities))]
        public async Task ProcessHostsBased()
            => await HostUtilities.ProcessHostsBased(new HashSet<string>(140_000),
                _stream!,
                BenchmarkTestData.Settings.HostsBased.SkipLinesBytes,
                BenchmarkTestData.Decoder);
    }

    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkProcessAdBlockBased : BenchmarkStreamBase
    {
        private Stream? _stream;

        [IterationSetup]
        public void IterationSetup() => _stream = PrepareStream();

        [IterationCleanup]
        public void IterationCleanup() => _stream?.Dispose();

        [Benchmark]
        [BenchmarkCategory(nameof(ProcessAdBlockBased), nameof(HostUtilities))]
        public async Task ProcessAdBlockBased()
            => await HostUtilities.ProcessAdBlockBased(new HashSet<string>(50_000),
                _stream!,
                BenchmarkTestData.Decoder);
    }

    [MemoryDiagnoser]
    [BenchmarkCategory(nameof(HostUtilities))]
    public class BenchmarkRemoveKnownBadHosts : BenchmarkStreamBase
    {
        [Benchmark]
        [BenchmarkCategory(nameof(RemoveKnownBadHosts), nameof(HostUtilities))]
        [ArgumentsSource(nameof(Source))]
        public void RemoveKnownBadHosts(HashSet<string> data)
            => HostUtilities.RemoveKnownBadHosts(BenchmarkTestData.Settings.KnownBadHosts, data);

#pragma warning disable CA1822 // Mark members as static
        public IEnumerable<HashSet<string>> Source()
#pragma warning restore CA1822 // Mark members as static
        {
            var stream = PrepareStream();
            var hostsBasedLines = new HashSet<string>(140_000);
            HostUtilities
                .ProcessHostsBased(hostsBasedLines,
                    stream,
                    BenchmarkTestData.Settings.HostsBased.SkipLinesBytes,
                    BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream = PrepareStream();
            var adBlockBasedLines = new HashSet<string>(50_000);
            HostUtilities.ProcessAdBlockBased(adBlockBasedLines,
                    stream,
                    BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream.Dispose();

            hostsBasedLines.UnionWith(adBlockBasedLines);
            yield return hostsBasedLines;
        }
    }
}
