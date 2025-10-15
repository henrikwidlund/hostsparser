// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Threading.Tasks;
using TUnit.Assertions.Enums;

namespace HostsParser.Tests;

public sealed class CollectionUtilitiesTests
{
    [Test]
    public async Task SortDnsList_Should_Be_Ordered()
    {
        // Arrange
        var dnsCollection = new List<string>
        {
            "dns.com",
            "first.com",
            "a.first.com",
            "bb.first.com",
            "second.co.jp",
            "2.second.co.jp",
            "1.2.second.co.jp"
        };

        var shuffled = new List<string>
        {
            "bb.first.com",
            "2.second.co.jp",
            "dns.com",
            "a.first.com",
            "second.co.jp",
            "first.com",
            "1.2.second.co.jp"
        };

        // Act
        var sortDnsList = CollectionUtilities.SortDnsList(shuffled);

        // Assert
        await Assert.That(sortDnsList).IsNotEmpty()
            .And.IsEquivalentTo(dnsCollection, CollectionOrdering.Matching);
    }

    [Test]
    public async Task FilterGrouped_Should_Not_Contain_SubDomains()
    {
        // Arrange
        var dnsCollection = new HashSet<string>
        {
            "dns.com",
            "first.com",
            "a.first.com",
            "bb.first.com",
            "second.co.jp",
            "2.second.co.jp",
            "1.2.second.co.jp",
            "1-2.second.co.jp"
        };

        var expected = new HashSet<string> { "dns.com", "first.com", "second.co.jp" };

        // Act
        CollectionUtilities.FilterGrouped(dnsCollection);

        // Assert
        await Assert.That<IEnumerable<string>>(dnsCollection).HasCount().EqualTo(expected.Count)
            .And.ContainsOnly(s => expected.Contains(s));
    }
}
