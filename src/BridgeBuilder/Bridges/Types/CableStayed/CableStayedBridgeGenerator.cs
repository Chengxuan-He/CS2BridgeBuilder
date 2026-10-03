using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected CableStayed style.</summary>
internal sealed class CableStayedBridgeGenerator : BridgeGeneratorBase
{
    internal CableStayedBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "CableStayed";
}
