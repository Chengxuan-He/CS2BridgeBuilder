using Game.Prefabs;
using Colossal.IO.AssetDatabase;
namespace UnityEngine { public static class Application { public static string persistentDataPath=""; } }
namespace Colossal.IO.AssetDatabase {
 public struct FakeHash {public string value;public override string ToString()=>value;}
 public class PrefabAsset {public string path="";public (FakeHash guid,int unused) id;private void SetUninitializedInstance(PrefabBase source){} }
}
namespace Game.Prefabs {
 public class ComponentBase {public string name="";public ComponentBase Copy(){var c=(ComponentBase)MemberwiseClone();c.name=GetType().Name;return c;}}
 public class UIObject:ComponentBase {public string m_Icon="";}
 public class Unlockable:ComponentBase {public bool m_IgnoreDependencies;}
 public class ManualUnlockable:ComponentBase {}
 public class AuxiliaryNets:ComponentBase {}
 public class AssetPackItem:ComponentBase {public object[] m_Packs=[];}
 public class PrefabBase {
  public string name="";public int version=1;public PrefabAsset? asset;public List<ComponentBase> components=[];
  public T? GetComponent<T>() where T:class => components.OfType<T>().FirstOrDefault();
  public T AddOrGetComponent<T>() where T:ComponentBase,new(){var c=GetComponent<T>();if(c==null){c=new T{name=typeof(T).Name};components.Add(c);}return c;}
  public void Remove<T>() => components.RemoveAll(c=>c is T);
  // Mirrors native Clone's fresh AddComponent/type-name behavior, without Unity native calls.
  public PrefabBase Clone(string n){var c=(PrefabBase)Activator.CreateInstance(GetType())!;c.name=n;c.version=version;c.components=components.Select(v=>v.Copy()).ToList();return c;}
 }
 public class NetGeometryPrefab:PrefabBase {} public class RoadPrefab:NetGeometryPrefab {}
 public class PrefabSystem { public object EntityManager=new(); }
}
namespace CS2Mods.Shared.Conversion {public static class PrefabGraphCloner { public static bool ShouldStripComponent(ComponentBase c,bool root)=>false;}}
namespace CS2Mods.Shared.Export {public record PrefabCloneNode(PrefabBase Source,PrefabBase Target,bool Root,bool NeedsSave,PrefabAsset? ReplacementAsset);}
namespace BridgeBuilder.Bridges {
 public class BridgeConstructionCost:ComponentBase {}
 public static class NetWidth {public static float RoadSurfaceOf(NetGeometryPrefab p)=>12;}
 public static class BridgeStyleCatalog {
  public static bool Scanned=true;public static void Rebuild(PrefabSystem s,string[] names){}
  public static bool Absent;
  public static Style? Find(string id)=>Absent?null:new();public class Style{public (Variant Variant,int unused) Select(float w,bool forRoad,bool doubleDeck)=>(new(),0);}
  public class Variant{public string RecordedIcon="assetdb://global/repaired";public PrefabBase Donor=new();}
 }
}
namespace BridgeBuilder {public static class Mod {public static Logger Log=new();}public class Logger{public void Error(string text){}public void Warn(string text){}public void Warn(Exception e,string text){}}}
namespace BridgeBuilder.Runtime {
 internal static class BridgeInspectionAssets {internal record Entry(PrefabBase? Prefab,string Path);}
 internal static class BridgeAssetPack {internal static object Pack=new();internal static object Ensure(PrefabSystem s)=>Pack;internal static void Assign(PrefabBase p,object pack)=>p.AddOrGetComponent<AssetPackItem>().m_Packs=[pack];}
 internal class BridgeUnlockExpression {}
 internal static class BridgeUnlockSnapshot {internal static bool Read(PrefabBase p,PrefabSystem s,object em,out BridgeUnlockExpression rule){rule=new();return true;}internal static bool Capture(PrefabBase p,PrefabSystem s,object em,out BridgeUnlockExpression rule)=>Read(p,s,em,out rule);}
 internal static class BridgeNativeUnlock {internal static bool Apply(PrefabBase p,BridgeUnlockExpression rule,PrefabSystem s,List<PrefabBase> created,out string error,bool includeAuxiliary){error="";p.AddOrGetComponent<Unlockable>().m_IgnoreDependencies=true;return true;}}
 internal sealed class BridgeCreationDependencies {internal void Record(PrefabAsset? asset){}}
 internal static class BridgeDependencyPersistence {internal static bool Save(string owner,IEnumerable<string> ids,BridgeCreationDependencies deps,out int count,out string error,out int timing){count=timing=0;error="";return true;}}
 internal static class BridgeNativePresentation {
  internal static void Prepare(PrefabBase p){}
  internal static bool Validate(string owner,IEnumerable<PrefabBase> p,out string error,BridgeCreationDependencies deps){error="";return true;}
  internal static bool EnsureDescriptions(BridgeAssetInfo info,out bool changed,out string error,IEnumerable<PrefabBase> p,ISet<string> paths){changed=false;error="";return true;}
 }
 internal static class BridgePrefabPersistence {
  internal static int Saves;
  internal static string RelativePathFor(string name)=>"ImportedData/"+name+"/Prefabs/Bridge.Prefab";
  internal static void Save(IEnumerable<CS2Mods.Shared.Export.PrefabCloneNode> nodes){foreach(var n in nodes){Write(n.Target);Saves++;}}
  internal static void Write(PrefabBase p){var ui=p.GetComponent<UIObject>()!;File.WriteAllText(p.asset!.path,System.Text.Json.JsonSerializer.Serialize(new{name=p.name,components=new Dictionary<string,object?>{{"$rcontent",new[]{new Dictionary<string,object?>{{"$type","Game.Prefabs.UIObject, Game"},{"name",ui.name},{"m_Icon",ui.m_Icon}}}}},ignored=p.GetComponent<Unlockable>()!.m_IgnoreDependencies,pack=p.GetComponent<AssetPackItem>()!.m_Packs.Length}));}
  internal static RoadPrefab Reload(PrefabAsset asset){using var j=System.Text.Json.JsonDocument.Parse(File.ReadAllText(asset.path));var r=j.RootElement;var p=new RoadPrefab{name=r.GetProperty("name").GetString()!,asset=asset};var ui=r.GetProperty("components").GetProperty("$rcontent")[0];p.AddOrGetComponent<UIObject>().name=ui.GetProperty("name").GetString()!;p.GetComponent<UIObject>()!.m_Icon=ui.GetProperty("m_Icon").GetString()!;p.AddOrGetComponent<Unlockable>().m_IgnoreDependencies=r.GetProperty("ignored").GetBoolean();p.AddOrGetComponent<AssetPackItem>().m_Packs=[BridgeAssetPack.Pack];return p;}
 }
}
