namespace Unity.Entities
{
    public record struct Entity(int Id);
    public class State
    {
        public bool Locked, Created, HasLocked = true;
        public Game.Prefabs.UnlockRequirement[]? Requirements;
    }
    public class Buffer<T>(T[] items) { public int Length => items.Length; public T this[int i] => items[i]; }
    public class EntityManager
    {
        public Dictionary<Entity, State> All = new();
        public bool Exists(Entity e) => All.ContainsKey(e);
        public bool HasComponent<T>(Entity e) => typeof(T) == typeof(Game.Common.Created) ? All[e].Created : All[e].HasLocked;
        public bool HasBuffer<T>(Entity e) => All[e].Requirements != null;
        public bool IsComponentEnabled<T>(Entity e) => All[e].Locked;
        public Buffer<T> GetBuffer<T>(Entity e, bool readOnly) => new((T[])(object)All[e].Requirements!);
    }
}
namespace Game.Common { public struct Created { } }
namespace Colossal { public record struct Hash128(string Text) { public static Hash128 Parse(string text) => new(text); } }
namespace Colossal.IO.AssetDatabase
{
    public class PrefabAsset { public string path = ""; public T? GetInstance<T>() where T : class => null; }
    public class Map { public bool TryGetObject(string id, out object? value) { value = null; return false; } }
    public class Resources { public Map prefabsMap = new(); }
    public class Database
    {
        public Resources resources = new();
        public bool TryGetAsset(Colossal.Hash128 id, out PrefabAsset value) { value = new(); return false; }
    }
    public static class AssetDatabase { public static Database global = new(); }
}
namespace Game.Prefabs
{
    public class ComponentBase { public string name = ""; public bool active = true; }
    public class UnlockableBase : ComponentBase { }
    public class ManualUnlockable : UnlockableBase { }
    public class Unlockable : UnlockableBase
    { public bool m_IgnoreDependencies; public PrefabBase[]? m_RequireAll, m_RequireAny; }
    public class PrefabBase : ComponentBase
    {
        public bool isBuiltin, isReadOnly;
        public Colossal.IO.AssetDatabase.PrefabAsset? asset;
        public List<ComponentBase> components = new();
        public T? GetComponent<T>() where T : ComponentBase => components.OfType<T>().FirstOrDefault();
        public T AddComponent<T>() where T : ComponentBase, new() { var c = new T(); components.Add(c); return c; }
        public PrefabID GetPrefabID() => new(GetType().Name, name);
    }
    public class NetPrefab : PrefabBase { }
    public class NetGeometryPrefab : NetPrefab { }
    public record struct PrefabID(string Type, string Name, Colossal.Hash128 Hash = default)
    { public string ToUrlSegment() => Uri.EscapeDataString(Type) + "/" + Uri.EscapeDataString(Name); }
    public class PrefabSystem
    {
        public Dictionary<Unity.Entities.Entity, PrefabBase> All = new();
        public bool TryGetEntity(PrefabBase prefab, out Unity.Entities.Entity entity)
        { entity = All.FirstOrDefault(p => ReferenceEquals(p.Value, prefab)).Key; return entity.Id != 0; }
        public bool TryGetPrefab<T>(Unity.Entities.Entity e, out T? prefab) where T : PrefabBase
        { prefab = All.GetValueOrDefault(e) as T; return prefab != null; }
        public bool TryGetPrefab(PrefabID id, out PrefabBase? prefab)
        { prefab = All.Values.FirstOrDefault(p => p.GetPrefabID() == id); return prefab != null; }
    }
    public struct Locked { }
    public enum UnlockFlags : uint { RequireAll = 1, RequireAny = 2 }
    public struct UnlockRequirement { public Unity.Entities.Entity m_Prefab; public UnlockFlags m_Flags; }
}
namespace BridgeBuilder.Runtime
{
    internal static class BridgeSessionState { internal static bool Restart; internal static void RequireRestart() { Restart = true; } internal static void Start() { } }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static TestLog Log = new(); }
    internal class TestLog { internal void Warn(string message) { } internal void Info(string message) { } }
}
namespace UnityEngine
{
    public static class Application { public static string persistentDataPath = Path.Combine(Path.GetTempPath(), "BBStartupCheck-" + Guid.NewGuid().ToString("N")); }
}
namespace BridgeBuilder.Runtime { internal static class BridgeAssetCatalog { internal static void ResetSession() { } } }
