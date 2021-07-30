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
        static async Task ReadPipeAsync(PipeReader reader,
            ICollection<string> stuff,
            byte[][]? skipLines,
            bool isSource, Decoder decoder)
        {
            while (true)
            {
                ReadResult result = await reader.ReadAsync();

                ReadOnlySequence<byte> buffer = result.Buffer;
                SequencePosition? position;

                do 
                {
                    // Look for a EOL in the buffer
                    position = buffer.PositionOf(Constants.NewLine);

                    if (position != null)
                    {
                        // Process the line
                        if (isSource)
                            ProcessSourceLine(buffer.Slice(0, position.Value), ref stuff, skipLines!, decoder);
                        else
                            ProcessAdGuardLine(buffer.Slice(0, position.Value), ref stuff, decoder);
                
                        // Skip the line + the \n character (basically position)
                        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
                    }
                }
                while (position != null);

                // Tell the PipeReader how much of the buffer we have consumed
                reader.AdvanceTo(buffer.Start, buffer.End);

                // Stop reading if there's no more data coming
                if (result.IsCompleted)
                {
                    break;
                }
            }

            // Mark the PipeReader as complete
            await reader.CompleteAsync();
        }

        private static void ProcessSourceLine(ReadOnlySequence<byte> slice, ref ICollection<string> stuff,
            byte[][] skipLines, Decoder decoder)
        {
            var realSlice = slice.IsSingleSegment ? slice.FirstSpan : slice.ToArray().AsSpan();
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
            
            // Span<char> c = stackalloc char[256];
            decoder.GetChars(tmp, Cache.Span, false);
            stuff.Add(Cache.Span[..tmp.Length].ToString());
        }
        
        private static void ProcessAdGuardLine(ReadOnlySequence<byte> slice, ref ICollection<string> stuff,
            Decoder decoder)
        {
            var realSlice = slice.IsSingleSegment ? slice.FirstSpan : slice.ToArray().AsSpan();
            if (realSlice.IsEmpty)
                return;

            if (AdGuardShouldSkipLine(realSlice))
                return;

            var tmp = HandlePipe(realSlice);
            HandleDelimiter(ref tmp, Constants.HatSign);
            if (IsWhiteSpace(tmp))
                return;
            
            // Span<char> c = stackalloc char[256];
            decoder.GetChars(tmp, Cache.Span, false);
            stuff.Add(Cache.Span[..tmp.Length].ToString());
        }

        // private static Memory<char>? m;

        private static readonly Memory<char> Cache = new char[256];
        // private static Span<char> Get()
        // {
        //     return new char[256];
        // }

        internal static async Task<List<string>> ProcessSource(Stream bytes,
            byte[][] skipLines,
            Decoder decoder)
        {
            var strings = new List<string>();
            var pipeReader = PipeReader.Create(bytes);
            await ReadPipeAsync(pipeReader, strings, skipLines, true, decoder);
            // var chars = ArrayPool<char>.Shared.Rent(256);

            // try
            // {
            //     await foreach (var readOnlySequence in Read(pipeReader))
            //     {
            //         if (SourceShouldSkipLine(readOnlySequence.Span, skipLines))
            //             continue;
            //
            //         DoStuff(readOnlySequence.Span, strings, ref chars, decoder);
            //         // HandleWwwPrefix(readOnlySequence.Span);
            //         // HandleDelimiter(ref current, Constants.HashSign);
            //         // if (IsWhiteSpace(current))
            //         //     continue;
            //         //
            //         // decoder.GetChars(current, chars, false);
            //         // var lineChars = chars[..current.Length];
            //         // strings.Add(lineChars.Trim().ToString());
            //     }
            // }
            // catch (Exception ex)
            // {
            //     
            // }
            // finally
            // {
            //     ArrayPool<char>.Shared.Return(chars);
            // }
            return new List<string>(new HashSet<string>(strings));
            // Span<char> chars = stackalloc char[256];
            
            // var read = 0;
            // Span<char> chars = stackalloc char[256];
            // while (read < bytes.Length)
            // {
            //     var current = bytes[read..];
            //     if (!HandleStartsWithNewLine(ref current,
            //         ref read,
            //         out var index))
            //         continue;
            //     
            //     current = current[..index];
            //     read += current.Length;
            //
            //     if (SourceShouldSkipLine(current, skipLines))
            //         continue;
            //
            //     HandleWwwPrefix(ref current);
            //     HandleDelimiter(ref current, Constants.HashSign);
            //     if (IsWhiteSpace(current))
            //         continue;
            //     
            //     decoder.GetChars(current, chars, false);
            //     var lineChars = chars[..current.Length];
            //     strings.Add(lineChars.Trim().ToString());
            // }

            
        }
        
        internal static async Task<HashSet<string>> ProcessAdGuard(Stream bytes,
            Decoder decoder)
        {
            var strings = new HashSet<string>();
            var pipeReader = PipeReader.Create(bytes);
            await ReadPipeAsync(pipeReader, strings, null, false, decoder);
            // var read = 0;
            // Span<char> chars = stackalloc char[256];
            // while (read < bytes.Length)
            // {
            //     var current = bytes[read..];
            //     
            //     if (!HandleStartsWithNewLine(ref current,
            //         ref read,
            //         out var index))
            //         continue;
            //
            //     current = current[..index];
            //     read += current.Length;
            //
            //     if (AdGuardShouldSkipLine(current))
            //         continue;
            //     
            //     HandlePipe(ref current);
            //     HandleDelimiter(ref current, Constants.HatSign);
            //     if (IsWhiteSpace(current))
            //         continue;
            //     
            //     decoder.GetChars(current, chars, false);
            //     var lineChars = chars[..current.Length];
            //     strings.Add(lineChars.Trim().ToString());
            // }

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

        private static void HandlePipe(ref ReadOnlySpan<byte> lineBytes)
        {
            var lastPipe = lineBytes.LastIndexOf(Constants.PipeSign);
            if (lastPipe > -1)
                lineBytes = lineBytes[(lastPipe == 0 ? 1 : lastPipe + 1)..];
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
            else if (lineBytes.StartsWith(Constants.NxIpWithSpace))
                return lineBytes[Constants.NxIpWithSpace.Length..];

            return lineBytes;
        }
    }
}
