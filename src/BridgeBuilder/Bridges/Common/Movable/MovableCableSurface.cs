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
    internal const string SourceCid = "106cc56e9f15a135bb6ea7b99d675c2d";

    internal static SurfaceAsset Create(string name, PreviewGeometry? preview, List<AssetData> assets)
    {
        var database = preview?.NativeAssets ?? AssetDatabase.user;
        using var data = new Surface(name + " Native Cable Surface", "Default");
        var surface = database.AddAsset(BridgePrefabPersistence.ResourcePath(data.name, "Appearance"), data);
        assets.Add(surface);

        // SG_Default's missing control map is white and would enable city palette
        // tinting. The native curved cable has no palette regions. Store zero once
        // alongside this owner-local descriptor, shared by every LOD in the graph.
        var control = database.AddAsset<TextureAsset>(
            BridgePrefabPersistence.ResourcePath(name + " Native Cable Control", "Appearance"));
        assets.Add(control);
        var zero = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
        try
        {
            zero.SetPixel(0, 0, Color.clear);
            control.SetData(zero);
            control.Save(compress: true);
            control.Unload();
        }
        finally { UnityEngine.Object.Destroy(zero); }

        surface.UpdateTextures(new Dictionary<string, TextureAsset> { ["_ControlMask"] = control });
        surface.UpdateColors(new Dictionary<string, Color>
        {
            ["_BaseColor"] = new Color(0.018867909908294678f, 0.018867909908294678f,
                0.018867909908294678f, 1f)
        });
        // Color is the source asset override; these scalars are the measured
        // CurvedTransparent template defaults, not Default's metallic=1 defaults.
        surface.UpdateFloats(new Dictionary<string, float>
        {
            ["_Metallic"] = 0f, ["_Smoothness"] = 0.5f, ["_Snow"] = 0f
        });
        surface.UpdateKeywords(new HashSet<string>(surface.keywords) { "_TANGENTSPACE_OCTO" });
        if (!surface.Save(mipBias: 0, force: true, saveTextures: false, vt: false))
            throw new InvalidOperationException("Could not persist native skinned cable material.");
        preview?.RetainSurfaceProperties(surface);
        surface.UnloadProperties();
        return surface;
    }
}
