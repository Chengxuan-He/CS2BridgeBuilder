namespace Game.Common { public struct Updated {} public struct Deleted {} }
namespace Colossal.Localization {
 public class LocalizationManager {
  public event Action? onActiveDictionaryChanged;
  public Dictionary<string,string> Dictionary=new();
  public int Listeners=>onActiveDictionaryChanged?.GetInvocationList().Length??0;
  public void Publish(string owner,string value){Dictionary["Assets.NAME["+owner+"]"]=value;onActiveDictionaryChanged?.Invoke();}
 }
}
namespace Game.SceneFlow { public class GameManager {public static GameManager instance=new(); public Colossal.Localization.LocalizationManager localizationManager=new();} }
namespace Game.Prefabs {
 public class Asset {public string path="";}
 public class PrefabBase {public string name="";public Asset? asset;public int Entity;public bool Live=true;}
 public class NetGeometryPrefab:PrefabBase {}
 public class PrefabSystem {public List<PrefabBase> Loaded=new();public bool TryGetEntity(PrefabBase p,out int entity){entity=p.Entity;return p.Live;}}
}
namespace CS2Mods.Shared.Infrastructure {public static class PrefabCatalog {public static int Reads;public static IEnumerable<Game.Prefabs.PrefabBase> GetAll(Game.Prefabs.PrefabSystem p){Reads++;return p.Loaded;}}}
namespace BridgeBuilder.Runtime {public static class BridgeAssetInfo {
 public static bool IsPrefabName(string name)=>name.Length==37&&name[0]=='b'&&Guid.TryParseExact(name.Substring(1),"D",out _);
 public static bool MatchesOwner(string path,string owner)=>path.Contains(owner,StringComparison.OrdinalIgnoreCase);
}}
namespace BridgeBuilder.Systems {
 public class Entities {public HashSet<int> Removed=new(), Updated=new(); public bool HasComponent<T>(int id)=>typeof(T)==typeof(Game.Common.Deleted)&&Removed.Contains(id);public void AddComponent<T>(int id){if(typeof(T)!=typeof(Game.Common.Updated))throw new Exception("Non-notification mutation");Updated.Add(id);}}
 public partial class BridgeGenerationSystem {
  private Game.Prefabs.PrefabSystem _prefabSystem=new(); public Entities EntityManager=new();
  public List<Game.Prefabs.PrefabBase> Prefabs=>_prefabSystem.Loaded;
  public void Start()=>InitializeNativeNames(); public void Stop()=>ReleaseNativeNames();
  public void ColdLoad()=>QueueNativeNames();public void Tick()=>RefreshNativeNames();
 }
}
