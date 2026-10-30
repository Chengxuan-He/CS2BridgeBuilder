using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Colossal;
using Colossal.IO.AssetDatabase;
using Colossal.PSI.Common;

namespace BridgeBuilder.Runtime;

// A catalogue of immutable, offline-authored native assets. This never stores
// generated bridge results and never writes into the installed mod directory.
internal static class BridgePreparedMaterials
{
    private readonly struct Descriptor : IAssetDatabaseDescriptor<Descriptor>
    {
        private readonly string _root;
        internal Descriptor(string root) => _root = root;
        public string name => "BridgeBuilderPreparedMaterials";
        public bool canWriteSettings => false;
        public DlcId dlcId => DlcId.Invalid;
        public IAssetFactory assetFactory => DefaultAssetFactory.instance;
        public IDataSourceProvider dataSourceProvider => new FileSystemDataSource(name, _root, assetFactory);
        public bool Equals(Descriptor other) => StringComparer.OrdinalIgnoreCase.Equals(_root, other._root);
        public override bool Equals(object? other) => other is Descriptor value && Equals(value);
        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(_root);
    }

    private static string? _root;
    private static ILocalAssetDatabase? _database;
    private static readonly Dictionary<string, (AssetData Asset, string Digest)> Assets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SurfaceAsset> Surfaces = new(StringComparer.Ordinal);
    private static readonly HashSet<SurfaceAsset> RetainedSurfaces = new();
    private static readonly Dictionary<string, string[]> MidMipEdges = new(StringComparer.OrdinalIgnoreCase);

    internal static string[] MidMipDependencies(string surfaceCid)
        => MidMipEdges.TryGetValue(surfaceCid, out var caches) ? caches : Array.Empty<string>();

    internal static void Initialize(string installationDirectory)
        => _root = Path.GetFullPath(Path.Combine(installationDirectory, "Materials"));

    internal static SurfaceAsset Paint(string styleId) => Surface(styleId, "Paint", "SkinnedPaint.Surface");
    internal static SurfaceAsset Cable(string styleId) => Surface(styleId, "Cable", "SkinnedCable.Surface");
    internal static SurfaceAsset Road(string styleId) => Surface(styleId, "Road", "NativeAsphalt.Surface");
    internal static SurfaceAsset NativeSource(string styleId,string descriptorCid)
        => Surface(styleId,"NativeSources/"+descriptorCid,"Object.Surface");

    internal static SurfaceAsset RepeatUnit(string styleId,string bindingKey)
        => Surface(styleId,"RepeatUnits/"+bindingKey,"Repeat.Surface");

    internal static TextureAsset ZeroControl(string styleId)
    {
        // Road already retains authoritative, style-local texture references.
        // Borrow this offline-authored constant; it is not a generated resource.
        var road = Road(styleId);
        if (road.TryGetTexture("_ControlMask", out var texture) && texture != null) return texture;
        throw new InvalidDataException("Prepared road control texture is missing: " + styleId);
    }

    internal static SurfaceAsset? Repeat(string styleId, string samplerKey)
    {
        if (_root == null) throw new InvalidOperationException("Prepared material installation directory is unavailable.");
        var unit=Bridges.MovableRepeatUnits.For(styleId,samplerKey);
        if(unit!=null)return Surface(styleId,"RepeatUnits/"+unit,"Repeat.Surface");
        var variant = "Repeat/" + samplerKey;
        return File.Exists(Path.Combine(_root, styleId, variant, "Repeat.Surface"))
            ? Surface(styleId, variant, "Repeat.Surface") : null;
    }

    internal static SurfaceAsset Structure(string styleId, string samplerKey)
    {
        if (_root == null) throw new InvalidOperationException("Prepared material installation directory is unavailable.");
        // A measured structural rectangle can use the same genuine unit as a
        // road appearance. The bridge's alias must preserve its actual UV frame;
        // an arbitrary triangular atlas island is not a repeating rectangle.
        var unit=Bridges.MovableRepeatUnits.For(styleId,samplerKey);
        if(unit!=null)return Surface(styleId,"RepeatUnits/"+unit,"Repeat.Surface");
        var variant = "Structure/" + samplerKey;
        // Missing immutable input is a packaging/source-version error, never a
        // request to recreate a constant texture during preview or generation.
        return Surface(styleId, variant, "Structure.Surface");
    }

    internal static SurfaceAsset? Lane(string styleId, byte[] source)
    {
        if (_root == null) throw new InvalidOperationException("Prepared material installation directory is unavailable.");
        using var sha = SHA256.Create();
        var digest = BitConverter.ToString(sha.ComputeHash(source)).Replace("-", "").ToLowerInvariant();
        var variant = "Lanes/" + digest;
        return File.Exists(Path.Combine(_root, styleId, variant, "SkinnedLane.Surface"))
            ? Surface(styleId, variant, "SkinnedLane.Surface") : null;
    }

