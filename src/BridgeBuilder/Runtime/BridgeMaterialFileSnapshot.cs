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
    internal string? Error { get; private set; }
    internal bool Cancelled { get; private set; }
    internal long BytesHashed { get; private set; }
    internal long ElapsedMilliseconds { get; private set; }
    internal int WorkerThreadId { get; private set; }
    internal int FileCount => _files.Count;
    internal bool TryGet(string path,out Entry entry,out string? error)
    {
        entry=null!;error=Error;
        if(error!=null)return false;
        try
        {
            if(_files.TryGetValue(Path.GetFullPath(path),out var found)){entry=found;return true;}
            error="Material file was not in the request snapshot: "+path;return false;
        }
        catch(Exception exception){error="Snapshot path lookup: "+exception.Message;return false;}
    }

    internal static BridgeMaterialFileSnapshot Read(string root, string style, CancellationToken cancellation)
    {
        var result = new BridgeMaterialFileSnapshot { WorkerThreadId = Thread.CurrentThread.ManagedThreadId };
        if(string.IsNullOrEmpty(style)||style.Any(c=>!char.IsLetterOrDigit(c)))
        {result.Error="Prepared material requires a stable style ID.";return result;}
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
                    if (string.Equals(Path.GetFileName(file), "SkinnedPaint.Surface", StringComparison.Ordinal)
                        && string.Equals(Path.GetFileName(Path.GetDirectoryName(file)), "Paint", StringComparison.Ordinal))
                    {
                        if(!result.TryGet(file,out var surfaceEntry,out var entryError)){result.Error=entryError;yield break;}
                        if(!BridgeSurfaceReferences.TryRead(surfaceEntry.Bytes!,(_,_)=>false,out var refs,out var cause))
                        {result.Error="Prepared snapshot: "+file+"; "+cause;yield break;}
                        foreach (var cid in refs.Where(BridgePreparedMaterialFiles.IsSharedPaint))
                            cachePaths.Add(BridgePreparedMaterialFiles.PaintPath(root, cid));
                    }
                    if (!file.EndsWith(".Surface.cid", StringComparison.OrdinalIgnoreCase)) continue;
                    if(!result.TryGet(file,out var cidEntry,out var cidError)){result.Error=cidError;yield break;}
                    using var reader = new StreamReader(new MemoryStream(cidEntry.Bytes!, false));
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
                bool retained=false;
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
                            if(read==0){result.Error="Truncated prepared material snapshot: "+path;return result;}
                            offset += read;
                        }
                    }
                    result._files.Add(Path.GetFullPath(path), new Entry(lease, BitConverter.ToString(sha.Hash!), bytes));
                    retained=true;
                    result.BytesHashed += lease.Length;
                }
                finally {if(!retained)lease.Dispose();}
            }
            cancellation.ThrowIfCancellationRequested();
            if(result.Error!=null){result.Dispose();return result;}
            result.ElapsedMilliseconds = clock.ElapsedMilliseconds;
            return result;
        }
        catch(OperationCanceledException){result.Cancelled=true;result.Error="Prepared material snapshot cancelled.";result.Dispose();return result;}
        catch(Exception exception){result.Error="Prepared material snapshot read: "+exception.Message;return result;}
        finally{if(result.Error!=null)result.Dispose();}
    }

    private int _references=1, _rootDisposed;
    internal IDisposable Retain()
    {
        lock(_files){if(_references==0)throw new ObjectDisposedException(nameof(BridgeMaterialFileSnapshot));_references++;}
        return new Reference(this);
    }
    private sealed class Reference : IDisposable
    {
        private BridgeMaterialFileSnapshot? _owner;
        internal Reference(BridgeMaterialFileSnapshot owner)=>_owner=owner;
        public void Dispose()=>Interlocked.Exchange(ref _owner,null)?.Release();
    }
    private void Release()
    {
        lock(_files){if(--_references!=0)return;foreach(var file in _files.Values)file.Lease.Dispose();_files.Clear();}
    }
    public void Dispose(){if(Interlocked.Exchange(ref _rootDisposed,1)==0)Release();}
}
