// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.IO;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace HostsParser.Tests;

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
        settings!.ExtraFiltering.Should().BeTrue();
        settings.HeaderLines.Should().ContainSingle();
        settings.Filters.Should().NotBeNull();
        settings.Filters.SkipLines.Should().NotBeNull();
        settings.Filters.SkipLines.Should().ContainSingle();
        settings.Filters.SkipLinesBytes.Should().NotBeNull();
        settings.Filters.SkipLinesBytes.Should().ContainSingle();
        settings.Filters.Sources.Should().NotBeNullOrEmpty();
        settings.Filters.Sources.Should().HaveCount(2);

        var hostsSource = settings.Filters.Sources.Should()
            .ContainSingle(item => item.Format == SourceFormat.Hosts).Subject;
        hostsSource.Uri.Should().Be("https://hosts-based.uri");
        hostsSource.Prefix.Should().Be("0.0.0.0 ");
        hostsSource.SourceAction.Should().Be(SourceAction.Combine);
        hostsSource.SourcePrefixes.PrefixBytes.Should().NotBeNull();
        hostsSource.SourcePrefixes.PrefixBytes.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(hostsSource.Prefix));
        hostsSource.SourcePrefixes.WwwPrefixBytes.Should().BeEquivalentTo(Encoding.UTF8.GetBytes(hostsSource.Prefix + "www."));

        var adBlockSource = settings.Filters.Sources.Should()
            .ContainSingle(item => item.Format == SourceFormat.AdBlock).Subject;
        adBlockSource.Uri.Should().Be("https://adblock-based.uri");
        adBlockSource.Prefix.Should().BeEmpty();
        adBlockSource.SourceAction.Should().Be(SourceAction.ExternalCoverage);
        adBlockSource.SourcePrefixes.PrefixBytes.Should().BeNull();
        adBlockSource.SourcePrefixes.PrefixBytes.Should().BeNull();

        settings.KnownBadHosts.Should().NotBeNull();
        settings.KnownBadHosts.Should().ContainSingle();
    }
}
