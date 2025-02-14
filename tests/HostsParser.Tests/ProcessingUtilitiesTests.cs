// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using System.Threading.Tasks;

namespace HostsParser.Tests;

public sealed class ProcessingUtilitiesTests
{
    [Test]
    public async Task ProcessCombined_Should_Remove_Redundant_Entries()
    {
        // Arrange
        var sortedDnsList = new List<string>
        {
            "dns.com",
            "first.com",
            "a.first.com",
            "bb.first.com",
            "second.co.jp",
            "2.second.co.jp",
            "1.2.second.co.jp"
        };

        var externalCoverageLines = new HashSet<string> { "first.com" };
        var filteredCache = new HashSet<string>();
        var expected = new List<string> { "dns.com", "second.co.jp" };

        // Act
        sortedDnsList = ProcessingUtilities.ProcessCombined(sortedDnsList,
            externalCoverageLines,
            filteredCache);

        // Assert
        await Assert.That(sortedDnsList).HasCount().EqualTo(expected.Count)
            .And.ContainsOnly(s => expected.Contains(s));
    }

    [Test]
    public async Task ProcessCombinedWithMultipleRounds_Should_Remove_Redundant_Entries()
    {
        // Arrange
        var sortedDnsList = new List<string>
        {
            "dns.com",
            "first.com",
            "a.first.com",
            "bb.first.com",
            "second.co.jp",
            "2.second.co.jp",
            "1.2.second.co.jp"
        };

        var externalCoverageLines = new HashSet<string> { "first.com" };
        var filteredCache = new HashSet<string>();
        var expected = new List<string> { "dns.com", "second.co.jp" };

        // Act
        sortedDnsList = ProcessingUtilities.ProcessCombinedWithMultipleRounds(sortedDnsList,
            externalCoverageLines,
            filteredCache);

        // Assert
        await Assert.That(sortedDnsList).HasCount().EqualTo(expected.Count)
            .And.ContainsOnly(s => expected.Contains(s));
    }

    [Test]
    public async Task ProcessWithExtraFiltering_Should_Remove_All_Matching_SubDomains()
    {
        // Arrange
        var sortedDnsList = new List<string>
        {
            "dns.com",
            "first.com",
            "a.first.com",
            "bb.first.com",
            "second.co.jp",
            "2.second.co.jp",
            "1.2.second.co.jp"
        };

        var externalCoverageLines = new HashSet<string> { "first.com", "second.co.jp" };
        var filteredCache = new HashSet<string>();
        var expected = new List<string> { "dns.com", "first.com", "second.co.jp" };

        // Act
        sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList,
            externalCoverageLines,
            filteredCache);

        // Assert
        await Assert.That(sortedDnsList).HasCount().EqualTo(expected.Count)
            .And.ContainsOnly(s => expected.Contains(s));
    }
}
