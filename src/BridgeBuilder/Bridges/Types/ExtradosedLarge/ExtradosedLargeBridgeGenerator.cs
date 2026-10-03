using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Generation entry for the stable player-selected ExtradosedLarge style.</summary>
internal sealed class ExtradosedLargeBridgeGenerator : BridgeGeneratorBase
{
    internal ExtradosedLargeBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "ExtradosedLarge";
}
