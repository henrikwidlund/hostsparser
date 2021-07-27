using System.IO;
using System.Text;
using System.Text.Json;

namespace HostsParser.Benchmarks
{
    internal static class BenchmarkTestData
    {
        public static readonly byte[] SourceTestBytes = File.ReadAllBytes("sourcehosts.txt");
        public static readonly byte[] AdGuardTestBytes = File.ReadAllBytes("adguardhosts.txt");

        public static readonly Settings Settings =
            JsonSerializer.Deserialize<Settings>(File.ReadAllBytes("appsettings.json"));

        public static readonly Decoder Decoder = Encoding.UTF8.GetDecoder();
    }
}