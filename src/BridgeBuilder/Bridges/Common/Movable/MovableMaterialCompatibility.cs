using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using TextureAsset = Colossal.IO.AssetDatabase.TextureAsset;

namespace BridgeBuilder.Bridges;

// A visible native-object result for valid source features outside the exact
// receiver protocol. Selection uses semantic properties, never source identities.
internal static class MovableMaterialCompatibility
{
    internal readonly struct Mapping
    {
        internal readonly bool Planar;
        internal readonly float Scale;
        internal readonly string? Differences;
        internal Mapping(bool planar, float scale, string? differences)
        { Planar = planar; Scale = scale; Differences = differences; }
        internal MovableRoadGeometry.Vertex Apply(MovableRoadGeometry.Vertex vertex)
            => !Planar ? vertex : new MovableRoadGeometry.Vertex(vertex.Position, vertex.Normal,
                vertex.Tangent, new Vector4(vertex.Position.x * Scale, vertex.Position.z * Scale, 0, 0), vertex.Color);
    }

    internal static bool TryCreate(SurfaceAsset source, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, string reason,
        out SurfaceAsset result, out Mapping mapping, out string? error, bool projected = false)
    {
        bool success=TryCreateCore(source,owner,styleId,preview,assets,reason,out result,out mapping,out error,projected);
        if(!success || error!=null){result=null!;mapping=default;return false;}
        return true;
    }

