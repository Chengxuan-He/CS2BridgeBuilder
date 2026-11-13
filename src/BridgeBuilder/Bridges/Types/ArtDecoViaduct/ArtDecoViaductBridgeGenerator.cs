using System.Collections.Generic;
using System.Threading.Tasks;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
namespace BridgeBuilder.Bridges;
internal sealed class ArtDecoViaductBridgeGenerator : BridgeGeneratorBase
{
    internal ArtDecoViaductBridgeGenerator(ExportReport report, TowerFactory? towers) : base(report,towers) { }
    internal override string StyleId => "ArtDecoViaduct";
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,List<PrefabBase> created)
    {
        try
        {
            PrefabID Resolve(string guid)
            {
                if(!Colossal.IO.AssetDatabase.AssetDatabase.global.resources.prefabsMap.TryGetObject(guid,out var value))
                    throw new System.InvalidOperationException("Recorded Art Deco native unlock resource unavailable: "+guid);
                return ((PrefabBase)value).GetPrefabID();
            }
            var dependencies=new[]{"0068b29c98fe44141817b4474ac3b78a","dac41b9bc6a90d14c8594948b57efb5c",
                "1b76d7274a126b445b53435de0ec19da","d4d220e637b0a2743b798fced877190d",
                "a8db8a5bd3190c1438bef98c26cbe366","a0f52491aafbfc144aefee1cc756c479",
                "6a2dbb5b0d5ed814d975ec201667b644","bc7450fe8a28cbe4c85deca204fabe12",
                "cdd7e9c271d101c4aa206a821456fe4c","9e3cf90a9e10fc2488b0e288cbd15cf1",
                "9c06c8c6137c2674c9e5e648ee1420c5","da433ea9371e47e43bcb1e752aeeed4d"};
            var ids=new List<PrefabID>();foreach(var guid in dependencies)ids.Add(Resolve(guid));
            ids.Add(new PrefabID("StaticObjectPrefab","BXP Art Deco Pylon"));
            ids.Add(new PrefabID("NetSectionPrefab","BXP Art Deco Sidewalk 3.5"));
            return MovableUnlockInputs.Apply(target,_report,created,ids.ToArray(),
                new[]{Resolve("cb37f95b1977c234ebb7b1862b145f36")},System.Array.Empty<PrefabID>(),false);
        }
        catch(System.Exception error){_report.Failed(target.name,error);return false;}
    }
    protected override bool HasRecordedConstruction => true;
    protected override async Task ConstructRecordedAsync(NetGeometryPrefab target, BridgeStyleVariant variant,
        TowerFactory factory, float targetWidth, RoadEdge left, RoadEdge right)
    {
        await ArtDecoViaductConstruction.ApplyAsync(target,factory,targetWidth);
    }
}

