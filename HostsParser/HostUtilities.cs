// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace HostsParser
{
    internal static class HostUtilities
    {
        internal static List<string> ProcessSource(in ReadOnlySpan<byte> bytes,
            byte[][] skipLines,
            Decoder decoder)
        {
            var strings = new List<string>();
            var read = 0;
            Span<char> chars = stackalloc char[256];
            while (read < bytes.Length)
            {
                var current = bytes[read..];
                if (!HandleStartsWithNewLine(ref current,
                    ref read,
                    out var index))
                    continue;
                
                current = current[..index];
                read += current.Length;

                if (SourceShouldSkipLine(current, skipLines))
                    continue;

                HandleWwwPrefix(ref current);
                HandleDelimiter(ref current, Constants.HashSign);
                
                decoder.GetChars(current, chars, false);
                var lineChars = chars[..current.Length];
                strings.Add(lineChars.Trim().ToString());
            }

            return new List<string>(new HashSet<string>(strings));
        }
        
        internal static List<string> ProcessAdGuard(in ReadOnlySpan<byte> bytes,
            Decoder decoder)
        {
            var strings = new List<string>();
            var read = 0;
            Span<char> chars = stackalloc char[256];
            while (read < bytes.Length)
            {
                var current = bytes[read..];
                
                if (!HandleStartsWithNewLine(ref current,
                    ref read,
                    out var index))
                    continue;
        
                current = current[..index];
                read += current.Length;

                if (AdGuardShouldSkipLine(current))
                    continue;
                
                HandlePipe(ref current);
                HandleDelimiter(ref current, Constants.HatSign);

                decoder.GetChars(current, chars, false);
                var lineChars = chars[..current.Length];
                strings.Add(lineChars.Trim().ToString());
            }

            return strings;
        }
        
        internal static List<string> RemoveKnownBadHosts(string[] knownBadHosts, List<string> hosts)
        {
            var except = new List<string>(hosts.Count);
            for (var i = 0; i < hosts.Count; i++)
            {
                var host = hosts[i];
                var found = false;
                for (var j = 0; j < knownBadHosts.Length; j++)
                {
                    if (!IsSubDomainOf(host, knownBadHosts[j])) continue;
                    found = true;
                    break;
                }
                
                if(!found)
                    except.Add(host);
            }
            
            return except;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsSubDomainOf(ReadOnlySpan<char> potentialSubDomain, ReadOnlySpan<char> potentialDomain)
        {
            if (potentialDomain.Length < 1
                || potentialSubDomain.Length < potentialDomain.Length
                || !potentialSubDomain.EndsWith(potentialDomain)
                || potentialDomain.Equals(potentialSubDomain, StringComparison.Ordinal))
                return false;

            return potentialSubDomain[(potentialSubDomain.IndexOf(potentialDomain) - 1)..][0] == Constants.DotSign;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HandleStartsWithNewLine(ref ReadOnlySpan<byte> bytes,
            ref int read,
            out int index)
        {
            if (bytes.Length < 1)
            {
                index = 0;
                return false;
            }
                
            index = bytes.IndexOf(Constants.NewLine);
            while (index == 0)
            {
                bytes = bytes[1..];
                index = bytes.IndexOf(Constants.NewLine);
                ++read;
            }
            
            return bytes.Length >= 1;
        }
        
        private static bool SourceShouldSkipLine(in ReadOnlySpan<byte> bytes, byte[][] skipLines)
        {
            if (bytes.TrimStart()[0] == Constants.HashSign)
                return true;
            
            for (var i = 0; i < skipLines.Length; i++)
            {
                if (bytes.SequenceEqual(skipLines[i]))
                    return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool AdGuardShouldSkipLine(in ReadOnlySpan<byte> current)
        {
            return current[0] != Constants.PipeSign;
        }

        private static ReadOnlySpan<byte> TrimStart(this ReadOnlySpan<byte> span)
        {
            var start = 0;
            for (; start < span.Length; start++)
            {
                if (span[start] != Constants.Space
                    && span[start] != Constants.Tab)
                    break;
            }

            return span[start..];
        }

        private static void HandlePipe(ref ReadOnlySpan<byte> lineBytes)
        {
            var lastPipe = lineBytes.LastIndexOf(Constants.PipeSign);
            if (lastPipe > -1)
                lineBytes = lineBytes[(lastPipe == 0 ? 1 : lastPipe + 1)..];
        }

        private static void HandleDelimiter(ref ReadOnlySpan<byte> lineChars,
            in byte delimiter)
        {
            var delimiterIndex = lineChars.IndexOf(delimiter);
            if (delimiterIndex > 0)
                lineChars = lineChars[..delimiterIndex];
        }

        private static void HandleWwwPrefix(ref ReadOnlySpan<byte> lineBytes)
        {
            if (lineBytes.StartsWith(Constants.NxIpWithWww))
                lineBytes = lineBytes[Constants.NxIpWithWww.Length..];
            else if (lineBytes.StartsWith(Constants.NxIpWithSpace))
                lineBytes = lineBytes[Constants.NxIpWithSpace.Length..];
        }
    }
}
