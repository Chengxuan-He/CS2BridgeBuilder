using Game.Prefabs;
using Colossal.IO.AssetDatabase;
using Colossal.IO.AssetDatabase.VirtualTexturing;
using Colossal.Mathematics;
using Colossal.Rendering;
using Unity.Entities;
using Unity.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Owns private preview meshes/materials and balances native surface leases. Source
/// geometry loading state belongs to the city's renderer and is never mutated.
/// </summary>
internal sealed class BridgePreviewRenderResources : IDisposable
{
    private delegate Mesh[] MeshCreator(string name, ref GeometryAsset.Data data);
    private static readonly MeshCreator? CreateMeshes = ResolveMeshCreator();
    private readonly BridgePreviewSession _session;
    private readonly Dictionary<RenderPrefab, Mesh[]> _sourceMeshes = new();
    private readonly Dictionary<(RenderPrefab, bool, bool), Material[]> _materials = new();
    private readonly Dictionary<RenderPrefab, MaterialPropertyBlock> _properties = new();
    private readonly Dictionary<(SurfaceAsset Surface, bool Network, bool Packed, RenderPrefab? Lane), Material> _surfaceMaterials = new();
    private readonly Dictionary<SurfaceAsset, Material> _sourceMaterials = new();
    private readonly List<Material> _ownedMaterials = new();
    private readonly TextureStreamingSystem? _streaming = World.DefaultGameObjectInjectionWorld?
        .GetExistingSystemManaged<TextureStreamingSystem>();
    private VTTextureRequester? _textureRequests;
    private readonly List<(int Stack, int Index)> _requestedTextures = new();
    private bool _disposed;

    internal BridgePreviewRenderResources(BridgePreviewSession session) => _session = session;

    internal MaterialPropertyBlock Properties(RenderPrefab prefab)
    {
        if (_properties.TryGetValue(prefab, out var block)) return block;
        block = new MaterialPropertyBlock();
        block.SetFloat("colossal_LodFade", 1f);
        block.SetVector("colossal_BuildingState", Vector4.zero);
        if (prefab.TryGet<ColorProperties>(out var colors) && colors.active)
            for (sbyte channel = 0; channel < 3; channel++)
                block.SetColor("colossal_ColorMask" + channel, colors.GetColor(0, channel));
        _properties.Add(prefab, block);
        return block;
    }

    internal Mesh[] Meshes(RenderPrefab prefab)
    {
        if (_disposed || prefab == null) return Array.Empty<Mesh>();
        if (_session.Geometry.TryGetMeshes(prefab, out var generated)) return generated;
        if (!_sourceMeshes.TryGetValue(prefab, out var meshes))
        {
            meshes = LoadPrivateMeshes(prefab);
            _sourceMeshes.Add(prefab, meshes);
        }
        return meshes;
    }

    private static MeshCreator? ResolveMeshCreator()
    {
        // Use the game's own converter to preserve packed vertex formats,
        // submeshes, normals, tangents and bounds exactly as authored.
        var method = typeof(GeometryAsset).GetMethod("CreateMeshes",
            BindingFlags.Static | BindingFlags.NonPublic, null,
            new[] { typeof(string), typeof(GeometryAsset.Data).MakeByRefType() }, null);
        return method == null ? null
            : Delegate.CreateDelegate(typeof(MeshCreator), method, false) as MeshCreator;
    }

    private Mesh[] LoadPrivateMeshes(RenderPrefab prefab)
    {
        var asset = prefab.geometryAsset;
        if (asset == null || CreateMeshes == null)
        {
            return Array.Empty<Mesh>();
        }

        var data = default(GeometryAsset.Data);
        var loading = default(GeometryAsset.Loading);
        try
        {
            // ObtainMeshes uses the asset's shared streaming buffers. A live
            // city may already have partially loaded/released those buffers.
            // Read the same asset into independent buffers instead; neither
            // reset the shared asset nor release the city's mesh references.
            var descriptor = asset.database.GetAsyncReadDescriptor(asset.id);
            GeometryAsset.LoadSync(descriptor, GeometryAsset.Attribute.All, ref data, ref loading);
            var meshes = CreateMeshes(_session.Name + "_" + prefab.name, ref data);
            foreach (var mesh in meshes)
                if (mesh != null) mesh.hideFlags = HideFlags.HideAndDontSave;
            return meshes;
        }
        catch (Exception)
        {
            return Array.Empty<Mesh>();
        }
        finally
        {
            try { loading.Dispose(); }
            finally { data.Dispose(); }
        }
    }

