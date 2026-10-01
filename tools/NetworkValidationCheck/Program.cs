using BridgeBuilder.Runtime;
using Game.Prefabs;

var passed = 0;
NetGeometryPrefab Good() => new() { name = "test", m_Sections = [new() { m_Section = new() }] };
void Check(string name, PrefabBase net, bool invalid, string contains = "")
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
lower.m_Sections = [new()]; Check("bad cyclic graph", net, true, "null required");
net = Good(); net.Add(new NetSubObjects { m_SubObjects = [new()] });
Check("missing tower rejects root", net, true, "NetSubObjects");
net = Good(); net.Add(new NetSubObjects { m_SubObjects = null });
Check("null subobject array", net, true, "NetSubObjects");
net = Good(); net.Add(new NetSubObjects { m_SubObjects = [] });
Check("empty subobjects valid", net, false);
net.Add(new NetSubObjects { m_SubObjects = [new() { m_Object = new() }] });
Check("healthy subobjects valid", net, false);
var section = new NetSectionPrefab();
Check("native nullable section arrays valid", section, false);
section.m_SubSections = [new()]; Check("missing subsection", section, true, "m_SubSections[0]");
section.m_SubSections = [new() { m_Section = section }];
Check("cyclic section graph terminates", section, false);
section.m_Pieces = [new()]; Check("missing piece", section, true, "m_Pieces[0]");
net = Good(); net.m_Sections[0].m_Section = section;
Check("nested missing piece rejects root", net, true, "m_Pieces[0]");
var piece = new NetPiecePrefab(); section.m_Pieces = [new() { m_Piece = piece }];
Check("recovered section accepted", net, false);
piece.Add(new NetPieceLanes { m_Lanes = [new()] });
Check("missing lane rejects piece", piece, true, "m_Lanes[0]");
Check("missing lane rejects owning root", net, true, "m_Lanes[0]");
piece.Add(new NetPieceLanes { m_Lanes = null });
Check("native nullable lane array valid", net, false);
piece.Add(new NetPieceObjects { m_PieceObjects = [new()] });
Check("missing piece object", net, true, "m_PieceObjects[0]");
piece.Add(new NetPieceObjects { m_PieceObjects = null });
Check("null piece object array", net, true, "m_PieceObjects is null");
piece.Add(new NetPieceObjects { m_PieceObjects = [] });
piece.Add(new NetPieceCrosswalk());
Check("missing crosswalk lane", net, true, "NetPieceCrosswalk");
piece.Add(new NetPieceCrosswalk { m_Lane = new() });
Check("recovered piece accepted", net, false);
net = Good(); net.Add(new OverheadNetSections { active = false });
Check("inactive overhead ignored", net, false);
net = Good(); net.Add(new UndergroundNetSections { active = false });
Check("inactive underground ignored", net, false);
net = Good(); net.Add(new AuxiliaryNets { active = false });
Check("inactive auxiliary ignored", net, false);
net = Good(); net.Add(new NetSubObjects { active = false });
Check("inactive subobjects ignored", net, false);
piece = new(); piece.Add(new NetPieceLanes { active = false, m_Lanes = [new()] });
Check("inactive lanes ignored", piece, false);
piece = new(); piece.Add(new NetPieceObjects { active = false });
Check("inactive piece objects ignored", piece, false);
piece = new(); piece.Add(new NetPieceCrosswalk { active = false });
Check("inactive crosswalk ignored", piece, false);

void Assert(string name, bool condition)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name); passed++;
}
BridgeLoadFailures.Start();
Colossal.Logging.UnityLogger.Emit(new InvalidOperationException("unrelated"));
Assert("unrelated errors do not block cleanup", !BridgeLoadFailures.NetworkInitializationFailed);
try { new NetInitializeSystem().OnUpdate(); }
catch (Exception exception) { Colossal.Logging.UnityLogger.Emit(exception); }
Assert("native batch failure blocks cleanup without prefab context", BridgeLoadFailures.NetworkInitializationFailed);
BridgeLoadFailures.Clear();
Assert("map preload cannot erase batch failure", BridgeLoadFailures.NetworkInitializationFailed);
BridgeLoadFailures.Stop();
Assert("new mod session resets batch failure", !BridgeLoadFailures.NetworkInitializationFailed);
BridgeLoadFailures.RequireRestart();
BridgeLoadFailures.Clear();
Assert("legacy migration blocks cleanup across map preload", BridgeLoadFailures.NetworkInitializationFailed);
BridgeLoadFailures.Stop();
Assert("restart clears migration latch", !BridgeLoadFailures.NetworkInitializationFailed);
Console.WriteLine($"{passed} checks passed. Reference validation and safety latch only; not a native game/runtime test.");

namespace Game.Prefabs
{
    public class PrefabBase : UnityEngine.Object
    {
        public string name = "";
        public bool isBuiltin, isReadOnly;
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
namespace UnityEngine { public class Object { } }
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
    public class TestLog { public void Warn(string message) { } }
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
