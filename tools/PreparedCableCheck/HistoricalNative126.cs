using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// Native Powerline Cables, game 1.6.2f1: textureless CurvedTransparent (30).
// Source payload SHA-256: 8ea1eb5ec3e434c92c7dd010f71aaab58e8cf37b1a28e48f2757412edf3ca219.
// Converted lane geometry already contains the curve deformation. Default (0)
// supplies object procedural skinning without re-applying the network curve.
internal static class MovableCableSurface
{
    internal static bool TryCreate(SurfaceAsset source, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, out SurfaceAsset material, out string? error)
    {
        material = null!; error = null;
        string stage = "source-header", identity = "unresolved";
        try
        {
            identity = source.id.ToString();
            source.LoadProperties(useVT: true);
            Color color;
            var scalars = new Dictionary<string, float>();
            try
            {
                if (source.isVTMaterial || source.textures.Count != 0)
                {
                    error = $"Material representation incomplete: stage={stage}; owner={owner}; style={styleId}; source={identity}; textured/VT curved cable requires a complete native adapter. Source is not corrupt.";
                    return false;
                }
                var template = AssetDatabase.global.resources.materialLibrary.GetMaterial(source.materialTemplateHash);
                color = source.colors.TryGetValue("_BaseColor", out var value)
                    ? value : template.GetColor("_BaseColor");
                foreach (var property in new[] { "_Metallic", "_Smoothness", "_Snow" })
                    scalars[property] = source.floats.TryGetValue(property, out var scalar)
                        ? scalar : template.GetFloat(property);
            }
            finally { source.UnloadProperties(); }
            stage = "prepared-dependencies";
            var preparedResult = BridgePreparedMaterials.Cable(styleId);
        if(!preparedResult.Success){error=preparedResult.Error;return false;}
        var prepared=preparedResult.Value!;
            bool matches = prepared.colors.TryGetValue("_BaseColor", out var preparedColor) && preparedColor.Equals(color);
            foreach (var scalar in scalars)
                matches &= prepared.floats.TryGetValue(scalar.Key, out var preparedValue) && preparedValue.Equals(scalar.Value);
            // Compare the complete resolved recipe, not CID/name or approximate color.
            // The immutable source catalogue owns this material and its properties.
            if (matches) { material = prepared; return true; }
            stage = "descriptor-registration";
            var name = owner + " Native Cable " + source.id;
            var database = preview?.NativeAssets ?? AssetDatabase.user;
            using var data = new Surface(name + " Surface", "Default");
            var surface = database.AddAsset(BridgePrefabPersistence.ResourcePath(data.name, "Appearance"), data);
            assets.Add(surface);

            try
            {
                // Differing overrides still keep their own descriptor while that conversion
                // coverage is migrated. The neutral control is a verified native game texture.
                // Never add it to assets: deleting this bridge must not destroy the source.
                var control = prepared.textures["_ControlMask"];

                surface.UpdateTextures(new Dictionary<string, TextureAsset> { ["_ControlMask"] = control });
                surface.UpdateColors(new Dictionary<string, Color>
                {
                    ["_BaseColor"] = color
                });
                // Retain source overrides and source-template defaults. Distinct cable
                // descriptors must not inherit the first cable's appearance.
                surface.UpdateFloats(scalars);
                surface.UpdateKeywords(new HashSet<string>(surface.keywords) { "_TANGENTSPACE_OCTO" });
                stage = "descriptor-save";
                if (!surface.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
                {
                    error = $"Material persistence failed: stage={stage}; owner={owner}; style={styleId}; source={identity}; native cable Save returned false.";
                    return false;
                }
                preview?.RetainSurfaceProperties(surface);
                material = surface; return true;
            }
            finally { surface.UnloadProperties(); }
        }
        catch (Exception exception)
        {
            material = null!;
            error = $"Material operation failed: stage={stage}; owner={owner}; style={styleId}; source={identity}; " +
                exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }
}
