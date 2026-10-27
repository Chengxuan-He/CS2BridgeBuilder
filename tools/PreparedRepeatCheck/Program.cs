using System.Text.Json;
using BridgeBuilder.Bridges;
using BridgeBuilder.Runtime;
using UnityEngine;

bool structure=args.Length>0 && args[0]=="--structure";
if(structure)args=args.Skip(1).ToArray();
if(args.Length<2)throw new ArgumentException("PreparedRepeatCheck [--structure] <Materials> <inventory.json> ...");
var variant=structure?"Structure":"Repeat";
var styles=new HashSet<string>();var keys=new HashSet<(string,string)>();
var cids=new Dictionary<string,string>();int graphs=0;
foreach(var path in args.Skip(1))
{
    using var json=JsonDocument.Parse(File.ReadAllBytes(path));
    if(json.RootElement.GetProperty("recipe").GetString()!=(structure?MovableStructureKey.Recipe:MovableRepeatKey.Recipe))throw new Exception("Wrong recipe: "+path);
    foreach(var u in json.RootElement.GetProperty("units").EnumerateArray())
    {
        Vector2 V(JsonElement parent,string name){var v=parent.GetProperty(name);return new(v[0].GetSingle(),v[1].GetSingle());}
        var style=u.GetProperty("style").GetString()!;styles.Add(style);
        string key;
        if(structure)
        {
            var b=u.GetProperty("bounds").EnumerateArray().Select(v=>v.GetSingle()).ToArray();
            key=MovableStructureKey.For(u.GetProperty("sourceDigest").GetString()!,u.GetProperty("triangle").GetBoolean(),
                new Vector4(b[0],b[1],b[2],b[3]),V(u,"a"),V(u,"b"),V(u,"c"));
        }
        else
        {
        var bands=u.GetProperty("bands").ValueKind==JsonValueKind.Null?null:u.GetProperty("bands").EnumerateArray()
            .Select(b=>new MovableExistingRoadSurface.RepeatBand(b.GetProperty("start").GetSingle(),b.GetProperty("end").GetSingle(),V(b,"min"),V(b,"max"),b.GetProperty("repeatY").GetSingle())).ToArray();
        key=MovableRepeatKey.For(u.GetProperty("sourceDigest").GetString()!,V(u,"min"),V(u,"max"),u.GetProperty("cutout").GetBoolean(),bands,u.GetProperty("rows").GetBoolean());
        }
        if(u.TryGetProperty("key",out var declared) && !string.IsNullOrEmpty(declared.GetString()) && declared.GetString()!=key)throw new Exception("Offline/runtime key mismatch: "+path);
        if(!keys.Add((style,key)))throw new Exception("Duplicate inventory unit: "+style+"/"+key);
        var inputs=BridgePreparedMaterialFiles.Read(Path.Combine(args[0],style),variant+"/"+key,variant+".Surface",
            bytes=>BridgeSurfaceReferences.Read(bytes,(_,_)=>throw new Exception("Unexpected VT binding")),cid=>throw new Exception("Missing style-local dependency: "+cid));
        if(structure?inputs.Count<2:inputs.Count!=5)throw new Exception("Incomplete sampled graph: "+style+"/"+key);
        foreach(var input in inputs)
        {
            if(cids.TryGetValue(input.Cid,out var hash) && hash!=input.Digest)throw new Exception("Conflicting CID: "+input.Cid);
            cids[input.Cid]=input.Digest;
        }
        graphs++;
    }
}
var packaged=Directory.GetFiles(args[0],variant+".Surface",SearchOption.AllDirectories);
if(packaged.Length!=graphs)throw new Exception($"Inventory covers {graphs} of {packaged.Length} packaged repeats.");
if(styles.Count!=10)throw new Exception("Expected all ten movable styles.");
Console.WriteLine($"PASS prepared {variant} integration: {styles.Count} styles, {graphs} production recipe keys, {cids.Count} unique CIDs; complete style-local graphs, identical CID bytes, exact package coverage.");
