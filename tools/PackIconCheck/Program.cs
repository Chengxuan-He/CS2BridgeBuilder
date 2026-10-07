using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;
static void Check(bool value,string why){if(!value)throw new Exception(why);}
var cid=new string('a',32);
var root=Path.Combine(Application.persistentDataPath,"ImportedData");
var canonical=Path.Combine(root,BridgeAssetPack.PrefabName,BridgeAssetPack.PrefabName+".Prefab");
var copy=Path.Combine(root,"b11111111-2222-3333-4444-555555555555_Dependencies",cid+".Prefab");
var other=Path.Combine(root,"Unrelated",cid+".Prefab");
var original="{\"$type\":\"Game.Prefabs.AssetPackPrefab, Game\",\"name\":\"BridgeBuilder Native Asset Pack\",\"components\":{\"$rcontent\":[{\"$type\":\"Game.Prefabs.UIObject, Game\",\"m_Icon\":\"Media/Placeholder.svg\",\"m_Priority\":0}]}}";
foreach(var p in new[]{canonical,copy,other}){Directory.CreateDirectory(Path.GetDirectoryName(p)!);File.WriteAllText(p,original);File.WriteAllText(p+".cid",cid);}
var svg=File.ReadAllBytes(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,"../../../../../assets/BridgeBuilderPack.svg")));
var icon="data:image/svg+xml;base64,"+Convert.ToBase64String(svg);
// Native CID resolution may select a snapshot rather than the canonical file.
var pack=new AssetPackPrefab{asset=new Asset{path=BridgeFileAccess.Native(copy),id=(cid,0)}};
Check(BridgePackIcon.Persist(pack,icon,out _),"Persistence failed");
Check(File.ReadAllText(canonical)==original.Replace("Media/Placeholder.svg",icon),"Changed fields other than icon");
Check(File.ReadAllBytes(copy).SequenceEqual(File.ReadAllBytes(canonical)),"CID snapshots differ");
Check(File.ReadAllText(other)==original,"Touched unowned copy");
Check(File.ReadAllText(copy+".cid")==cid,"CID changed");
Check(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(icon.Split(',')[1])).Contains("<svg"),"Not standalone SVG");
var stamp=File.GetLastWriteTimeUtc(copy);
Check(BridgePackIcon.Persist(pack,icon,out _)&&File.GetLastWriteTimeUtc(copy)==stamp,"Not idempotent");
Console.WriteLine("PASS embedded SVG, same-CID copies, unchanged other fields/CIDs, unowned exclusion and idempotence. Rendering requires in-game acceptance.");
