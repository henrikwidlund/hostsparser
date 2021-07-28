using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace HostsParser.Benchmarks
{
    internal static class Program
    {
        private static void Main()
        {
            // BenchmarkRunner.Run(typeof(Program).Assembly, ManualConfig.Create(DefaultConfig.Instance)
            //     .WithOption(ConfigOptions.JoinSummary, true)
            //     .WithOption(ConfigOptions.DisableLogFile, true));
            // BenchmarkRunner.Run<BenchmarkCollectionUtilities>();
            BenchmarkRunner.Run<BenchmarkProgram>();
        }
    }

    [MemoryDiagnoser]
    public class BenchmarkProgram
    {
        [Benchmark]
        public async Task Program()
        {
            await HostsParser.Program.Main();
        }
    }
}