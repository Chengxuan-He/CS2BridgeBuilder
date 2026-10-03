using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Extradosed02 style.</summary>
internal sealed class Extradosed02BridgeGenerator : BridgeGeneratorBase
{
    internal Extradosed02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Extradosed02";
}
