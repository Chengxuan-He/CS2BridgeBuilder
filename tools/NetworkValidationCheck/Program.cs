using BridgeBuilder.Runtime;
using Game.Prefabs;

var passed = 0;
NetGeometryPrefab Good() => new() { name = "test", m_Sections = [new() { m_Section = new() }] };
void Check(string name, NetGeometryPrefab net, bool invalid, string contains = "")
{
    var actual = BridgeNetworkValidation.IsInvalid(net, out var reason);
    if (actual != invalid || !reason.Contains(contains)) throw new Exception(name + ": " + reason);
    Console.WriteLine("PASS " + name); passed++;
}
Check("healthy network", Good(), false);
var net = Good(); net.m_Sections = null; Check("null root array", net, true, "m_Sections is null");
net = Good(); net.m_Sections = []; Check("empty root", net, true, "empty");
net = Good(); net.m_Sections = [null]; Check("null descriptor", net, true, "[0]");
net = Good(); net.m_Sections[0].m_Section = null; Check("null section", net, true, "[0]");
if (net.m_Sections[0].m_Section != null) throw new Exception("validator modified prefab");
foreach (var overhead in new[] { true, false })
{
    net = Good();
    if (overhead) net.Add(new OverheadNetSections { m_Sections = null });
    else net.Add(new UndergroundNetSections { m_Sections = null });
    Check("null optional array " + overhead, net, true, overhead ? "Overhead" : "Underground");
    net = Good();
    if (overhead) net.Add(new OverheadNetSections { m_Sections = [new()] });
    else net.Add(new UndergroundNetSections { m_Sections = [new()] });
    Check("null optional section " + overhead, net, true, "[0]");
    net = Good();
    if (overhead) net.Add(new OverheadNetSections { m_Sections = [] });
    else net.Add(new UndergroundNetSections { m_Sections = [] });
    Check("empty optional array is valid " + overhead, net, false);
}
net = Good(); net.Add(new AuxiliaryNets()); Check("null auxiliary array", net, true, "AuxiliaryNets");
net = Good(); net.Add(new AuxiliaryNets { m_AuxiliaryNets = [] }); Check("empty auxiliaries valid", net, false);
net = Good(); net.Add(new AuxiliaryNets { m_AuxiliaryNets = [null] }); Check("null auxiliary descriptor", net, true, "[0]");
net = Good(); net.Add(new AuxiliaryNets { m_AuxiliaryNets = [new()] }); Check("missing lower file", net, true, "[0]");
var lower = Good(); lower.m_Sections = null;
net = Good(); net.Add(new AuxiliaryNets { m_AuxiliaryNets = [new() { m_Prefab = lower }] });
Check("bad lower rejects parent", net, true, "m_Sections is null");
lower.m_Sections = Good().m_Sections; Check("recovered child is revalidated", net, false);
lower.Add(new AuxiliaryNets { m_AuxiliaryNets = [new() { m_Prefab = net }] });
Check("cycle terminates", net, false);
lower.m_Sections = [new()]; Check("bad cyclic graph", net, true, "m_Section is null");
Console.WriteLine($"{passed} checks passed. Array validation only; not a native game/runtime test.");

namespace Game.Prefabs
{
    public class PrefabBase { public string name = ""; }
    public class NetGeometryPrefab : PrefabBase
    {
        public NetSectionInfo[] m_Sections;
        private readonly Dictionary<Type, object> components = new();
        public void Add<T>(T item) => components[typeof(T)] = item;
        public bool TryGet<T>(out T value)
        {
            if (components.TryGetValue(typeof(T), out var item)) { value = (T)item; return true; }
            value = default; return false;
        }
    }
    public class NetSectionInfo { public PrefabBase m_Section; }
    public class OverheadNetSections { public NetSectionInfo[] m_Sections; }
    public class UndergroundNetSections { public NetSectionInfo[] m_Sections; }
    public class AuxiliaryNets { public AuxiliaryNet[] m_AuxiliaryNets; }
    public class AuxiliaryNet { public PrefabBase m_Prefab; }
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
