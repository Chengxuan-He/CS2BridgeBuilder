using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected GoldenGate style.</summary>
internal sealed class GoldenGateBridgeGenerator : BridgeGeneratorBase
{
    internal GoldenGateBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "GoldenGate";
}
