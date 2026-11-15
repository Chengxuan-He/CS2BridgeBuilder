using BridgeBuilder.Runtime;
var game=Directory.CreateTempSubdirectory("BridgeUuid-").FullName;
var fixtureLinks=new List<string>();
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
    Save(Path.Combine(game,"ModsData","BridgeBuilder","DeletionPending",occupied[6]+".pending"));
    var fresh=NewId();var ids=new Queue<string>(occupied.Append(fresh));
    if(BridgeUuidReservation.Reserve(game,id=>id==occupied[7],ids.Dequeue)!=fresh || ids.Count!=0)
        throw new Exception("Residues and active ownership must retry");
    var many=Enumerable.Repeat(fresh,32).Append(NewId()).ToArray();
    if(Claim(many)!=many.Last())throw new Exception("Collisions must not exhaust after sixteen attempts");
    if(BridgeUuidReservation.TryClaim(Path.Combine(imported,fresh)))throw new Exception("Native EEXIST must retry");
    try { BridgeUuidReservation.TryClaim(Path.Combine(game,"missing-parent","child"));throw new Exception("Expected native IO failure"); }
    catch(IOException e) when(e.InnerException is System.ComponentModel.Win32Exception) { }
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
    if(OperatingSystem.IsWindows())
    {
        void Junction(string link,string target)
        {
            string Q(string x)=>"'"+x.Replace("'","''")+"'";
            var script="$ErrorActionPreference='Stop'; New-Item -ItemType Junction -Path "+Q(link)+" -Target "+Q(target)+" | Out-Null";
            var start=new System.Diagnostics.ProcessStartInfo("powershell.exe") {UseShellExecute=false,CreateNoWindow=true};
            start.ArgumentList.Add("-NoProfile");start.ArgumentList.Add("-NonInteractive");start.ArgumentList.Add("-EncodedCommand");
            start.ArgumentList.Add(Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script)));
            using var process=System.Diagnostics.Process.Start(start)!;process.WaitForExit();
            if(process.ExitCode!=0)throw new Exception("Junction fixture creation failed");
            fixtureLinks.Add(link);
        }
        var physical=Directory.CreateDirectory(Path.Combine(game,"physical-root")).FullName;
        var alias=Path.Combine(game,"mapped-root");Junction(alias,physical);
        var mappedOwner=BridgeUuidReservation.Reserve(alias,_=>false,NewId);
        if(!Directory.Exists(Path.Combine(physical,"ImportedData",mappedOwner)))throw new Exception("Mapped game root rejected");
        BridgeDeletionIntent.Accept(alias,mappedOwner);BridgeDeletionIntent.Cleanup(alias,mappedOwner);
        var mappedGame=Directory.CreateDirectory(Path.Combine(game,"mapped-imported-game")).FullName;
        var physicalImported=Directory.CreateDirectory(Path.Combine(game,"physical-imported")).FullName;
        Junction(Path.Combine(mappedGame,"ImportedData"),physicalImported);
        var mappedLedger=Directory.CreateDirectory(Path.Combine(game,"physical-ledger")).FullName;
        Directory.CreateDirectory(Path.GetDirectoryName(BridgeDeletionIntent.Ledger(mappedGame))!);
        Junction(BridgeDeletionIntent.Ledger(mappedGame),mappedLedger);
        var mapped=BridgeUuidReservation.Reserve(mappedGame,_=>false,NewId);
        var unrelated=Directory.CreateDirectory(Path.Combine(game,"external-target")).FullName;
        File.WriteAllText(Path.Combine(unrelated,"sentinel"),"never delete this target");
        Junction(Path.Combine(physicalImported,mapped,"nested-link"),unrelated);
        BridgeDeletionIntent.Accept(mappedGame,mapped);BridgeDeletionIntent.Cleanup(mappedGame,mapped);
        if(Directory.Exists(unrelated)||Directory.Exists(Path.Combine(physicalImported,mapped)))throw new Exception("Nested mapped payload remains");
        Directory.CreateDirectory(unrelated);File.WriteAllText(Path.Combine(unrelated,"sentinel"),"owner payload");
        var linkedOwner=NewId();Junction(Path.Combine(physicalImported,linkedOwner),unrelated);
        BridgeDeletionIntent.Accept(mappedGame,linkedOwner);
        if(!File.Exists(Path.Combine(unrelated,BridgeDeletionIntent.Marker)))throw new Exception("Owner mapped marker missing");
        BridgeDeletionIntent.Cleanup(mappedGame,linkedOwner);
        if(Directory.Exists(unrelated)||Directory.Exists(Path.Combine(physicalImported,linkedOwner)))throw new Exception("Owner mapped payload remains");
        Console.WriteLine("PASS actual Windows game-root, ImportedData and ledger junctions; owner/nested mapped payloads removed, parent mappings retained");
    }
    Console.WriteLine("PASS fresh allocation, active and on-disk collisions, more than sixteen retries, native EEXIST/non-collision failure, real concurrent claims and existing bytes.");
}
finally { foreach(var link in fixtureLinks.AsEnumerable().Reverse()) { try { Directory.Delete(link,false); } catch(DirectoryNotFoundException) {} } Directory.Delete(game,true); }
