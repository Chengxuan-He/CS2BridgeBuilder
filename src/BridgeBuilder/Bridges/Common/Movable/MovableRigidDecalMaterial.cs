using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Evaluated projection frames consume the mechanism's current skeleton. Keep the
// native DBuffer pixel semantics and source texture payloads, without child motion.
internal static class MovableRigidDecalMaterial
{
    internal static bool TryCreate(SurfaceAsset source, string owner, PreviewGeometry? preview,
        List<AssetData> assets, out SurfaceAsset result, out string? error,
        int rendererPriority = 0, float distanceFactor = 1f)
    {
        result=null!; error=null;
        SurfaceAsset? output=null;
        bool held=false;
        try
        {
            if(!MovableSurfaceAssets.TryReadSource(source,out _,out error))return false;
            var library=AssetDatabase.global.resources.materialLibrary;
            var from=library.GetMaterial(source.materialTemplateHash);
            if (!MovableCustomShaderAsset.TryPrepareBoneDecal(owner, preview, assets, rendererPriority, out var targetTemplate, out error)) return false;
            var target=library.GetMaterial(targetTemplate);
            byte[] data;
            using(var input=source.GetReadStream())using(var bytes=new MemoryStream())
            {input.CopyTo(bytes);data=bytes.ToArray();}
            var database=preview?.NativeAssets??AssetDatabase.user;
            output=database.AddAsset<SurfaceAsset>(BridgePrefabPersistence.ResourcePath(owner+" Native Decal", "Appearance"));
            assets.Add(output);
            if(!MovableMaterialConversion.TryWriteDescriptor(data,targetTemplate,output,out error))return false;
            held=true;output.LoadProperties(true);
            if(!MovableMaterialConversion.TryCopyDefaults(output,from,target,out error,evaluatedDecal:true))return false;
            {
                // Size and cropped atlas area live in each immutable projection frame.
                // This lets all volumes of one source appearance share one Surface.
                var vectors = new Dictionary<string, UnityEngine.Vector4>(output.vectors)
                {
                    ["colossal_MeshSize"] = UnityEngine.Vector4.one,
                    ["colossal_TextureArea"] = new UnityEngine.Vector4(0, 0, 1, 1)
                };
                var floats = new Dictionary<string, float>(output.floats)
                {
                    ["colossal_DecalLayerMask"] = (float)Game.Rendering.DecalLayers.Other,
                    ["colossal_LodDistanceFactor"] = distanceFactor,
                    ["_DrawOrder"] = rendererPriority
                };
                output.UpdateVectors(vectors); output.UpdateFloats(floats);
            }
            output.SaveData(force:true);
            if(output.isVTMaterial && !MovableLaneMidMips.TryComplete(source,output,owner,preview,assets,out error))return false;
            preview?.RetainSurfaceProperties(output);
            result=output;
            return true;
        }
        catch(Exception exception){error="Native rigid decal material: "+exception;return false;}
        finally
        {
            if(held)try{output!.UnloadProperties();}catch(Exception e){error=(error??"Decal cleanup")+": "+e.Message;result=null!;}
        }
    }
}
