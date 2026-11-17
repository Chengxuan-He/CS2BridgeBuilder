using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;
using UnityEngine.Rendering;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    // Explicit native vertex-protocol conversion; source appearance channels are unchanged.
    internal async Task<SurfaceAsset[]> CreateRecordedNetworkSurfacesAsync(RenderPrefab source, string name, Color[] colors, int[]? sourceMeshIndices = null)
    {
        var originals = source.surfaceAssets.ToArray();
        var result = new SurfaceAsset[originals.Length];
        foreach (var slot in sourceMeshIndices ?? Enumerable.Range(0, result.Length).ToArray())
        {
            var original = originals[slot];
            original.LoadProperties(true);
            try
            {
                var library = AssetDatabase.global.resources.materialLibrary;
                var from = library.GetMaterial(original.materialTemplateHash);
                var to = library.GetMaterial(library.GetMaterialHash("NetCompositionMeshLit"));
                if (from == null || to == null)
                    throw new InvalidOperationException("Recorded native network material input is unavailable.");
                byte[] Read(AssetData asset)
                {
                    using var stream = asset.GetReadStream(); using var buffer = new MemoryStream();
                    stream.CopyTo(buffer); return buffer.ToArray();
                }
                var individual = new Dictionary<(int Stack, int Layer), byte[]>();
                byte[]? input = original.hasVTSurfaceAsset ? Read(original.vtSurfaceAsset) : null;
                int tile = input == null ? 0 : BitConverter.ToInt32(input, 18), levels = 0;
                string? error;
                if (input != null && !BridgeMidMipReferences.TryRequiredLevels(new MemoryStream(input, false), true, out levels, out error))
                    throw new InvalidDataException(error);
                int ordinal = 0;
                foreach (var cid in original.preProcessedTextureGuids)
                {
                    int stack = ordinal / 4, layer = ordinal++ % 4;
                    // Follow SurfaceAsset.LoadTexturesVT: packed layers take precedence.
                    if (input != null && stack < BitConverter.ToInt32(input, 490) &&
                        layer < BitConverter.ToInt32(input, 26 + stack * 40 + 24) && input[106 + (stack * 4 + layer) * 48] != 0) continue;
                    if (!cid.isValid) continue;
                    if (!original.database.TryGetAsset(cid, out VTTextureAsset asset)) asset = AssetDatabase.global.GetAsset<VTTextureAsset>(cid);
                    var bytes = Read(asset);
                    tile = BitConverter.ToUInt16(bytes, 21);
                    if (!BridgeMidMipReferences.TryRequiredLevels(new MemoryStream(bytes, false), false, out var required, out error))
                        throw new InvalidDataException(error);
                    levels = Math.Max(levels, required);
                    individual.Add((stack, layer), bytes);
                }
                byte[]? subset = null;
                if (levels > 0)
                {
                    if (!MovableLaneMidMips.TryRequiredCaches(original, _bridgeName, out var caches, out error)) throw new InvalidDataException(error);
                    foreach (var cache in caches)
                    {
                        using var stream = cache.GetReadStream();
                        if (!MovableMidMipSubset.TryCopy(stream, original.id.guid.ToString(), original.id.guid.ToString(), tile, levels, out var candidate, out error))
                            throw new InvalidDataException(error);
                        if (candidate == null) continue;
                        if (subset != null && !subset.SequenceEqual(candidate)) throw new InvalidDataException("Conflicting original material mip carriers.");
                        subset = candidate;
                    }
                }
                var channels = await GenerationWork.Background("native network material mip blocks", () =>
                {
                    var extracted = input == null ? new Dictionary<(int Stack, int Layer), byte[]>() :
                        NativeVtMipPyramid.Convert(input, subset, Colossal.Compression.CompressionUtils.DecompressZstdWithMarker);
                    foreach (var entry in individual)
                        extracted.Add(entry.Key, NativeVtMipPyramid.ConvertTexture(entry.Value, subset, entry.Key.Stack, entry.Key.Layer,
                            Colossal.Compression.CompressionUtils.DecompressZstdWithMarker));
                    return extracted;
                });
                var database = _previewGeometry?.NativeAssets ?? AssetDatabase.user;
                var textures = new Dictionary<string, TextureAsset>();
                var created = new List<AssetData>();
                bool complete = false;
                try
                {
                    var description = library.GetMaterialDescription(original.materialTemplateHash);
                    foreach (var channel in channels)
                    {
                        var property = description.m_Stacks[channel.Key.Stack].m_PropertyNames[channel.Key.Layer];
                        var texture = database.AddAsset<TextureAsset>(BridgePrefabPersistence.ResourcePath(name + " " + original.id + " " + property, "Appearance"));
                        created.Add(texture);
                        using (var output = texture.GetWriteStream()) output.Write(channel.Value, 0, channel.Value.Length);
                        textures.Add(property, texture);
                    }
                    using var data = new Surface(name + " " + original.id, "NetCompositionMeshLit");
                    var surface = database.AddAsset(BridgePrefabPersistence.ResourcePath(data.name, "Appearance"), data);
                    created.Add(surface);
                    try
                    {
                        var floats = new Dictionary<string, float>(); var ints = new Dictionary<string, int>();
                        var vectors = new Dictionary<string, Vector4>(); var tint = new Dictionary<string, Color>();
                        for (int i = 0; i < from.shader.GetPropertyCount(); i++)
                        {
                            string property = from.shader.GetPropertyName(i);
                            if (!to.HasProperty(property)) continue; // Native instance/VT interfaces are replaced by explicit network inputs.
                            switch (from.shader.GetPropertyType(i))
                            {
                                case ShaderPropertyType.Float: case ShaderPropertyType.Range:
                                    floats[property] = original.floats.TryGetValue(property, out var f) ? f : from.GetFloat(property); break;
                                case ShaderPropertyType.Int:
                                    ints[property] = original.ints.TryGetValue(property, out var n) ? n : from.GetInteger(property); break;
                                case ShaderPropertyType.Vector:
                                    vectors[property] = original.vectors.TryGetValue(property, out var v) ? v : from.GetVector(property); break;
                                case ShaderPropertyType.Color:
                                    tint[property] = original.colors.TryGetValue(property, out var c) ? c : from.GetColor(property); break;
                            }
                        }
                        for (int i = 0; i < colors.Length; i++) tint["_ColorMask" + i] = colors[i];
                        surface.UpdateFloats(floats); surface.UpdateInts(ints); surface.UpdateVectors(vectors); surface.UpdateColors(tint);
                        surface.UpdateTextures(textures);
                        var keywords = new HashSet<string>(from.shaderKeywords.Concat(original.keywords).Where(k => to.shader.keywordSpace.FindKeyword(k).isValid)) { "_TANGENTSPACE_OCTO" };
                        surface.UpdateKeywords(keywords);
                        if (!surface.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
                            throw new InvalidOperationException("Native network Surface save failed.");
                        _previewGeometry?.RetainSurfaceProperties(surface);
                        if (BridgeRoadGeneration.CurrentDependencies is { } dependencies && !dependencies.Material(surface, Array.Empty<AssetData>()))
                            throw new InvalidOperationException(dependencies.ResolutionError);
                        result[slot] = surface; complete = true;
                    }
                    finally { surface.UnloadProperties(); }
                }
                finally
                {
                    if (!complete) foreach (var asset in created.AsEnumerable().Reverse()) database.DeleteAsset(asset);
                }
            }
            finally { original.UnloadProperties(); }
        }
        return result;
    }
}
