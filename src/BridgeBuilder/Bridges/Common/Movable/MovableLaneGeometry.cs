using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

/// <summary>Materializes non-decal native lane meshes in the closed road frame.</summary>
internal static class MovableLaneGeometry
{
    internal sealed class Result
    {
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly MovableLaneVisuals.Batch[] Decals;
        internal Result(List<MovableRoadGeometry.Triangle> triangles, List<SurfaceAsset> surfaces,
            List<MovableLaneVisuals.Batch> decals)
        { Triangles = triangles.ToArray(); Surfaces = surfaces.ToArray(); Decals = decals.ToArray(); }
        internal Result(MovableRoadGeometry.Triangle[] triangles,SurfaceAsset[] surfaces,
            MovableLaneVisuals.Batch[] decals)
        { Triangles=triangles;Surfaces=surfaces;Decals=decals; }

    }

    internal static Result Compile(IReadOnlyList<MovableLaneVisuals.Batch> batches,
        IReadOnlyList<MovableRoadGeometry.Triangle> receiver, int lod = 0) =>
        GenerationWork.Synchronous(() => CompileAsync(batches, receiver, lod));

    internal sealed class Cache
    {
        internal readonly GenerationStageCache Stages=new();
        internal readonly Dictionary<RenderPrefab,Part[]> Meshes=new();
        internal string Report()=>Stages.Report()+"; lane-source-meshes="+Meshes.Count;
        internal void Clear(){Stages.Clear();Meshes.Clear();}
    }
    internal sealed class Part
    {
        internal readonly MovableRoadGeometry.Vertex[] Vertices;
        internal readonly int[][] Indices;
        internal Part(MovableRoadGeometry.Vertex[] vertices,int[][] indices){Vertices=vertices;Indices=indices;}
    }
    internal static async System.Threading.Tasks.Task<Result> CompileAsync(IReadOnlyList<MovableLaneVisuals.Batch> batches,
        IReadOnlyList<MovableRoadGeometry.Triangle> receiver,int lod=0,Cache? cache=null)
    {
        cache??=new Cache();
        var renders=new RenderPrefab[batches.Count];var bindings=new SurfaceAsset[batches.Count][];
        var key=new List<object>{batches};
        if(batches.Any(batch=>!batch.Decal && batch.Parameters.y!=0f))key.Add(receiver);
        for(int i=0;i<batches.Count;i++)
        {
            var batch=batches[i];var render=batch.Source;
            if(!batch.Decal)
            {
                var levels=render.GetComponent<LodProperties>()?.m_LodMeshes;
                if(lod>0 && levels!=null && levels.Length>0)render=levels[Math.Min(lod-1,levels.Length-1)];
            }
            renders[i]=render;key.Add(render);
            bindings[i]=render==batch.Source?batch.Surfaces:render.surfaceAssets.ToArray();
            foreach(var binding in bindings[i])key.Add(binding);
        }
        return await cache.Stages.Get("lane-deform-expand",key.ToArray(),Build);
        async System.Threading.Tasks.Task<Result> Build()
        {
            var surfaces=new List<SurfaceAsset>();var decals=new List<MovableLaneVisuals.Batch>();
            var jobs=new List<(MovableLaneDeformation.Input Input,Part[] Parts,int Material,int Occurrence)>();
            for(int occurrence=0;occurrence<batches.Count;occurrence++)
            {
                await GenerationWork.Checkpoint("lane source snapshot");
                var batch=batches[occurrence];if(batch.Decal){decals.Add(batch);continue;}
                var render=renders[occurrence];
                if(!cache.Meshes.TryGetValue(render,out var parts))
                {
                    Mesh[]? meshes=null;
                    try
                    {
                        meshes=PrivateGeometryReader.Read(render);
                        if(meshes.Length==0)throw new InvalidDataException("No lane geometry could be read for "+render.name);
                        parts=new Part[meshes.Length];
                        for(int m=0;m<meshes.Length;m++)
                        {
                            var mesh=meshes[m];var indices=new int[mesh.subMeshCount][];
                            for(int slot=0;slot<indices.Length;slot++)
                            {
                                if(mesh.GetTopology(slot)!=MeshTopology.Triangles)throw new InvalidDataException("Invalid lane topology.");
                                indices[slot]=mesh.GetIndices(slot);
                                if(indices[slot].Length%3!=0)throw new InvalidDataException("Incomplete lane triangle.");
                            }
                            parts[m]=new Part(await ReadVerticesAsync(mesh),indices);
                        }
                        cache.Meshes.Add(render,parts);
                    }
                    finally{PrivateGeometryReader.Release(meshes);}
                }
                int slots=0;foreach(var part in parts)slots+=part.Indices.Length;
                if(slots!=bindings[occurrence].Length)throw new InvalidDataException("Lane surface slots do not match geometry: "+render.name);
                jobs.Add((new MovableLaneDeformation.Input(batch),parts,surfaces.Count,occurrence));
                surfaces.AddRange(bindings[occurrence]);
            }
            // Only value snapshots and owned managed mesh/receiver arrays cross this boundary.
            var triangles=await GenerationWork.Background("lane deform and triangle expansion",()=>
            {
                var output=new List<MovableRoadGeometry.Triangle>();
                foreach(var job in jobs)
                {
                    int slot=0,sourceTriangle=0;
                    foreach(var part in job.Parts)
                    {
                        var vertices=new MovableRoadGeometry.Vertex[part.Vertices.Length];
                        for(int i=0;i<vertices.Length;i++)vertices[i]=MovableLaneDeformation.EvaluateInput(job.Input,part.Vertices[i],p=>ReceiverHeight(receiver,p));
                        foreach(var indices in part.Indices)
                        {
                            for(int i=0;i<indices.Length;i+=3)output.Add(new MovableRoadGeometry.Triangle(vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]],job.Material+slot,job.Occurrence,sourceTriangle++));
                            slot++;
                        }
                    }
                }
                return output;
            });
            return new Result(triangles,surfaces,decals);
        }
    }

    private static System.Threading.Tasks.Task<MovableRoadGeometry.Vertex[]> ReadVerticesAsync(Mesh mesh)
    {
        if(!mesh.HasVertexAttribute(VertexAttribute.Normal)||!mesh.HasVertexAttribute(VertexAttribute.Tangent))
            throw new InvalidDataException("Lane mesh has no normal/tangent frame: "+mesh.name);
        MovablePackedFrame.Snapshot packedNormals,packedTangents;
        using(var raw=Mesh.AcquireReadOnlyMeshData(mesh))
        {
            var streams=new Dictionary<int,byte[]>();
            packedNormals=MovablePackedFrame.Read(mesh,raw[0],VertexAttribute.Normal,streams);
            packedTangents=MovablePackedFrame.Read(mesh,raw[0],VertexAttribute.Tangent,streams);
        }
        var uv=new List<Vector4>();mesh.GetUVs(0,uv);
        if(uv.Count!=mesh.vertexCount)throw new InvalidDataException("Lane mesh has incomplete UVs: "+mesh.name);
        var positions=mesh.vertices;var colors=mesh.colors;var name=mesh.name;
        return GenerationWork.Background("lane packed frame decode",()=>
        {
            var decoded=MovablePackedFrame.DecodeSnapshots(packedNormals,packedTangents);
            var output=new MovableRoadGeometry.Vertex[positions.Length];
            for(int i=0;i<output.Length;i++)
            {
                output[i]=new MovableRoadGeometry.Vertex(positions[i],decoded.Normals[i],decoded.Tangents[i],uv[i],colors.Length==0?Color.white:colors[i]);
                if(!MovableRoadGeometry.Valid(output[i]))throw new InvalidDataException("Invalid lane vertex in "+name);
            }
            return output;
        });
    }

    // Project onto the selected road's actual closed surface, never city terrain. This is a
    // triangle/ray intersection for newly baked visual geometry, not structural-part inference.
    private static float ReceiverHeight(IReadOnlyList<MovableRoadGeometry.Triangle> receiver, Vector3 point)
    {
        var closest = float.PositiveInfinity;
        var height = 0f;
        foreach (var triangle in receiver)
        {
            var a = triangle.A.Position; var ab = triangle.B.Position - a; var ac = triangle.C.Position - a;
            var q = point - a;
            var determinant = ab.x * ac.z - ab.z * ac.x;
            if (determinant == 0f) continue;
            var u = (q.x * ac.z - q.z * ac.x) / determinant;
            var v = (ab.x * q.z - ab.z * q.x) / determinant;
            if (u < 0f || v < 0f || 1f - u - v < 0f) continue;
            var y = a.y + ab.y * u + ac.y * v;
            var distance = Mathf.Abs(y - point.y);
            if (distance < closest) { closest = distance; height = y; }
        }
        return float.IsPositiveInfinity(closest) ? point.y : height;
    }
}
