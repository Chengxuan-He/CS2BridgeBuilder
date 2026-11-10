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
    internal Material Material { get; private set; } = null!;
    internal IEnumerable<string> TextureProperties => _textureProperties;
    private readonly HashSet<Texture> _textures = new();
    private readonly HashSet<string> _textureProperties = new(StringComparer.Ordinal);

    // A VT layer can exist without a standalone texture binding. Only decoded
    // source textures count; inherited template defaults are not authored data.
    internal bool HasAuthoredTexture(string property) => Material.HasProperty(property) &&
        _textures.Contains(Material.GetTexture(property));

    private MovableSurfaceReader() { }

    internal static bool TryRead(SurfaceAsset source, out MovableSurfaceReader reader, out string? error)
    {
        var candidate = new MovableSurfaceReader(); reader = null!; error = null;
        try
        {
            if (!candidate.Read(source, out error)) { if(!candidate.TryDispose(out var cleanup))error += "; cleanup="+cleanup; return false; }
            reader = candidate; return true;
        }
        catch (Exception exception)
        {
            error = "Source material read failed: " + exception.GetType().Name + ": " + exception.Message;
            if(!candidate.TryDispose(out var cleanup))error += "; cleanup="+cleanup; return false;
        }
    }

    private bool Read(SurfaceAsset source, out string? error)
    {
        error = null;
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
                            if(!MovableTextureSource.TryRead(stream, TryDecompress,out texture,out error))return false;
                            _textures.Add(texture);
                            texture.name = item.Value.name;
                            textures.Add(item.Value, texture);
                        }
                        Material.SetTexture(item.Key, texture);
                        _textureProperties.Add(item.Key);
                    }
                    return true;
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
                    if (!MovableVtTextureSource.TryReadSurface(stream, Colossal.Compression.CompressionUtils.DecompressZstdWithMarker, out var textures, out error)) return false;
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
                        { error = $"Missing native VT texture {id} on {source.name}."; return false; }
                    if (!standalone.TryGetValue(asset, out var texture))
                    {
                        using var stream = asset.GetReadStream();
                        if (!MovableVtTextureSource.TryReadTexture(stream, Colossal.Compression.CompressionUtils.DecompressZstdWithMarker, out texture, out error)) return false;
                        _textures.Add(texture); texture.name = asset.name; standalone.Add(asset,texture);
                    }
                    Material.SetTexture(description.m_Stacks[stack].m_PropertyNames[layer], texture);
                }
            }
            finally { source.UnloadProperties(); }
        }
        catch (Exception exception) { error = exception.GetType().Name + ": " + exception.Message; return false; }
        return true;
    }

    internal bool TryDispose(out string? error)
    {
        string? failure=null;
        void Destroy(UnityEngine.Object value)
        {
            try{if(value!=null)UnityEngine.Object.Destroy(value);}
            catch(Exception exception){failure=(failure==null?"":failure+"; ")+exception;}
        }
        Destroy(Material);Material=null!;
        foreach(var texture in _textures)Destroy(texture);
        _textures.Clear();_textureProperties.Clear();error=failure;return error==null;
    }
    public void Dispose(){if(!TryDispose(out var error))Mod.Log.Error("Material reader cleanup: "+error);}

    private static bool TryDecompress(byte[] bytes,out byte[] result,out string? error)
    {
        result=Array.Empty<byte>();error=null;
        try
        {
            using var input = new NativeArray<byte>(bytes, Allocator.Temp);
            using var output = CompressionUtilities.Decompress(input, input.Length, CompressionFormat.ZSTD, out var length);
            if(length!=output.Length){error="Invalid native decompressed texture length.";return false;}
            result=output.ToArray();return true;
        }
        catch(Exception exception){error="Native texture decompression: "+exception;return false;}
    }
}
