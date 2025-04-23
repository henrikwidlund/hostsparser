// Copyright Henrik Widlund
// GNU General Public License v3.0

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZLinq;

namespace HostsParser;

public static class HostUtilities
{
    /// <summary>
    /// Reads the <paramref name="stream"/> and returns a collection based on the items in it.
    /// </summary>
    /// <param name="dnsHashSet">The <see cref="HashSet{T}"/> that results are added to.</param>
    /// <param name="stream">The <see cref="Stream"/> to process.</param>
    /// <param name="skipLines">The lines that should be excluded from the returned result.</param>
    /// <param name="skipBlockedHosts">The hosts that should be excluded from the result, even if it's present in the <paramref name="stream"/>.</param>
    /// <param name="sourcePrefix">The <see cref="SourcePrefix"/> with definitions on what should be removed from each row.</param>
    /// <param name="decoder">The <see cref="Decoder"/> used when converting the bytes in <paramref name="stream"/>.</param>
    public static Task ProcessHostsBased(HashSet<string> dnsHashSet,
        Stream stream,
        byte[][]? skipLines,
        byte[][]? skipBlockedHosts,
        in SourcePrefix sourcePrefix,
        Decoder decoder)
    {
        var pipeReader = PipeReader.Create(stream);
        return ReadPipeAsync(pipeReader, dnsHashSet, null, skipLines, skipBlockedHosts, sourcePrefix, decoder);
    }

    /// <summary>
    /// Reads the <paramref name="stream"/> and returns a collection based on the items in it.
    /// </summary>
    /// <param name="dnsHashSet">The <see cref="HashSet{T}"/> that blocked results are added to.</param>
    /// <param name="allowedOverrides">The <see cref="HashSet{T}"/> that allowed results are added to.</param>
    /// <param name="skipBlockedHosts">The hosts that should be excluded from the result, even if it's present in the <paramref name="stream"/>.</param>
    /// <param name="stream">The <see cref="Stream"/> to process.</param>
    /// <param name="decoder">The <see cref="Decoder"/> used when converting the bytes in <paramref name="stream"/>.</param>
    public static Task ProcessAdBlockBased(HashSet<string> dnsHashSet,
        HashSet<string> allowedOverrides,
        byte[][]? skipBlockedHosts,
        Stream stream,
        Decoder decoder)
    {
        var pipeReader = PipeReader.Create(stream);
        return ReadPipeAsync(pipeReader, dnsHashSet, allowedOverrides, null, skipBlockedHosts, null, decoder);
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

        var knownBadHostValueEnumerable = knownBadHosts.AsValueEnumerable();
        foreach (var host in hosts)
        {
            var found = knownBadHostValueEnumerable.Any(knownBadHost => IsSubDomainOf(host, knownBadHost));

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

        return potentialSubDomain[(potentialSubDomain.LastIndexOf(potentialDomain) - 1)..][0] == Constants.DotSign;
    }

    private static async Task ReadPipeAsync(PipeReader reader,
        ICollection<string> resultCollection,
        ICollection<string>? allowedOverrides,
        byte[][]? skipLines,
        byte[][]? skipBlockedHosts,
        SourcePrefix? sourcePrefix,
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

                if (position is null) continue;

                ProcessLine(buffer.Slice(0, position.Value), resultCollection, allowedOverrides, skipLines, skipBlockedHosts, sourcePrefix, decoder);
                buffer = buffer.Slice(buffer.GetPosition(1, position.Value));
            } while (position is not null);

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (!result.IsCompleted) continue;
            ProcessLastChunk(resultCollection, allowedOverrides, skipLines, skipBlockedHosts, sourcePrefix, decoder, buffer);

            break;
        }

