using System.Security.Cryptography;

string root=Path.Combine(Path.GetTempPath(),"BridgeBuilder-PreparedBakeStore-"+Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
int checks=0;
void Require(bool condition,string message){if(!condition)throw new Exception(message);checks++;}
string Key(string value)=>PreparedBakeStore.Key(w=>w.Write(value));
byte[] Data()=>new byte[]{1,2,3,0,255,64};
int invoked=0;
byte[] Produce(){invoked++;return Data();}
void Reject(Action action,string message)
{
    int before=invoked;
    try{action();throw new Exception("Accepted "+message);}
    catch(InvalidDataException){Require(invoked==before,"Rebaked "+message);}
}
try
{
    var first=new PreparedBakeStore(root,"test-recipe");
    var a=first.Get(Key("source-1"),".Texture",Produce);
    Require(invoked==1 && first.Created==1 && first.Reused==0,"First preparation did not execute exactly once.");
    var second=new PreparedBakeStore(root,"test-recipe");
    var b=second.Get(Key("source-1"),".Texture",Produce);
    Require(invoked==1 && second.Created==0 && second.Reused==1,"Restart repeated preparation.");
    Require(a.Cid==b.Cid && a.Bytes.SequenceEqual(b.Bytes),"Restart changed payload or CID.");
    var c=second.Get(Key("source-2"),".Texture",Produce);
    Require(invoked==2 && a.Cid==c.Cid,"Equal output bytes acquired different identities.");
    Require(Key("source-1")!=Key("source-2"),"Different inputs collided.");
    Require(PreparedBakeStore.Key(w=>w.Write(0f))!=PreparedBakeStore.Key(w=>w.Write(-0f)),"Float input bits were normalized.");

    string payload=Path.Combine(root,Key("source-1")+".Texture");
    var original=File.ReadAllBytes(payload);File.WriteAllBytes(payload,new byte[]{9});
    Reject(()=>second.Get(Key("source-1"),".Texture",Produce),"corrupted payload");
    Require(File.ReadAllBytes(payload).SequenceEqual(new byte[]{9}),"Corruption was overwritten.");
    File.WriteAllBytes(payload,original);File.Delete(payload+".sha256");
    Reject(()=>second.Get(Key("source-1"),".Texture",Produce),"missing receipt");
    File.Delete(payload);File.WriteAllText(payload+".sha256","missing-payload");
    Reject(()=>second.Get(Key("source-1"),".Texture",Produce),"missing payload");
    Reject(()=>second.Get(Key("empty"),".Surface",()=>Array.Empty<byte>()),"empty conversion result");
    Require(!Directory.EnumerateFiles(root,Key("empty")+"*").Any(),"Empty producer left a product.");
    try{second.Get(Key("throws"),".Surface",()=>throw new IOException("producer failed"));}
    catch(IOException){checks++;}
    Require(!Directory.EnumerateFiles(root,Key("throws")+"*").Any(),"Failed producer left a product.");
    Require(!Directory.EnumerateFiles(root,"*.tmp").Any(),"Preparation leaked temporary files.");

    string sourceRoot=Path.Combine(root,"read-only"),aliasRoot=Path.Combine(root,"aliases");
    var sourceStore=new PreparedBakeStore(sourceRoot,"test-recipe");
    string oldKey=Key("old-control"),otherKey=Key("other-control"),canonical=Key("constant-control");
    var old=sourceStore.Get(oldKey,".Texture",Produce);
    sourceStore.Get(otherKey,".Texture",Produce);
    var sourceFiles=Directory.GetFiles(sourceRoot).ToDictionary(p=>p,p=>File.ReadAllBytes(p));
    var aliases=new PreparedBakeStore(aliasRoot,"test-recipe");
    int beforeAlias=invoked;
    var adopted=aliases.GetEquivalent(canonical,oldKey,".Texture",new[]{sourceRoot},Produce);
    var restarted=new PreparedBakeStore(aliasRoot,"test-recipe");
    var again=restarted.GetEquivalent(canonical,otherKey,".Texture",new[]{sourceRoot},Produce);
    Require(invoked==beforeAlias && aliases.Created==0 && restarted.Created==0,"Alias adoption invoked encoding.");
    Require(old.Cid==adopted.Cid && old.Cid==again.Cid && old.Bytes.SequenceEqual(again.Bytes),"Alias changed bytes or CID.");
    Require(Directory.GetFiles(aliasRoot).Length==1 && Directory.GetFiles(aliasRoot,"*.alias").Length==1,"Alias copied a texture payload.");
    foreach(var file in sourceFiles)Require(File.ReadAllBytes(file.Key).SequenceEqual(file.Value),"Read-only product changed.");

    string aliasPath=Path.Combine(aliasRoot,canonical+".Texture.alias"),validAlias=File.ReadAllText(aliasPath);
    void BadAlias(string value,string message){File.WriteAllText(aliasPath,value);Reject(()=>restarted.GetEquivalent(canonical,otherKey,".Texture",new[]{sourceRoot},Produce),message);File.WriteAllText(aliasPath,validAlias);}
    BadAlias(canonical+"\n"+new string('0',64),"self alias");
    BadAlias(Key("absent")+"\n"+new string('0',64),"absent alias target");
    BadAlias("../outside\n"+new string('0',64),"alias traversal");
    BadAlias(oldKey+"\n"+new string('0',64),"incorrect alias checksum");
    string chain=Key("chain");File.WriteAllText(Path.Combine(aliasRoot,chain+".Texture.alias"),validAlias);
    BadAlias(chain+"\n"+validAlias.Split('\n')[1],"recursive alias");
    string conflicting=Key("conflicting");sourceStore.Get(conflicting,".Texture",()=>new byte[]{8});
    Reject(()=>restarted.GetEquivalent(canonical,conflicting,".Texture",new[]{sourceRoot},Produce),"conflicting equivalent legacy");
    File.WriteAllText(Path.Combine(sourceRoot,oldKey+".Texture.sha256"),"broken");
    Reject(()=>restarted.GetEquivalent(canonical,otherKey,".Texture",new[]{sourceRoot},Produce),"corrupt alias target receipt");
    File.WriteAllBytes(Path.Combine(sourceRoot,oldKey+".Texture.sha256"),sourceFiles[Path.Combine(sourceRoot,oldKey+".Texture.sha256")]);
    var freshKey=Key("new-constant");int beforeFresh=invoked;
    restarted.GetEquivalent(freshKey,Key("unknown-legacy"),".Texture",new[]{sourceRoot},Produce);
    restarted.GetEquivalent(freshKey,Key("unknown-legacy"),".Texture",new[]{sourceRoot},Produce);
    Require(invoked==beforeFresh+1,"New constant was not encoded exactly once.");
    Require(!Directory.EnumerateFiles(root,"*.tmp",SearchOption.AllDirectories).Any(),"Alias creation leaked temporary files.");

    if(args.Length==2)
    {
        Dictionary<string,string> Files(string directory)=>Directory.EnumerateFiles(Path.GetFullPath(directory),"*",SearchOption.AllDirectories)
            .ToDictionary(p=>Path.GetRelativePath(directory,p).Replace('\\','/'),p=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p))));
        var before=Files(args[0]);var after=Files(args[1]);
        Require(before.Count>0 && before.Count==after.Count,"Native run file inventories differ or are empty.");
        foreach(var file in before)Require(after.TryGetValue(file.Key,out var hash) && hash==file.Value,"Native rerun differs: "+file.Key);
        Console.WriteLine($"PASS native prepared-run equality: files={before.Count}; all payloads and CID sidecars byte-identical.");
    }
    else if(args.Length!=0)throw new ArgumentException("Supply both native preparation output roots, or neither.");
    Console.WriteLine($"PASS prepared-product lifecycle: checks={checks}; restart reuse, exact identities, failure preservation and temporary cleanup.");
}
finally
{
    // Only the unique directory created by this invocation is owned by this check.
    Directory.Delete(root,true);
}
