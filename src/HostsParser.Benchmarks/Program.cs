// Copyright Henrik Widlund
// GNU General Public License v3.0

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, DefaultConfig.Instance
        .WithOptions(ConfigOptions.DisableLogFile)
        .WithOptions(ConfigOptions.JoinSummary));
