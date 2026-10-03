using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected TrussArch03 style.</summary>
internal sealed class TrussArch03BridgeGenerator : BridgeGeneratorBase
{
    internal TrussArch03BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "TrussArch03";
}
