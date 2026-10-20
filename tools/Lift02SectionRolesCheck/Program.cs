using BridgeBuilder.Bridges;
using Game.Prefabs;

var intersection = NetPieceRequirements.Intersection;
var node = NetPieceRequirements.Node;
var cases = new[]
{
    (new NetSectionInfo(), Lift02NetworkData.Role.Span),
    (new NetSectionInfo { m_Median = true }, Lift02NetworkData.Role.Span),
    (new NetSectionInfo { m_Median = true, m_RequireAll = new[] { intersection, node } }, Lift02NetworkData.Role.IntersectionMedian),
    (new NetSectionInfo { m_Median = true, m_RequireAny = new[] { intersection } }, Lift02NetworkData.Role.IntersectionMedian),
    // This median may appear at a non-intersection node, so it needs span branches.
    (new NetSectionInfo { m_Median = true, m_RequireAny = new[] { intersection, node } }, Lift02NetworkData.Role.Span),
    (new NetSectionInfo { m_RequireAll = new[] { intersection } }, Lift02NetworkData.Role.Span)
};
var inputs = cases.Select(x => x.Item1).ToArray();
var roles = Lift02SectionRoles.Resolve(inputs);
for (var i = 0; i < cases.Length; i++)
    if (roles[i] != cases[i].Item2) throw new Exception($"Role mismatch at {i}");
if (inputs[2].m_RequireAll!.Length != 2 || inputs[4].m_RequireAny!.Length != 2)
    throw new Exception("Source requirements changed");
foreach (var invalid in new NetSectionInfo[] { null!, new() { m_Section = null! } })
{
    var failed = false;
    try { Lift02SectionRoles.Resolve(new[] { invalid }); }
    catch (InvalidOperationException) { failed = true; }
    if (!failed) throw new Exception("Missing external section accepted");
}
Console.WriteLine("PASS Lift02 conditional median/span roles, source preservation and missing-section errors; native assembly remains unverified.");

namespace BridgeBuilder.Bridges
{
    internal static class Lift02NetworkData { internal enum Role { IntersectionMedian, Span } }
}
namespace Game.Prefabs
{
    public enum NetPieceRequirements { Intersection, Node }
    public class NetSectionPrefab { }
    public class NetSectionInfo
    {
        public NetSectionPrefab m_Section = new();
        public bool m_Median;
        public NetPieceRequirements[]? m_RequireAll;
        public NetPieceRequirements[]? m_RequireAny;
    }
}
