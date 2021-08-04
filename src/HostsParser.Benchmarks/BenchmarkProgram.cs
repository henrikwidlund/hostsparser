// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkProgram
    {
        [Benchmark]
        [BenchmarkCategory(nameof(Program), nameof(BenchmarkProgram))]
        public async Task Program() => await HostsParser.Program.Main();
    }
}