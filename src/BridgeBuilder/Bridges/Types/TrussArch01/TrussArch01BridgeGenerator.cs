using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected TrussArch01 style.</summary>
internal sealed class TrussArch01BridgeGenerator : BridgeGeneratorBase
{
    internal TrussArch01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "TrussArch01";
}
