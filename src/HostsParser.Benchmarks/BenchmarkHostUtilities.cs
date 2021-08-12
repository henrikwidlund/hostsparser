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
        public async Task<HashSet<string>> ProcessHostsBased()
        {
            return await HostUtilities.ProcessHostsBased(_stream!,
                BenchmarkTestData.Settings.HostsBased.SkipLinesBytes,
                BenchmarkTestData.Decoder);
        }
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
        public async Task<HashSet<string>> ProcessAdBlockBased()
            => await HostUtilities.ProcessAdBlockBased(_stream!, BenchmarkTestData.Decoder);
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
            var hostsBasedLines = HostUtilities
                .ProcessHostsBased(stream, BenchmarkTestData.Settings.HostsBased.SkipLinesBytes,
                    BenchmarkTestData.Decoder).GetAwaiter().GetResult();

            stream = PrepareStream();
            var adBlockBasedLines = HostUtilities.ProcessAdBlockBased(stream, BenchmarkTestData.Decoder)
                .GetAwaiter().GetResult();

            stream.Dispose();

            hostsBasedLines.UnionWith(adBlockBasedLines);
            yield return hostsBasedLines;
        }
    }
}
