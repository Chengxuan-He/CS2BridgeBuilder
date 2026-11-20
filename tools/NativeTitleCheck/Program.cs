using BridgeBuilder.Systems;
using Game.Prefabs;
using Game.SceneFlow;
using CS2Mods.Shared.Infrastructure;
static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
const string owner="b11111111-2222-3333-4444-555555555555";
var system=new BridgeGenerationSystem(); var locale=GameManager.instance.localizationManager;
for(int i=0;i<3;i++)system.Prefabs.Add(new NetGeometryPrefab{name=owner+new[]{"","_Upper","_Lower"}[i],Entity=i,asset=new(){path="ImportedData/"+owner+"/Prefabs/Bridge.Prefab"}});
system.Prefabs.Add(new NetGeometryPrefab{name=owner+" piece",Entity=3,asset=new(){path=owner}});
system.Prefabs.Add(new NetGeometryPrefab{name=owner,Entity=4,asset=new(){path="Unrelated/source.Prefab"}});
system.Prefabs.Add(new NetGeometryPrefab{name="ordinary road",Entity=5,asset=new(){path=owner}});
system.Prefabs.Add(new NetGeometryPrefab{name=owner,Entity=6,asset=new(){path=owner}});system.EntityManager.Removed.Add(6);
system.Prefabs.Add(new NetGeometryPrefab{name=owner,Entity=7,asset=new(){path=owner},Live=false});
var cache=new Dictionary<int,string>();
// Reproduce inspected FindIt GetAssetName: native key, dictionary, identity fallback.
void Index(int id){var p=system.Prefabs[id];cache[id]=locale.Dictionary.GetValueOrDefault("Assets.NAME["+p.name+"]",p.name);}
void Consume(){foreach(var id in system.EntityManager.Updated)Index(id);system.EntityManager.Updated.Clear();}
system.Start();system.Tick();Check(PrefabCatalog.Reads==0,"Idle scanned prefabs");
Index(0);Check(cache[0]==owner,"Fixture did not reproduce initial UUID cache");
locale.Publish(owner,"Published bridge");system.Tick();Check(system.EntityManager.Updated.SetEquals(new[]{0,1,2}),"Notification ownership scope changed");Consume();Check(cache[0]=="Published bridge","New publication retained cached UUID");
locale.Publish(owner,"Renamed bridge");locale.Publish(owner,"Latest rename");int reads=PrefabCatalog.Reads;system.Tick();Check(PrefabCatalog.Reads==reads+1,"Events did not coalesce");Consume();Check(cache[0]=="Latest rename","Rename not observed");
locale.Publish(owner,"Localized bridge");system.Tick();Consume();Check(cache[0]=="Localized bridge","Locale change not observed");
cache[0]=owner;system.ColdLoad();system.Tick();Consume();Check(cache[0]=="Localized bridge","Cold load retained stale cache");
reads=PrefabCatalog.Reads;for(int i=0;i<100;i++)system.Tick();Check(PrefabCatalog.Reads==reads,"Idle repeated scan");
// The same path runs with no index consumer installed and changes no identities/files.
locale.Publish(owner,"No optional consumer");system.Tick();Check(system.Prefabs[0].name==owner&&system.Prefabs[0].Entity==0,"Identity changed");
system.Stop();Check(locale.Listeners==0,"Locale handler leaked");locale.Publish(owner,"Stopped");reads=PrefabCatalog.Reads;system.Tick();Check(PrefabCatalog.Reads==reads,"Stopped handler scheduled work");
Console.WriteLine("PASS actual notification implementation: publication, coalesced rename, locale, cold load, absent optional consumer, owner scope, stable identity, idle and lifecycle cleanup.");
