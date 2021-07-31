using System.IO;
using System.Text;

namespace HostsParser.Benchmarks
{
    public abstract class BenchmarkStreamBase
    {
        protected static Stream PrepareStream()
        {
            var stream = new MemoryStream();

            using var sw = new BinaryWriter(stream, Encoding.UTF8, true);
            sw.Write(BenchmarkTestData.SourceTestBytes);
            sw.Write(BenchmarkTestData.AdGuardTestBytes);
            sw.Flush();

            stream.Seek(0, SeekOrigin.Begin);

            return stream;
        }
    }
}