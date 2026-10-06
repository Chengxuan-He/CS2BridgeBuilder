// Models ECS topology and job dispatch only. This does not simulate game geometry or Burst.
using System.Collections;
namespace Unity.Entities
{
    public record struct Entity(int Index) { public static Entity Null => default; }
    public class ComponentLookup<T> : Dictionary<Entity,T>
    {
        public bool HasComponent(Entity e) => ContainsKey(e);
        public bool TryGetComponent(Entity e, out T value) => TryGetValue(e, out value!);
    }
    public class DynamicBuffer<T> : List<T> { public int Length => Count; }
    public class BufferLookup<T> : Dictionary<Entity,DynamicBuffer<T>>
    {
        public bool HasBuffer(Entity e) => ContainsKey(e);
        public bool TryGetBuffer(Entity e, out DynamicBuffer<T> value) => TryGetValue(e, out value!);
    }
    public class EntityManager
    {
        public readonly HashSet<Entity> Existing = new();
        private readonly Dictionary<(Entity,Type),object> data = new();
        public bool Exists(Entity e) => Existing.Contains(e);
        public bool HasComponent<T>(Entity e) => data.ContainsKey((e,typeof(T)));
        public T GetComponentData<T>(Entity e) => (T)data[(e,typeof(T))];
        public void SetComponentData<T>(Entity e, T value) { data[(e,typeof(T))]=value!; Existing.Add(e); }
    }
    public class World
    {
        public Game.Prefabs.PrefabSystem Prefabs = new();
        public T? GetExistingSystemManaged<T>() where T: class => Prefabs as T;
    }
}
namespace Unity.Collections
{
    public enum Allocator { TempJob }
    public class NativeArray<T> : List<T> { public int Length=>Count; }
    public class NativeList<T> : List<T>, IDisposable
    {
        public NativeList(int capacity, Allocator allocator) : base(capacity) { }
        public int Length=>Count;
        public NativeArray<T> AsArray() { var r=new NativeArray<T>(); r.AddRange(this); return r; }
        public NativeArray<T> AsDeferredJobArray()=>AsArray();
        public void Dispose() { }
    }
}
namespace Unity.Jobs
{
    public struct JobHandle { public void Complete() { } }
    public interface IJobParallelForDefer { void Execute(int index); }
    public static class IJobParallelForDeferExtensions
    {
        public static bool InNativePass;
        public static JobHandle Schedule<T,U>(this T job, Unity.Collections.NativeList<U> list, int batch, JobHandle dependency) where T:struct,IJobParallelForDefer
        {
            InNativePass=true;
            try { for(int i=0;i<list.Length;i++) job.Execute(i); }
            finally { InNativePass=false; }
            return default;
        }
    }
}
namespace UnityEngine { public static class Time { public static int frameCount; } }
namespace Game
{
    public class GameSystemBase { public Unity.Entities.EntityManager EntityManager=new(); public Unity.Entities.World World=new(); }
}
namespace Game.Common { public struct Deleted { } public struct Owner { public Unity.Entities.Entity m_Owner; } }
namespace Game.Tools
{
    [Flags] public enum TempFlags { Delete=1 }
    public struct Temp { public Unity.Entities.Entity m_Original; public TempFlags m_Flags; }
    public struct Hidden { }
}
namespace Game.Net
{
    using Unity.Entities;
    public class GeometrySystem : Game.GameSystemBase { }
    public struct Edge { public Entity m_Start,m_End; }
    public struct SubNet { public Entity m_SubNet; }
    public struct ConnectedEdge { public Entity m_Edge; }
    public struct Composition { public Entity m_Edge,m_StartNode,m_EndNode; }
    public struct EdgeGeometry { }
    public struct StartNodeGeometry { }
    public struct EndNodeGeometry { }
    public struct EdgeIteratorValue { public Entity m_Edge; public bool m_End,m_Middle; }
    public struct EdgeIterator
    {
        private readonly List<EdgeIteratorValue> values;
        private int index;
        public EdgeIterator(Entity first, Entity node, BufferLookup<ConnectedEdge> connected, ComponentLookup<Edge> edges,
            ComponentLookup<Game.Tools.Temp> temp, ComponentLookup<Game.Tools.Hidden> hidden, bool middle)
        {
            values=new(); index=0;
            bool permanent=!temp.HasComponent(node);
            var seen=new HashSet<Entity>();
            while(node!=Entity.Null && seen.Add(node) && connected.TryGetBuffer(node,out var buffer))
            {
                foreach(var c in buffer)
                {
                    if(!permanent && (hidden.HasComponent(c.m_Edge) || (temp.TryGetComponent(c.m_Edge,out var t) && (t.m_Flags&Game.Tools.TempFlags.Delete)!=0))) continue;
                    var edge=edges[c.m_Edge];
                    values.Add(new(){m_Edge=c.m_Edge,m_End=edge.m_End==node,m_Middle=edge.m_End!=node && edge.m_Start!=node});
                }
                node=temp.TryGetComponent(node,out var original)?original.m_Original:Entity.Null;
            }
        }
        public bool GetNext(out EdgeIteratorValue v) { if(index<values.Count){v=values[index++];return true;} v=default;return false; }
    }
}
namespace Game.Prefabs
{
    using Unity.Entities;
    public class SubNet { }
    public struct PrefabRef { public Entity m_Prefab; }
    public class NetPrefab { public string name=""; public bool isBuiltin,isReadOnly; }
    public class TrackPrefab : NetPrefab { }
    public class NetGeometryPrefab : NetPrefab
    {
        public AuxiliaryNets Auxiliary=new();
        public bool TryGet<T>(out T value) where T:class { value=(Auxiliary as T)!;return value!=null; }
    }
    public class AuxiliaryNets { public AuxiliaryNetInfo[] m_AuxiliaryNets=Array.Empty<AuxiliaryNetInfo>(); }
    public class AuxiliaryNetInfo { public NetPrefab? m_Prefab; public Vector m_Position; }
    public class PrefabSystem
    {
        public Dictionary<Entity,NetPrefab> Values=new();
        public bool TryGetPrefab<T>(Entity e,out T value) where T:NetPrefab { Values.TryGetValue(e,out var p);value=(p as T)!;return value!=null; }
        public bool TryGetPrefab<T>(PrefabRef r,out T value) where T:NetPrefab =>TryGetPrefab(r.m_Prefab,out value);
    }
    public record struct Vector(float x,float y,float z);
    [Flags] public enum LaneFlags { Track=1, Invert=2 }
    [Flags] public enum CompositionState { HasForwardRoadLanes=1,HasBackwardRoadLanes=2,HasForwardTrackLanes=4,HasBackwardTrackLanes=8 }
    public struct CompositionFlags
    {
        [Flags] public enum General { Intersection=1,LevelCrossing=2,Roundabout=4,DeadEnd=8 }
        public General m_General;
    }
    public struct NetCompositionData { public CompositionFlags m_Flags; public CompositionState m_State; }
    public struct NetCompositionLane { public Entity m_Lane; public Vector m_Position; public LaneFlags m_Flags; }
}
namespace BridgeBuilder
{
    internal static class Mod { public static Logger Log=new(); }
    internal class Logger { public void Info(string message){} public void Warn(string message){} public void Warn(Exception e,string message){} }
}
namespace BridgeBuilder.Runtime
{
    internal static class BridgeAssetInfo { public static bool IsPrefabName(string s)=>s.Length==37 && s[0]=='b' && Guid.TryParse(s.Substring(1),out _); }
}
namespace BridgeBuilder.Systems
{
    public class BridgeRailSeamAuditSystem { public void Observe(Unity.Entities.Entity e){} }
}
namespace NodeController.Main.Systems
{
    // Exact field aliases of the inspected NC job; geometry remains a test probe.
    public class NcGeometrySystem : Game.GameSystemBase
    {
        public struct CalculateEdgeGeometryJob : Unity.Jobs.IJobParallelForDefer
        {
            public Unity.Collections.NativeArray<Unity.Entities.Entity> MEntities;
            public Unity.Entities.ComponentLookup<Game.Net.Edge> MEdgeData;
            public Unity.Entities.ComponentLookup<Game.Common.Owner> MOwnerData;
            public Unity.Entities.ComponentLookup<Game.Prefabs.PrefabRef> MPrefabRefDataFromEntity;
            public Unity.Entities.ComponentLookup<Game.Net.Composition> MCompositionDataFromEntity;
            public Unity.Entities.ComponentLookup<Game.Prefabs.NetCompositionData> MPrefabCompositionData;
            public Unity.Entities.BufferLookup<Game.Prefabs.NetCompositionLane> MPrefabCompositionLanes;
            public Unity.Entities.BufferLookup<Game.Net.ConnectedEdge> MEdges;
            public Unity.Entities.BufferLookup<Game.Net.SubNet> MSubNets;
            public Unity.Entities.ComponentLookup<Game.Tools.Temp> MTempData;
            public Unity.Entities.ComponentLookup<Game.Tools.Hidden> MHiddenData;
            public CalculateEdgeGeometryJob(ProbeJob job)
            {
                MEntities=job.m_Entities;MEdgeData=job.m_EdgeData;MOwnerData=job.m_OwnerData;
                MPrefabRefDataFromEntity=job.m_PrefabRefDataFromEntity;MCompositionDataFromEntity=job.m_CompositionDataFromEntity;
                MPrefabCompositionData=job.m_PrefabCompositionData;MPrefabCompositionLanes=job.m_PrefabCompositionLanes;
                MEdges=job.m_Edges;MSubNets=job.m_SubNets;MTempData=job.m_TempData;MHiddenData=job.m_HiddenData;
            }
            public void Execute(int index) { var probe=new ProbeJob{m_Entities=MEntities};probe.Execute(index); }
        }
    }
}
