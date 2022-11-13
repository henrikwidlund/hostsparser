// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace HostsParser.Tests;

public class HostUtilitiesTests
{
    [Fact]
    public async Task ProcessHostsBased_Should_Only_Return_Dns_Entries()
    {
        // Arrange
        const string HostsSource = "#"
                                   + "\n"
                                   + "some bad line"
                                   + "\n"
                                   + "another bad line"
                                   + "\n"
                                   + "0.0.0.0 0.0.0.0"
                                   + "\n"
                                   + "# commented"
                                   + "\n"
                                   + "\n"
                                   + " #"
                                   + "\n"
                                   + "0.0.0.0  dns-a.com"
                                   + "\n"
                                   + "0.0.0.0 www.dns-a.com"
                                   + "\n"
                                   + "dns-a.com"
                                   + "\n"
                                   + " \t #"
                                   + "\n"
                                   + "0.0.0.0 dns-b.com"
                                   + "\n"
                                   + "0.0.0.0 dns-c.com #Comment";

        var skipLines = new[] { "some bad line", "another bad line", "0.0.0.0 0.0.0.0" }
            .Select(s => Encoding.UTF8.GetBytes(s))
            .ToArray();
        const string Prefix = "0.0.0.0 ";
        var expected = new HashSet<string> { "dns-a.com", "dns-b.com", "dns-c.com" };
        await using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await streamWriter.WriteAsync(HostsSource);
        await streamWriter.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var decoder = Encoding.UTF8.GetDecoder();
        var dnsCollection = new HashSet<string>();

        // Act
        await HostUtilities.ProcessHostsBased(dnsCollection, memoryStream, skipLines, new SourcePrefix(Prefix), decoder);

        // Assert
        dnsCollection.Should().NotBeEmpty();
        dnsCollection.Should().HaveSameCount(expected);
        dnsCollection.Should().OnlyContain(s => expected.Contains(s));
    }

    [Fact]
    public async Task ProcessAdBlockBased_Should_Only_Return_Dns_Entries()
    {
        // Arrange
        const string AdBlockSource = "!a comment"
                                     + "\n"
                                     + "!another comment"
                                     + "\n"
                                     + "||dns-a.com^"
                                     + "\n"
                                     + "||dns-b.com^"
                                     + "\n"
                                     + "|| "
                                     + "\n"
                                     + " \t"
                                     + "\n"
                                     + "ab"
                                     + "\n"
                                     + "||dns-c.com^ #Comment"
                                     + "\n"
                                     + "\n";

        var expected = new HashSet<string> { "dns-a.com", "dns-b.com", "dns-c.com" };
        await using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await streamWriter.WriteAsync(AdBlockSource);
        await streamWriter.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var decoder = Encoding.UTF8.GetDecoder();
        var dnsCollection = new HashSet<string>();

        // Act
        await HostUtilities.ProcessAdBlockBased(dnsCollection, memoryStream, decoder);

        // Assert
        dnsCollection.Should().NotBeEmpty();
        dnsCollection.Should().HaveSameCount(expected);
        dnsCollection.Should().OnlyContain(s => expected.Contains(s));
    }

    [Fact]
    public void RemoveKnownBadHosts_Should_Remove_All_SubDomain_Entries_Of_Known_Bad_Hosts()
    {
        // Arrange
        var knownBadHosts = new[] { "bad-dns.com", "another-bad-dns.co.com" };
        var dnsCollection = new HashSet<string>
        {
            "a.bad-dns.com",
            "bad-dns.com",
            "b.another-bad-dns.co.com",
            "dns-a.com",
            "dns-b.com",
            "dns-c.com"
        };
        var expected = new HashSet<string> { "bad-dns.com", "dns-a.com", "dns-b.com", "dns-c.com" };

        // Act
        dnsCollection = HostUtilities.RemoveKnownBadHosts(knownBadHosts, dnsCollection);

        // Assert
        dnsCollection.Should().NotBeEmpty();
        dnsCollection.Should().HaveSameCount(expected);
        dnsCollection.Should().OnlyContain(s => expected.Contains(s));
    }

    [Theory]
    [InlineData("subdomain.domain.com", "domain.com", true)]
    [InlineData("a.subdomain.domain.com", "domain.com", true)]
    [InlineData("subdomain.domain.co.com", "domain.co.com", true)]
    [InlineData("different.subdomain.com", "domain.com", false)]
    [InlineData("b.different.subdomain.com", "domain.com", false)]
    public void IsSubDomainOf_Should_Validate_SubDomain(string potentialSubDomain,
        string potentialDomain,
        bool expected)
    {
        // Arrange
        // Act
        var isSubDomainOf = HostUtilities.IsSubDomainOf(potentialSubDomain, potentialDomain);

        // Assert
        isSubDomainOf.Should().Be(expected);
    }
}
