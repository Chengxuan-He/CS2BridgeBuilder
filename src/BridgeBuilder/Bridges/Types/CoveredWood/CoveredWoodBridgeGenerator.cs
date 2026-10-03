using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected CoveredWood style.</summary>
internal sealed class CoveredWoodBridgeGenerator : BridgeGeneratorBase
{
    internal CoveredWoodBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "CoveredWood";
}