        await reader.CompleteAsync();
    }

    private static void ProcessLastChunk(ICollection<string> resultCollection,
        ICollection<string>? allowedOverrides,
        byte[][]? skipLines,
        byte[][]? skipBlockedHosts,
        in SourcePrefix? sourcePrefix,
        Decoder decoder,
        in ReadOnlySequence<byte> buffer)
    {
        if (buffer.IsEmpty) return;
        ProcessLine(buffer, resultCollection, allowedOverrides, skipLines, skipBlockedHosts, sourcePrefix, decoder);
    }

    private static void ProcessLine(in ReadOnlySequence<byte> slice,
        ICollection<string> resultCollection,
        ICollection<string>? allowedOverrides,
        byte[][]? skipLines,
        byte[][]? skipBlockedHosts,
        in SourcePrefix? sourcePrefix,
        Decoder decoder)
    {
        if (skipLines is null)
        {
            Debug.Assert(allowedOverrides is not null);
            ProcessAdBlockBasedLine(slice, resultCollection, allowedOverrides, skipBlockedHosts, decoder);
        }
        else
            ProcessHostsBasedLine(slice, resultCollection, skipLines, skipBlockedHosts, sourcePrefix, decoder);
    }

    private static void ProcessHostsBasedLine(in ReadOnlySequence<byte> slice,
        ICollection<string> resultCollection,
        byte[][] skipLines,
        byte[][]? skipBlockedHosts,
        in SourcePrefix? sourcePrefix,
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

        realSlice = HandlePrefixes(realSlice, sourcePrefix, skipBlockedHosts);
        HandleDelimiter(ref realSlice, Constants.HashSign);

        var chars = ArrayPool<char>.Shared.Rent(256);
        try
        {
            var span = chars.AsSpan();
            decoder.GetChars(realSlice, span, false);
            AddItem(resultCollection, span[..realSlice.Length].Trim().ToString());
        }
        finally
        {
            ArrayPool<char>.Shared.Return(chars);
        }
    }

    private static void ProcessAdBlockBasedLine(in ReadOnlySequence<byte> slice,
        ICollection<string> resultCollection,
        ICollection<string> allowedOverrides,
        byte[][]? skipBlockedHosts,
        Decoder decoder)
    {
        byte[]? bytes = null;
        try
        {
            var realSlice = slice.IsSingleSegment
                ? slice.FirstSpan
                : slice.Length <= 128 ? Get(slice, stackalloc byte[(int)slice.Length]):
                (bytes = GetBytes(slice, out var length)).AsSpan()[..length!.Value];
            if (realSlice.IsEmpty)
                return;

            if (AdBlockBasedShouldSkipLine(realSlice))
                return;

            var isAllow = realSlice[0] == Constants.AtSign;

            realSlice = HandlePipeOrAt(realSlice, isAllow);

            HandleDelimiter(ref realSlice, Constants.HatSign);
            if (realSlice.IndexOfAnyExcept(Constants.Space, Constants.Tab) == -1)
                return;

            if (IsSkipBlockedHosts(realSlice, skipBlockedHosts))
                return;

            Span<char> chars = stackalloc char[256];
            decoder.GetChars(realSlice, chars, false);
            AddItem(isAllow ? allowedOverrides : resultCollection, chars[..realSlice.Length].Trim().ToString());
        }
        finally
        {
            if (bytes is not null)
                ArrayPool<byte>.Shared.Return(bytes);
        }
    }

    private static ReadOnlySpan<byte> Get(in ReadOnlySequence<byte> sequence, in Span<byte> buffer)
    {
        sequence.CopyTo(buffer);
        return buffer;
    }

    private static byte[] GetBytes(in ReadOnlySequence<byte> sequence, out int? length)
    {
        if (sequence.Length > int.MaxValue)
        {
            length = null;
            return sequence.ToArray();
        }
        var intLength = (int)sequence.Length;
        var bytes = ArrayPool<byte>.Shared.Rent(intLength);
        length = intLength;
        sequence.CopyTo(bytes);
        return bytes;
    }

    private static readonly Lock Lock = new();

    private static void AddItem(ICollection<string> resultCollection, string item)
    {
        try
        {
            resultCollection.Add(item);
        }
        catch (InvalidOperationException)
        {
            // Try add again if collection was modified.
            lock (Lock)
            {
                resultCollection.Add(item);
            }
        }
    }

    private static bool HostsBasedShouldSkipLine(in ReadOnlySpan<byte> bytes,
        byte[][] skipLines)
    {
        var trimmedStart = bytes.TrimStart(Constants.SpaceTab.Span);
        if (trimmedStart.IsEmpty
            || trimmedStart[0] == Constants.HashSign)
            return true;

        foreach (var t in skipLines)
        {
            if (bytes.SequenceEqual(t))
                return true;
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool AdBlockBasedShouldSkipLine(in ReadOnlySpan<byte> current)
        => current[0] is not Constants.PipeSign and not Constants.AtSign;

    private static ReadOnlySpan<byte> HandlePipeOrAt(ReadOnlySpan<byte> lineBytes, bool isAllow)
    {
        var lastPipe = lineBytes.LastIndexOf(isAllow ? Constants.AtSign : Constants.PipeSign);
        return lineBytes[(lastPipe == 0 ? 1 : lastPipe + 1)..];
    }

    private static void HandleDelimiter(ref ReadOnlySpan<byte> lineChars,
        in byte delimiter)
    {
        var delimiterIndex = lineChars.IndexOf(delimiter);
        if (delimiterIndex > 0)
            lineChars = lineChars[..delimiterIndex];
    }

    private static ReadOnlySpan<byte> HandlePrefixes(ReadOnlySpan<byte> lineBytes,
        SourcePrefix? sourcePrefix,
        byte[][]? skipBlockedHosts)
    {
        if (sourcePrefix?.WwwPrefixBytes is not null
            && lineBytes.StartsWith(sourcePrefix.Value.WwwPrefixBytes))
        {
            var readOnlySpan = lineBytes[sourcePrefix.Value.WwwPrefixBytes.Length..];
            if (!IsSkipBlockedHosts(readOnlySpan, skipBlockedHosts))
            {
                return readOnlySpan;
            }
        }

        if (sourcePrefix?.PrefixBytes is not null
            && lineBytes.StartsWith(sourcePrefix.Value.PrefixBytes))
        {
            var readOnlySpan = lineBytes[sourcePrefix.Value.PrefixBytes.Length..];
            return IsSkipBlockedHosts(readOnlySpan, skipBlockedHosts) ? [] : readOnlySpan;
        }

        return lineBytes;
    }

    private static bool IsSkipBlockedHosts(in ReadOnlySpan<byte> lineBytes,
        byte[][]? skipBlockedHosts)
    {
        if (skipBlockedHosts is null)
            return false;

        foreach (var t in skipBlockedHosts)
        {
            if (lineBytes.SequenceEqual(t))
                return true;
        }

        return false;
    }
}
