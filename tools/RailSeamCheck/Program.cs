using System.Collections;
using System.Reflection;
using BridgeBuilder.Runtime;
using Game;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using SubNet=Game.Net.SubNet;

var checks=0;
void Check(bool value,string message) { if(!value) throw new Exception(message); checks++; Console.WriteLine("PASS "+message); }
NativeList<Entity> Batch(params int[] ids) { var l=new NativeList<Entity>(ids.Length,Allocator.TempJob);foreach(var id in ids)l.Add(new(id));return l; }
var backendType=typeof(BridgeRailSeamPatch).GetNestedType("Backend",BindingFlags.NonPublic)!;
// No Harmony installation in this harness: test production dispatch, not runtime detours.
var backend=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(backendType);
foreach(var (field,value) in new (string,object)[]{("SystemType",typeof(GeometrySystem)),("JobType",typeof(ProbeJob)),("Entities",typeof(ProbeJob).GetField("m_Entities")!)})
    backendType.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(backend,value);
var backends=(IDictionary)typeof(BridgeRailSeamPatch).GetField("Backends",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)!;
backends.Add(typeof(ProbeJob),backend);
var schedule=typeof(BridgeRailSeamPatch).GetMethod("Schedule",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(typeof(ProbeJob));
foreach(var ids in new[]{new[]{1},new[]{2},new[]{3},new[]{4},new[]{1,3},new[]{1,2,3,4},new[]{1,2,3,4,5}})
{
    var f=new Fixture(); using var batch=Batch(ids);
    var scope=new BridgeRailSeamScope(f.System,f.Job,batch);
    Check(scope.Seams.Count==2 && scope.Indices.Select(i=>batch[i].Index).SequenceEqual(ids.Where(i=>i!=5)),"eligible partial write set: "+string.Join(',',ids));
    ProbeJob.Calls.Clear();
    schedule.Invoke(null,new object[]{f.Job,batch,1,default(JobHandle),f.System});
    Check(ProbeJob.Calls.Count==ids.Length && ProbeJob.Calls.Select(c=>c.Entity).Order().SequenceEqual(ids.Order()),"each requested edge calculated once; neighbours never written: "+string.Join(',',ids));
    Check(ProbeJob.Calls.All(c=>c.Entity==5?c.Native:!c.Native&&c.Straight),"eligible edges get straight predicate on their FIRST calculation: "+string.Join(',',ids));
}
{
    var f=new Fixture(); f.Job.m_HiddenData[new(1)]=default; f.Job.m_HiddenData[new(3)]=default;
    using var batch=Batch(1);
    Check(new BridgeRailSeamScope(f.System,f.Job,batch).Indices.Count==1,"hidden original remains protected");
}
{
    var f=new Fixture(); f.Job.m_Edges[new(11)].Add(new(){m_Edge=new(5)});
    using var batch=Batch(1,2,3,4,5);
    Check(new BridgeRailSeamScope(f.System,f.Job,batch).Seams.Count==0,"real third connection retains native junction behaviour");
}
{
    var f=new Fixture();f.Job.m_PrefabCompositionLanes[new(102)][0]=new(){m_Lane=new(999),m_Flags=LaneFlags.Track};
    using var batch=Batch(1);
    Check(new BridgeRailSeamScope(f.System,f.Job,batch).Seams.Count==0,"mismatched tracks retain native behaviour");
}
{
    var f=new Fixture(); f.Job.m_TempData[new(1)]=new(){m_Flags=TempFlags.Delete};
    using var batch=Batch(1);
    Check(new BridgeRailSeamScope(f.System,f.Job,batch).Indices.Count==0,"deleted preview excluded");
}
{
    var f=new Fixture(); using var batch=Batch(1); ProbeJob.Calls.Clear(); ProbeJob.FailOnce=true;
    schedule.Invoke(null,new object[]{f.Job,batch,1,default(JobHandle),f.System});
    Check(ProbeJob.Calls.Single().Native,"managed failure explicitly falls back for current batch");
    ProbeJob.Calls.Clear(); schedule.Invoke(null,new object[]{f.Job,batch,1,default(JobHandle),f.System});
    Check(ProbeJob.Calls.Single() is {Native:false,Straight:true},"next batch retries straight calculation instead of disabling session");
}
{
    var ncType=typeof(NodeController.Main.Systems.NcGeometrySystem.CalculateEdgeGeometryJob);
    var ncBackend=System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(backendType);
    foreach(var (field,value) in new (string,object)[]{("SystemType",typeof(NodeController.Main.Systems.NcGeometrySystem)),("JobType",ncType),("Entities",ncType.GetField("MEntities")!)})
        backendType.GetField(field,BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(ncBackend,value);
    backends.Add(ncType,ncBackend);
    var ncSchedule=typeof(BridgeRailSeamPatch).GetMethod("Schedule",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(ncType);
    Check(BridgeRailSeamScope.Supports(ncType),"Node Controller field aliases supported");
    foreach(var id in new[]{1,2,3,4})
    {
        var f=new Fixture(); using var batch=Batch(id);ProbeJob.Calls.Clear();
        ncSchedule.Invoke(null,new object[]{new NodeController.Main.Systems.NcGeometrySystem.CalculateEdgeGeometryJob(f.Job),batch,1,default(JobHandle),f.System});
        Check(ProbeJob.Calls.Single() is {Native:false,Straight:true},"NC direct partial batch: "+id);
    }
}
Console.WriteLine($"PASS {checks} topology/dispatch checks (not a game geometry or Burst acceptance test).");

public class Fixture
{
    public GameSystemBase System=new();
    public ProbeJob Job=new();
    public Fixture()
    {
        const string name="b11111111-1111-1111-1111-111111111111";
        var lower=new TrackPrefab{name=name+"_Lower"};
        var upper=new NetGeometryPrefab{name=name,Auxiliary=new(){m_AuxiliaryNets=new[]{new AuxiliaryNetInfo{m_Prefab=lower,m_Position=new(0,-8.3f,0)}}}};
        System.World.Prefabs.Values[new(50)]=lower;System.World.Prefabs.Values[new(51)]=upper;
        for(int i=1;i<=200;i++) System.EntityManager.Existing.Add(new(i));
        for(int i=1;i<=5;i++)
        {
            var e=new Entity(i); Job.m_EdgeData[e]=new(){m_Start=new(i==1?10:i==2?11:i==3?20:i==4?21:11),m_End=new(i==1?11:i==2?12:i==3?21:i==4?22:13)};
            Job.m_PrefabRefDataFromEntity[e]=new(){m_Prefab=new(i<3?50:51)};
            Job.m_CompositionDataFromEntity[e]=new(){m_Edge=new(100+i),m_StartNode=new(100+i),m_EndNode=new(100+i)};
            Job.m_PrefabCompositionData[new(100+i)]=new(){m_State=CompositionState.HasForwardTrackLanes};
            Job.m_PrefabCompositionLanes[new(100+i)]=new(){new(){m_Lane=new(70),m_Position=new(-3,0,0),m_Flags=LaneFlags.Track},new(){m_Lane=new(70),m_Position=new(3,0,0),m_Flags=LaneFlags.Track}};
            System.EntityManager.SetComponentData(e,new EdgeGeometry());System.EntityManager.SetComponentData(e,new StartNodeGeometry());System.EntityManager.SetComponentData(e,new EndNodeGeometry());
        }
        Job.m_OwnerData[new(1)]=new(){m_Owner=new(3)}; Job.m_OwnerData[new(2)]=new(){m_Owner=new(4)};
        Job.m_SubNets[new(3)]=new(){new(){m_SubNet=new(1)}};Job.m_SubNets[new(4)]=new(){new(){m_SubNet=new(2)}};
        foreach(var (n,edges) in new[]{(10,new[]{1}),(11,new[]{1,2}),(12,new[]{2}),(20,new[]{3}),(21,new[]{3,4}),(22,new[]{4})})
        { Job.m_Edges[new(n)]=new();foreach(var e in edges) Job.m_Edges[new(n)].Add(new(){m_Edge=new(e)}); }
    }
}
public struct ProbeJob : IJobParallelForDefer
{
    public NativeArray<Entity> m_Entities=new();
    public ComponentLookup<Edge> m_EdgeData=new();
    public ComponentLookup<Owner> m_OwnerData=new();
    public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity=new();
    public ComponentLookup<Composition> m_CompositionDataFromEntity=new();
    public ComponentLookup<NetCompositionData> m_PrefabCompositionData=new();
    public BufferLookup<NetCompositionLane> m_PrefabCompositionLanes=new();
    public BufferLookup<ConnectedEdge> m_Edges=new();
    public BufferLookup<SubNet> m_SubNets=new();
    public ComponentLookup<Temp> m_TempData=new();
    public ComponentLookup<Hidden> m_HiddenData=new();
    public ProbeJob(){}
    public static bool FailOnce;
    public static readonly List<(int Entity,bool Native,bool Straight)> Calls=new();
    public void Execute(int i)
    {
        if(FailOnce && !IJobParallelForDeferExtensions.InNativePass){FailOnce=false;throw new InvalidOperationException("injected managed failure");}
        var edge=m_Entities[i];var lower=edge.Index==3?new Entity(1):edge.Index==4?new Entity(2):edge;
        var straight=(bool)typeof(BridgeRailSeamPatch).GetMethod("KeepStraight",BindingFlags.NonPublic|BindingFlags.Static)!.Invoke(null,new object[]{false,lower,new Entity(11)})!;
        Calls.Add((edge.Index,IJobParallelForDeferExtensions.InNativePass,straight));
    }
}
