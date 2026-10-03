using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected TiedArch style.</summary>
internal sealed class TiedArchBridgeGenerator : BridgeGeneratorBase
{
    internal TiedArchBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "TiedArch";
}
