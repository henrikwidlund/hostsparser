// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Text;

namespace HostsParser.Benchmarks;

public abstract class BenchmarkStreamBase
{
    protected static MemoryStream PrepareStream()
    {
        var stream = new MemoryStream();

        using var sw = new BinaryWriter(stream, Encoding.UTF8, true);
        sw.Write(BenchmarkTestData.HostsBasedTestBytes);
        sw.Write(BenchmarkTestData.AdBlockBasedTestBytes);
        sw.Flush();

        stream.Seek(0, SeekOrigin.Begin);

        return stream;
    }
}