    internal Material[] Materials(RenderPrefab prefab, bool network = false, bool lane = false)
    {
        if (_disposed) return Array.Empty<Material>();
        if (!_materials.TryGetValue((prefab, network, lane), out var materials))
        {
            // ReleaseMaterials is a no-op. Acquire balanced surface leases here,
            // then copy the native material, including its virtual-texture bindings.
            var surfaces = prefab.surfaceAssets?.ToArray();
            var meshes = Meshes(prefab);
            var packed = network || (meshes.Length != 0 &&
                meshes[0].GetVertexAttributeDimension(VertexAttribute.Normal) == 2);
            materials = new Material[surfaces?.Length ?? 0];
            for (var i = 0; i < materials.Length; i++)
            {
                if (surfaces![i] == null) return Array.Empty<Material>();
                var material = MaterialFor(surfaces[i], network, packed, lane ? prefab : null);
                if (material == null) return Array.Empty<Material>();
                materials[i] = material;
            }
            _materials.Add((prefab, network, lane), materials);
        }
        return materials;
    }

    private Material? MaterialFor(SurfaceAsset surface, bool network, bool packed, RenderPrefab? lane)
    {
        var key = (surface, network, packed, lane);
        if (_surfaceMaterials.TryGetValue(key, out var cached)) return cached;
        var propertiesAcquired = false;
        var loadStarted = false;
        var retained = false;
        try
        {
            // SurfaceAsset.Load acquires properties only on the first object
            // reference, whereas Unload releases properties on every call. A
            // cached native material therefore needs one extra property lease
            // to keep preview cleanup from unloading the city's surface data.
            if (!_sourceMaterials.TryGetValue(surface, out var source))
            {
                if (surface.isObjectLoaded)
                {
                    propertiesAcquired = true;
                    surface.LoadProperties(useVT: true);
                }
                loadStarted = true;
                source = surface.Load(useVT: true);
                if (source == null) return null;
                _sourceMaterials.Add(surface, source);
            }
            retained = true;

            // VT-only textures are dummy TextureAssets without a database/file.
            // Native SurfaceAsset.Load deliberately skips those TextureAsset.Load
            // calls and binds their atlas parameters instead. Do not disable VT
            // or replace missing source files with blank textures in the preview.
            var material = new Material(source)
            {
                name = _session.Name + "_" + surface.name,
                hideFlags = HideFlags.HideAndDontSave,
                enableInstancing = false
            };
            _ownedMaterials.Add(material);
            HDMaterial.ValidateMaterial(material);
            ConfigurePreviewDepth(material, network, packed, lane);
            BindVirtualTextures(surface, material, true);
            _surfaceMaterials.Add(key, material);
            return material;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            if (!retained)
            {
                if (loadStarted) surface.Unload();
                else if (propertiesAcquired) surface.UnloadProperties();
            }
        }
    }

    internal void RefreshMaterialBindings()
    {
        // Streaming may update atlas parameters between scene assembly and the
        // render callback. Copy the latest native state onto our private material.
        foreach (var pair in _surfaceMaterials)
        {
            pair.Value.CopyPropertiesFromMaterial(_sourceMaterials[pair.Key.Surface]);
            pair.Value.enableInstancing = false;
            ConfigurePreviewDepth(pair.Value, pair.Key.Network, pair.Key.Packed, pair.Key.Lane);
            BindVirtualTextures(pair.Key.Surface, pair.Value, false);
        }
        if (_textureRequests == null) return;
        foreach (var request in _requestedTextures)
            _textureRequests.UpdateMaxPixel(request.Stack, request.Index, 1024f);
        _textureRequests.UpdateTexturesVTRequests();
    }

    private void BindVirtualTextures(SurfaceAsset surface, Material material, bool register)
    {
        // Same binding path as Game.Rendering.Debug.RenderPrefabRenderer.Instance.
        // CopyPropertiesFromMaterial/SetTextureParamBlock alone do NOT bind the
        // procedural VT stack. A city batch's global bindings are not a preview's.
        if (_streaming == null) return;
        var atlases = surface.VTAtlassingInfos ?? surface.PreReservedAtlassingInfos;
        if (atlases == null) return;
        for (var stack = 0; stack < Math.Min(2, atlases.Length); stack++)
        {
            var atlas = atlases[stack];
            if (atlas.indexInStack < 0) continue;
            SetKeyword(material, "ENABLE_VT", true);
            _streaming.BindMaterial(material, atlas.stackGlobalIndex, stack,
                _streaming.GetTextureParamBlock(atlas));
            if (!register) continue;
            _textureRequests ??= new VTTextureRequester(_streaming);
            var index = _textureRequests.RegisterTexture(stack, atlas.stackGlobalIndex,
                atlas.indexInStack, MathUtils.Bounds(new float2(0f), new float2(1f)), 1024f);
            if (index >= 0) _requestedTextures.Add((stack, index));
        }
    }

