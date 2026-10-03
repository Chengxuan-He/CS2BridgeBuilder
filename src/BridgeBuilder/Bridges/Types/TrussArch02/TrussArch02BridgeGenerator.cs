using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected TrussArch02 style.</summary>
internal sealed class TrussArch02BridgeGenerator : BridgeGeneratorBase
{
    internal TrussArch02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "TrussArch02";

    // Keep the measured inner-footway and outer-visible-deck envelopes separate.
    protected override bool WidthFollowsSidewalks => true;
}
