using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Owned by one TryBuild graph. Entries borrow immutable level inputs and importer models;
// the graph's single owned-model list disposes the buffers after every LOD has consumed them.
internal sealed class MovableRoadLodCache
{
    internal sealed class Key
    {
        internal readonly IReadOnlyList<MovableRoadGeometry.Triangle> Triangles;
        internal readonly Pose Pose;
        internal readonly MovableRoadGeometry.Partition Partition;
        internal readonly IReadOnlyList<MovableRoadGeometry.Partition>? Occurrences;
        internal readonly IReadOnlyDictionary<(int Occurrence,int SourceTriangle),MovableRoadGeometry.Partition>? Faces;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly MovableMaterialCompatibility.Mapping[] Mappings;
        internal readonly ProceduralAnimationProperties Skeleton;
        internal Key(IReadOnlyList<MovableRoadGeometry.Triangle> triangles,Pose pose,
            MovableRoadGeometry.Partition partition,IReadOnlyList<MovableRoadGeometry.Partition>? occurrences,
            IReadOnlyDictionary<(int Occurrence,int SourceTriangle),MovableRoadGeometry.Partition>? faces,
            SurfaceAsset[] sources,SurfaceAsset[] surfaces,
            IReadOnlyDictionary<SurfaceAsset,MovableMaterialCompatibility.Mapping> mappings,
            ProceduralAnimationProperties skeleton)
        {
            Triangles=triangles;Pose=pose;Partition=partition;Occurrences=occurrences;Faces=faces;
            Surfaces=surfaces;Skeleton=skeleton;
            Mappings=new MovableMaterialCompatibility.Mapping[sources.Length];
            for(int i=0;i<sources.Length;i++)if(sources[i]!=null && mappings.TryGetValue(sources[i],out var mapping))Mappings[i]=mapping;
        }
    }
    internal sealed class Result
    {
        internal readonly Key Input;
        internal readonly string Name;
        internal readonly ModelImporter.Model[] Models;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly int TriangleCount;
        internal readonly Vector3 Minimum,Maximum;
        internal Result(Key input,string name,ModelImporter.Model[] models,SurfaceAsset[] surfaces,
            IReadOnlyList<MovableRoadGeometry.BoundTriangle> triangles)
        {
            Input=input;Name=name;Models=models;Surfaces=surfaces;TriangleCount=triangles.Count;
            if(triangles.Count==0){Minimum=Maximum=default;return;}
            var minimum=triangles[0].Geometry.A.Position;var maximum=minimum;
            void Include(Vector3 v){minimum=Vector3.Min(minimum,v);maximum=Vector3.Max(maximum,v);}
            foreach(var t in triangles){Include(t.Geometry.A.Position);Include(t.Geometry.B.Position);Include(t.Geometry.C.Position);}
            Minimum=minimum;Maximum=maximum;
        }
        internal ModelImporter.Model[] NamedModels(string name) => Models.Select(m =>
            new ModelImporter.Model(name+m.name.Substring(Name.Length),m.transform,m.vertexCount,
                m.indices,m.ToArray(),m.subMeshes,m.rootBoneIndex,m.bones)).ToArray();
    }
    private readonly List<Result> _entries=new();
    internal Result? Find(Key key)
    {
        foreach(var entry in _entries)if(Equal(entry.Input,key))return entry;
        return null;
    }
    internal void Add(Result result)=>_entries.Add(result);
    private static bool Bits(float a,float b)=>BitConverter.SingleToInt32Bits(a)==BitConverter.SingleToInt32Bits(b);
    private static bool Matrix(Matrix4x4 a,Matrix4x4 b){for(int i=0;i<16;i++)if(!Bits(a[i],b[i]))return false;return true;}
    private static bool Vector(Vector3 a,Vector3 b)=>Bits(a.x,b.x)&&Bits(a.y,b.y)&&Bits(a.z,b.z);
    private static bool Rotation(Quaternion a,Quaternion b)=>Bits(a.x,b.x)&&Bits(a.y,b.y)&&Bits(a.z,b.z)&&Bits(a.w,b.w);
    internal static bool Tree(MovableRoadGeometry.Partition? a,MovableRoadGeometry.Partition? b)
    {
        if(ReferenceEquals(a,b))return true;
        if(a==null||b==null||a.Bone!=b.Bone||a.IsGap!=b.IsGap||!Matrix(a.ObjectToSeam,b.ObjectToSeam))return false;
        return Tree(a.Negative,b.Negative)&&Tree(a.Positive,b.Positive);
    }
    internal static bool Equal(Key a,Key b)
    {
        if(a.Triangles.Count!=b.Triangles.Count||a.Surfaces.Length!=b.Surfaces.Length||a.Mappings.Length!=b.Mappings.Length
            ||!Vector(a.Pose.position,b.Pose.position)||!Rotation(a.Pose.rotation,b.Pose.rotation)||a.Skeleton.m_Bones.Length!=b.Skeleton.m_Bones.Length)return false;
        for(int i=0;i<a.Surfaces.Length;i++)if(!ReferenceEquals(a.Surfaces[i],b.Surfaces[i]))return false;
        for(int i=0;i<a.Mappings.Length;i++)if(a.Mappings[i].Planar!=b.Mappings[i].Planar||!Bits(a.Mappings[i].Scale,b.Mappings[i].Scale))return false;
        if(!Tree(a.Partition,b.Partition)||(a.Occurrences==null)!=(b.Occurrences==null)||(a.Faces==null)!=(b.Faces==null))return false;
        if(a.Occurrences!=null)
        {
            if(a.Occurrences.Count!=b.Occurrences!.Count)return false;
            for(int i=0;i<a.Occurrences.Count;i++)if(!Tree(a.Occurrences[i],b.Occurrences[i]))return false;
        }
        if(a.Faces!=null)
        {
            if(a.Faces.Count!=b.Faces!.Count)return false;
            foreach(var face in a.Faces)if(!b.Faces.TryGetValue(face.Key,out var tree)||!Tree(face.Value,tree))return false;
        }
        if(ReferenceEquals(a.Triangles,b.Triangles))return true;
        // Triangle consists exclusively of sequential float/int fields. No approximate
        // Vector equality: preserve signed zero, UVs, colors and provenance exactly.
        if(a.Triangles is MovableRoadGeometry.Triangle[] aa && b.Triangles is MovableRoadGeometry.Triangle[] bb)
            return MemoryMarshal.AsBytes(aa.AsSpan()).SequenceEqual(MemoryMarshal.AsBytes(bb.AsSpan()));
        for(int i=0;i<a.Triangles.Count;i++)
        {
            var x=a.Triangles[i];var y=b.Triangles[i];
            if(!MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref x,1)).SequenceEqual(
                MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref y,1))))return false;
        }
        return true;
    }
}
