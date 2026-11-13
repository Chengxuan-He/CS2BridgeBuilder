using BridgeBuilder.Runtime;
using Game.Prefabs;

var passed = 0;
NetGeometryPrefab Good() => new() { name = "test", m_Sections = [new() { m_Section = new() }] };
void Assert(string name, bool condition)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name); passed++;
}
var cid = "11111111111111111111111111111111";
var net = Good(); net.m_Sections[0].m_Section = null;
net.asset = new() { Text = "{\"name\":\"test\",\"m_Sections\":{\"$rcontent\":[{\"m_Section\":$fstrref:\"CID:" + cid + "\"}]}}" };
var loaded = new NetSectionPrefab();
Colossal.IO.AssetDatabase.AssetDatabase.global.Assets[cid] = new() { id = new() { guid = cid }, Instance = loaded };
Assert("serialized CID with loaded asset recoverable", BridgeLoadedCidRecovery.Inspect(net,out var cids,out var detail) == BridgeLoadedCidRecovery.Result.Recoverable && cids.Contains(cid));
Assert("recovery inspection does not mutate live slot", net.m_Sections[0].m_Section == null);
var database = Colossal.IO.AssetDatabase.AssetDatabase.global;
database.Enumerations = 0;
var sharedLookup = new BridgeLoadedCidRecovery.LoadedIndex();
BridgeLoadedCidRecovery.Inspect(net,out cids,out detail, sharedLookup);
BridgeLoadedCidRecovery.Inspect(net,out cids,out detail, sharedLookup);
Assert("shared inspection lookup never enumerates all assets", database.Enumerations == 0);
Colossal.IO.AssetDatabase.AssetDatabase.global.Assets[cid].Instance = null;
Assert("registered CID without loaded instance is broken", BridgeLoadedCidRecovery.Inspect(net,out cids,out detail) == BridgeLoadedCidRecovery.Result.Broken);
var alternateDatabase = new Colossal.IO.AssetDatabase.AssetDatabase();
alternateDatabase.Assets[cid] = new() { id = new() { guid = cid }, Instance = loaded };
database.ExtraDatabases.Add(alternateDatabase);
Assert("loaded identical CID copy recognized despite unloaded first entry", BridgeLoadedCidRecovery.Inspect(net,out cids,out detail) == BridgeLoadedCidRecovery.Result.Recoverable);
net.asset.Text = "{\"name\":\"test\",\"m_Sections\":{\"$rcontent\":[{\"m_Section\":null}]}}";
Assert("explicit null without CID cannot be recovered", BridgeLoadedCidRecovery.Inspect(net,out cids,out detail) == BridgeLoadedCidRecovery.Result.Broken);
net.m_Sections = null;
Assert("null array cannot be reconstructed by copying", BridgeLoadedCidRecovery.Inspect(net,out cids,out detail) == BridgeLoadedCidRecovery.Result.Broken);
Console.WriteLine($"{passed} serialized dependency recovery checks passed.");

