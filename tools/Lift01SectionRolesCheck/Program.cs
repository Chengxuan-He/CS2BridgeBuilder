using BridgeBuilder.Bridges;
using Game.Prefabs;

var checks = 0;
void Check(bool condition)
{
    if (!condition) throw new Exception("Section role mismatch");
    checks++;
}
NetSectionPrefab Section(params NetPieceLayer[] layers) => new()
{ m_Pieces = layers.Select(layer => new NetPieceInfo { m_Piece = new() { m_Layer = layer } }).ToArray() };
Lift01NetworkData.Role[] Read(params NetSectionPrefab[] sections) => Lift01SectionRoles.Resolve(
    sections.Select(section => new NetSectionInfo { m_Section = section }).ToArray());
var side = Section(NetPieceLayer.Side, NetPieceLayer.Side);
var shoulder = Section(NetPieceLayer.Surface, NetPieceLayer.Bottom, NetPieceLayer.Top);
var wrapper = new NetSectionPrefab { m_SubSections = new[] { new NetSubSectionInfo { m_Section = side } } };
var empty = Section();
var mixed = new NetSectionPrefab { m_SubSections = new[] {
    new NetSubSectionInfo { m_Section = wrapper }, new NetSubSectionInfo { m_Section = shoulder } } };
var roles = Read(side, shoulder, wrapper, empty, mixed, side);
Check(roles.SequenceEqual(new[] { Lift01NetworkData.Role.Side, Lift01NetworkData.Role.Deck,
    Lift01NetworkData.Role.Side, Lift01NetworkData.Role.Deck, Lift01NetworkData.Role.Deck,
    Lift01NetworkData.Role.Side }));
Check(Read(Section(NetPieceLayer.Bottom))[0] == Lift01NetworkData.Role.Deck);
// Cyclic/missing external graphs must fail explicitly, not recurse or emit a guessed role.
foreach (var corrupt in new[] { new NetSectionPrefab(), new NetSectionPrefab() })
{
    corrupt.m_SubSections = new[] { new NetSubSectionInfo { m_Section = checks == 2 ? corrupt : null! } };
    var failed = false;
    try { Read(corrupt); } catch (InvalidOperationException) { failed = true; }
    Check(failed);
}
Check(side.m_Pieces.Length == 2 && wrapper.m_SubSections[0].m_Section == side);
Console.WriteLine($"PASS {checks} selected-layer role checks; native game composition remains unverified.");

namespace BridgeBuilder.Bridges
{
    internal static class Lift01NetworkData { internal enum Role { Side, Deck } }
}
namespace Game.Prefabs
{
    public enum NetPieceLayer { Surface, Bottom, Top, Side }
    public class NetPiecePrefab { public NetPieceLayer m_Layer; }
    public class NetPieceInfo { public NetPiecePrefab m_Piece = null!; }
    public class NetSubSectionInfo { public NetSectionPrefab m_Section = null!; }
    public class NetSectionInfo { public NetSectionPrefab m_Section = null!; }
    public class NetSectionPrefab
    {
        public NetPieceInfo[] m_Pieces = Array.Empty<NetPieceInfo>();
        public NetSubSectionInfo[] m_SubSections = Array.Empty<NetSubSectionInfo>();
    }
}
