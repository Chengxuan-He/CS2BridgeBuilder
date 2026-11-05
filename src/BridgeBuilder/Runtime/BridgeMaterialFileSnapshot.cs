using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace BridgeBuilder.Runtime;

// Pure managed IO only. The lease prevents replacement between validation and native registration.
// No Unity object, asset registration, preview or generated-owner data crosses this boundary.
internal sealed class BridgeMaterialFileSnapshot : IDisposable
{
    internal sealed class Entry
    {
        internal readonly FileStream Lease;
        internal readonly string Digest;
        internal readonly byte[]? Bytes;
        internal Entry(FileStream lease, string digest, byte[]? bytes)
        { Lease = lease; Digest = digest; Bytes = bytes; }
    }
    private readonly Dictionary<string, Entry> _files = new(StringComparer.OrdinalIgnoreCase);
    internal long BytesHashed { get; private set; }
    internal long ElapsedMilliseconds { get; private set; }
    internal int WorkerThreadId { get; private set; }
    internal int FileCount => _files.Count;
    internal Entry Get(string path) => _files.TryGetValue(Path.GetFullPath(path), out var entry)
        ? entry : throw new InvalidDataException("Material file was not in the request snapshot: " + path);

    internal static BridgeMaterialFileSnapshot Read(string root, string style, CancellationToken cancellation)
    {
        if (string.IsNullOrEmpty(style) || style.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("Prepared material requires a stable style ID.", nameof(style));
        var result = new BridgeMaterialFileSnapshot { WorkerThreadId = Thread.CurrentThread.ManagedThreadId };
        var clock = Stopwatch.StartNew();
        try
        {
            string styleFolder = Path.Combine(root, style);
            var buffer = new byte[65536];
            var cachePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            IEnumerable<string> Files()
            {
                if (!Directory.Exists(styleFolder)) yield break;
                foreach (var file in Directory.EnumerateFiles(styleFolder, "*", SearchOption.AllDirectories)
                    .OrderBy(path => path, StringComparer.Ordinal))
                {
                    if (Path.GetExtension(file) is not (".Surface" or ".Texture" or ".MidMips" or ".cid")) continue;
                    yield return file;
                    if (!file.EndsWith(".Surface.cid", StringComparison.OrdinalIgnoreCase)) continue;
                    using var reader = new StreamReader(new MemoryStream(result.Get(file).Bytes!, false));
                    string? cache = BridgePreparedMaterialFiles.CacheFor(reader.ReadToEnd().Trim());
                    if (cache != null) cachePaths.Add(BridgePreparedMaterialFiles.CachePath(root, cache));
                }
                foreach (var path in cachePaths.OrderBy(path => path, StringComparer.Ordinal))
                {
                    yield return path;
                    yield return path + ".cid";
                }
            }
            foreach (var path in Files())
            {
                cancellation.ThrowIfCancellationRequested();
                var lease = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    using var sha = SHA256.Create();
                    int count;
                    while ((count = lease.Read(buffer, 0, buffer.Length)) != 0)
                    {
                        cancellation.ThrowIfCancellationRequested();
                        sha.TransformBlock(buffer, 0, count, buffer, 0);
                    }
                    sha.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                    byte[]? bytes = null;
                    if (Path.GetExtension(path) != ".Texture")
                    {
                        lease.Position = 0;
                        bytes = new byte[checked((int)lease.Length)];
                        int offset = 0;
                        while (offset < bytes.Length)
                        {
                            cancellation.ThrowIfCancellationRequested();
                            int read = lease.Read(bytes, offset, bytes.Length - offset);
                            if (read == 0) throw new EndOfStreamException(path);
                            offset += read;
                        }
                    }
                    result._files.Add(Path.GetFullPath(path), new Entry(lease, BitConverter.ToString(sha.Hash!), bytes));
                    result.BytesHashed += lease.Length;
                }
                catch { lease.Dispose(); throw; }
            }
            cancellation.ThrowIfCancellationRequested();
            result.ElapsedMilliseconds = clock.ElapsedMilliseconds;
            return result;
        }
        catch { result.Dispose(); throw; }
    }

    public void Dispose()
    {
        foreach (var file in _files.Values) file.Lease.Dispose();
        _files.Clear();
    }
}