namespace Game.Prefabs
{
    public class PrefabBase : UnityEngine.Object
    {
        public string name = "";
        public bool isBuiltin, isReadOnly;
        public Colossal.IO.AssetDatabase.PrefabAsset asset;
        private readonly Dictionary<Type, object> components = new();
        public void Add<T>(T item) => components[typeof(T)] = item;
        public bool TryGet<T>(out T value)
        {
            if (components.TryGetValue(typeof(T), out var item)) { value = (T)item; return true; }
            value = default; return false;
        }
    }
    public class NetPrefab : PrefabBase { }
    public class NetGeometryPrefab : NetPrefab { public NetSectionInfo[] m_Sections; }
    public class NetSectionPrefab : PrefabBase { public NetSectionInfo[] m_SubSections; public NetPieceInfo[] m_Pieces; }
    public class NetPiecePrefab : PrefabBase { }
    public class NetLanePrefab : PrefabBase { }
    public class NetLaneGeometryPrefab : NetLanePrefab { public NetLaneMeshInfo[] m_Meshes; }
    public class NetLaneMeshInfo { public PrefabBase m_Mesh; }
    public class SecondaryLane : ComponentBase { public NetLaneInfo[] m_LeftLanes, m_RightLanes, m_CrossingLanes; }
    public class AuxiliaryLanes : ComponentBase { public NetLaneInfo[] m_AuxiliaryLanes; }
    public class PrefabSystem
    {
        public HashSet<PrefabBase> Registered = new();
        public bool TryGetEntity(PrefabBase prefab, out int entity) { entity = 0; return Registered.Contains(prefab); }
    }
    public class NetPieceInfo { public PrefabBase m_Piece; }
    public class ComponentBase { public bool active = true; }
    public class NetSubObjects : ComponentBase { public NetSubObjectInfo[] m_SubObjects; }
    public class NetSubObjectInfo { public PrefabBase m_Object; }
    public class NetPieceLanes : ComponentBase { public NetLaneInfo[] m_Lanes; }
    public class NetLaneInfo { public PrefabBase m_Lane; }
    public class NetPieceObjects : ComponentBase { public NetSubObjectInfo[] m_PieceObjects; }
    public class NetPieceCrosswalk : ComponentBase { public PrefabBase m_Lane; }
    public class NetInitializeSystem
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void OnUpdate() => throw new ArgumentNullException("key");
    }
    public class NetSectionInfo { public PrefabBase m_Section; }
    public class OverheadNetSections : ComponentBase { public NetSectionInfo[] m_Sections; }
    public class UndergroundNetSections : ComponentBase { public NetSectionInfo[] m_Sections; }
    public class AuxiliaryNets : ComponentBase { public AuxiliaryNet[] m_AuxiliaryNets; }
    public class AuxiliaryNet { public PrefabBase m_Prefab; }
}
namespace UnityEngine
{
    public class Object { }
    public static class Application { public static string persistentDataPath = ""; }
}
namespace Colossal.Logging
{
    public interface ILog { }
    public enum Level { Critical }
    public static class UnityLogger
    {
        public static event Action<ILog, Level, string, Exception, UnityEngine.Object> OnErrorOrHigher;
        public static void Emit(Exception exception) => OnErrorOrHigher?.Invoke(null, Level.Critical,
            "System update error during PrefabUpdate->NetInitializeSystem", exception, null);
    }
}
namespace BridgeBuilder
{
    public static class Mod { public static TestLog Log = new(); }
    public class TestLog { public void Warn(string message) { } public void Info(string message) { } }
}
namespace Colossal { public class Hash128 { public static string Parse(string value) => value; } }
namespace Colossal.IO.AssetDatabase
{
    public class AssetId { public string guid = ""; }
    public class PrefabAsset
    {
        public string path = ""; public string Text = ""; public AssetId id = new();
        public System.IO.Stream GetReadStream() => new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(Text));
        public object database;
        public Game.Prefabs.PrefabBase Instance;
        public T GetInstance<T>() where T : class => Instance as T;
    }
    public class AssetDatabase
    {
        public static AssetDatabase global = new(); public static object user = new();
        public Dictionary<string, PrefabAsset> Assets = new();
        public int Enumerations; public List<AssetDatabase> ExtraDatabases = new(); public IEnumerable<AssetDatabase> databases => new[]{this}.Concat(ExtraDatabases);
        public IEnumerable<T> GetAssets<T>() { Enumerations++; return Assets.Values.Cast<T>(); }
        public Resources resources = new();
        public bool TryGetAsset(string id, out PrefabAsset asset) => Assets.TryGetValue(id, out asset);
    }
    public class Resources { public Map prefabsMap = new(); }
    public class Map
    {
        public Dictionary<string, object> Objects = new();
        public bool TryGetObject(string id, out object value) => Objects.TryGetValue(id, out value);
    }
}
namespace CS2Mods.Shared.Infrastructure
{
    public class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public static readonly ReferenceEqualityComparer<T> Instance = new();
        public bool Equals(T a, T b) => ReferenceEquals(a, b);
        public int GetHashCode(T value) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
    }
}
