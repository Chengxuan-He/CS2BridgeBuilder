using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System.Text.Json;
var scratch=Path.Combine(Path.GetTempPath(),"BBPortable-"+Guid.NewGuid());
UnityEngine.Application.persistentDataPath=scratch;
var count=0;
void Check(bool value,string label){if(!value)throw new Exception(label);count++;}
foreach(var cause in new[]{"unlock","icon","pack","cleanup","metadata"}) {
 var owner="b"+Guid.NewGuid();
 var path=Path.Combine(scratch,"ImportedData",owner,"Prefabs","Bridge.Prefab");
 Directory.CreateDirectory(Path.GetDirectoryName(path)!);
 var metadata=new BridgeAssetInfo(owner,"GO original name","selected-road",null,"GOViaduct","2026-11-13T13:38:26Z");
 var source=new RoadPrefab{name=owner,asset=new PrefabAsset{path=path}};
 source.asset.id=(new FakeHash{value=Guid.NewGuid().ToString("N")},0);
 source.AddOrGetComponent<UIObject>().name=cause=="metadata"?"UIObject":BridgeAssetMetadata.Encode(metadata);
 source.GetComponent<UIObject>()!.m_Icon=cause=="icon"?"coui://bridgebuilder/old":"assetdb://global/original";
 source.AddOrGetComponent<Unlockable>().m_IgnoreDependencies=cause!="unlock";
 source.AddOrGetComponent<AssetPackItem>().m_Packs=cause=="pack"?[]:[BridgeAssetPack.Pack];
 if(cause=="cleanup")source.components.Add(new BridgeBuilder.Bridges.BridgeConstructionCost());
 BridgePrefabPersistence.Write(source);File.WriteAllText(path+".cid",source.asset.id.guid.ToString());
 var baseline=File.ReadAllText(path);
 var dependencies=new BridgeCreationDependencies();
 var paths=new HashSet<string>{path,path+".cid"};
 bool Run(NetGeometryPrefab input,out bool changed,out string error)=>BridgePortableMigration.Run(owner,new PrefabSystem(),Path.Combine(scratch,"backup",owner),out changed,out error,[new(input,path)],dependencies,paths);
 Check(Run(source,out var changed,out var error)&&changed,cause+" first migration: "+error);
 var migrated=File.ReadAllText(path);
 Check(BridgeAssetMetadata.TryRead(migrated,out var saved),cause+" metadata saved");
 if(cause!="metadata") Check(BridgeAssetMetadata.Encode(saved)==BridgeAssetMetadata.Encode(metadata),cause+" complete metadata preserved");
 Check(File.ReadAllText(path+".cid")==source.asset.id.guid.ToString(),cause+" CID preserved");
 Check(File.ReadAllText(Path.Combine(scratch,"backup",owner,"Portable","ImportedData",owner,"Prefabs","Bridge.Prefab"))==baseline,cause+" original backed up");
 // Simulate the next native load from the just-written file, not the restored cached source.
 var next=BridgePrefabPersistence.Reload(source.asset);
 var saves=BridgePrefabPersistence.Saves;
 BridgeBuilder.Bridges.BridgeStyleCatalog.Absent=true;
 Check(Run(next,out changed,out error)&&!changed,cause+" second run no migration: "+error);
 BridgeBuilder.Bridges.BridgeStyleCatalog.Absent=false;
 Check(BridgePrefabPersistence.Saves==saves&&File.ReadAllText(path)==migrated,cause+" second run no writes");
 Check(next.GetComponent<Unlockable>()!.m_IgnoreDependencies,cause+" native unlock retained");
}
Console.WriteLine($"PASS {count} production portable migration assertions: unlock/icon/pack/cleanup clone preservation, missing metadata completion, cold-reload reruns, unchanged CID and originals. Native API test doubles; scratch={scratch}");
