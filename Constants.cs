// Copyright Henrik Widlund
// GNU General Public License v3.0

namespace HostsParser
{
    internal readonly ref struct Constants
    {
        internal const string PipeSign = "|";
        internal const string HatSign = "^";
        internal const char NewLine = '\n';
        internal const char DotSign = '.';
        internal const string DotSignString = ".";
        internal const char HashSign = '#';
        internal const string IpFilter = "0.0.0.0 ";
        internal static readonly int IpFilterLength = IpFilter.Length;
        internal const string LoopbackEntry = "0.0.0.0 0.0.0.0";
        internal const string WwwPrefix = "www.";
    }
}
