using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Convert only the native network worldspace layer. The constant original recipe
// is prepared offline; selected-road composition remains a separate operation.
internal static class MovableRoadSurface
{
    internal static SurfaceAsset Create(string owner, string styleId, SurfaceAsset source,
        PreviewGeometry? preview, List<AssetData> owned, out float uvScale, out TextureAsset asphalt)
    {
        source.LoadProperties(useVT: true);
        var textures = new Dictionary<string, TextureAsset>();
        try
        {
            if (source.materialTemplateHash != 17)
                throw new NotSupportedException($"No measured worldspace conversion for '{source.name}'.");
            foreach (var pair in new[] { ("_WorldspaceAlbedo", "_BaseColorMap"),
                ("_WorldspaceNormalMap", "_NormalMap"), ("_WorldspaceMaskMap", "_MaskMap") })
            {
                if (!source.TryGetTexture(pair.Item1, out var texture) || texture == null || texture.isDummy)
                    throw new InvalidOperationException($"'{source.name}' has no persistent native texture for {pair.Item1}.");
                textures.Add(pair.Item2, texture);
            }
            asphalt = textures["_BaseColorMap"];
            uvScale = source.TryGetFloatProperty("_WorldspaceUVScale", out var value) ? value :
                AssetDatabase.global.resources.materialLibrary.GetMaterial(17).GetFloat("_WorldspaceUVScale");
            if (!float.IsFinite(uvScale) || uvScale <= 0f)
                throw new InvalidOperationException($"Invalid native road UV scale: {uvScale}.");
        }
        finally { source.UnloadProperties(); }

        var prepared = BridgePreparedMaterials.Road(styleId);
        if (textures.All(p => prepared.textures.TryGetValue(p.Key, out var texture)
                && texture != null && texture.id.Equals(p.Value.id))) return prepared;

        // Preserve different custom source textures until that source's offline
        // conversion is supplied. The constant control payload is never owned.
        textures.Add("_ControlMask", prepared.textures["_ControlMask"]);
        var database = preview?.NativeAssets ?? AssetDatabase.user;
        using var data = new Surface(owner + " Road Surface", "Default");
        var result = database.AddAsset(BridgePrefabPersistence.ResourcePath(data.name, "Appearance"), data);
        owned.Add(result);
        result.UpdateTextures(textures);
        result.UpdateKeywords(new HashSet<string>(result.keywords) { "_TANGENTSPACE_OCTO" });
        result.UpdateFloats(new Dictionary<string, float> { ["_Snow"] = 0f });
        if (!result.Save(mipBias: 0, force: false, saveTextures: false, vt: false))
            throw new InvalidOperationException("Could not save the native moving-road material descriptor.");
        preview?.RetainSurfaceProperties(result);
        result.UnloadProperties();
        return result;
    }
}
