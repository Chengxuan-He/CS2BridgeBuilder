using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Grand style.</summary>
internal sealed class GrandBridgeGenerator : BridgeGeneratorBase
{
    internal GrandBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Grand";
}