    private static SurfaceAsset Surface(string styleId, string variant, string name)
    {
        if (_root == null) throw new InvalidOperationException("Prepared material installation directory is unavailable.");
        if (string.IsNullOrEmpty(styleId) || styleId.Any(c => !char.IsLetterOrDigit(c)))
            throw new ArgumentException("Prepared material requires a stable style ID.", nameof(styleId));
        string key = styleId + "/" + variant + "/" + name;
        if (Surfaces.TryGetValue(key, out var existing)) return existing;
        // Preflight the complete variant before native registration. Sidecars retain
        // the prepared identity; a shared CID must always describe identical bytes.
        var inputs = BridgePreparedMaterialFiles.Read(Path.Combine(_root, styleId), variant, name,
            bytes => BridgeSurfaceReferences.Read(bytes, (template, property) =>
                AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(template).GetStackConfigIndex(property) != -1),
            cid => RequireBuiltin(cid, key), cid =>
            {
                var asset = RequireBuiltin(cid, key);
                if (asset is not VTTextureAsset && asset is not VTSurfaceAsset) return 0;
                using var stream = asset.GetReadStream();
                return BridgeMidMipReferences.RequiredLevels(stream, asset is VTSurfaceAsset);
            });
        foreach (var input in inputs)
        {
            if (Assets.TryGetValue(input.Cid, out var registered) && registered.Digest != input.Digest)
                throw new InvalidDataException("Prepared material CID has different payloads: " + input.Cid);
        }
        var surfaceInput = inputs.Single(i => Path.GetFileName(i.Path) == name);
        var cacheInputs = inputs.Where(i => Path.GetExtension(i.Path) == ".MidMips").ToArray();
        MidMipEdges[surfaceInput.Cid] = cacheInputs.Select(i => i.Cid).Distinct().ToArray();
        if (_database == null)
        {
            _database = AssetDatabase<Descriptor>.GetInstance(new Descriptor(_root));
            AssetDatabase.global.RegisterDatabase(_database).GetAwaiter().GetResult();
        }
        bool newCache = false;
        foreach (var input in inputs)
        {
            if (Assets.ContainsKey(input.Cid)) continue;
            var path = AssetDataPath.Create(Path.GetDirectoryName(input.Path), Path.GetFileName(input.Path), true, EscapeStrategy.None);
            // AddAsset registers an existing file with a forced CID. It does not
            // serialize, compress, copy, or save the file (native API verified).
            var asset = (AssetData)_database.AddAsset(path, Hash128.Parse(input.Cid));
            Assets.Add(input.Cid, (asset, input.Digest));
            newCache |= asset is MidMipCacheAsset;
        }
        if (newCache)
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var streaming = world?.GetExistingSystemManaged<Colossal.IO.AssetDatabase.VirtualTexturing.TextureStreamingSystem>();
            if (streaming == null) throw new InvalidOperationException("Native VT streaming is unavailable for prepared mid-mip registration.");
            streaming.MarkVTAssetsDirty();
            streaming.RefreshVT(_database);
        }
        var surface = (SurfaceAsset)Assets[surfaceInput.Cid].Asset;
        // Keep the immutable source descriptor's direct texture references alive.
        // Global CID resolution can otherwise choose an old bridge's private copy.
        if (!RetainedSurfaces.Contains(surface))
        {
            surface.LoadProperties(useVT: true);
            try
            {
                // Native VT readers resolve their backing assets by CID. Check
                // metadata before any VT payload stream can be opened; never use
                // a private/deleted bridge snapshot as an installed source.
                if (surface.isVTMaterial && surface.hasVTSurfaceAsset && !surface.vtSurfaceAsset.isBuiltin)
                    throw new InvalidDataException("Prepared VT source is not built-in: " + key);
                var description = AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(surface.materialTemplateHash);
                var textures = new Dictionary<string, TextureAsset>();
                foreach (var texture in surface.textures)
                {
                    // VT stack bindings are original authoring identities, not
                    // runtime Texture payload dependencies (some no longer exist).
                    if (surface.isVTMaterial && description.GetStackConfigIndex(texture.Key) != -1)
                    {
                        // Preserve authoring/provenance bindings even when no original
                        // Texture payload exists. Lane compatibility compares these CIDs;
                        // clearing them incorrectly falls back to a new runtime descriptor.
                        textures.Add(texture.Key, texture.Value);
                        continue;
                    }
                    if (texture.Value == null)
                        throw new InvalidDataException("Prepared material texture is missing: " + key + "/" + texture.Key);
                    var cid = texture.Value.id.guid.ToString();
                    var payload = Assets.TryGetValue(cid, out var dependency) ? dependency.Asset : RequireBuiltin(cid, key);
                    if (payload is not TextureAsset nativeTexture)
                        throw new InvalidDataException("Prepared texture has an incompatible asset type: " + key + "/" + texture.Key);
                    textures.Add(texture.Key, nativeTexture);
                }
                surface.UpdateTextures(textures);
                RetainedSurfaces.Add(surface);
            }
            catch { surface.UnloadProperties(); throw; }
        }
        Surfaces.Add(key, surface);
        return surface;
    }

    private static AssetData RequireBuiltin(string cid, string key)
    {
        if (AssetDatabase.global.TryGetAsset(Hash128.Parse(cid), out AssetData asset) && asset.isBuiltin) return asset;
        throw new InvalidDataException("Prepared material requires an authoritative built-in dependency: " + key + "/" + cid);
    }

    internal static bool TryGet(string cid, out AssetData asset)
    {
        if (Assets.TryGetValue(cid, out var entry)) { asset = entry.Asset; return true; }
        asset = null!;
        return false;
    }

    internal static void Stop()
    {
        foreach (var surface in RetainedSurfaces) surface.UnloadProperties();
        RetainedSurfaces.Clear();
        Surfaces.Clear(); Assets.Clear(); MidMipEdges.Clear(); _root = null;
        BridgePreparedMaterialFiles.Clear();
        if (_database == null) return;
        AssetDatabase.global.UnregisterDatabase(_database).GetAwaiter().GetResult();
        _database.Dispose(); // Never MarkForDeletion: these are installed source assets.
        _database = null;
    }
}
