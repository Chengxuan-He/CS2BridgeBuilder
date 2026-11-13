using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Read source metadata for prototype composition while retaining the complete
// original native Surface/shader in the experimental source-material route.
internal static class MovableRoadSurface
{
    internal static bool TryCreate(string owner, string styleId, SurfaceAsset source,
        PreviewGeometry? preview, List<AssetData> owned, out SurfaceAsset material, out float uvScale, out TextureAsset asphalt, out string? error)
    {
        material = null!; uvScale = 0; asphalt = null!; error = null;
        string stage = "source-properties";
        try
        {
        var textures = new Dictionary<string, TextureAsset>();
        try
        {
            source.LoadProperties(useVT: true);
            if (source.materialTemplateHash != 17)
                { error = "Source does not provide the required prototype worldspace layer."; return false; }
            foreach (var pair in new[] { ("_WorldspaceAlbedo", "_BaseColorMap"),
                ("_WorldspaceNormalMap", "_NormalMap"), ("_WorldspaceMaskMap", "_MaskMap") })
            {
                if (!source.TryGetTexture(pair.Item1, out var texture) || texture == null || texture.isDummy)
                    { error = $"Missing required native texture {pair.Item1}."; return false; }
                textures.Add(pair.Item2, texture);
            }
            asphalt = textures["_BaseColorMap"];
            uvScale = source.TryGetFloatProperty("_WorldspaceUVScale", out var value) ? value :
                AssetDatabase.global.resources.materialLibrary.GetMaterial(17).GetFloat("_WorldspaceUVScale");
        }
        finally { source.UnloadProperties(); }

        stage = "original-shader";
        // Keep metadata used by prototype composition, but do not replace the
        // selected native shader with a base-only Default descriptor.
        return MovableSurfaceAssets.TryReadSource(source,out material,out error);

        }
        catch (Exception exception) { error = exception.GetType().Name + ": " + exception.Message; return false; }
        finally
        {
            if (error != null) { material = null!; asphalt = null!; uvScale = 0; }
            if (error != null) error = $"Material failure: stage={stage}; owner={owner}; style={styleId}; source={source?.name}; {error}";
        }
    }
}
