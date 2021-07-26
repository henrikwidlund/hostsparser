// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Collections.Generic;
using System.Text;

namespace HostsParser
{
    internal static class HostUtilities
    {
        internal static List<string> ProcessSource(in ReadOnlySpan<byte> bytes,
            string[] skipLines,
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

                if (StartsWithHash(current))
                    continue;

                decoder.GetChars(current, chars, false);
                var lineChars = chars[..current.Length];
                if (ShouldAddSource(lineChars, skipLines))
                {
                    lineChars = lineChars[Constants.IpFilterLength..];
                    HandleHash(ref lineChars);

                    lineChars = lineChars.Trim();
                    strings.Add(lineChars.ToString());
                }
            }
        
            return strings;
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
                
                if (ShouldAddAdGuard(current))
                {
                    HandlePipe(ref current);
                    HandleHat(ref current);
                    
                    decoder.GetChars(current, chars, false);
                    var lineChars = chars[..current.Length];
                    strings.Add(lineChars.Trim().ToString());
                }
            }

            return strings;
        }

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

        private static bool ShouldAddSource(in ReadOnlySpan<char> current,
            string[] skipLines)
        {
            if (current.Length == 1
                && current[0] == Constants.NewLine)
                return false;
            
            for (var i = 0; i < skipLines.Length; i++)
            {
                if (skipLines[i].AsSpan().SequenceEqual(current))
                    return false;
            }

            return true;
        }
        
        private static bool ShouldAddAdGuard(in ReadOnlySpan<byte> current)
        {
            if (current.Length == 1
                && current[0] == Constants.NewLine)
                return false;

            return current[0] == Constants.PipeSign;
        }
        
        private static bool StartsWithHash(in ReadOnlySpan<byte> bytes)
        {
            return bytes.TrimStart()[0] == Constants.HashSign;
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
        
        private static void HandleHash(ref Span<char> lineChars)
        {
            var hashIndex = lineChars.IndexOf(Constants.HashSignChar);
            if (hashIndex > 0)
                lineChars = lineChars[..hashIndex];
        }
        
        private static void HandleHat(ref ReadOnlySpan<byte> lineBytes)
        {
            var hatIndex = lineBytes.IndexOf(Constants.HatSign);
            if (hatIndex > 0)
                lineBytes = lineBytes[..hatIndex];
        }
    }
}
