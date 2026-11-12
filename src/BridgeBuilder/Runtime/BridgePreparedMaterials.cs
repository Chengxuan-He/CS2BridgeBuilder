using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Colossal;
using SurfaceResult = BridgeBuilder.Bridges.MovableExistingRoadSurface.SurfaceResult;
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
    private static Game.Prefabs.ContentPrefab? _content;
    internal static void SetContent(Game.Prefabs.ContentPrefab? content) => _content = content;
    internal static bool HasUnavailableRequirement(Game.Prefabs.PrefabBase prefab)
    {
        var prerequisite = prefab.GetComponent<Game.Prefabs.ContentPrerequisite>();
        return prerequisite != null && !Bridges.BridgePrototypeSource.ContentAvailable(prerequisite.m_ContentPrerequisite);
    }
    internal static bool RefreshContent(Game.Prefabs.PrefabSystem system)
    {
        _content = CS2Mods.Shared.Infrastructure.PrefabCatalog.GetAll(system).OfType<Game.Prefabs.ContentPrefab>()
            .FirstOrDefault(content => content.isBuiltin && content.name == "BridgesAndPorts");
        return ContentAvailable;
    }
    private static bool ContentAvailable => Bridges.BridgePrototypeSource.ContentAvailable(_content);
    // Main-thread request scope flows across its asynchronous continuations only.
    private static readonly System.Threading.AsyncLocal<BridgeMaterialFileSnapshot?> Files = new();
    internal static BridgeMaterialFileSnapshot? RequestFiles { get=>Files.Value;set=>Files.Value=value; }
    internal static bool TrySourceRoot(out string root,out string? error)
    {root=_root??"";error=!ContentAvailable?"Prepared material content is unavailable.":_root==null?"Prepared material installation directory is unavailable.":null;return error==null;}
    private static ILocalAssetDatabase? _database;
    private static bool _pendingCacheRefresh;
    private static readonly Dictionary<string, (AssetData Asset, string Digest)> Assets = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, SurfaceAsset> Surfaces = new(StringComparer.Ordinal);
    private static readonly HashSet<SurfaceAsset> RetainedSurfaces = new();
    private static readonly Dictionary<string, string[]> MidMipEdges = new(StringComparer.OrdinalIgnoreCase);

    internal static bool TryMidMipDependencies(string surfaceCid, out string[] caches)
        => MidMipEdges.TryGetValue(surfaceCid, out caches!);

    // Native parent discovery skips this directory; explicit AddAsset below registers only
    // preflighted inputs. RegisterDatabase itself does not populate the data source.
    internal static void Initialize(string installationDirectory)
        => _root = Path.GetFullPath(Path.Combine(installationDirectory, "~Materials"));

    internal static SurfaceResult Paint(string styleId) => Surface(styleId, "Paint", "SkinnedPaint.Surface");
    internal static SurfaceResult Cable(string styleId) => Surface(styleId, "Cable", "SkinnedCable.Surface");
    internal static SurfaceResult Road(string styleId) => Surface(styleId, "Road", "NativeAsphalt.Surface");
    internal static SurfaceResult NativeSource(string styleId,string descriptorCid)
        => Surface(styleId,"NativeSources/"+descriptorCid,"Object.Surface");

    internal static SurfaceResult RepeatUnit(string styleId,string bindingKey)
        => Surface(styleId,"RepeatUnits/"+bindingKey,"Repeat.Surface");

    internal static bool TryZeroControl(string styleId,out TextureAsset texture,out string? error)
    {
        texture=null!;var road=Road(styleId);error=road.Error;if(!road.Success)return false;
        try
        {
            if(road.Value!.TryGetTexture("_ControlMask",out texture)&&texture!=null)return true;
            error="Prepared road control texture is missing: "+styleId;return false;
        }
        catch(Exception exception){texture=null!;error="Prepared control lookup: "+exception;return false;}
    }

    internal static SurfaceResult Repeat(string styleId, string samplerKey)
    {
        try
        {
        if (!ContentAvailable)return new SurfaceResult(null,"Required native content is unavailable.");
        if (_root == null)return new SurfaceResult(null,"Prepared material installation directory is unavailable.");
        var unit=Bridges.MovableRepeatUnits.For(styleId,samplerKey);
        if(unit!=null)return Surface(styleId,"RepeatUnits/"+unit,"Repeat.Surface");
        var variant = "Repeat/" + samplerKey;
        return File.Exists(Path.Combine(_root, styleId, variant, "Repeat.Surface"))
            ? Surface(styleId, variant, "Repeat.Surface") : new SurfaceResult(null);
        }
        catch(Exception exception){return new SurfaceResult(null,"Prepared Repeat lookup: "+styleId+"; "+exception);}
    }

    private static readonly HashSet<string> RoadTextureSurfaces = new(StringComparer.OrdinalIgnoreCase);

    internal static bool TryStructuralBinding(SurfaceAsset surface,out string? error)
    {
        error=null;
        try
        {
            if(surface==null){error="Structural material result is absent.";return false;}
            if(RoadTextureSurfaces.Contains(surface.id.guid.ToString())){error="Structural geometry cannot consume a packaged road texture: "+surface.id.guid;return false;}
            return true;
        }
        catch(Exception exception){error="Structural material binding: "+exception;return false;}
    }

    internal static SurfaceResult Lane(string styleId, byte[] source)
    {
        try
        {
        if (!ContentAvailable)return new SurfaceResult(null,"Required native content is unavailable.");
        if (_root == null)return new SurfaceResult(null,"Prepared material installation directory is unavailable.");
        using var sha = SHA256.Create();
        var digest = BitConverter.ToString(sha.ComputeHash(source)).Replace("-", "").ToLowerInvariant();
        var variant = "Lanes/" + digest;
        return File.Exists(Path.Combine(_root, styleId, variant, "SkinnedLane.Surface"))
            ? Surface(styleId, variant, "SkinnedLane.Surface") : new SurfaceResult(null);
        }
        catch(Exception exception){return new SurfaceResult(null,"Prepared Lane lookup: "+styleId+"; "+exception);}
    }

    private static SurfaceResult Surface(string styleId, string variant, string name)
    {
        SurfaceResult Fail(string message)=>new SurfaceResult(null,$"Prepared material: style={styleId}; variant={variant}; name={name}; {message}");
        try
        {
        if (!ContentAvailable)return new SurfaceResult(null,"Required native content is unavailable.");
        if (_root == null)return new SurfaceResult(null,"Prepared material installation directory is unavailable.");
        if (string.IsNullOrEmpty(styleId) || styleId.Any(c => !char.IsLetterOrDigit(c)))
            return Fail("Prepared material requires a stable style ID.");
        string key = styleId + "/" + variant + "/" + name;
        if (Surfaces.TryGetValue(key, out var existing)) return Record(existing);
        // Preflight the complete variant before native registration. Sidecars retain
        // the prepared identity; a shared CID must always describe identical bytes.
        string? callbackError=null;
        if(!BridgePreparedMaterialFiles.TryRead(Path.Combine(_root, styleId), variant, name,out var inputs,out var preflightError,
            bytes =>
            {
                if(BridgeDependencyPersistence.TryReadSurfaceReferences(bytes,out var references,out var cause))return references;
                callbackError??=cause;return System.Array.Empty<string>();
            },
            cid => {if(!TryBuiltin(cid,key,out _,out var cause))callbackError??=cause;}, cid =>
            {
                if(!TryBuiltin(cid,key,out var asset,out var cause)){callbackError??=cause;return 0;}
                if (asset is not VTTextureAsset && asset is not VTSurfaceAsset) return 0;
                using var stream = asset.GetReadStream();
                if(!BridgeMidMipReferences.TryRequiredLevels(stream,asset is VTSurfaceAsset,out var levels,out var levelError))
                    {callbackError??="Prepared mip requirements: "+key+"; "+levelError;return 0;}
                return levels;
            }, RequestFiles))return Fail(preflightError!);
        if(callbackError!=null)return Fail(callbackError);
        foreach (var input in inputs)
        {
            if (Assets.TryGetValue(input.Cid, out var registered) && registered.Digest != input.Digest)
                return Fail("Prepared material CID has different payloads: " + input.Cid);
        }
        var surfaceInput = inputs.Single(i => Path.GetFileName(i.Path) == name);
        bool roadTextures=inputs.Any(i => Path.GetExtension(i.Path)==".Texture");
        var cacheInputs = inputs.Where(i => Path.GetExtension(i.Path) == ".MidMips").ToArray();
        var cacheEdges = cacheInputs.Where(i => i.Cid == BridgePreparedMaterialFiles.CacheFor(surfaceInput.Cid))
            .Select(i => i.Cid).Distinct().ToArray();
        if (_database == null)
        {
            _database = AssetDatabase<Descriptor>.GetInstance(new Descriptor(_root));
            AssetDatabase.global.RegisterDatabase(_database).GetAwaiter().GetResult();
        }

        foreach (var input in inputs)
        {
            if (Assets.ContainsKey(input.Cid)) continue;
            var path = AssetDataPath.Create(Path.GetDirectoryName(input.Path), Path.GetFileName(input.Path), true, EscapeStrategy.None);
            // AddAsset registers an existing file with a forced CID. It does not
            // serialize, compress, copy, or save the file (native API verified).
            var asset = (AssetData)_database.AddAsset(path, Hash128.Parse(input.Cid));
            Assets.Add(input.Cid, (asset, input.Digest));
            _pendingCacheRefresh |= asset is MidMipCacheAsset;
        }
        if (_pendingCacheRefresh)
        {
            var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
            var streaming = world?.GetExistingSystemManaged<Colossal.IO.AssetDatabase.VirtualTexturing.TextureStreamingSystem>();
            if (streaming == null) return Fail("Native VT streaming is unavailable for prepared mid-mip registration.");
            streaming.MarkVTAssetsDirty();
            streaming.RefreshVT(_database);
            _pendingCacheRefresh=false;
        }
        var surface = (SurfaceAsset)Assets[surfaceInput.Cid].Asset;
        // Keep the immutable source descriptor's direct texture references alive.
        // Global CID resolution can otherwise choose an old bridge's private copy.
        if (!RetainedSurfaces.Contains(surface))
        {
            bool retained=false;
            surface.LoadProperties(useVT: true);
            try
            {
                // Native VT readers resolve their backing assets by CID. Check
                // metadata before any VT payload stream can be opened; never use
                // a private/deleted bridge snapshot as an installed source.
                if (surface.isVTMaterial && surface.hasVTSurfaceAsset && !surface.vtSurfaceAsset.isBuiltin)
                    return Fail("Prepared VT source is not built-in: " + key);
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
                        return Fail("Prepared material texture is missing: " + key + "/" + texture.Key);
                    var cid = texture.Value.id.guid.ToString();
                    AssetData payload;
                    if(Assets.TryGetValue(cid,out var dependency))payload=dependency.Asset;
                    else if(!TryBuiltin(cid,key,out payload,out var cause))return Fail(cause!);
                    if (payload is not TextureAsset nativeTexture)
                        return Fail("Prepared texture has an incompatible asset type: " + key + "/" + texture.Key);
                    textures.Add(texture.Key, nativeTexture);
                }
                surface.UpdateTextures(textures);
                RetainedSurfaces.Add(surface);retained=true;
            }
            finally {if(!retained)surface.UnloadProperties();}
        }
        MidMipEdges[surfaceInput.Cid]=cacheEdges;
        if(roadTextures)RoadTextureSurfaces.Add(surfaceInput.Cid);
        Surfaces.Add(key, surface);
        return Record(surface);
        }
        catch(Exception exception){return Fail(exception.ToString());}
    }

    private static SurfaceResult Record(SurfaceAsset surface)
    {
        var dependencies = Bridges.BridgeRoadGeneration.CurrentDependencies;
        if (dependencies != null)
        {
            string cid = surface.id.guid.ToString();
            if(!dependencies.Material(surface,MidMipEdges[cid].Select(cache=>Assets[cache].Asset)))return new SurfaceResult(null,dependencies.ResolutionError);
        }
        return new SurfaceResult(surface);
    }

    private static bool TryBuiltin(string cid,string key,out AssetData asset,out string? error)
    {
        asset=null!;error=null;
        try
        {
            if(AssetDatabase.global.TryGetAsset(Hash128.Parse(cid),out asset)&&asset.isBuiltin)return true;
            error="Prepared material requires an authoritative built-in dependency: "+key+"/"+cid;asset=null!;return false;
        }
        catch(Exception exception){asset=null!;error="Prepared built-in lookup: "+key+"/"+cid+"; "+exception;return false;}
    }

    internal static bool TryGet(string cid, out AssetData asset)
    {
        if (Assets.TryGetValue(cid, out var entry)) { asset = entry.Asset; return true; }
        asset = null!;
        return false;
    }

    internal static bool TryStop(out string? error)
    {
        string? failure=null;
        void Fault(Exception exception){failure=(failure==null?"":failure+"; ")+exception;}
        foreach(var surface in RetainedSurfaces)try{surface.UnloadProperties();}catch(Exception exception){Fault(exception);}
        RetainedSurfaces.Clear();RoadTextureSurfaces.Clear();Surfaces.Clear();Assets.Clear();MidMipEdges.Clear();_root=null;_pendingCacheRefresh=false;
        if(_database!=null)
        {
            var database=_database;_database=null;
            try{AssetDatabase.global.UnregisterDatabase(database).GetAwaiter().GetResult();}catch(Exception exception){Fault(exception);}
            try{database.Dispose();}catch(Exception exception){Fault(exception);}
        }
        error=failure;return error==null;
    }
}
