using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected SuspensionGolden style.</summary>
internal sealed class SuspensionGoldenBridgeGenerator : BridgeGeneratorBase
{
    internal SuspensionGoldenBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "SuspensionGolden";
}
