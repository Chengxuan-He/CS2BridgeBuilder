using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Curve evaluation is represented by native rigid projection frames, not skinning.
// Keep native DBuffer alpha blending and the source's unmodified texture payloads.
internal static class MovableRigidDecalMaterial
{
    internal static bool TryCreate(SurfaceAsset source, string owner, PreviewGeometry? preview,
        List<AssetData> assets, out SurfaceAsset result, out string? error)
    {
        result=null!; error=null;
        SurfaceAsset? output=null;
        bool held=false;
        try
        {
            if(!MovableSurfaceAssets.TryReadSource(source,out _,out error))return false;
            var library=AssetDatabase.global.resources.materialLibrary;
            var from=library.GetMaterial(source.materialTemplateHash);
            var target=library.GetMaterial(18);
            // Even an original static decal may need packed tangent metadata;
            // derive a descriptor without changing its artwork or alpha rules.
            if(from.FindPass("DBufferMesh")<0 || target.FindPass("DBufferMesh")<0)
            {error="Native rigid decal requires a DBuffer mesh protocol.";return false;}
            byte[] data;
            using(var input=source.GetReadStream())using(var bytes=new MemoryStream())
            {input.CopyTo(bytes);data=bytes.ToArray();}
            var database=preview?.NativeAssets??AssetDatabase.user;
            output=database.AddAsset<SurfaceAsset>(BridgePrefabPersistence.ResourcePath(owner+" Native Decal", "Appearance"));
            assets.Add(output);
            if(!MovableMaterialConversion.TryWriteDescriptor(data,18,output,out error))return false;
            held=true;output.LoadProperties(true);
            if(!MovableMaterialConversion.TryCopyDefaults(output,from,target,out error,evaluatedDecal:true))return false;
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
