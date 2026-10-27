using System;
using System.Collections.Generic;
using System.IO;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Source ownership and sampling are separate. A successful binding retains the
// complete original Surface/VT graph; it creates no material or texture asset.
// The common caller must handle unsupported mappings explicitly, never rebake.
internal sealed class Draw02NativeSourceBindings
{
    private readonly Dictionary<SurfaceAsset, string> _verified = new();

    internal bool TryBind(SurfaceAsset source, MovableStructurePacking.Unit sampling,
        out MovableStructureTiles.Slot? slot, out string requirement)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (sampling == null) throw new ArgumentNullException(nameof(sampling));
        slot = null;
        var cid = source.id.guid.ToString();
        if (!Draw02NativeSourceCatalog.Sources.TryGetValue(cid, out var expected))
        {
            requirement = "Draw02 native source has no recorded binding: " + cid;
            return false;
        }
        if (!source.isBuiltin)
            throw new InvalidOperationException("Draw02 native source must retain its built-in identity: " + cid);
        if (!_verified.TryGetValue(source, out var actual))
        {
            using var stream = source.GetReadStream();
            using var bytes = new MemoryStream();
            stream.CopyTo(bytes);
            actual = MovableStructureKey.Digest(bytes.ToArray());
            if (!string.Equals(actual, expected, StringComparison.Ordinal))
                throw new InvalidDataException("Draw02 native source content differs: " + cid);
            _verified.Add(source, actual);
        }
        // This applies the measured receiver UVs only when containment and
        // Float32 gradient preservation are already proven by the common unit.
        // A legacy triangular bake is not itself evidence that the prototype
        // requires reflection. Such receivers need their original mapping derived.
        if (!sampling.CanUseSource)
        {
            requirement = "Draw02 receiver needs a validated original-UV/native sampling mapping: "
                + MovableStructureKey.For(actual, sampling.Triangle, sampling.Bounds,
                    sampling.A, sampling.B, sampling.C);
            return false;
        }
        slot = new MovableStructureTiles.Slot { Surface = source, Original = sampling };
        requirement = string.Empty;
        return true;
    }
}
