using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using Stage = BridgeBuilder.Runtime.DependencySaveTimings.Stage;

using System.Text;
using System.Text.RegularExpressions;

namespace BridgeBuilder.Runtime;

/// <summary>Byte-identical, per-bridge snapshots of serialized external asset dependencies.</summary>
internal static class BridgeDependencyCopies
{
    private const string EmptyCid = "00000000000000000000000000000000";
    internal sealed class Source
    {
        internal bool Builtin;
        internal bool Owned;
        internal string Extension = "";
        internal string PhysicalPath = "";
        internal string NativeFileName = "";
        internal byte[] Bytes = Array.Empty<byte>();
        internal string[]? SerializedReferences;
    }

    internal static string Folder(string gameRoot, string owner) =>
        Path.Combine(gameRoot, "ImportedData", owner, "Dependencies");

    // MidMipCacheAsset.PostCreate derives its streaming tag from the basename.
    // Scope that native name by CID; ordinary snapshots retain CID.ext filenames.
    internal static bool TryDestination(string gameRoot,string owner,string cid,Source source,out string path,out string? error)
    {
        path="";error=null;
        try
        {
            var folder=Folder(gameRoot,owner);
            if(!source.Extension.Equals(".MidMips",StringComparison.OrdinalIgnoreCase)) {path=Path.Combine(folder,cid+source.Extension);return true;}
            if(!Regex.IsMatch(source.NativeFileName,@"\ATS[0-9]+_MB[0-9]+\.MidMips\z")){error="Missing native MidMips tag filename: "+cid;return false;}
            path=Path.Combine(folder,cid,source.NativeFileName);return true;
        }
        catch(Exception exception){error="Dependency destination: "+exception.Message;return false;}
    }

    internal static IEnumerable<string> References(Source source) => source.SerializedReferences
        ?? (source.Extension.Equals(".Prefab", StringComparison.OrdinalIgnoreCase)
            || source.Extension.Equals(".Material", StringComparison.OrdinalIgnoreCase)
            ? References(source.Bytes) : Array.Empty<string>());

    // Inspect serialized identifiers only. Never load, validate or repair a prefab instance.
    internal static IEnumerable<string> References(byte[] bytes)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var text in new[] { Encoding.UTF8.GetString(bytes), Encoding.Unicode.GetString(bytes),
                     bytes.Length > 1 ? Encoding.Unicode.GetString(bytes, 1, bytes.Length - 1) : "" })
            foreach (Match match in Regex.Matches(text, @"CID:([a-fA-F0-9]{32})(?![a-fA-F0-9])"))
            {
                var cid = match.Groups[1].Value.ToLowerInvariant();
                // Native AssetReference serializes an absent optional asset as Hash128.Empty.
                // For example, lane-only NetPiecePrefabs legitimately have no geometry asset.
                // This is not a dependency edge; required prefab slots are checked separately.
                if (cid != EmptyCid) result.Add(cid);
            }
        // UnityGUID references resolve to the game's built-in resource map and are terminal.
        return result;
    }

    internal static bool Save(string gameRoot, string owner, IEnumerable<string> seeds,
        Func<string, Source?> resolve, out int count, out string error, out int writtenFiles,
        Action<string, Source>? observed = null, Action<Exception>? failed = null, DependencySaveTimings? timing = null)
    {
        writtenFiles = 0;
        count = 0;
        error = "";
        if (!BridgeAssetInfo.IsPrefabName(owner)) { error = "Invalid bridge owner"; return false; }
        string activeCid = "", phase = "initialize graph";
        try
        {
            var copies = new Dictionary<string, Source>(StringComparer.OrdinalIgnoreCase);
            using (timing?.Measure(Stage.Traversal))
            {
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var pending = new Stack<string>(seeds);
                while (pending.Count != 0)
                {
                    var cid = pending.Pop().ToLowerInvariant();
                    if (cid == EmptyCid || !Regex.IsMatch(cid, @"\A[a-f0-9]{32}\z"))
                    { error = "Invalid dependency CID"; return false; }
                    if (!visited.Add(cid)) continue;
                    activeCid = cid; phase = "resolve source";
                    Source? source;
                    using (timing?.Measure(Stage.Resolve))
                    {
                        if (timing != null) timing.ResolveCalls++;
                        source = resolve(cid);
                    }
                    if (source == null) { error = "Dependency unavailable: " + cid; return false; }
                    phase = "observe references";
                    observed?.Invoke(cid, source);
                    if (source.Builtin) continue;
                    if (!Regex.IsMatch(source.Extension, @"\A\.[A-Za-z0-9]+\z"))
                    { error = "Unsupported dependency extension: " + source.Extension; return false; }
                    if (!source.Owned) copies.Add(cid, source);
                    phase = "traverse references";
                    foreach (var dependency in References(source)) pending.Push(dependency);
                }
            }
            var folder = Folder(gameRoot, owner);
            // Preflight every destination before publishing any file. A changed same-CID asset
            // must not silently replace a snapshot on which this bridge already depends.
            var verifiedExisting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (timing?.Measure(Stage.Preflight))
            {
                foreach (var copy in copies)
                {
                    activeCid = copy.Key; phase = "verify/copy destination";
                    if(!TryDestination(gameRoot,owner,copy.Key,copy.Value,out var path,out var destinationError)){error=destinationError!;return false;}
                    if (timing != null) timing.PreflightCopies++;
                    var payloadExists = BridgeFileAccess.Exists(path);
                    var sidecarExists = BridgeFileAccess.Exists(path + ".cid");
                    if (payloadExists && !Matches(path, copy.Value.Bytes))
                    { error = "Different contents for dependency CID " + copy.Key; return false; }
                    if (sidecarExists && BridgeFileAccess.ReadText(path + ".cid").Trim() != copy.Key)
                    { error = "Dependency sidecar mismatch: " + copy.Key; return false; }
                    if (payloadExists && sidecarExists) verifiedExisting.Add(copy.Key);
                }
            }
            if (copies.Count == 0) return true;
            using var write = timing?.Measure(Stage.Write);
            Directory.CreateDirectory(BridgeFileAccess.Native(folder));
            foreach (var copy in copies)
            {
                // Preflight already verified these immutable destinations; no writes or second read.
                if (verifiedExisting.Contains(copy.Key)) { count++; continue; }
                activeCid = copy.Key; phase = "verify/copy destination";
                if(!TryDestination(gameRoot,owner,copy.Key,copy.Value,out var path,out var destinationError)){error=destinationError!;return false;}
                Directory.CreateDirectory(BridgeFileAccess.Native(Path.GetDirectoryName(path)!));
                if (!BridgeFileAccess.Exists(path))
                {
                    using var stream = new FileStream(BridgeFileAccess.Native(path), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    stream.Write(copy.Value.Bytes, 0, copy.Value.Bytes.Length);
                    writtenFiles++;
                    if (timing != null) timing.PayloadBytesWritten += copy.Value.Bytes.LongLength;
                }
                if (!BridgeFileAccess.Exists(path + ".cid"))
                {
                    File.WriteAllText(BridgeFileAccess.Native(path + ".cid"), copy.Key, new UTF8Encoding(false));
                    writtenFiles++;
                }
                using (timing?.Measure(Stage.Verify))
                {
                    if (timing != null) timing.PostWriteChecks++;
                    if (!Matches(path, copy.Value.Bytes))
                    { error = "Dependency copy verification failed: " + copy.Key; return false; }
                }
                count++;
            }
            return true;
        }
        catch (Exception exception)
        {
            error = $"Dependency persistence failed: owner={owner}; CID={activeCid}; phase={phase}; {exception.Message}";
            failed?.Invoke(new IOException(error, exception));
            return false;
        }
    }
    private static bool Matches(string path, byte[] expected)
    {
        using var stream = BridgeFileAccess.OpenRead(path);
        if (stream.Length != expected.Length) return false;
        var buffer = new byte[32768];
        var offset = 0;
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
        {
            for (var i = 0; i < read; i++) if (buffer[i] != expected[offset + i]) return false;
            offset += read;
        }
        return offset == expected.Length;
    }

}

