using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

// Native network input, not evaluated/skinned road geometry. The caller supplies
// measured face membership and an occurrence-specific transverse projection.
// Network batching derives longitudinal density from source UV edge differences;
// its mapping origin is native, so this API cannot encode a longitudinal phase.
internal sealed class MovableNetworkRoadMapping
{
    internal readonly SurfaceAsset Surface;
    internal readonly string? Error;
    internal readonly IReadOnlyDictionary<int, int[]> Faces;
    internal readonly Vector4 U;
    internal readonly float LongitudinalDensity, TangentSign;

    internal MovableNetworkRoadMapping(SurfaceAsset surface,
        IReadOnlyDictionary<int, int[]> faces, Vector4 u, float longitudinalDensity,
        float tangentSign)
    {
        Surface = surface; Faces = faces.ToDictionary(p => p.Key, p => (int[])p.Value.Clone());
        U = u; LongitudinalDensity = longitudinalDensity; TangentSign = tangentSign;
    }

    internal MovableNetworkRoadMapping(MovableExistingRoadSurface.SurfaceResult surface,IReadOnlyDictionary<int,int[]> faces,
        Vector4 u,float longitudinalDensity,float tangentSign):this(surface.Value!,faces,u,longitudinalDensity,tangentSign){Error=surface.Error??Error;}

    internal bool TryAppend(string name, RenderPrefab source, List<ModelImporter.Model> owned,
        List<ModelImporter.Model> batch, List<SurfaceAsset> surfaces,out string? error)
    {
        string? failure=Error;
        bool Fail(string message){failure=message;return false;}
        bool Build()
        {
        if(failure!=null)return false;
        Surface.LoadHeader();
        if (Surface.materialTemplateHash != 17 || Surface.hasVTSurfaceAsset)
            return Fail("Native network road replacement requires its non-VT template 17 descriptor.");
        var piece = source as NetPiecePrefab;
        if (piece?.GetComponent<NetPieceTiling>()?.m_DisableTextureTiling == true)
            return Fail("Native longitudinal road projection requires texture tiling enabled.");
        var loaded = PrivateGeometryReader.Read(source);
        try
        {
            if (loaded.Length == 0 || Faces.Keys.Any(i => i < 0 || i >= loaded.Length))
                return Fail("Native network road membership has an invalid mesh.");
            int material = 0;
            for (int meshIndex = 0; meshIndex < loaded.Length; meshIndex++)
            {
                var mesh = loaded[meshIndex];
                if (mesh.subMeshCount != 1)
                    return Fail("Measured native road input requires one original surface per mesh.");
                var original = source.GetSurfaceAsset(material++);
                if(!TryAppendMesh(name, mesh, meshIndex, original, owned, batch, surfaces,out var cause))return Fail(cause!);
            }
        }
        finally { PrivateGeometryReader.Release(loaded); }
        return true;
        }
        return Transaction(Build,owned,batch,surfaces,()=>failure,out error);
    }

    internal bool TryAppendMesh(string name, Mesh mesh, int meshIndex, SurfaceAsset original,
        List<ModelImporter.Model> owned, List<ModelImporter.Model> batch, List<SurfaceAsset> surfaces,out string? error)
    {
        string? failure=Error;
        bool Fail(string message){failure=message;return false;}
        bool Build()
        {
        if(failure!=null)return false;
    var points = mesh.vertices;
    var indices = mesh.GetIndices(0);
    var selected = new bool[indices.Length / 3];
    if (Faces.TryGetValue(meshIndex, out var measured))
        foreach (var face in measured)
        {
            if (face < 0 || face >= selected.Length || selected[face])
                return Fail("Native road membership contains an invalid or duplicate face.");
            selected[face] = true;
        }
    if (selected.Any(v => v))
    {
        var minimum = points.Min(p => p.z);
        var corrections = points.Select((p, i) => new KeyValuePair<int, Vector2>(i,
            new Vector2(Vector4.Dot(new Vector4(p.x, p.y, p.z, 1), U),
                (p.z - minimum) * LongitudinalDensity))).ToArray();
        if(!Add(selected.Select(v => !v).ToArray(), corrections, Surface, true))return false;
    }
    if (selected.Any(v => !v) && !Add(selected, null, original, false))return false;
    return true;

    bool Add(bool[] remove, KeyValuePair<int, Vector2>[]? uv, SurfaceAsset surface, bool mapped)
    {
        var plan = new MovableStructureModel.Plan(points, points, new[] { indices },
            new[] { remove }, uvCorrections: uv);
        if (!MovableStructureModel.TryBuild(name + " Network Material " + batch.Count,
            mesh, plan, 0, out var model, out var error))
            return Fail(error??"Native network geometry construction failed.");
        owned.Add(model!);
        if(mapped && TangentSign<0 && !TryReverseUFrame(model!,out var cause))return Fail(cause!);
        batch.Add(model!); surfaces.Add(surface);return true;
    }
        }
        return Transaction(Build,owned,batch,surfaces,()=>failure,out error);
    }

    // Reversing U reverses T and its handedness, retaining the original V basis.
    // Native batching subsequently applies the original section flip/invert rules.
    private static bool TryReverseUFrame(ModelImporter.Model model,out string? error)
    {
        error=null;
        var attribute = model.FirstOrDefault(a => a.attribute == VertexAttribute.Tangent);
        using var tangents = new NativeArray<MeshTangent>(model.vertexCount, Allocator.Temp);
        MeshTangent.Unpack(new NativeSlice<byte>(attribute.data), tangents,
            model.vertexCount, attribute.format, attribute.dimension);
        var data = attribute.data;
        for (int i = 0; i < model.vertexCount; i++)
        {
            var value = -tangents[i].m_Tangent;
            var bytes = BitConverter.GetBytes(MathUtils.TangentToOctahedral(value));
            for (int j = 0; j < bytes.Length; j++) data[i * 4 + j] = bytes[j];
        }
        return true;
    }
    private static bool Transaction(Func<bool> build,List<ModelImporter.Model> owned,List<ModelImporter.Model> batch,
        List<SurfaceAsset> surfaces,Func<string?> failure,out string? error)
    {
        int o=owned.Count,b=batch.Count,s=surfaces.Count;bool success=false;error=null;
        try{success=build();if(!success)error=failure();return success;}
        catch(Exception exception){error="Native network mapping: "+exception;return false;}
        finally
        {
            if(!success)
            {
                for(int i=owned.Count-1;i>=o;i--)try{owned[i].Dispose();}catch(Exception exception){error=(error??"Mapping failed")+"; cleanup: "+exception;}
                owned.RemoveRange(o,owned.Count-o);batch.RemoveRange(b,batch.Count-b);surfaces.RemoveRange(s,surfaces.Count-s);
            }
        }
    }

}
