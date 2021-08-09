// Copyright Henrik Widlund
// GNU General Public License v3.0

using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace HostsParser.Tests
{
    public class ProcessingUtilitiesTests
    {
        [Fact]
        public void ProcessCombined_Should_Remove_Redundant_Entries()
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

            var adBlockBasedLines = new HashSet<string> { "first.com" };
            var filteredCache = new HashSet<string>();
            var expected = new List<string> { "dns.com", "second.co.jp" };

            // Act
            sortedDnsList = ProcessingUtilities.ProcessCombined(sortedDnsList,
                adBlockBasedLines,
                filteredCache);

            // Assert
            sortedDnsList.Should().NotBeEmpty();
            sortedDnsList.Should().HaveSameCount(expected);
            sortedDnsList.Should().OnlyContain(s => expected.Contains(s));
        }
        
        [Fact]
        public void ProcessWithExtraFiltering_Should_Remove_All_Matching_SubDomains()
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

            var adBlockBasedLines = new HashSet<string> { "first.com", "second.co.jp" };
            var filteredCache = new HashSet<string>();
            var expected = new List<string> { "dns.com", "first.com", "second.co.jp" };

            // Act
            sortedDnsList = ProcessingUtilities.ProcessWithExtraFiltering(sortedDnsList,
                adBlockBasedLines,
                filteredCache);

            // Assert
            sortedDnsList.Should().NotBeEmpty();
            sortedDnsList.Should().HaveSameCount(expected);
            sortedDnsList.Should().OnlyContain(s => expected.Contains(s));
        }
    }
}
