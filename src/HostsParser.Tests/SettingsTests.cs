// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HostsParser.Tests
{
    public class SettingsTests
    {
        [Fact]
        public void Settings_Should_Be_Deserialized_From_AppSettings()
        {
            // Arrange
            var appSettings = File.ReadAllText("appsettings.json");

            // Act
            var settings = JsonSerializer.Deserialize<Settings>(appSettings);

            // Assert
            settings.Should().NotBeNull();
            settings!.AdBlockBased.Should().NotBeNull();
            settings.AdBlockBased.SkipLines.Should().BeNull();
            settings.AdBlockBased.SkipLinesBytes.Should().BeNull();
            settings.ExtraFiltering.Should().BeTrue();
            settings.HeaderLines.Should().ContainSingle();
            settings.HostsBased.Should().NotBeNull();
            settings.HostsBased.SkipLines.Should().NotBeNull();
            settings.HostsBased.SkipLines.Should().ContainSingle();
            settings.HostsBased.SkipLinesBytes.Should().NotBeNull();
            settings.HostsBased.SkipLinesBytes.Should().ContainSingle();
            settings.HostsBased.SourceUri.Should().NotBeNull();
            settings.KnownBadHosts.Should().NotBeNull();
            settings.KnownBadHosts.Should().ContainSingle();
        }
    }
}
