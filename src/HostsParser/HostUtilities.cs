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
    public static class HostUtilities
    {
        private static readonly Memory<char> Cache = new char[256];

        /// <summary>
        /// Reads the <paramref name="stream"/> and returns a collection based on the items in it.
        /// </summary>
        /// <param name="dnsHashSet">The <see cref="HashSet{T}"/> that results are added to.</param>
        /// <param name="stream">The <see cref="Stream"/> to process.</param>
        /// <param name="skipLines">The lines that should be excluded from the returned result.</param>
        /// <param name="sourcePrefixes">The <see cref="SourcePrefixes"/> with definitions on what should be removed from each row.</param>
        /// <param name="decoder">The <see cref="Decoder"/> used when converting the bytes in <paramref name="stream"/>.</param>
        public static Task ProcessHostsBased(HashSet<string> dnsHashSet,
            Stream stream,
            byte[][]? skipLines,
            in SourcePrefixes sourcePrefixes,
            Decoder decoder)
        {
            var pipeReader = PipeReader.Create(stream);
            return ReadPipeAsync(pipeReader, dnsHashSet, skipLines, sourcePrefixes, decoder);
        }

        /// <summary>
        /// Reads the <paramref name="stream"/> and returns a collection based on the items in it.
        /// </summary>
        /// <param name="dnsHashSet">The <see cref="HashSet{T}"/> that results are added to.</param>
        /// <param name="stream">The <see cref="Stream"/> to process.</param>
        /// <param name="decoder">The <see cref="Decoder"/> used when converting the bytes in <paramref name="stream"/>.</param>
        public static Task ProcessAdBlockBased(HashSet<string> dnsHashSet,
            Stream stream,
            Decoder decoder)
        {
            var pipeReader = PipeReader.Create(stream);
            return ReadPipeAsync(pipeReader, dnsHashSet, null, null, decoder);
        }

        /// <summary>
        /// Removes all sub domains to the entries in <paramref name="knownBadHosts"/> from the <paramref name="hosts"/>.
        /// </summary>
        /// <param name="knownBadHosts">Array of hosts used for removing sub domains.</param>
        /// <param name="hosts">The collection of hosts that sub domains should be removed from.</param>
        public static HashSet<string> RemoveKnownBadHosts(string[] knownBadHosts,
            HashSet<string> hosts)
        {
            var except = new List<string>(hosts.Count);

            foreach (var host in hosts)
            {
                var found = false;
                for (var j = 0; j < knownBadHosts.Length; j++)
                {
                    if (!IsSubDomainOf(host, knownBadHosts[j])) continue;
                    found = true;
                    break;
                }

                if (found)
                    except.Add(host);
            }

            hosts.ExceptWith(except);
            return hosts;
        }

        /// <summary>
        /// Checks if <paramref name="potentialSubDomain"/> is a sub domain of <paramref name="potentialDomain"/>.
        /// </summary>
        /// <param name="potentialSubDomain">The potential sub domain.</param>
        /// <param name="potentialDomain">The potential domain.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubDomainOf(in ReadOnlySpan<char> potentialSubDomain,
            in ReadOnlySpan<char> potentialDomain)
        {
            if (potentialDomain.Length < 1
                || potentialSubDomain.Length < potentialDomain.Length
                || !potentialSubDomain.EndsWith(potentialDomain)
                || potentialDomain.Equals(potentialSubDomain, StringComparison.Ordinal))
                return false;

            return potentialSubDomain[(potentialSubDomain.IndexOf(potentialDomain) - 1)..][0] == Constants.DotSign;
        }

        private static async Task ReadPipeAsync(PipeReader reader,
            ICollection<string> resultCollection,
            byte[][]? skipLines,
            SourcePrefixes? sourcePrefixes,
            Decoder decoder)
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

                    ProcessLine(buffer.Slice(0, position.Value), resultCollection, skipLines, sourcePrefixes, decoder);
                    buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                }
                while (position != null);

                reader.AdvanceTo(buffer.Start, buffer.End);

                if (!result.IsCompleted) continue;
                ProcessLastChunk(resultCollection, skipLines, sourcePrefixes, decoder, buffer);

                break;
            }

            await reader.CompleteAsync();
        }

        private static void ProcessLastChunk(ICollection<string> resultCollection,
            byte[][]? skipLines,
            in SourcePrefixes? sourcePrefixes,
            Decoder decoder,
            in ReadOnlySequence<byte> buffer)
        {
            if (buffer.IsEmpty) return;
            ProcessLine(buffer, resultCollection, skipLines, sourcePrefixes, decoder);
        }

        private static void ProcessLine(in ReadOnlySequence<byte> slice,
            ICollection<string> resultCollection,
            byte[][]? skipLines,
            in SourcePrefixes? sourcePrefixes,
            Decoder decoder)
        {
            if (skipLines == null)
                ProcessAdBlockBasedLine(slice, resultCollection, decoder);
            else
                ProcessHostsBasedLine(slice, resultCollection, skipLines, sourcePrefixes, decoder);
        }

        private static void ProcessHostsBasedLine(in ReadOnlySequence<byte> slice,
            ICollection<string> resultCollection,
            byte[][] skipLines,
            in SourcePrefixes? sourcePrefixes,
            Decoder decoder)
        {
            var realSlice = slice.IsSingleSegment
                ? slice.FirstSpan
                : slice.ToArray().AsSpan();
            if (realSlice.IsEmpty)
                return;

            if (realSlice[0] == Constants.HashSign)
                return;

            if (HostsBasedShouldSkipLine(realSlice, skipLines))
                return;

            realSlice = HandlePrefixes(realSlice, sourcePrefixes);
            HandleDelimiter(ref realSlice, Constants.HashSign);
            if (IsWhiteSpace(realSlice))
                return;

            decoder.GetChars(realSlice, Cache.Span, false);
            resultCollection.Add(Cache.Span[..realSlice.Length].Trim().ToString());
        }

        private static void ProcessAdBlockBasedLine(in ReadOnlySequence<byte> slice,
            ICollection<string> resultCollection,
            Decoder decoder)
        {
            var realSlice = slice.IsSingleSegment
                ? slice.FirstSpan
                : slice.ToArray().AsSpan();
            if (realSlice.IsEmpty)
                return;

            if (AdBlockBasedShouldSkipLine(realSlice))
                return;

            realSlice = HandlePipe(realSlice);
            HandleDelimiter(ref realSlice, Constants.HatSign);
            if (IsWhiteSpace(realSlice))
                return;

            decoder.GetChars(realSlice, Cache.Span, false);
            resultCollection.Add(Cache.Span[..realSlice.Length].ToString());
        }

        private static bool HostsBasedShouldSkipLine(in ReadOnlySpan<byte> bytes,
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
        private static bool AdBlockBasedShouldSkipLine(in ReadOnlySpan<byte> current)
            => current[0] != Constants.PipeSign;

        private static ReadOnlySpan<byte> TrimStart(in this ReadOnlySpan<byte> span)
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

        private static ReadOnlySpan<byte> HandlePipe(in ReadOnlySpan<byte> lineBytes)
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

        private static ReadOnlySpan<byte> HandlePrefixes(in ReadOnlySpan<byte> lineBytes,
            in SourcePrefixes? sourcePrefixes)
        {
            if (sourcePrefixes?.WwwPrefixBytes != null
                && lineBytes.StartsWith(sourcePrefixes.Value.WwwPrefixBytes))
                return lineBytes[sourcePrefixes.Value.WwwPrefixBytes.Length..];
            
            if (sourcePrefixes?.PrefixBytes != null 
                && lineBytes.StartsWith(sourcePrefixes.Value.PrefixBytes))
                return lineBytes[sourcePrefixes.Value.PrefixBytes.Length..];

            return lineBytes;
        }
    }
}
