using System.Collections.Generic;
using System.Threading.Tasks;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal sealed class ArcPedestrianBridgeGenerator : BridgeGeneratorBase
{
    internal ArcPedestrianBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "ArcPedestrian";
    protected override bool HasRecordedConstruction => true;
    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant,
        TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        await ArcPedestrianConstruction.ApplyAsync(target, factory, targetWidth);
    }
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,
        List<PrefabBase> created) => MovableUnlockInputs.Apply(target, _report, created,
            new[] { new PrefabID("NetSectionPrefab", "Pavement Path Side Section 0"),
                new PrefabID("NetSectionPrefab", "Pavement Path Section 3"),
                new PrefabID("AggregateNetPrefab", "Pathway"), new PrefabID("ServicePrefab", "Landscaping"),
                new PrefabID("UIAssetCategoryPrefab", "Pathways") },
            new[] { new PrefabID("FeaturePrefab", "Pathways") }, System.Array.Empty<PrefabID>(), false);
}
