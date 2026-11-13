using System.Collections.Generic;
using System.Threading.Tasks;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
namespace BridgeBuilder.Bridges;
internal sealed class TrussViaductBridgeGenerator : BridgeGeneratorBase
{
    internal TrussViaductBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report,towers) {}
    internal override string StyleId => "TrussViaduct";
    protected override bool HasRecordedConstruction => true;
    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant, TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    { await TrussViaductConstruction.ApplyAsync(target,factory,targetWidth); }
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,List<PrefabBase> created)
        {
        Colossal.IO.AssetDatabase.AssetDatabase.global.resources.prefabsMap.TryGetObject("971bf1cf514571340be61c80b7c703d7",out var value);
        var prerequisite = (PrefabBase)value;
        return MovableUnlockInputs.Apply(target,_report,created,System.Array.Empty<PrefabID>(),new[]{prerequisite.GetPrefabID()},System.Array.Empty<PrefabID>(),false);
    }
}



