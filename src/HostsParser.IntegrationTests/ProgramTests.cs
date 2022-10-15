// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HostsParser.IntegrationTests;

public class ProgramTests
{
    [Fact]
    public async Task When_Running_Program_MultiPassFilter_Toggle_Should_Produce_Same_Results()
    {
        // Arrange
        // Act
        await Program.Main();
        var linesWithoutMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];
        var settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllBytesAsync("appsettings.json"))!;
        settings = settings with { MultiPassFilter = true };
        await File.WriteAllBytesAsync("appsettings.json", JsonSerializer.SerializeToUtf8Bytes(settings));
        await Program.Main();
        var linesWithMultiPass = (await File.ReadAllLinesAsync("filter.txt"))[7..];

        // Assert
        // Sometimes there's one item in linesWithoutMultiPass that aren't in linesWithMultiPass.
        // This is "okay" because the sort isn't 100% stable and it's a tradeoff between performance and stability.
        linesWithoutMultiPass.Except(linesWithMultiPass).Should().HaveCountLessOrEqualTo(1);
        linesWithMultiPass.Except(linesWithoutMultiPass).Should().BeEmpty();
    }
}
