using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Native;
using Colossal.IO.AssetDatabase;
using Unity.Collections;
using UnityEngine;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Generation-scoped private mip-zero inputs. Never modify or unload a live
// game texture/material, and never inherit its current rendering-quality bias.
internal sealed class MovableSurfaceReader : IDisposable
{
    internal Material Material { get; }
    internal IEnumerable<string> TextureProperties => _textureProperties;
    private readonly HashSet<Texture> _textures = new();
    private readonly HashSet<string> _textureProperties = new(StringComparer.Ordinal);

    // A VT layer can exist without a standalone texture binding. Only decoded
    // source textures count; inherited template defaults are not authored data.
    internal bool HasAuthoredTexture(string property) => Material.HasProperty(property) &&
        _textures.Contains(Material.GetTexture(property));

    internal MovableSurfaceReader(SurfaceAsset source)
    {
        source.LoadHeader();
        var library = AssetDatabase.global.resources.materialLibrary;
        var template = library.GetMaterial(source.materialTemplateHash);
        Material = new Material(template);
        try
        {
            source.LoadProperties(true);
            try
            {
                foreach (var item in source.floats) Material.SetFloat(item.Key, item.Value);
                foreach (var item in source.ints) Material.SetInt(item.Key, item.Value);
                foreach (var item in source.vectors) Material.SetVector(item.Key, item.Value);
                foreach (var item in source.colors) Material.SetColor(item.Key, item.Value);
                foreach (var keyword in source.keywords) Material.EnableKeyword(keyword);
                if (!source.isVTMaterial)
                {
                    // Private full-resolution data is independent of quality settings
                    // and the game's cached TextureAsset instance/reference counts.
                    var textures = new Dictionary<TextureAsset, Texture2D>();
                    foreach (var item in source.textures)
                    {
                        if (item.Value == null) continue;
                        if (!textures.TryGetValue(item.Value, out var texture))
                        {
                            using var stream = item.Value.GetReadStream();
                            texture = MovableTextureSource.Read(stream, Decompress);
                            texture.name = item.Value.name;
                            _textures.Add(texture);
                            textures.Add(item.Value, texture);
                        }
                        Material.SetTexture(item.Key, texture);
                        _textureProperties.Add(item.Key);
                    }
                    return;
                }
                var description = library.GetMaterialDescription(source.materialTemplateHash);
                // VT channels are declared by stacks. Ordinary materials above use
                // their authored TextureAsset bindings; their stack list may be empty.
                foreach (var stack in description.m_Stacks)
                    foreach (var property in stack.m_PropertyNames) _textureProperties.Add(property);
                var handled = new HashSet<(int,int)>();
                if (source.hasVTSurfaceAsset)
                {
                    using var stream = source.vtSurfaceAsset.GetReadStream();
                    var textures = MovableVtTextureSource.ReadSurface(stream, Colossal.Compression.CompressionUtils.DecompressZstdWithMarker);
                    foreach (var texture in textures.Values) _textures.Add(texture);
                    foreach (var item in textures)
                    {
                        Material.SetTexture(description.m_Stacks[item.Key.Stack].m_PropertyNames[item.Key.Layer], item.Value);
                        handled.Add(item.Key);
                    }
                }
                var ids = source.preProcessedTextureGuids.ToArray();
                var standalone = new Dictionary<VTTextureAsset,Texture2D>();
                for (int stack = 0; stack < source.stackCount; stack++)
                for (int layer = 0; layer < description.m_Stacks[stack].m_PropertyNames.Length; layer++)
                {
                    if (handled.Contains((stack, layer))) continue;
                    var id = ids[stack * 4 + layer];
                    if (!id.isValid) continue;
                    if (!source.database.TryGetAsset(id, out VTTextureAsset asset) &&
                        !AssetDatabase.global.TryGetAsset(id, out asset))
                        throw new InvalidOperationException($"Missing native VT texture {id} on {source.name}.");
                    if (!standalone.TryGetValue(asset, out var texture))
                    {
                        using var stream = asset.GetReadStream();
                        texture = MovableVtTextureSource.ReadTexture(stream, Colossal.Compression.CompressionUtils.DecompressZstdWithMarker);
                        texture.name = asset.name; _textures.Add(texture); standalone.Add(asset,texture);
                    }
                    Material.SetTexture(description.m_Stacks[stack].m_PropertyNames[layer], texture);
                }
            }
            finally { source.UnloadProperties(); }
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        UnityEngine.Object.Destroy(Material);
        foreach (var texture in _textures) UnityEngine.Object.Destroy(texture);
    }

    private static byte[] Decompress(byte[] bytes)
    {
        using var input = new NativeArray<byte>(bytes, Allocator.Temp);
        using var output = CompressionUtilities.Decompress(input, input.Length, CompressionFormat.ZSTD, out var length);
        if (length != output.Length) throw new InvalidDataException("Invalid native decompressed texture length.");
        return output.ToArray();
    }
}
