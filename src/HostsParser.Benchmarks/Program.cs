// Copyright Henrik Widlund
// GNU General Public License v3.0

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace HostsParser.Benchmarks;

file static class Program
{
    private static void Main(string[] args) =>
        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, DefaultConfig.Instance
                .WithOptions(ConfigOptions.DisableLogFile)
                .WithOptions(ConfigOptions.JoinSummary));
}
