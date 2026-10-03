using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected TrussArch style.</summary>
internal sealed class TrussArchBridgeGenerator : BridgeGeneratorBase
{
    internal TrussArchBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "TrussArch";
}