    private static bool TryCreateCore(SurfaceAsset source, string owner, string styleId,
        PreviewGeometry? preview, List<AssetData> assets, string reason,
        out SurfaceAsset result, out Mapping mapping, out string? error, bool projected = false)
    {
        result=null!;mapping=default;error=null;
        MovableSurfaceReader? decoded=null;
        try
        {
            source.LoadProperties(true);
            var template=AssetDatabase.global.resources.materialLibrary.GetMaterial(source.materialTemplateHash);
            var keywords=new HashSet<string>(template.shaderKeywords);keywords.UnionWith(source.keywords);
            float Scalar(string name,float fallback) => source.floats.TryGetValue(name,out var value)?value:template.HasProperty(name)?template.GetFloat(name):fallback;
            Color ColorValue(string name,Color fallback)=>source.colors.TryGetValue(name,out var value)?value:template.HasProperty(name)?template.GetColor(name):fallback;
            var color=ColorValue("_BaseColor",ColorValue("_Color",Color.white));
            bool alpha=keywords.Contains("_SURFACE_TYPE_TRANSPARENT") || keywords.Contains("_ALPHATEST_ON") || keywords.Contains("_MATERIAL_AFFECTS_ALBEDO") || template.renderQueue>=2450;
            var properties=new HashSet<string>(source.textures.Keys,StringComparer.Ordinal);
            if(source.isVTMaterial)
                foreach(var stack in AssetDatabase.global.resources.materialLibrary.GetMaterialDescription(source.materialTemplateHash).m_Stacks)
                    foreach(var property in stack.m_PropertyNames)properties.Add(property);
            string? Select(params string[] names)
            {
                foreach(var name in names)
                    if(properties.Contains(name) && (source.isVTMaterial || source.textures.TryGetValue(name,out var value)&&value!=null))return name;
                return null;
            }
            var albedo=Select("_BaseColorMap","_BaseMap","_MainTex","_AlbedoMap","_DiffuseMap","_WorldspaceAlbedo");
            albedo ??= properties.OrderBy(p=>p,StringComparer.Ordinal).FirstOrDefault(p=>
                (p.IndexOf("albedo",StringComparison.OrdinalIgnoreCase)>=0 || p.IndexOf("diffuse",StringComparison.OrdinalIgnoreCase)>=0 || p.IndexOf("basecolor",StringComparison.OrdinalIgnoreCase)>=0)
                && (source.isVTMaterial || source.textures.TryGetValue(p,out var value)&&value!=null));
            bool planar=albedo=="_WorldspaceAlbedo";
            float scale=planar?Scalar("_WorldspaceUVScale",1):1;
            var selected=new Dictionary<string,string>();
            if(albedo!=null)selected.Add("_BaseColorMap",albedo);
            void Channel(string target,params string[] names){var property=Select(names);if(property!=null)selected.Add(target,property);}
            if(planar){Channel("_NormalMap","_WorldspaceNormalMap");Channel("_MaskMap","_WorldspaceMaskMap");}
            else {Channel("_NormalMap","_NormalMap","_BumpMap");Channel("_MaskMap","_MaskMap","_MetallicGlossMap");}
            Channel("_ControlMask","_ControlMask");Channel("_EmissiveColorMap","_EmissiveColorMap","_EmissionMap");
            var database=preview?.NativeAssets??AssetDatabase.user;
            var textures=new Dictionary<string,TextureAsset>();
            var extracted=new Dictionary<string,TextureAsset>();
            foreach(var pair in selected)
            {
                if(source.textures.TryGetValue(pair.Value,out var existing)&&existing!=null&&!existing.isDummy)
                {textures.Add(pair.Key,existing);continue;}
                if(!source.isVTMaterial){error="Missing required source texture: "+pair.Value;return false;}
                if(decoded==null && !MovableSurfaceReader.TryRead(source,out decoded,out error))return false;
                if(!decoded.HasAuthoredTexture(pair.Value))continue; // Unpopulated native optional layer.
                if(!extracted.TryGetValue(pair.Value,out var extractedTexture))
                {
                    if(decoded.Material.GetTexture(pair.Value) is not Texture2D pixels)
                    {error="Native source layer is not readable texture data: "+pair.Value;return false;}
                    extractedTexture=database.AddAsset<TextureAsset>(BridgePrefabPersistence.ResourcePath(owner+" Compatible Texture "+source.id+" "+pair.Key,"Appearance"));
                    assets.Add(extractedTexture);extractedTexture.SetData(pixels);extractedTexture.Save(compress:preview==null);extractedTexture.Unload();
                    extracted.Add(pair.Value,extractedTexture);
                }
                textures.Add(pair.Key,extractedTexture);
            }
            if(!textures.ContainsKey("_ControlMask"))
            {
                var neutral=BridgePreparedMaterials.Road(styleId);
                if(!neutral.Success){error=neutral.Error;return false;}
                if(neutral.Value!.textures.TryGetValue("_ControlMask",out var control)&&control!=null)textures.Add("_ControlMask",control);
            }
            using var data=new Surface(owner+" Compatible Material "+source.id,projected?"DefaultCutout":alpha?"DefaultCutoutDoubleSided":"DefaultDoubleSided");
            var output=database.AddAsset(BridgePrefabPersistence.ResourcePath(data.name,"Appearance"),data);assets.Add(output);
            try
            {
                output.UpdateTextures(textures);
                output.UpdateColors(new Dictionary<string,Color>{["_BaseColor"]=color,["_EmissiveColor"]=ColorValue("_EmissiveColor",ColorValue("_EmissionColor",Color.black))});
                var floats=new Dictionary<string,float>{["_Metallic"]=Scalar("_Metallic",0),["_Smoothness"]=Scalar("_Smoothness",.5f),
                    ["_NormalScale"]=Scalar("_NormalScale",1),["_EmissiveIntensity"]=Scalar("_EmissiveIntensity",1),["_Snow"]=0};
                // Preserve native deterministic coverage; do not turn transparent
                // compression residue into solid artwork with an epsilon cutoff.
                if(!projected){floats["_AlphaClipThreshold"]=.001f;floats["_AlphaCutoff"]=.001f;}
                output.UpdateFloats(floats);
                var outputKeywords=new HashSet<string>(output.keywords){"_TANGENTSPACE_OCTO"};
                if(projected)outputKeywords.Add("_ALPHATEST_ON");
                output.UpdateKeywords(outputKeywords);
                if(!output.Save(mipBias:0,force:true,saveTextures:false,vt:false))
                {error="Native compatibility Surface save returned false.";return false;}
                preview?.RetainSurfaceProperties(output);result=output;
            }
            finally{output.UnloadProperties();}
            var differences="source-layer selection; unsupported blend/vertex features omitted; template textures without native identities use target defaults; "+
                (planar?"world projection frozen in source road coordinates; ":"source UV retained; ")+
                (projected?"source alpha represented by native deterministic cutout; soft opacity and decal blend/channel composition approximated; ":alpha?"soft transparency approximated as native alpha-cutout; ":"")+reason;
            mapping=new Mapping(planar,scale,differences);
            Mod.Log.Warn($"Processed material compatibility: owner={owner}; style={styleId}; source={source.id}; template={source.materialTemplateHash}; output={result.id}; textures={textures.Count}; extracted={extracted.Count}; {differences}");
            return true;
        }
        catch(Exception exception){result=null!;error="Compatibility material realization: "+exception;return false;}
        finally
        {
            try{source?.UnloadProperties();}catch(Exception exception){result=null!;error=(error??"")+"; source lease cleanup: "+exception;}
            if(decoded!=null&&!decoded.TryDispose(out var cleanup)){result=null!;error=(error??"")+"; decoded cleanup: "+cleanup;}
        }
    }
}
