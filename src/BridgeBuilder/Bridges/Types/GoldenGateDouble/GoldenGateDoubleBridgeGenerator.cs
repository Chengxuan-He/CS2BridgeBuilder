using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected GoldenGateDouble style.</summary>
internal sealed class GoldenGateDoubleBridgeGenerator : BridgeGeneratorBase
{
    internal GoldenGateDoubleBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "GoldenGateDouble";
}
