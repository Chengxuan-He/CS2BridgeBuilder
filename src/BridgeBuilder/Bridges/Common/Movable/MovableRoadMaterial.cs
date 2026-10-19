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

// Owns the native tiled source descriptor and the composed appearance resources.
// The latter are shared across LODs and persist without manager callbacks.
internal sealed class MovableRoadMaterial : IDisposable
{
    private const int RequestedMidMipLevels = 3;
    internal SurfaceAsset Asset { get; }
    internal float UVScale { get; }
    private bool _keep;
    private readonly PreviewGeometry? _preview;
    private readonly string _owner;
    private readonly List<AssetData> _composed = new();

    internal TextureAsset SaveTexture(UnityEngine.Texture2D texture, int channel, int page)
    {
        var asset = Asset.database.AddAsset<TextureAsset>(AssetDataPath.Create("BridgeBuilder",
            Asset.name + " Composite " + page + " " + channel, EscapeStrategy.None));
        _composed.Add(asset);
        asset.SetData(texture);
        // Use the native texture writer's lossless compression setting as well as BC7.
        asset.Save(compress: true);
        asset.Unload();
        return asset;
    }

    internal SurfaceAsset SaveRepeatingAsphalt(Dictionary<string, TextureAsset> textures)
    {
        // Match the release below: the existing preview lease must remain alive.
        Asset.LoadProperties(useVT: false);
        Asset.UpdateTextures(textures);
        if (!Asset.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
            throw new InvalidOperationException("Could not persist the prototype asphalt repeat unit.");
        _preview?.RetainSurfaceProperties(Asset);
        Asset.UnloadProperties();
        return Asset;
    }

    internal SurfaceAsset SaveRepeatingGrating(Dictionary<string, TextureAsset> textures, int page)
    {
        using var data = new Surface(Asset.name + " Repeat " + page, "DefaultCutoutDoubleSided");
        var asset = Asset.database.AddAsset(AssetDataPath.Create("BridgeBuilder", data.name, EscapeStrategy.None), data);
        _composed.Add(asset);
        asset.UpdateTextures(textures);
        asset.UpdateKeywords(new HashSet<string>(asset.keywords) { "_TANGENTSPACE_OCTO" });
        asset.UpdateFloats(new Dictionary<string,float> { ["_Snow"] = 0f });
        if (!asset.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
            throw new InvalidOperationException("Could not save native repeating grating.");
        _preview?.RetainSurfaceProperties(asset);
        asset.UnloadProperties();
        return asset;
    }

    internal SurfaceAsset SaveComposite(Dictionary<string, TextureAsset> textures, int page)
    {
        var timer = System.Diagnostics.Stopwatch.StartNew();
        using var data = new Surface(Asset.name + " Composite " + page, "DefaultCutoutDoubleSided");
        var asset = Asset.database.AddAsset(AssetDataPath.Create("BridgeBuilder", data.name, EscapeStrategy.None), data);
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
        MoveResource(Asset);
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
            var cache = Asset.database.AddAsset<MidMipCacheAsset>(AssetDataPath.Create(
                BridgeBuilder.Runtime.BridgeAssetInfo.OwnedDirectory(_owner, "Appearance/MidMips"),
                AtlasMaterialsGrouper.GetAssetName(config.tileSize, RequestedMidMipLevels), EscapeStrategy.None));
            _composed.Add(cache);
            using (var writer = new BinaryWriter(cache.GetWriteStream())) grouper.Write(writer);
            var streaming = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TextureStreamingSystem>();
            // Use native cache discovery/reservation before any generated render is published.
            streaming.MarkVTAssetsDirty();
            streaming.RefreshVT(Asset.database);
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

    internal MovableRoadMaterial(string owner, SurfaceAsset source, PreviewGeometry? preview)
    {
        _preview = preview;
        _owner = owner;
        if (!MovableAssetOwnership.Matches(owner, owner, preview))
            throw new ArgumentException("Moving road material requires an owned generation identity.");
        source.LoadProperties(useVT: true);
        SurfaceAsset? created = null;
        try
        {
            // This conversion is defined for the native network worldspace layer.
            // Atlas bindings and VT provenance IDs are not interchangeable with it.
            if (source.materialTemplateHash != 17)
                throw new NotSupportedException($"No measured worldspace conversion for '{source.name}'.");
            var textures = new Dictionary<string, TextureAsset>();
            Bind("_WorldspaceAlbedo", "_BaseColorMap");
            Bind("_WorldspaceNormalMap", "_NormalMap");
            Bind("_WorldspaceMaskMap", "_MaskMap");
            UVScale = source.TryGetFloatProperty("_WorldspaceUVScale", out var scale) ? scale :
                AssetDatabase.global.resources.materialLibrary.GetMaterial(17).GetFloat("_WorldspaceUVScale");
            if (!float.IsFinite(UVScale) || UVScale <= 0f)
                throw new InvalidOperationException($"Invalid native road UV scale: {UVScale}.");

            var database = preview?.NativeAssets ?? AssetDatabase.user;
            using var data = new Surface(owner + " Road Surface", "Default");
            created = database.AddAsset(AssetDataPath.Create("BridgeBuilder", data.name,
                EscapeStrategy.None), data);
            // Network worldspace asphalt has no object palette regions. SG_Default's
            // missing _ControlMask is WHITE, enabling every city color region instead.
            // Persist a zero mask so preview, native batches and cold loads agree.
            var control = database.AddAsset<TextureAsset>(AssetDataPath.Create("BridgeBuilder",
                owner + " Road Surface Control", EscapeStrategy.None));
            _composed.Add(control);
            var zero = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            try
            {
                zero.SetPixel(0, 0, Color.clear);
                control.SetData(zero);
                control.Save(compress: true);
                control.Unload();
            }
            finally { UnityEngine.Object.Destroy(zero); }
            textures.Add("_ControlMask", control);
            created.UpdateTextures(textures);
            created.UpdateKeywords(new HashSet<string>(created.keywords) { "_TANGENTSPACE_OCTO" });
            created.UpdateFloats(new Dictionary<string,float> { ["_Snow"] = 0f });
            // The overload explicitly disables texture saving and VT production.
            // Save only the descriptor and its CID; native source textures are unchanged.
            if (!created.Save(mipBias: 0, force: false, saveTextures: false, vt: false))
                throw new InvalidOperationException("Could not save the native moving-road material descriptor.");
            _preview?.RetainSurfaceProperties(created);
            created.UnloadProperties();
            Asset = created;
            Mod.Log.Info($"{owner}: native tiled road material={Asset.id}; UV/metre={UVScale}; " +
                "descriptors=1; generated control mask=1x1 zero; native road textures unchanged; texture baking=0.");

            void Bind(string from, string to)
            {
                if (!source.TryGetTexture(from, out var texture) || texture == null || texture.isDummy)
                    throw new InvalidOperationException($"'{source.name}' has no persistent native texture for {from}.");
                textures.Add(to, texture);
            }
        }
        catch
        {
            if (created != null) created.database.DeleteAsset(created);
            foreach (var asset in _composed) asset.database.DeleteAsset(asset);
            throw;
        }
        finally { source.UnloadProperties(); }
    }

    // The completed render graph owns the saved descriptor. Preview descriptors
    // remain in PreviewGeometry's private database and die with that session.
    internal void Keep()
    {
        // Native VT writers derive streaming filenames from the full asset name.
        // Relocate only after all VT/cache writes have completed; keep every CID/payload.
        if (_preview == null)
        {
            MoveResource(Asset);
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
        if (!_keep) Asset.database.DeleteAsset(Asset);
        if (!_keep) for (var i = _composed.Count - 1; i >= 0; i--)
            _composed[i].database.DeleteAsset(_composed[i]);
    }
}
