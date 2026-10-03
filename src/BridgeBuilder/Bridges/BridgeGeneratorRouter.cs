using System;
using System.Collections.Generic;
using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Bridges;

/// <summary>Explicit routing by persisted style ID, never by localized text or a fallback family.</summary>
internal static class BridgeGeneratorRouter
{
    private static readonly IReadOnlyDictionary<string, Func<ExportReport, TowerFactory?, BridgeGeneratorBase>>
        Routes = new Dictionary<string, Func<ExportReport, TowerFactory?, BridgeGeneratorBase>>(StringComparer.Ordinal)
    {
        ["WoodenCovered"] = (report, towers) => new WoodenCoveredBridgeGenerator(report, towers),
        ["CoveredWood"] = (report, towers) => new CoveredWoodBridgeGenerator(report, towers),
        ["Suspension"] = (report, towers) => new SuspensionBridgeGenerator(report, towers),
        ["SuspensionDouble"] = (report, towers) => new SuspensionDoubleBridgeGenerator(report, towers),
        ["Suspension01"] = (report, towers) => new Suspension01BridgeGenerator(report, towers),
        ["Suspension02"] = (report, towers) => new Suspension02BridgeGenerator(report, towers),
        ["SuspensionGolden"] = (report, towers) => new SuspensionGoldenBridgeGenerator(report, towers),
        ["GoldenGate"] = (report, towers) => new GoldenGateBridgeGenerator(report, towers),
        ["GoldenGateDouble"] = (report, towers) => new GoldenGateDoubleBridgeGenerator(report, towers),
        ["Extradosed01"] = (report, towers) => new Extradosed01BridgeGenerator(report, towers),
        ["Extradosed02"] = (report, towers) => new Extradosed02BridgeGenerator(report, towers),
        ["Extradosed03"] = (report, towers) => new Extradosed03BridgeGenerator(report, towers),
        ["ExtradosedLarge"] = (report, towers) => new ExtradosedLargeBridgeGenerator(report, towers),
        ["CableStayed"] = (report, towers) => new CableStayedBridgeGenerator(report, towers),
        ["TrussArch"] = (report, towers) => new TrussArchBridgeGenerator(report, towers),
        ["TrussArch01"] = (report, towers) => new TrussArch01BridgeGenerator(report, towers),
        ["TrussArch02"] = (report, towers) => new TrussArch02BridgeGenerator(report, towers),
        ["TrussArch03"] = (report, towers) => new TrussArch03BridgeGenerator(report, towers),
        ["TiedArch"] = (report, towers) => new TiedArchBridgeGenerator(report, towers),
        ["Grand"] = (report, towers) => new GrandBridgeGenerator(report, towers),
    };

    internal static BridgeGeneratorBase? Create(string? styleId, ExportReport report, TowerFactory? towers)
    {
        if (styleId != null && Routes.TryGetValue(styleId, out var create)) return create(report, towers);
        report.Failed(styleId ?? "<none>", new NotSupportedException(
            $"No bridge generator is registered for selected style '{styleId}'."));
        return null;
    }
}
