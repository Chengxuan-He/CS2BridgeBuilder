// Entity lifetime/topology only. No geometry, rendering, save or asset database operations.
namespace Unity.Collections { public enum Allocator { Temp } }
namespace Unity.Entities
{
    public readonly record struct Entity(int Index) { public static Entity Null => default; }
    public readonly record struct ComponentType(Type Type)
    {
        public static ComponentType ReadOnly<T>() => new(typeof(T));
    }
    public sealed class EntityQueryDesc
    {
        public ComponentType[] All = [];
        public ComponentType[] Any = [];
        public ComponentType[] None = [];
    }
    public sealed class EntityArray(IEnumerable<Entity> source) : List<Entity>(source), IDisposable
    {
        public void Dispose() { }
    }
    public sealed class EntityQuery(EntityManager manager, EntityQueryDesc query) : IDisposable
    {
        public EntityArray ToEntityArray(Unity.Collections.Allocator allocator) => new(manager.Items
            .Where(pair => query.All.All(c => pair.Value.ContainsKey(c.Type))
                && (query.Any.Length == 0 || query.Any.Any(c => pair.Value.ContainsKey(c.Type)))
                && query.None.All(c => !pair.Value.ContainsKey(c.Type))).Select(pair => pair.Key).ToArray());
        public void Dispose() { }
    }
    public sealed class EntityManager
    {
        private int _next;
        internal readonly Dictionary<Entity, Dictionary<Type, object>> Items = new();
        public Entity Create(params object[] components)
        {
            var entity = new Entity(++_next);
            Items[entity] = components.ToDictionary(c => c.GetType());
            return entity;
        }
        public void Set<T>(Entity entity, T value) where T : notnull => Items[entity][typeof(T)] = value;
        public bool Exists(Entity entity) => Items.ContainsKey(entity);
        public bool HasComponent<T>(Entity entity) => Items[entity].ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity entity) => (T)Items[entity][typeof(T)];
        public void AddComponent<T>(Entity entity) where T : notnull, new() => Set(entity, new T());
        public bool HasBuffer<T>(Entity entity) => Items[entity].ContainsKey(typeof(List<T>));
        public List<T> GetBuffer<T>(Entity entity, bool readOnly = false) => (List<T>)Items[entity][typeof(List<T>)];
        public EntityQuery CreateEntityQuery(EntityQueryDesc query) => new(this, query);
        public void Destroy(Entity entity) => Items.Remove(entity);
    }
}
namespace Game.Common { public struct Deleted { } public struct Updated { } }
namespace Game.Tools { public struct Temp { } }
namespace Game.Prefabs
{
    public struct PrefabRef { public Unity.Entities.Entity m_Prefab; }
    public struct PrefabData { }
    public struct NetCompositionData { }
}
namespace Game.Net
{
    public struct Node { }
    public struct Edge { public Unity.Entities.Entity m_Start, m_End; }
    public struct ConnectedEdge { public Unity.Entities.Entity m_Edge; }
}
