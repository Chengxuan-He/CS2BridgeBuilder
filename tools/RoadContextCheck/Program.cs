using System;
using System.Collections.Generic;
using BridgeBuilder.Bridges;
using BridgeBuilder.Systems;
using Game.Prefabs;
using Game.Net;
using Unity.Entities;
static class Check {
 static void Need(bool x,string m){if(!x)throw new Exception(m);}
 static void Main(){
  var prefabs=new PrefabSystem();var a=new NetGeometryPrefab();var b=new NetGeometryPrefab();
  var widths=World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BridgeRoadWidthSystem>();
  IReadOnlyDictionary<NetGeometryPrefab,BridgeRoadContext>? captured=null;
  using(var scope=new BridgeRoadGeneration(prefabs,a,a,b)){
   Need(widths.Resolutions==2&&widths.Reads==4,"contexts must resolve once per distinct source");
   var ca=BridgeRoadGeneration.Find(a)!;var cb=BridgeRoadGeneration.Find(b)!;
   Need(ca!=cb&&ReferenceEquals(ca,BridgeRoadGeneration.Find(ca.SourceEntity)),"independent source identity");
   var clone=new NetGeometryPrefab();BridgeRoadGeneration.RegisterClone(a,clone);
   Need(ReferenceEquals(ca,BridgeRoadGeneration.Find(clone)),"clone context mismatch");
   captured=scope.Capture();
   Need(ReferenceEquals(ca,captured[clone]),"captured preview context not identical");
   bool rejected=false;try{BridgeRoadGeneration.ForSampling(new Entity(123456));}catch(InvalidOperationException){rejected=true;}Need(rejected,"missing active sampling context accepted");
   rejected=false;try{BridgeRoadGeneration.RegisterClone(new NetGeometryPrefab(),new NetGeometryPrefab());}catch(InvalidOperationException){rejected=true;}Need(rejected,"missing clone source accepted");
   Need(!ca.AllowsLevel(new[]{NetPieceRequirements.Tunnel},null,null),"AllTunnel accepted");
   Need(!ca.AllowsLevel(null,null,new[]{NetPieceRequirements.Elevated}),"NoneElevated accepted");
   Need(!ca.AllowsLevel(null,new[]{NetPieceRequirements.Tunnel},null),"AnyTunnel accepted");
   Need(ca.AllowsLevel(null,new[]{NetPieceRequirements.Tunnel,NetPieceRequirements.Node},null),"physical Node condition discarded");
   Need(ca.AllowsLevel(null,new[]{NetPieceRequirements.Local},null),"section-local condition discarded");
   var flags=new CompositionFlags{m_General=CompositionFlags.General.Tunnel|CompositionFlags.General.Node,m_Left=17,m_Right=31};
   var locked=ca.ApplyLevel(flags);Need(locked.m_General==(CompositionFlags.General.Elevated|CompositionFlags.General.Node)&&locked.m_Left==17&&locked.m_Right==31,"local flags lost");
   using(var nested=new BridgeRoadGeneration(prefabs,b)){Need(BridgeRoadGeneration.Find(a)==null,"scope isolation");}
   Need(ReferenceEquals(ca,BridgeRoadGeneration.Find(a)),"nested scope failed restore");
   var bad=new NetGeometryPrefab{Invalid=true};try{using var failed=new BridgeRoadGeneration(prefabs,bad);throw new Exception("failure accepted");}catch(InvalidOperationException){}
   Need(ReferenceEquals(ca,BridgeRoadGeneration.Find(a)),"constructor failure changed active context");
   var raw=new NetGeometryData{m_Flags=GeometryFlags.ElevatedIsRaised|GeometryFlags.Other,Height=123};var sel=BridgeRoadContext.SelectionGeometry(raw);
   Need(raw.m_Flags==(GeometryFlags.ElevatedIsRaised|GeometryFlags.Other)&&raw.Height==sel.Height&&sel.m_Flags==(GeometryFlags.RequireElevated|GeometryFlags.Other),"selection changed unrelated metadata");
  }
  Need(BridgeRoadGeneration.Find(a)==null&&BridgeRoadGeneration.Find(b)==null,"scope leaked");
  Need(captured!=null&&captured[a].Flags.m_General==CompositionFlags.General.Elevated,"preview context lost after scope disposal");
  Need(BridgeRoadGeneration.ForSampling(new Entity(123456))==null,"standalone native tool unexpectedly requires generation scope");
  Console.WriteLine("PASS captured preview lifetime, missing active-context negatives, standalone isolation; production source-linked context: distinct-source once, clone/entity reuse, nested/dispose/failure restore, level-only metadata and flags. Native ECS tested separately.");
 }
}
namespace Game.Prefabs {
 class NetGeometryPrefab{internal bool Invalid;}
 struct CompositionFlags{[Flags] internal enum General{Elevated=1,Tunnel=2,Node=4} internal General m_General;internal int m_Left,m_Right; public static bool operator ==(CompositionFlags a,CompositionFlags b)=>a.m_General==b.m_General&&a.m_Left==b.m_Left&&a.m_Right==b.m_Right;public static bool operator !=(CompositionFlags a,CompositionFlags b)=>!(a==b);public override bool Equals(object? b)=>b is CompositionFlags f&&this==f;public override int GetHashCode()=>HashCode.Combine(m_General,m_Left,m_Right);}
 struct NetGeometryData{internal GeometryFlags m_Flags;internal int Height;}
 enum NetPieceRequirements{Elevated,Tunnel,Node,Local}
 static class NetCompositionHelpers{internal static void GetRequirementFlags(NetPieceRequirements[] requirements,out CompositionFlags flags,out int section){flags=default;section=0;foreach(var r in requirements){switch(r){case NetPieceRequirements.Elevated:flags.m_General|=CompositionFlags.General.Elevated;break;case NetPieceRequirements.Tunnel:flags.m_General|=CompositionFlags.General.Tunnel;break;case NetPieceRequirements.Node:flags.m_General|=CompositionFlags.General.Node;break;default:section|=1;break;}}}}
 class PrefabSystem{readonly Dictionary<NetGeometryPrefab,Entity> ids=new();internal bool TryGetEntity(NetGeometryPrefab p,out Entity e){if(p.Invalid){e=default;return false;}if(!ids.TryGetValue(p,out e))ids[p]=e=new Entity(ids.Count+1);return true;}}
}
namespace Unity.Entities{readonly record struct Entity(int Index);class World{internal static readonly World DefaultGameObjectInjectionWorld=new();readonly BridgeRoadWidthSystem widths=new();internal T GetOrCreateSystemManaged<T>()=>(T)(object)widths;}}
namespace BridgeBuilder.Systems{class BridgeRoadWidthSystem{internal int Resolutions,Reads;internal static CompositionFlags ElevatedFlags(NetGeometryPrefab p){World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BridgeRoadWidthSystem>().Resolutions++;return new(){m_General=CompositionFlags.General.Elevated};}internal float Read(NetGeometryPrefab p,object? breakdown,List<(string Name,float Width)>? sections=null,bool extensions=false,CompositionFlags? selectedFlags=null){Reads++;sections?.Add(("native section",12));return extensions?1:12;}}}

namespace Game.Net{[Flags] enum GeometryFlags{RequireElevated=1,ElevatedIsRaised=2,Other=4}}
