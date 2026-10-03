using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Extradosed01 style.</summary>
internal sealed class Extradosed01BridgeGenerator : BridgeGeneratorBase
{
    internal Extradosed01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Extradosed01";
}