    private static void SetKeyword(Material material, string name, bool enabled)
    {
        var keyword = material.shader.keywordSpace.FindKeyword(name);
        if (keyword.isValid) material.SetKeyword(keyword, enabled);
    }

    private static void ConfigurePreviewDepth(Material material, bool network, bool packed, RenderPrefab? lane)
    {
        // Native net batches retain the surface's network shader variant. The
        // generated mesh and CompositionMatrix properties supply its deformation.
        // Object meshes use either packed source normals or unpacked derived ones.
        SetKeyword(material, "_TANGENTSPACE_OCTO", packed);
        if (network)
        {
            // ManagedBatchSystem assigns Roads to all native compositions.
            // SurfaceAsset alone does not carry this per-batch receiver mask.
            material.SetFloat("colossal_DecalLayerMask", math.asfloat((int)Game.Rendering.DecalLayers.Roads));
        }
        if (lane != null)
        {
            // ManagedBatchSystem selects these from CurveProperties, not from
            // the base surface material. Keep native curve/decal deformation.
            var tiling = lane.TryGet<CurveProperties>(out var curve) && curve.m_GeometryTiling;
            SetKeyword(material, "COLOSSAL_GEOMETRY_TILING", tiling);
            SetKeyword(material, "COLOSSAL_GEOMETRY_DILATED", false);
            SetKeyword(material, "COLOSSAL_GEOMETRY_DEFAULT", !tiling);
            if (lane.TryGet<DecalProperties>(out var decal))
                material.renderQueue = material.shader.renderQueue + decal.m_RendererPriority;
        }
        else if (!network)
        {
            SetKeyword(material, "COLOSSAL_GEOMETRY_TILING", false);
            SetKeyword(material, "COLOSSAL_GEOMETRY_DILATED", false);
            SetKeyword(material, "COLOSSAL_GEOMETRY_DEFAULT", true);
            SetKeyword(material, "_GPU_ANIMATION_PROCEDURAL", false);
            SetKeyword(material, "_GPU_ANIMATION_SHAPE", false);
            SetKeyword(material, "_GPU_ANIMATION_NORMAL", false);
            SetKeyword(material, "_GPU_ANIMATION_OFF", true);
        }
        if (material.HasProperty("colossal_LodFade")) material.SetFloat("colossal_LodFade", 1f);
        // HDRP BaseUnlitAPI validates ordinary opaque Forward passes as Equal:
        // that assumes the city's opaque depth prepass already drew this mesh.
        // Our explicit offscreen draws have no such prepass. Match the native
        // ThumbnailCustomPass LessEqual test on PRIVATE materials, including after
        // each native property refresh; never alter the shared city material.
        // Decal volume meshes must retain their native depth/blend state.
        if (lane != null && lane.Has<DecalProperties>()) return;
        var opaque = !material.HasProperty("_SurfaceType") || material.GetFloat("_SurfaceType") == 0f;
        if (!opaque) return;
        if (material.HasProperty("_ZTestDepthEqualForOpaque"))
            material.SetInt("_ZTestDepthEqualForOpaque", (int)CompareFunction.LessEqual);
        if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 1);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _textureRequests?.Dispose();
        _requestedTextures.Clear();
        foreach (var material in _ownedMaterials)
            if (material != null) UnityEngine.Object.DestroyImmediate(material);
        foreach (var surface in _sourceMaterials.Keys)
            try { surface.Unload(); }
            catch (Exception) {  }
        foreach (var meshes in _sourceMeshes.Values)
            foreach (var mesh in meshes)
                if (mesh != null) UnityEngine.Object.DestroyImmediate(mesh);
        _materials.Clear();
        _properties.Clear();
        _surfaceMaterials.Clear();
        _ownedMaterials.Clear();
        _sourceMaterials.Clear();
        _sourceMeshes.Clear();
    }
}
