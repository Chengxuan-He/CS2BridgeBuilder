using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Original native road LODs bypass all road/structure mapping. Native draw batches
// use one model per Surface; split only the existing submesh index streams.
internal static class MovableNativeRoadOriginal
{
    internal static bool TryAppend(string name,RenderPrefab source,List<ModelImporter.Model> owned,
        List<ModelImporter.Model> batch,List<SurfaceAsset> surfaces,out string? error)
    {
        error=null;Mesh[]? meshes=null;string? failure=null;bool complete=false;
        int ownedStart=owned.Count,batchStart=batch.Count,surfaceStart=surfaces.Count;
        try
        {
            meshes=PrivateGeometryReader.Read(source);int slot=0;
            foreach(var mesh in meshes)
            {
            if(failure!=null)break;
            for(int submesh=0;submesh<mesh.subMeshCount;submesh++)
            {
                var surface=source.GetSurfaceAsset(slot++);
                if(surface==null){failure="Original native road has a missing Surface.";break;}
                if(!TryCopy(name+" Original "+(slot-1),mesh,submesh,out var model,out failure))break;
                owned.Add(model);batch.Add(model);surfaces.Add(surface);
            }
            }
            if(failure==null && (slot==0 || source.surfaceAssets==null || slot!=source.surfaceAssets.Count()))failure="Original native road has inconsistent material slots.";
            complete=failure==null;
        }
        catch(Exception exception){failure="Original native road assembly: "+exception.Message;}
        finally
        {
            try{PrivateGeometryReader.Release(meshes);}catch(Exception exception){complete=false;failure=(failure??"Original native road cleanup failed")+"; source mesh release: "+exception.Message;}
            if(!complete)
            {
                for(int i=owned.Count-1;i>=ownedStart;i--)try{owned[i].Dispose();}catch(Exception exception){failure+="; model cleanup: "+exception.Message;}
                owned.RemoveRange(ownedStart,owned.Count-ownedStart);batch.RemoveRange(batchStart,batch.Count-batchStart);surfaces.RemoveRange(surfaceStart,surfaces.Count-surfaceStart);
            }
            error=failure;
        }
        return complete;
    }

    internal static bool TryCopy(string name,Mesh source,int submesh,out ModelImporter.Model result,out string? error)
    {
        result=null!;error=null;string? failure=null;
        var attributes = new List<ModelImporter.Model.VertexData>();
        NativeArray<int> indices = default;
        bool transferred = false;
        try
        {
            using var read = Mesh.AcquireReadOnlyMeshData(source);
            foreach (var attribute in source.GetVertexAttributes())
            {
                int size = Size(attribute.format) * attribute.dimension;
                if(size==0){failure="Unsupported original native attribute format.";return false;}
                int stride = source.GetVertexBufferStride(attribute.stream);
                int offset = source.GetVertexAttributeOffset(attribute.attribute);
                var raw = read[0].GetVertexData<byte>(attribute.stream);
                var bytes = new byte[checked(source.vertexCount * size)];
                for (int vertex = 0; vertex < source.vertexCount; vertex++)
                    NativeArray<byte>.Copy(raw, vertex * stride + offset, bytes, vertex * size, size);
                attributes.Add(new ModelImporter.Model.VertexData(attribute.attribute, attribute.format,
                    attribute.dimension, new NativeArray<byte>(bytes, Allocator.Persistent), false));
            }
            // GetIndices applies the original baseVertex; do not apply it twice.
            indices = new NativeArray<int>(source.GetIndices(submesh), Allocator.Persistent);
            var descriptor = source.GetSubMesh(submesh);
            descriptor.indexStart = 0; descriptor.baseVertex = 0;
            var model = new ModelImporter.Model(name, Matrix4x4.identity, source.vertexCount, indices,
                attributes.ToArray(), new[] { descriptor }, -1, Array.Empty<ModelImporter.Model.BoneInfo>());
            transferred = true;
            result=model;return true;
        }
        catch(Exception exception){failure="Original native attribute copy: "+exception.Message;return false;}
        finally
        {
            if (!transferred)
            {
                if(indices.IsCreated)try{indices.Dispose();}catch(Exception exception){failure+="; index cleanup: "+exception.Message;}
                foreach(var attribute in attributes)try{attribute.Dispose();}catch(Exception exception){failure+="; attribute cleanup: "+exception.Message;}
            }
            error=failure;
        }
    }

    private static int Size(VertexAttributeFormat format) => format switch
    {
        VertexAttributeFormat.Float32 or VertexAttributeFormat.UInt32 or VertexAttributeFormat.SInt32 => 4,
        VertexAttributeFormat.Float16 or VertexAttributeFormat.UNorm16 or VertexAttributeFormat.SNorm16 or
            VertexAttributeFormat.UInt16 or VertexAttributeFormat.SInt16 => 2,
        VertexAttributeFormat.UNorm8 or VertexAttributeFormat.SNorm8 or VertexAttributeFormat.UInt8 or
            VertexAttributeFormat.SInt8 => 1,
        _ => 0
    };
}
