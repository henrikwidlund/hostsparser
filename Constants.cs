namespace HostsParser
{
    internal readonly struct Constants
    {
        internal const string PipeSign = "|";
        internal const string HatSign = "^";
        internal const char NewLine = '\n';
        internal const char DotSign = '.';
        internal const char ExclamationMark = '!';
        internal const char AtSign = '@';
        internal const char HashSign = '#';
        internal const string IpFilter = "0.0.0.0 ";
        internal static readonly int IpFilterLength = IpFilter.Length;
        internal const string LoopbackEntry = "0.0.0.0 0.0.0.0";
        internal const string WwwPrefix = "www.";
        internal const string UseExternalLastRun = "USE_EXTERNAL_LAST_RUN";
        internal const string ModifiedFile = "MODIFIED";
    }
}
