using BridgeBuilder.Runtime;
var game=Directory.CreateTempSubdirectory("BridgeUuid-").FullName;
string NewId()=>BridgeAssetInfo.NewPrefabName();
string Claim(params string[] ids)
{
    var choices=new Queue<string>(ids);
    return BridgeUuidReservation.Reserve(game,_=>false,choices.Dequeue) ?? throw new Exception("Claim failed");
}
try
{
    var imported=Directory.CreateDirectory(Path.Combine(game,"ImportedData")).FullName;
    var occupied=Enumerable.Range(0,8).Select(_=>NewId()).ToArray();
    var expected=new Dictionary<string,byte[]>();
    void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        expected[path]=Guid.NewGuid().ToByteArray();File.WriteAllBytes(path,expected[path]);
    }
    Save(Path.Combine(imported,occupied[0],"Prefabs","Bridge.Prefab"));
    Directory.CreateDirectory(Path.Combine(imported,occupied[1]));
    Save(Path.Combine(imported,occupied[2]));
    Save(Path.Combine(imported,occupied[3]+" Native Piece","irrelevant"));
    Save(Path.Combine(game,"BridgeBuilder",occupied[4]+" Column.Geometry"));
    Save(Path.Combine(game,"StreamingData~","VT",occupied[5]+".MidMips"));
    Save(Path.Combine(game,"ModsData","BridgeBuilder","PendingRemoval",occupied[6]+".ticket"));
    var fresh=NewId();var ids=new Queue<string>(occupied.Append(fresh));
    if(BridgeUuidReservation.Reserve(game,id=>id==occupied[7],ids.Dequeue)!=fresh || ids.Count!=0)
        throw new Exception("Residues and active ownership must retry");
    if(BridgeUuidReservation.Reserve(game,_=>false,()=>fresh)!=null || Directory.EnumerateFileSystemEntries(Path.Combine(imported,fresh)).Any())
        throw new Exception("Exhausted claim changed owner");
    var winner=NewId();var fallback=NewId();using var barrier=new Barrier(2);
    string Race()
    {
        int attempt=0;
        return BridgeUuidReservation.Reserve(game,_=>false,()=>{
            if(++attempt!=1)return fallback;
            if(!barrier.SignalAndWait(TimeSpan.FromSeconds(10)))throw new TimeoutException();
            return winner;
        }) ?? throw new Exception("Concurrent contender lost its retry");
    }
    var results=await Task.WhenAll(Task.Run(Race),Task.Run(Race));
    if(!results.ToHashSet().SetEquals(new[]{winner,fallback}))throw new Exception("Double claim");
    // A creator that arrives during the identity check must keep its payload.
    var external=NewId();var recovery=NewId();var seq=new Queue<string>(new[]{external,recovery});
    if(BridgeUuidReservation.Reserve(game,id=>{if(id==external)Save(Path.Combine(imported,id,"payload"));return false;},seq.Dequeue)!=recovery)
        throw new Exception("Existing external owner overwritten");
    if(Claim(NewId()).Length!=37 || expected.Any(pair=>!File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value)))
        throw new Exception("Existing bytes changed");
    Console.WriteLine("PASS fresh allocation, active and on-disk collisions, bounded exhaustion, real concurrent claims and existing bytes.");
}
finally { Directory.Delete(game,true); }
