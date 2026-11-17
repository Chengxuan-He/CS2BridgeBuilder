using BridgeBuilder.Runtime;
using Game.Prefabs;
using Game.Tools;

int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new Exception(message);}
NetPrefab doomed=new("owner"), lower=new("owner-lower"), survivor=new("other"), road=new("base");
bool Owns(PrefabBase p)=>p.name.StartsWith("owner",StringComparison.Ordinal);
foreach(bool selected in new[]{false,true})
foreach(bool cached in new[]{false,true})
foreach(bool active in new[]{false,true})
{
 var registry=new HashSet<PrefabBase>{doomed,lower,survivor,road};
 var net=new NetToolSystem(registry){prefab=selected?doomed:survivor};net.Cache(cached?lower:survivor);
 var tools=new ToolSystem{activePrefab=active?doomed:survivor};int replacements=0;
 BridgeToolRetirement.Release(tools,net,Owns,()=>{replacements++;return road;});
 registry.Remove(doomed);registry.Remove(lower);
 net.GetAvailableSnapMask(); // Same two-state native lookup contract; would throw before this fix.
 Check(net.prefab==(selected?road:survivor),"Selected owner replaced, unrelated selection preserved");
 Check(net.Effective==(cached?null:survivor),"Effective cache ownership is independent of selected prefab");
 Check(tools.activePrefab==(active?null:survivor),"Only owned active tool stopped");
 Check(replacements==(selected?1:0),"No fallback lookup for unrelated selection");
 BridgeToolRetirement.Release(tools,net,Owns,()=>throw new Exception("Repeated retirement replaced healthy selection"));
 net.GetAvailableSnapMask();Check(true,"Repeated retirement unchanged");
 net.Cache(net.prefab);net.GetAvailableSnapMask();Check(true,"Later normal raycast rebuild remains valid");
}
{
 var registry=new HashSet<PrefabBase>{doomed,road};var net=new NetToolSystem(registry){prefab=doomed};net.Cache(doomed);
 net.prefab=road;registry.Remove(doomed);bool oldThrows=false;try{net.GetAvailableSnapMask();}catch(KeyNotFoundException){oldThrows=true;}
 Check(oldThrows,"Regression reproduces old setter-only failure");
 BridgeToolRetirement.Release(new ToolSystem(),net,Owns,()=>road);net.GetAvailableSnapMask();Check(true,"Already changed selected prefab still releases stale effective cache");
}
{
 var registry=new HashSet<PrefabBase>{road};var net=new NetToolSystem(registry){lane=new PrefabBase("owner-lane")};net.Cache(doomed);
 BridgeToolRetirement.Release(new ToolSystem(),net,Owns,()=>road);
 Check(net.prefab==road&&net.lane==null&&net.Effective==null,"Owned lane and effective prefab released");
 BridgeToolRetirement.Release(new ToolSystem{activePrefab=survivor},null,Owns,()=>throw new Exception("No NetTool"));
 Check(true,"World without NetTool");
}
Console.WriteLine($"PASS {checks} production retirement checks; native selected/effective cache semantics, no game or player assets used.");

namespace Game.Prefabs
{
 public class PrefabBase(string value){public string name=value;}
 public class NetPrefab(string value):PrefabBase(value){}
}
namespace Game.Tools
{
 public class ToolSystem{public PrefabBase? activePrefab;public bool ActivatePrefabTool(PrefabBase? prefab){activePrefab=prefab;return prefab!=null;}}
 public class NetToolSystem(HashSet<PrefabBase> registry)
 {
  // Native NetToolSystem setter only changes m_SelectedPrefab. The snap getter reads m_Prefab.
  private NetPrefab? m_Prefab;
  private NetPrefab? m_SelectedPrefab;
  public PrefabBase? lane;
  public NetPrefab? prefab{get=>m_SelectedPrefab;set{if(value==m_SelectedPrefab)return;m_SelectedPrefab=value;if(value!=null)lane=null;if(value==null||!registry.Contains(value))throw new KeyNotFoundException();}}
  public PrefabBase? Effective=>m_Prefab;
  public void Cache(NetPrefab? value)=>m_Prefab=value;
  public void GetAvailableSnapMask(){if(m_Prefab!=null&&!registry.Contains(m_Prefab))throw new KeyNotFoundException(m_Prefab.name);}
 }
}
