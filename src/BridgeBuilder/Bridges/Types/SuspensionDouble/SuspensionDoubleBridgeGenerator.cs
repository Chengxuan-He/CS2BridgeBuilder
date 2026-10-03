using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected SuspensionDouble style.</summary>
internal sealed class SuspensionDoubleBridgeGenerator : BridgeGeneratorBase
{
    internal SuspensionDoubleBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "SuspensionDouble";
}
