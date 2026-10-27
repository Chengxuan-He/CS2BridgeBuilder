using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Hash128 = Colossal.Hash128;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using UnityEngine;
using Unity.Entities;
using Colossal.Rendering;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Borrows the prepared tiled source and owns only newly composed resources.
// Both are shared across LODs and persist without manager callbacks.
internal sealed class MovableRoadMaterial : IDisposable
{
    private const int RequestedMidMipLevels = 3;
    internal SurfaceAsset Asset { get; }
    internal string StyleId { get; }
    internal float UVScale { get; }
    internal TextureAsset AsphaltTexture { get; }
    private bool _keep;
    private readonly PreviewGeometry? _preview;
    private readonly string _owner, _name;
    private ILocalAssetDatabase Database => _preview?.NativeAssets ?? AssetDatabase.user;
    private readonly List<AssetData> _composed = new();

    internal TextureAsset SaveTexture(UnityEngine.Texture2D texture, int channel, int page)
    {
        var asset = Database.AddAsset<TextureAsset>(AssetDataPath.Create("BridgeBuilder",
            _name + " Composite " + page + " " + channel, EscapeStrategy.None));
        _composed.Add(asset);
        asset.SetData(texture);
        // Preview intermediates are short-lived native database files. BC7 mip bytes
        // are identical without the second, lossless file-compression pass.
        // Persisted bridges retain the native compressed representation.
        asset.Save(compress: _preview == null);
        if (channel == 0)
            Mod.Log.Info($"Appearance intermediate textures: {_name}; page={page}; losslessFileCompression={_preview == null}; BC7=true; resultCache=false.");
        asset.Unload();
        return asset;
    }

    internal SurfaceAsset SaveRepeat(Dictionary<string, TextureAsset> textures, int page, bool grating)
    {
        // A repeat page is one distinct source rectangle/band recipe. Do not update
        // Asset in place: earlier pages and LODs would then sample the last unit's
        // textures with a different unit's UVs. The input descriptor is immutable.
        using var data = new Surface(_name + " Repeat " + page,
            grating ? "DefaultCutoutDoubleSided" : "Default");
        var asset = Database.AddAsset(AssetDataPath.Create("BridgeBuilder", data.name, EscapeStrategy.None), data);
        _composed.Add(asset);
        asset.UpdateTextures(textures);
        asset.UpdateKeywords(new HashSet<string>(asset.keywords) { "_TANGENTSPACE_OCTO" });
        asset.UpdateFloats(new Dictionary<string,float> { ["_Snow"] = 0f });
        if (!asset.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
            throw new InvalidOperationException("Could not save native repeating surface.");
        _preview?.RetainSurfaceProperties(asset);
        asset.UnloadProperties();
        return asset;
    }

    internal SurfaceAsset SaveComposite(Dictionary<string, TextureAsset> textures, int page)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using var data = new Surface(_name + " Composite " + page, "DefaultCutoutDoubleSided");
        var asset = Database.AddAsset(AssetDataPath.Create("BridgeBuilder", data.name, EscapeStrategy.None), data);
        _composed.Add(asset);
        asset.UpdateTextures(textures);
        // MovableRoadModel stores octahedral normals and tangents. The preview
        // enables this decoder from mesh attributes, but native city batches use
        // the persisted surface keywords; the bare template does not enable it.
        var keywords = new HashSet<string>(asset.keywords) { "_TANGENTSPACE_OCTO" };
        asset.UpdateKeywords(keywords);
        // Native moving-deck and marking surfaces disable the worldspace snow layer.
        // Keep their authored setting when composing an object surface.
        asset.UpdateFloats(new Dictionary<string,float> { ["_Snow"] = 0f });
        var config = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");
        var references = new Dictionary<TextureAsset, List<SurfaceAsset>>();
        foreach (var texture in textures.Values) references[texture] = new List<SurfaceAsset> { asset };
        // Use the same writer and configured BC7 stack formats as the native importer.
        // Unique owner-local layers are packed into VTSurface, with native tile borders/mips.
        // Keep the native importer setting (11 in the installed game). Never override
        // the global configuration for a generated surface or its shared texture layers.
        var compressionLevel = VirtualTexturingConfig.zStdCompressionLevel;
        try
        {
            if (!asset.Save(mipBias: 0, force: true, saveTextures: false, vt: true,
                virtualTexturingConfig: config, textureReferencesMap: references,
                tileSize: config.tileSize, nbMidMipLevelsRequested: RequestedMidMipLevels))
                throw new InvalidOperationException("Could not persist moving-road composite material.");
        }
        finally
        {
            if (asset.hasVTSurfaceAsset) _composed.Add(asset.vtSurfaceAsset);
            foreach (var texture in textures.Values) texture.Unload();
        }
        Mod.Log.Info($"Composite native VT write: {asset.name}; ms={timer.ElapsedMilliseconds}; ZSTD={compressionLevel}.");
        // Reloading serialized CIDs through the global database cannot resolve private
        // preview textures. Retain the authored references for the renderer's lifetime.
        if (_preview == null) return asset;
        _preview?.RetainSurfaceProperties(asset);
        asset.UnloadProperties();
        return asset;
    }

