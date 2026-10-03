using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected Suspension02 style.</summary>
internal sealed class Suspension02BridgeGenerator : BridgeGeneratorBase
{
    internal Suspension02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Suspension02";

    // This prototype owns the lower network, but every tower follows the upper deck's width.
    internal override bool StructureFollowsUpperAuxiliary => true;
    protected override bool AllTowersUseReferenceDeck => true;
}
