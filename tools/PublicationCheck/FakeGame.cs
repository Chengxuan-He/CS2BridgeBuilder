using System.Runtime.CompilerServices;

// Minimal native boundary. AddElement is intentionally append-only, as in installed Game.dll.
namespace Unity.Entities
{
    public readonly record struct Entity(int Index);
    public struct EntityArchetype { public bool Valid; }
    public sealed class DynamicBuffer<T> : List<T>
    {
        public int Length => Count;
    }
    public sealed class EntityManager
    {
        private int _next;
        private readonly Dictionary<Entity, Dictionary<Type, object>> _data = new();
        public Entity Create()
        {
            var entity = new Entity(++_next);
            _data[entity] = new();
            return entity;
        }
        public bool Exists(Entity entity) => _data.ContainsKey(entity);
        public bool HasComponent<T>(Entity entity) => _data[entity].ContainsKey(typeof(T));
        public T GetComponentData<T>(Entity entity) => (T)_data[entity][typeof(T)];
        public void Set<T>(Entity entity, T value) where T : notnull => _data[entity][typeof(T)] = value;
        public bool HasBuffer<T>(Entity entity) => HasComponent<DynamicBuffer<T>>(entity);
        public DynamicBuffer<T> GetBuffer<T>(Entity entity, bool readOnly = false) => GetComponentData<DynamicBuffer<T>>(entity);
    }
    public sealed class World
    {
        private readonly Dictionary<Type, object> _systems = new();
        public EntityManager EntityManager { get; } = new();
        public T GetOrCreateSystemManaged<T>() where T : new()
        {
            if (!_systems.TryGetValue(typeof(T), out var system))
            {
                system = new T();
                _systems[typeof(T)] = system;
                if (system is Game.GameSystemBase game) game.Initialize(this);
                if (system is Game.Prefabs.PrefabSystem prefabs) prefabs.World = this;
            }
            return (T)system;
        }
        public T? GetExistingSystemManaged<T>() where T : class => GetExistingSystemManaged(typeof(T)) as T;
        public object? GetExistingSystemManaged(Type type) => _systems.GetValueOrDefault(type);
    }
}

namespace Game
{
    public abstract class GameSystemBase
    {
        public Unity.Entities.World World { get; private set; } = null!;
        public Unity.Entities.EntityManager EntityManager => World.EntityManager;
        public bool Enabled { get; set; } = true;
        public void Initialize(Unity.Entities.World world) { World = world; OnCreate(); }
        protected virtual void OnCreate() { }
        protected abstract void OnUpdate();
        public void Update() { if (Enabled) OnUpdate(); }
    }
}
namespace Game.Common { public struct Deleted { } }

namespace Game.Prefabs
{
    using Unity.Entities;
    public class PrefabBase
    {
        public string name = "";
        public List<object> components = new();
    }
    public class UIGroupPrefab : PrefabBase { }
    public class NetGeometryPrefab : PrefabBase { public object[]? m_Sections; }
    public class NetPrefab : NetGeometryPrefab { }
    public sealed class UIObject { public UIGroupPrefab? m_Group; public bool m_IsDebugObject; }
    public struct UIObjectData { public Entity m_Group; }
    public struct UIGroupElement { public Entity m_Prefab; }
    public struct NetGeometrySection { }
    public struct NetData { public EntityArchetype m_NodeArchetype; public EntityArchetype m_EdgeArchetype; }

    public sealed class PrefabSystem
    {
        public World World = null!;
        public List<PrefabBase> AddCalls = new();
        public Dictionary<PrefabBase, int> Initializations = new();
        private readonly Dictionary<PrefabBase, Entity> _entities = new();
        private readonly HashSet<PrefabBase> _created = new();
        private readonly HashSet<PrefabBase> _updates = new();
        public PrefabBase? Reject;
        public Entity EntityOf(PrefabBase prefab) => _entities[prefab];
        public bool TryGetEntity(PrefabBase prefab, out Entity entity) => _entities.TryGetValue(prefab, out entity);
        public void AddOrUpdatePrefab(PrefabBase prefab)
        {
            AddCalls.Add(prefab);
            if (prefab == Reject) return;
            if (_entities.ContainsKey(prefab)) { _updates.Add(prefab); return; }
            _entities[prefab] = World.EntityManager.Create();
            _created.Add(prefab);
        }
        public void ApplyQueuedUpdates()
        {
            foreach (var prefab in _updates)
            {
                var oldEntity = _entities[prefab];
                foreach (var ui in prefab.components.OfType<UIObject>())
                    if (ui.m_Group != null)
                        World.EntityManager.GetBuffer<UIGroupElement>(_entities[ui.m_Group])
                            .RemoveAll(entry => entry.m_Prefab == oldEntity);
                _entities[prefab] = World.EntityManager.Create();
                _created.Add(prefab);
            }
            _updates.Clear();
        }
        public void NativePass()
        {
            var em = World.EntityManager;
            foreach (var prefab in _created)
            {
                Initializations[prefab] = Initializations.GetValueOrDefault(prefab) + 1;
                var entity = _entities[prefab];
                if (prefab is UIGroupPrefab) em.Set(entity, new DynamicBuffer<UIGroupElement>());
                if (prefab is NetPrefab) em.Set(entity, new NetData
                {
                    m_NodeArchetype = new() { Valid = true }, m_EdgeArchetype = new() { Valid = true }
                });
                if (prefab is NetGeometryPrefab) em.Set(entity, new DynamicBuffer<NetGeometrySection> { new() });
                foreach (var ui in prefab.components.OfType<UIObject>())
                {
                    if (ui.m_Group == null) continue;
                    var group = _entities[ui.m_Group];
                    em.Set(entity, new UIObjectData { m_Group = group });
                    em.GetBuffer<UIGroupElement>(group).Add(new UIGroupElement { m_Prefab = entity });
                }
            }
            _created.Clear();
        }
    }
}

namespace Game.UI.InGame
{
    public sealed class Binding
    {
        public int Count;
        public void Update() => Count++;
        public void UpdateAll() => Count++;
    }
    public sealed class ToolbarUISystem
    {
        private readonly Binding m_ToolbarGroupsBinding = new();
        private readonly Binding m_AssetMenuCategoriesBinding = new();
        private readonly Binding m_AssetsBinding = new();
        public int AssetRefreshes => m_AssetsBinding.Count;
        public int CategoryRefreshes => m_AssetMenuCategoriesBinding.Count;
    }
}

namespace CS2Mods.Shared.Conversion
{
    internal sealed record PrefabCloneNode(Game.Prefabs.PrefabBase Target, bool IsRoot, bool NeedsSave);
}
namespace CS2Mods.Shared.Infrastructure
{
    internal sealed class ExportReport
    {
        internal List<string> Failures = new();
        internal void Failed(string target, Exception exception) => Failures.Add(target + ": " + exception.Message);
        internal void Note(string message) { }
        internal void Warning(string message) { }
    }
    internal sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        internal static readonly ReferenceEqualityComparer<T> Instance = new();
        public bool Equals(T? a, T? b) => ReferenceEquals(a, b);
        public int GetHashCode(T item) => RuntimeHelpers.GetHashCode(item);
    }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static Logger Log = new(); }
    internal sealed class Logger
    {
        internal void Error(Exception exception, string message) => throw new Exception(message, exception);
        internal void Warn(Exception exception, string message) => throw new Exception(message, exception);
    }
}
