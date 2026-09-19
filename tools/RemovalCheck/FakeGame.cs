// Only persistence/reference boundaries; no geometry or rendering is simulated by these tests.
namespace UnityEngine
{
    public class Object { }
    public sealed class SerializeField : Attribute { }
    public sealed class SerializeReference : Attribute { }
}
namespace Colossal.IO.AssetDatabase
{
    public sealed class PrefabAsset
    {
        public bool Deleted;
        public bool Fail;
        public Action? BeforeDelete;
        public void Delete()
        {
            if (Fail) throw new IOException("test deletion failure");
            BeforeDelete?.Invoke();
            Deleted = true;
        }
    }
}
namespace Game.Prefabs
{
    public class PrefabBase : UnityEngine.Object
    {
        public string name = "";
        public bool isBuiltin;
        public bool isReadOnly;
        public readonly Colossal.IO.AssetDatabase.PrefabAsset SavedAsset = new();
        public Colossal.IO.AssetDatabase.PrefabAsset? asset;
        public PrefabBase() { asset = SavedAsset; }
        public List<object> components = new();
        public PrefabBase[] References = [];
        public bool TryGet<T>(out T value) where T : class
        {
            value = components.OfType<T>().FirstOrDefault()!;
            return value != null;
        }
    }
    public class ObjectPrefab : PrefabBase { }
    public class SpawnableObject { public ObjectPrefab[] m_Placeholders = []; }
}
namespace CS2Mods.Shared.Infrastructure
{
    public sealed class ExportReport
    {
        public List<string> Warnings = new();
        public void Warning(string message) => Warnings.Add(message);
        public void RemovedDependency(string name) { }
    }
}
