// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HostsParser
{
    internal static class HostUtilities
    {
        private static async Task ReadPipeAsync(PipeReader reader,
            ICollection<string> resultCollection,
            byte[][]? skipLines,
            Decoder decoder)
        {
            var cache = ArrayPool<char>.Shared.Rent(256);
            try
            {
                while (true)
                {
                    var result = await reader.ReadAsync();

                    var buffer = result.Buffer;
                    SequencePosition? position;

                    do 
                    {
                        position = buffer.PositionOf(Constants.NewLine);

                        if (position == null) continue;
                    
                        ProcessLine(buffer.Slice(0, position.Value), ref resultCollection, skipLines, decoder, cache);
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                    while (position != null);

                    reader.AdvanceTo(buffer.Start, buffer.End);

                    if (result.IsCompleted)
                        break;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(cache);
            }

            await reader.CompleteAsync();
        }

        private static void ProcessLine(in ReadOnlySequence<byte> slice,
            ref ICollection<string> resultCollection,
            byte[][]? skipLines,
            Decoder decoder, char[] cache)
        {
            if (skipLines == null)
                ProcessAdGuardLine(slice, ref resultCollection, decoder, cache);
            else
                ProcessSourceLine(slice, ref resultCollection, skipLines, decoder, cache);
        }
        
        private static void ProcessSourceLine(in ReadOnlySequence<byte> slice,
            ref ICollection<string> resultCollection,
            byte[][] skipLines,
            Decoder decoder,
            char[] cache)
        {
            var realSlice = slice.IsSingleSegment
                ? slice.FirstSpan
                : slice.ToArray().AsSpan();
            if (realSlice.IsEmpty)
                return;

            if (realSlice[0] == Constants.HashSign)
                return;
            
            if (SourceShouldSkipLine(realSlice, skipLines))
                return;
            
            var tmp = HandleWwwPrefix(realSlice);
            HandleDelimiter(ref tmp, Constants.HashSign);
            if (IsWhiteSpace(tmp))
                return;

            decoder.GetChars(tmp, cache, false);
            var c = string.Create(tmp.Length, cache[..tmp.Length], (a, b) =>
            {
                for (var i = 0; i < b.Length; i++)
                {
                    a[i] = b[i];
                }
            });

            resultCollection.Add(c);
        }
        
        private static void ProcessAdGuardLine(in ReadOnlySequence<byte> slice,
            ref ICollection<string> resultCollection,
            Decoder decoder,
            char[] cache)
        {
            var realSlice = slice.IsSingleSegment
                ? slice.FirstSpan
                : slice.ToArray().AsSpan();
            if (realSlice.IsEmpty)
                return;

            if (AdGuardShouldSkipLine(realSlice))
                return;

            var tmp = HandlePipe(realSlice);
            HandleDelimiter(ref tmp, Constants.HatSign);
            if (IsWhiteSpace(tmp))
                return;

            decoder.GetChars(tmp, cache, false);

            var c = string.Create(tmp.Length, cache[..tmp.Length], (a, b) =>
            {
                for (var i = 0; i < b.Length; i++)
                {
                    a[i] = b[i];
                }
            });

            resultCollection.Add(c);
        }
        
        internal static async Task<List<string>> ProcessSource(Stream bytes,
            byte[][] skipLines,
            Decoder decoder)
        {
            var strings = new List<string>();
            var pipeReader = PipeReader.Create(bytes);
            await ReadPipeAsync(pipeReader, strings, skipLines, decoder);
            return new List<string>(new HashSet<string>(strings));
        }
        
        internal static async Task<HashSet<string>> ProcessAdGuard(Stream bytes,
            Decoder decoder)
        {
            var strings = new HashSet<string>();
            var pipeReader = PipeReader.Create(bytes);
            await ReadPipeAsync(pipeReader, strings, null, decoder);
            return strings;
        }
        
        internal static List<string> RemoveKnownBadHosts(string[] knownBadHosts,
            List<string> hosts)
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
        internal static bool IsSubDomainOf(ReadOnlySpan<char> potentialSubDomain,
            ReadOnlySpan<char> potentialDomain)
        {
            if (potentialDomain.Length < 1
                || potentialSubDomain.Length < potentialDomain.Length
                || !potentialSubDomain.EndsWith(potentialDomain)
                || potentialDomain.Equals(potentialSubDomain, StringComparison.Ordinal))
                return false;

            return potentialSubDomain[(potentialSubDomain.IndexOf(potentialDomain) - 1)..][0] == Constants.DotSign;
        }

        private static bool SourceShouldSkipLine(in ReadOnlySpan<byte> bytes,
            byte[][] skipLines)
        {
            if (TrimStart(bytes)[0] == Constants.HashSign)
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
            => current[0] != Constants.PipeSign;

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
        
        private static bool IsWhiteSpace(in ReadOnlySpan<byte> span)
        {
            var start = 0;
            for (; start < span.Length; start++)
            {
                if (span[start] != Constants.Space
                    && span[start] != Constants.Tab)
                    return false;
            }

            return true;
        }

        private static ReadOnlySpan<byte> HandlePipe(ReadOnlySpan<byte> lineBytes)
        {
            var lastPipe = lineBytes.LastIndexOf(Constants.PipeSign);
            if (lastPipe > -1)
                return lineBytes[(lastPipe == 0 ? 1 : lastPipe + 1)..];
            return lineBytes;
        }

        private static void HandleDelimiter(ref ReadOnlySpan<byte> lineChars,
            in byte delimiter)
        {
            var delimiterIndex = lineChars.IndexOf(delimiter);
            if (delimiterIndex > 0)
                lineChars = lineChars[..delimiterIndex];
        }

        private static ReadOnlySpan<byte> HandleWwwPrefix(ReadOnlySpan<byte> lineBytes)
        {
            if (lineBytes.StartsWith(Constants.NxIpWithWww))
                return lineBytes[Constants.NxIpWithWww.Length..];
            
            if (lineBytes.StartsWith(Constants.NxIpWithSpace))
                return lineBytes[Constants.NxIpWithSpace.Length..];

            return lineBytes;
        }
    }
}
