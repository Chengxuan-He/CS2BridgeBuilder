using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Read the actual initialized game library. This observer neither creates a
// renderer nor certifies that a keyword supplies a valid native render context.
internal static class MovableNativeMaterialCapabilities
{
    private static bool _reported;

    internal static void Report(SurfaceAsset source, Material sourceSnapshot)
    {
        if (_reported) return;
        _reported = true;
        try
        {
            var library = AssetDatabase.global.resources.materialLibrary;
            var inspected = new HashSet<Shader>();
            bool Describe(Shader shader, string origin)
            {
                var properties = Enumerable.Range(0, shader.GetPropertyCount())
                    .Select(shader.GetPropertyName).ToArray();
                var keywords = shader.keywordSpace.keywords.Select(keyword => keyword.name).ToArray();
                bool world = new[] { "_WorldspaceAlbedo", "_WorldspaceNormalMap", "_WorldspaceMaskMap", "_WorldspaceUVScale" }
                    .All(properties.Contains);
                bool procedural = keywords.Contains("_GPU_ANIMATION_PROCEDURAL");
                Mod.Log.Info($"Native appearance capability: origin={origin}; shader={shader.name}; supported={shader.isSupported}; " +
                    $"worldPbrInterface={world}; proceduralKeyword={procedural}; properties=[{string.Join(",",properties)}]; " +
                    $"keywords=[{string.Join(",",keywords)}]. Interface presence is not rendering equivalence.");
                inspected.Add(shader);
                return world && procedural;
            }
            // Native Default family and original NetComposition family. Do not
            // enable keywords, change properties or register cloned resources.
            foreach (int template in new[] { 0, 13, 14, 17, 33 })
            {
                try
                {
                var material = library.GetMaterial(template);
                Describe(material.shader, "template=" + template);
                Mod.Log.Info($"Native appearance template: id={template}; name={material.name}; " +
                    $"passes=[{string.Join(",",Enumerable.Range(0,material.passCount).Select(material.GetPassName))}]; " +
                    $"enabledKeywords=[{string.Join(",",material.shaderKeywords)}].");
                }
                catch(Exception error) { Mod.Log.Error(error,"Native appearance template capture failed: "+template); }
            }
            int candidates = 0;
            var loaded = Resources.FindObjectsOfTypeAll<Shader>();
            foreach (var shader in loaded)
            {
                try
                {
                bool world = shader.FindPropertyIndex("_WorldspaceAlbedo") >= 0 && shader.FindPropertyIndex("_WorldspaceNormalMap") >= 0 &&
                    shader.FindPropertyIndex("_WorldspaceMaskMap") >= 0 && shader.FindPropertyIndex("_WorldspaceUVScale") >= 0;
                bool procedural = shader.keywordSpace.keywords.Any(keyword => keyword.name == "_GPU_ANIMATION_PROCEDURAL");
                if (world && procedural) candidates++;
                if (!inspected.Contains(shader) && (world || procedural)) Describe(shader, "loaded-game-shader");
                }
                catch(Exception error) { Mod.Log.Error(error,"Native appearance loaded-shader capture failed: "+shader.name); }
            }
            // Compose owns this private snapshot, already populated from the real
            // native template and source overrides. Do not load/unload shared assets.
            string V(Vector4 value) => $"({value.x:R},{value.y:R},{value.z:R},{value.w:R})";
            var values=new List<string>();
            for(int i=0;i<sourceSnapshot.shader.GetPropertyCount();i++)
            {
                var name=sourceSnapshot.shader.GetPropertyName(i);
                var type=sourceSnapshot.shader.GetPropertyType(i);
                if(type==UnityEngine.Rendering.ShaderPropertyType.Texture)
                {
                    var texture=sourceSnapshot.GetTexture(name);
                    values.Add(name+"="+(texture==null?"null":$"{texture.name}/{texture.width}x{texture.height}/{texture.filterMode}/{texture.wrapModeU}/{texture.wrapModeV}/aniso{texture.anisoLevel}"));
                }
                else if(type==UnityEngine.Rendering.ShaderPropertyType.Vector)values.Add(name+"="+V(sourceSnapshot.GetVector(name)));
                else if(type==UnityEngine.Rendering.ShaderPropertyType.Color)values.Add(name+"="+V(sourceSnapshot.GetColor(name)));
                else if(type==UnityEngine.Rendering.ShaderPropertyType.Int)values.Add(name+"="+sourceSnapshot.GetInteger(name));
                else values.Add(name+"="+sourceSnapshot.GetFloat(name).ToString("R"));
            }
            Mod.Log.Info($"Native appearance source snapshot: source={source.id}; template={source.materialTemplateHash}; VT={source.isVTMaterial}; values=[{string.Join(";",values)}].");
            Mod.Log.Info($"Native appearance capability summary: loadedShaders={loaded.Length}; worldPbrAndProceduralCandidates={candidates}; " +
                "readOnly=true; no resources registered; loaded-library scope only; no native render comparison performed.");
        }
        catch (Exception error)
        {
            // A diagnostic native API failure must not hide the original material
            // failure or be mistaken for a successful capability measurement.
            Mod.Log.Error(error, "Native appearance capability capture failed.");
        }
    }
}
