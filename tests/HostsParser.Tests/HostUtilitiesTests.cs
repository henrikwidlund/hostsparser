// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace HostsParser.Tests;

public sealed class HostUtilitiesTests
{
    private static readonly byte[][] SkipLines = ["some bad line"u8.ToArray(), "another bad line"u8.ToArray(), "0.0.0.0 0.0.0.0"u8.ToArray()];

    private static readonly byte[][] OverrideAllowedHosts = ["b-cdn.net"u8.ToArray()];

    [Test]
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
                                   + "0.0.0.0 dns-c.com #Comment"
                                   + "\n"
                                   + "0.0.0.0 www.b-cdn.net";

        const string Prefix = "0.0.0.0 ";
        var expected = new HashSet<string> { "dns-a.com", "dns-b.com", "dns-c.com", "www.b-cdn.net" };
        await using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await streamWriter.WriteAsync(HostsSource);
        await streamWriter.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var decoder = Encoding.UTF8.GetDecoder();
        var dnsCollection = new HashSet<string>();

        // Act
        await HostUtilities.ProcessHostsBased(dnsCollection,
            memoryStream,
            SkipLines,
            OverrideAllowedHosts,
            new SourcePrefix(Prefix),
            decoder);

        // Assert
        await Assert.That(dnsCollection).HasCount().EqualTo(expected.Count).And
            .ContainsOnly(s => expected.Contains(s));
    }

    [Test]
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
                                     + "@@||dns-d.com^"
                                     + "\n"
                                     + "||www.b-cdn.net^"
                                     + "\n"
                                     + "\n"
                                     + "@@||explicit.com^|"
                                     + "\n"
                                     + "\n";

        var expectedBlocked = new HashSet<string> { "dns-a.com", "dns-b.com", "dns-c.com", "www.b-cdn.net" };
        var expectedAllowed = new HashSet<string> { "||dns-d.com", "||explicit.com^|" };
        await using var memoryStream = new MemoryStream();
        await using var streamWriter = new StreamWriter(memoryStream);
        await streamWriter.WriteAsync(AdBlockSource);
        await streamWriter.FlushAsync();
        memoryStream.Seek(0, SeekOrigin.Begin);

        var decoder = Encoding.UTF8.GetDecoder();
        var dnsCollection = new HashSet<string>();
        var allowedOverrides = new HashSet<string>();

        // Act
        await HostUtilities.ProcessAdBlockBased(dnsCollection, allowedOverrides, OverrideAllowedHosts, memoryStream, decoder);

        // Assert
        await Assert.That(dnsCollection).HasCount().EqualTo(expectedBlocked.Count).And
            .ContainsOnly(s => expectedBlocked.Contains(s));

        await Assert.That(allowedOverrides).HasCount().EqualTo(expectedAllowed.Count).And
            .ContainsOnly(s => expectedAllowed.Contains(s));
    }

    [Test]
    public async Task RemoveKnownBadHosts_Should_Remove_All_SubDomain_Entries_Of_Known_Bad_Hosts()
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
        await Assert.That(dnsCollection).HasCount().EqualTo(expected.Count).And
            .ContainsOnly(s => expected.Contains(s));
    }

    [Test]
    [Arguments("subdomain.domain.com", "domain.com", true)]
    [Arguments("a.subdomain.domain.com", "domain.com", true)]
    [Arguments("subdomain.domain.co.com", "domain.co.com", true)]
    [Arguments("different.subdomain.com", "domain.com", false)]
    [Arguments("b.different.subdomain.com", "domain.com", false)]
    public async Task IsSubDomainOf_Should_Validate_SubDomain(string potentialSubDomain,
        string potentialDomain,
        bool expected)
    {
        // Arrange
        // Act
        var isSubDomainOf = HostUtilities.IsSubDomainOf(potentialSubDomain, potentialDomain);

        // Assert
        await Assert.That(isSubDomainOf).IsEqualTo(expected);
    }
}
