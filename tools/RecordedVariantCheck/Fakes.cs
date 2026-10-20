// Native API boundary substitutes. Catalogue selection uses the real BridgeStyle.cs.
namespace Game.Prefabs
{
    public class PrefabBase
    {
        public string name = "";
        public bool isBuiltin = true;
        public Dictionary<Type, object> Components = new();
        public T? GetComponent<T>() where T : class => Components.GetValueOrDefault(typeof(T)) as T;
    }
    public class NetGeometryPrefab : PrefabBase { }
    public class RoadPrefab : NetGeometryPrefab { }
    public class Bridge { }
    public class ObjectPrefab : PrefabBase { }
    public class ObjectGeometryPrefab : ObjectPrefab { public ObjectMeshInfo[]? m_Meshes; }
    public class ObjectMeshInfo { public PrefabBase? m_Mesh; public Point m_Position; }
    public struct Point { public float x, y; }
    public struct Bounds { public Point min, max; }
    public class RenderPrefab : PrefabBase { public Bounds bounds; }
    public class OverheadNetSections { public Section[]? m_Sections; }
    public class Section { public NetGeometryPrefab? m_Section; public Point m_Offset; }
    public class NetSubObjects { public SubObject[]? m_SubObjects; }
    public class SubObject { public ObjectPrefab? m_Object; public Point m_Position; }
    public class AuxiliaryNets { public AuxiliaryNetInfo[]? m_AuxiliaryNets; }
    public class AuxiliaryNetInfo { public Point m_Position; }
}
namespace BridgeBuilder.Bridges
{
    internal class BridgePrototypeSource
    {
        internal bool IsAvailable = true;
        internal string Label => "Test DLC";
        internal string Key => "content:test";
        internal bool HasSingleSource => true;
        internal int Priority => 1;
        internal static BridgePrototypeSource Inspect(Game.Prefabs.PrefabBase prefab, bool pack) => new();
    }
    internal static class NetWidth
    {
        internal static float Of(Game.Prefabs.NetGeometryPrefab prefab) => 8;
        internal static float RoadSurfaceOf(Game.Prefabs.NetGeometryPrefab prefab) => 8;
    }
    internal static class BridgeStyleDefinitions
    {
        internal static bool SupportsDeckMode(string id, bool doubleDeck) => id == "Lift03" ? doubleDeck : !doubleDeck;
    }
    internal static class BridgeTowers
    {
        internal readonly record struct Tower(string Name, float Road);
        internal static Tower? Select(string id, float width, Func<string, bool> hasDonor) => null;
        internal static float RoadOf(string id) => 0;
    }
}
