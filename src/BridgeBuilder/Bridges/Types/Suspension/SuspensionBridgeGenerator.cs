using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Suspension style.</summary>
internal sealed class SuspensionBridgeGenerator : BridgeGeneratorBase
{
    internal SuspensionBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Suspension";
}
