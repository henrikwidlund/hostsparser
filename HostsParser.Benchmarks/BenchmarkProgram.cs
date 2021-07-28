using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace HostsParser.Benchmarks
{
    [MemoryDiagnoser]
    public class BenchmarkProgram
    {
        [Benchmark]
        public async Task Program() => await HostsParser.Program.Main();
    }
}