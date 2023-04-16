// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace HostsParser.Tests;

public sealed class CollectionUtilitiesTests
{
    [Fact]
    public void SortDnsList_Should_Be_Ordered()
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
        sortDnsList.Should().NotBeNullOrEmpty();
        sortDnsList.Should().ContainInOrder(dnsCollection);
    }

    [Fact]
    public void FilterGrouped_Should_Not_Contain_SubDomains()
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
        dnsCollection.Should().NotBeNullOrEmpty();
        dnsCollection.Should().HaveSameCount(expected);
        dnsCollection.Should().OnlyContain(s => expected.Contains(s));
    }
}
