// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HostsParser.Tests;

public sealed class SettingsTests
{
    [Test]
    public async Task Settings_Should_Be_Deserialized_From_AppSettings()
    {
        // Arrange
        await using var fileStream = File.OpenRead("appsettings.json");

        // Act
        var settings = await JsonSerializer.DeserializeAsync<Settings>(fileStream);

        // Assert
        await Assert.That(settings).IsNotNull();
        await Assert.That(settings!.ExtraFiltering).IsTrue();
        await Assert.That(settings.HeaderLines).HasSingleItem();
        await Assert.That(settings.Filters).IsNotNull();
        await Assert.That(settings.Filters.SkipLines).HasSingleItem();
        await Assert.That(settings.Filters.SkipLinesBytes).HasSingleItem();
        await Assert.That(settings.Filters.Sources).Count().IsEqualTo(2);

        await Assert.That(settings.Filters.Sources.Count(static item => item.Format == SourceFormat.Hosts)).IsEqualTo(1);
        var hostsSource = settings.Filters.Sources.First(static item => item.Format == SourceFormat.Hosts);
        await Assert.That(hostsSource.Uri).IsEqualTo(new Uri("https://hosts-based.uri"));
        await Assert.That(hostsSource.Prefix).IsEqualTo("0.0.0.0 ");
        await Assert.That(hostsSource.SourceAction).IsEqualTo(SourceAction.Combine);
        await Assert.That(hostsSource.SourcePrefix.PrefixBytes).IsEquivalentTo(Encoding.UTF8.GetBytes(hostsSource.Prefix));
        await Assert.That(hostsSource.SourcePrefix.WwwPrefixBytes).IsEquivalentTo(Encoding.UTF8.GetBytes(hostsSource.Prefix + "www."));

        await Assert.That(settings.Filters.Sources.Count(static item => item.Format == SourceFormat.AdBlock)).IsEqualTo(1);
        var adBlockSource = settings.Filters.Sources.First(static item => item.Format == SourceFormat.AdBlock);
        await Assert.That(adBlockSource.Uri).IsEqualTo(new Uri("https://adblock-based.uri"));
        await Assert.That(adBlockSource.Prefix).IsEmpty();
        await Assert.That(adBlockSource.SourceAction).IsEqualTo(SourceAction.ExternalCoverage);
        await Assert.That(adBlockSource.SourcePrefix.PrefixBytes).IsNull();
        await Assert.That(adBlockSource.SourcePrefix.WwwPrefixBytes).IsNull();

        await Assert.That(settings.KnownBadHosts).HasSingleItem();
    }
}