// Per-save wall time. A nested operation pauses its parent, including on early returns
// and exceptions. The hot path uses a value-type scope and never logs individual assets.
internal sealed class DependencySaveTimings
{
    internal enum Stage { Other, Traversal, Resolve, SourceRead, GlobalIndex, MidMipIndex, Preflight, Write, Verify, Footprint }
    private readonly long[] _ticks = new long[(int)Stage.Footprint + 1];
    private readonly long _start = Stopwatch.GetTimestamp();
    private long _last;
    private Stage _current;
    internal int ResolveCalls, PreflightCopies, PostWriteChecks;
    internal long SourceBytesRead, PayloadBytesWritten;
    // Retained diagnostic fields for log consumers; dependency lookup no longer builds indices.
    internal const int GlobalIndexBuilds = 0, MidMipIndexBuilds = 0, MidMipCacheReads = 0;
    internal const long MidMipBytesRead = 0;

    internal DependencySaveTimings() => _last = _start;

    private Stage Switch(Stage next, long now)
    {
        var previous = _current;
        _ticks[(int)previous] += now - _last;
        _last = now;
        _current = next;
        return previous;
    }

    internal readonly struct Scope : IDisposable
    {
        private readonly DependencySaveTimings _timing;
        private readonly Stage _previous;
        internal Scope(DependencySaveTimings timing, Stage next)
        {
            _timing = timing;
            _previous = timing.Switch(next, Stopwatch.GetTimestamp());
        }
        public void Dispose() => _timing.Switch(_previous, Stopwatch.GetTimestamp());
    }

    internal Scope Measure(Stage stage) => new Scope(this, stage);

    internal string Summary(out long elapsedMs)
    {
        long end = Stopwatch.GetTimestamp();
        Switch(Stage.Other, end);
        elapsedMs = (long)((end - _start) * 1000d / Stopwatch.Frequency);
        string Milliseconds(Stage stage) => (_ticks[(int)stage] * 1000d / Stopwatch.Frequency)
            .ToString("F3", CultureInfo.InvariantCulture);
        return $"traversalMs={Milliseconds(Stage.Traversal)}, resolveMs={Milliseconds(Stage.Resolve)}, sourceReadMs={Milliseconds(Stage.SourceRead)}, " +
            $"globalIndexMs={Milliseconds(Stage.GlobalIndex)}, midMipIndexMs={Milliseconds(Stage.MidMipIndex)}, preflightMs={Milliseconds(Stage.Preflight)}, " +
            $"writeMs={Milliseconds(Stage.Write)}, verifyMs={Milliseconds(Stage.Verify)}, footprintMs={Milliseconds(Stage.Footprint)}, otherMs={Milliseconds(Stage.Other)}, " +
            $"resolveCalls={ResolveCalls}, globalIndexBuilds={GlobalIndexBuilds}, midMipIndexBuilds={MidMipIndexBuilds}, " +
            $"sourceBytesRead={SourceBytesRead}, midMipCacheReads={MidMipCacheReads}, midMipBytesRead={MidMipBytesRead}, " +
            $"preflightCopies={PreflightCopies}, postWriteChecks={PostWriteChecks}, payloadBytesWritten={PayloadBytesWritten}";
    }
}
