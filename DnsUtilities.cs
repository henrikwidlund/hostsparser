using System;

namespace HostsParser
{
    internal static class DnsUtilities
    {
        internal static string ReplaceSource(ReadOnlySpan<char> item, in int length)
        {
            var item2 = item[length..];
            if (item2.Contains(Constants.HashSign))
                item2 = item2[..item2.IndexOf(Constants.HashSign)];
            item2 = item2.Trim();

            return item2.ToString();
        }

        internal static string ReplaceAdGuard(ReadOnlySpan<char> item)
        {
            if (item.StartsWith(Constants.PipeSign))
                item = item[(item.LastIndexOf(Constants.PipeSign) + 1)..];

            if (item.EndsWith(Constants.HatSign))
                item = item[..^1];

            return item.ToString();
        }
    }
}
