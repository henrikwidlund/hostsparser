// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HostsParser.IntegrationTests
{
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
            linesWithoutMultiPass.Except(linesWithMultiPass).Should().BeEmpty();
            linesWithMultiPass.Except(linesWithoutMultiPass).Should().BeEmpty();
        }
    }
}
