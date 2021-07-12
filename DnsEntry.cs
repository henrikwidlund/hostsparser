namespace HostsParser
{
    internal readonly struct DnsEntry
    {
        public DnsEntry(string raw)
        {
            Prefixed = string.Concat(Constants.DotSign, raw);
            UnPrefixed = raw;
        }
    
        public readonly string Prefixed;
        public readonly string UnPrefixed;
    }
}