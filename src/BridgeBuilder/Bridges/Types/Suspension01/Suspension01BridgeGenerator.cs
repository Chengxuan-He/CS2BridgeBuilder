using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Suspension01 style.</summary>
internal sealed class Suspension01BridgeGenerator : BridgeGeneratorBase
{
    internal Suspension01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Suspension01";
}
