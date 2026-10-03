using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Extradosed03 style.</summary>
internal sealed class Extradosed03BridgeGenerator : BridgeGeneratorBase
{
    internal Extradosed03BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Extradosed03";
}
