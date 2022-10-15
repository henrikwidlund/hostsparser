// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Text;
using System.Text.Json;

namespace HostsParser.Benchmarks;

internal static class BenchmarkTestData
{
    public static readonly byte[] HostsBasedTestBytes = File.ReadAllBytes("hostsbased.txt");
    public static readonly byte[] AdBlockBasedTestBytes = File.ReadAllBytes("adbockbased.txt");

    public static readonly Settings Settings =
        JsonSerializer.Deserialize<Settings>(File.ReadAllBytes("appsettings.json"))!;

    public static readonly Decoder Decoder = Encoding.UTF8.GetDecoder();
}