    // Native AssetImportPipeline.BuildMidMipsCache writes the missing 256/128/64
    // levels separately from VTSurface tiles and the <=32-pixel mip tail. Keep
    // that native format and discovery tag, but scope the file to this owner.
    internal void CompleteComposites(IReadOnlyList<SurfaceAsset> surfaces)
    {
        if (_preview != null || surfaces.Count == 0) return;
        // Finish relocation before native streaming captures any file descriptors.
        foreach (var asset in _composed) MoveResource(asset);
        var config = Resources.Load<VirtualTexturingConfig>("VirtualTexturingConfig");
        var grouper = new AtlasMaterialsGrouper(config.stackDatas.Length, config.tileSize, RequestedMidMipLevels);
        var pixels = new Dictionary<Hash128, NativeArray<byte>>();
        var references = new Dictionary<Hash128, Hash128[]>[config.stackDatas.Length];
        for (var stack = 0; stack < references.Length; stack++) references[stack] = new();
        try
        {
            foreach (var surface in surfaces)
            {
                surface.LoadProperties(useVT: true);
                var description = AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(surface.materialTemplateHash);
                var hashes = new long[description.m_Stacks.Length];
                for (var stack = 0; stack < hashes.Length; stack++)
                {
                    references[stack][surface.id] = new Hash128[4];
                    surface.AddMidMipTexturesDataToDictionary(stack, RequestedMidMipLevels,
                        config.tileSize, description, pixels);
                }
                var mask = surface.ComputeVTLayersMask(description, references, hashes);
                for (var stack = 0; stack < surface.stackCount; stack++)
                {
                    var size = surface.GetUnbiasedStackTextureSize(stack);
                    if (size.x >= 0) grouper.Add(stack, size, mask, surface, hashes, description.m_MipBiasOverride);
                }
            }
            grouper.ResolveDuplicates(references, references.Length);
            grouper.GroupEntries(config, pixels, references);
            var cache = Database.AddAsset<MidMipCacheAsset>(AssetDataPath.Create(
                BridgeBuilder.Runtime.BridgeAssetInfo.OwnedDirectory(_owner, "Appearance/MidMips"),
                AtlasMaterialsGrouper.GetAssetName(config.tileSize, RequestedMidMipLevels), EscapeStrategy.None));
            _composed.Add(cache);
            using (var writer = new BinaryWriter(cache.GetWriteStream())) grouper.Write(writer);
            var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
            // Use native cache discovery/reservation before any generated render is published.
            streaming.MarkVTAssetsDirty();
            streaming.RefreshVT(Database);
            foreach (var surface in surfaces)
                surface.LoadVTAsync(streaming, streaming.mipBias, streaming.tileSize, RequestedMidMipLevels);
            Mod.Log.Info($"Composite native mid-mip cache saved: {cache.path}; surfaces={surfaces.Count}; levels={RequestedMidMipLevels}.");
        }
        finally
        {
            foreach (var data in pixels.Values) if (data.IsCreated) data.Dispose();
            grouper.Dispose();
        }
    }

    internal MovableRoadMaterial(string owner, string styleId, SurfaceAsset source, PreviewGeometry? preview)
    {
        StyleId = styleId;
        _preview = preview;
        _owner = owner;
        _name = owner + " Road Surface";
        if (!MovableAssetOwnership.Matches(owner, owner, preview))
            throw new ArgumentException("Moving road material requires an owned generation identity.");
        try
        {
            Asset = MovableRoadSurface.Create(owner, styleId, source, preview, _composed,
                out var uvScale, out var asphalt);
            UVScale = uvScale;
            AsphaltTexture = asphalt;
            Mod.Log.Info($"{owner}: native tiled road material={Asset.id}; UV/metre={UVScale}; " +
                $"prepared={_composed.Count == 0}; native road textures unchanged; texture baking=0.");
        }
        catch
        {
            for (var i = _composed.Count - 1; i >= 0; i--)
                _composed[i].database.DeleteAsset(_composed[i]);
            throw;
        }
    }
    // Transfer locally created resources only. Prepared materials remain immutable
    // source dependencies, outside preview cleanup and owner relocation.
    internal void Keep()
    {
        // Native VT writers derive streaming filenames from the full asset name.
        // Relocate only after all VT/cache writes have completed; keep every CID/payload.
        if (_preview == null)
        {
            foreach (var asset in _composed)
                MoveResource(asset);
        }
        _keep = true;
        // The repeating asphalt descriptor is a retained render dependency.
    }
    private void MoveResource(AssetData asset) => BridgeBuilder.Runtime.BridgePrefabPersistence.RelocateGenerated(
        asset, _owner, BridgeBuilder.Runtime.BridgeAssetInfo.AssetCategory(asset.GetMeta().extension)
            ?? throw new InvalidOperationException("Unknown composed resource type: " + asset.GetType().Name));

    public void Dispose()
    {
        if (!_keep) for (var i = _composed.Count - 1; i >= 0; i--)
            _composed[i].database.DeleteAsset(_composed[i]);
    }
}
