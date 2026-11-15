using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using System.Text.Json;
using System.Text;
var sandbox=Path.Combine(Path.GetTempPath(),"BBPortable-"+Guid.NewGuid());
var scratch=Path.Combine(sandbox,"game");
UnityEngine.Application.persistentDataPath=scratch;
var count=0;
void Check(bool value,string label){if(!value)throw new Exception(label);count++;}
foreach(var cause in new[]{"unlock","icon","pack","cleanup","metadata","legacy-component","short-v2-1","short-v2-2","short-v2-3","short-v2-4","short-v2-5","short-v2-6"}) {
 var owner="b"+Guid.NewGuid();
 var path=Path.Combine(scratch,"ImportedData",owner,"Prefabs","Bridge.Prefab");
 Directory.CreateDirectory(Path.GetDirectoryName(path)!);
 var metadata=new BridgeAssetInfo(owner,"Original bridge name","selected-road",null,"TrussArch01","2026-11-13T13:38:26Z");
 var source=new RoadPrefab{name=owner,asset=new PrefabAsset{path=path}};
 source.asset.id=(new FakeHash{value=Guid.NewGuid().ToString("N")},0);
 source.AddOrGetComponent<UIObject>().name=cause=="metadata"?"UIObject":BridgeAssetMetadata.Encode(metadata);
 source.GetComponent<UIObject>()!.m_Icon=cause=="icon"?"coui://bridgebuilder/old":"assetdb://global/original";
 source.AddOrGetComponent<Unlockable>().m_IgnoreDependencies=cause!="unlock";
 source.AddOrGetComponent<AssetPackItem>().m_Packs=cause=="pack"?[]:[BridgeAssetPack.Pack];
 if(cause=="cleanup")source.components.Add(new BridgeBuilder.Bridges.BridgeConstructionCost());
 BridgePrefabPersistence.Write(source);File.WriteAllText(path+".cid",source.asset.id.guid.ToString());
 if(cause=="legacy-component") {
  source.components.Add(new BridgeBuilder.Bridges.BridgeConstructionCost());
  source.GetComponent<UIObject>()!.name="UIObject";
  File.WriteAllText(path,JsonSerializer.Serialize(new {name=owner,components=new Dictionary<string,object?>{{"$rcontent",new[]{new Dictionary<string,object?>{
   {"$type","1|BridgeBuilder.Bridges.BridgeConstructionCost, BridgeBuilder"},
   {"m_BridgeDisplayName",metadata.DisplayName},{"m_BridgeUpperDeckId",metadata.UpperDeckId},
   {"m_BridgeLowerDeckId",metadata.LowerDeckId},{"m_BridgeStyleId",metadata.StyleId},
   {"m_BridgeCreatedUtc",metadata.CreatedUtc},{"m_BridgeCreationPending",false}}}}}}));
 }
 if(cause.StartsWith("short-v2-")) {
  var fields=int.Parse(cause.Substring("short-v2-".Length));
  var values=new[]{owner,metadata.DisplayName,metadata.UpperDeckId,metadata.LowerDeckId??"",metadata.StyleId,metadata.CreatedUtc}.Take(fields);
  source.GetComponent<UIObject>()!.name=BridgeAssetMetadata.Prefix+Convert.ToBase64String(Encoding.UTF8.GetBytes(string.Join("\n",values.Select(v=>Convert.ToBase64String(Encoding.UTF8.GetBytes(v))))));
  BridgePrefabPersistence.Write(source);
 }
 var baseline=File.ReadAllText(path);
 var historical=cause=="legacy-component"||cause.StartsWith("short-v2-");
 BridgeAssetInfo? recovered=null;
 if(historical) {
  Check(!BridgeAssetMetadata.TryRead(baseline,out _),cause+" normal reader rejects historical payload");
  Check(BridgeInspectionMetadata.TryReadHistorical(baseline,out recovered),cause+" inspection reads historical values");
 }
 var dependencies=new BridgeCreationDependencies();
 var paths=new HashSet<string>{path,path+".cid"};
 bool Run(NetGeometryPrefab input,out bool changed,out string error)=>BridgePortableMigration.Run(owner,new PrefabSystem(),Path.Combine(sandbox,"backup",owner),out changed,out error,[new(input,path)],dependencies,paths);
 var beforeRecoveryCheck=BridgePrefabPersistence.Saves;
 Check(!BridgePortableMigration.Run(owner,new PrefabSystem(),Path.Combine(sandbox,"backup",owner),out _,out var recoveryError,[new(source,path)],dependencies,paths,true)
  && recoveryError.Contains("cannot safely serialize") && BridgePrefabPersistence.Saves==beforeRecoveryCheck && File.ReadAllText(path)==baseline,
  cause+" recovered disk references are not overwritten by stale live conversion");
 Check(Run(source,out var changed,out var error)&&changed,cause+" first migration: "+error);
 var migrated=File.ReadAllText(path);
 Check(BridgeAssetMetadata.TryRead(migrated,out var saved),cause+" metadata saved");
 if(historical) Check(BridgeAssetMetadata.Encode(saved)==BridgeAssetMetadata.Encode(recovered!),cause+" historical known values preserved without inventing missing values");
 else if(cause!="metadata") Check(BridgeAssetMetadata.Encode(saved)==BridgeAssetMetadata.Encode(metadata),cause+" complete metadata preserved");
 Check(saved.PrefabName==owner,cause+" UUID preserved");
 Check(!migrated.Contains("BridgeConstructionCost"),cause+" migration output has no legacy component");
 Check(File.ReadAllText(path+".cid")==source.asset.id.guid.ToString(),cause+" CID preserved");
 Check(File.ReadAllText(Path.Combine(sandbox,"backup",owner,"Portable","ImportedData",owner,"Prefabs","Bridge.Prefab"))==baseline,cause+" original backed up");
 // Simulate the next native load from the just-written file, not the restored cached source.
 var next=BridgePrefabPersistence.Reload(source.asset);
 var saves=BridgePrefabPersistence.Saves;
 BridgeBuilder.Bridges.BridgeStyleCatalog.Absent=true;
 Check(Run(next,out changed,out error)&&!changed,cause+" second run no migration: "+error);
 BridgeBuilder.Bridges.BridgeStyleCatalog.Absent=false;
 Check(BridgePrefabPersistence.Saves==saves&&File.ReadAllText(path)==migrated,cause+" second run no writes");
 Check(next.GetComponent<Unlockable>()!.m_IgnoreDependencies,cause+" native unlock retained");
}
Console.WriteLine($"PASS {count} production portable migration assertions: current repairs, legacy component and short Data.v2 migration, current-reader rejection before migration, UUID/CID preservation and unchanged second cold reread. Native API/serialization test doubles; scratch={scratch}");
Directory.Delete(sandbox,true);
