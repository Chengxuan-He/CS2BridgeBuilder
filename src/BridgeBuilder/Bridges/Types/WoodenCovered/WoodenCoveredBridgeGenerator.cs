using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected WoodenCovered style.</summary>
internal sealed class WoodenCoveredBridgeGenerator : BridgeGeneratorBase
{
    internal WoodenCoveredBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "WoodenCovered";
}
