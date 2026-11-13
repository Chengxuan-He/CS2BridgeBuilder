using BridgeBuilder.Bridges;
using System.Text;
using System.Reflection;

var output = args.Length == 0 ? "artifacts/shader-variable" : args[0];
Directory.CreateDirectory(output);
foreach (var name in new[]{"BoneDecal","WorldLayeredPbr"})
{
    var path="assets/MaterialPrograms/"+name+".bbs1";
    using var input=File.OpenRead(path);
    if(!MovableCustomShaderProgram.TryCompilePackage(input,out var actual,out var error))throw new Exception(name+": "+error);
    File.WriteAllBytes(Path.Combine(output,name+"-actual.bundle"),actual);
    using var reader=new BinaryReader(File.OpenRead(path));reader.ReadUInt32();int version=reader.ReadInt32();
    byte[] Bytes()=>reader.ReadBytes(reader.ReadInt32());
    var seed=Bytes();int count=reader.ReadInt32();var slots=new List<MovableShaderContainer.Replacement>();
    var sources=new List<(string Source,string Entry,string Profile,int Copies)>();
    for(int i=0;i<count;i++)
    {
        string source=Encoding.UTF8.GetString(Bytes()),entry="BBNetworkVertex",profile="vs_5_0";
        if(version==2){entry=Encoding.UTF8.GetString(Bytes());profile=Encoding.UTF8.GetString(Bytes());}var expected=Bytes();int copies=reader.ReadInt32();
        sources.Add((source,entry,profile,copies));
        for(int j=0;j<copies;j++)slots.Add(new(){Offset=reader.ReadInt32(),Expected=expected,Compiled=expected});
    }
    foreach(uint flags in new uint[]{0,4})
    {
        int slotIndex=0;
        foreach(var source in sources)
        {
            var bytes=Encoding.UTF8.GetBytes(source.Source);
            object?[] values={bytes,(UIntPtr)bytes.Length,"BridgeBuilder.GeneratedMaterial",IntPtr.Zero,IntPtr.Zero,source.Entry,source.Profile,flags,0u,IntPtr.Zero,IntPtr.Zero};
            var type=typeof(MovableCustomShaderProgram);var binding=BindingFlags.Static|BindingFlags.NonPublic;
            int status=(int)type.GetMethod("D3DCompile",binding)!.Invoke(null,values)!;
            byte[] code;
            try { if(status<0)throw new Exception("Alternate compiler settings failed");code=(byte[])type.GetMethod("ReadBlob",binding)!.Invoke(null,new[]{values[9]})!; }
            finally { foreach(var index in new[]{9,10})type.GetMethod("ReleaseBlob",binding)!.Invoke(null,new[]{values[index]}); }
            for(int copy=0;copy<source.Copies;copy++)slots[slotIndex++].Compiled=code;
        }
        var result=MovableShaderContainer.Rebuild(seed,slots);
        File.WriteAllBytes(Path.Combine(output,name+"-compiler-"+flags+".bundle"),result);
        Console.WriteLine($"PASS {name} actual compiler flags={flags}: {seed.Length} -> {result.Length}; changed-size occurrences={slots.Count(s=>s.Expected.Length!=s.Compiled.Length)}");
        if(flags==4)
        {
            using var package=new MemoryStream();using var w=new BinaryWriter(package,Encoding.UTF8,true);
            void WriteBytes(byte[] bytes){w.Write(bytes.Length);w.Write(bytes);}
            w.Write(0x31534242u);w.Write(2);WriteBytes(result);w.Write(sources.Count);int index=0;
            foreach(var source in sources)
            {
                WriteBytes(Encoding.UTF8.GetBytes(source.Source));WriteBytes(Encoding.UTF8.GetBytes(source.Entry));WriteBytes(Encoding.UTF8.GetBytes(source.Profile));
                var code=slots[index].Compiled;index+=source.Copies;WriteBytes(code);var positions=new List<int>();
                for(int at=0;at<=result.Length-code.Length;at++)if(result.AsSpan(at,code.Length).SequenceEqual(code)){positions.Add(at);at+=code.Length-1;}
                if(positions.Count!=source.Copies)throw new Exception("Compiled occurrence identity mismatch");
                w.Write(positions.Count);foreach(var at in positions)w.Write(at);
            }
            package.Position=0;
            if(!MovableCustomShaderProgram.TryCompilePackage(package,out var shorter,out error))throw new Exception(error);
            if(!shorter.SequenceEqual(actual))throw new Exception("Longer seed did not recompile to exact production bytes");
            File.WriteAllBytes(Path.Combine(output,name+"-compiler-short.bundle"),shorter);
            Console.WriteLine($"PASS {name} real shorter recompilation: {result.Length} -> {shorter.Length}");
        }
    }
    foreach(var mode in new[]{"identity","short","long","mixed"})
    {
        for(int i=0;i<slots.Count;i++)
        {
            var slot=slots[i];int delta=mode=="identity"?0:mode=="short"?-16:mode=="long"?20:(i%2==0?-12:28);
            var bytes=new byte[slot.Expected.Length+delta];Array.Copy(slot.Expected,bytes,Math.Min(bytes.Length,slot.Expected.Length));
            BitConverter.GetBytes(bytes.Length).CopyTo(bytes,24);slot.Compiled=bytes;
        }
        var result=MovableShaderContainer.Rebuild(seed,slots);
        var repeated=MovableShaderContainer.Rebuild(seed,slots);
        if(!result.SequenceEqual(repeated))throw new Exception("Nondeterministic rebuild");
        if(mode=="identity" && !result.SequenceEqual(seed))throw new Exception("Identity rebuild changed seed bytes");
        File.WriteAllBytes(Path.Combine(output,name+"-"+mode+".bundle"),result);
        Console.WriteLine($"PASS {name} {mode}: {slots.Count} occurrences; {seed.Length} -> {result.Length}");
    }
    void Reject(byte[] input,IReadOnlyList<MovableShaderContainer.Replacement> changes,string label)
    {try{MovableShaderContainer.Rebuild(input,changes);throw new Exception(label+" accepted");}catch(InvalidDataException){}}
    slots[0].Offset++;Reject(seed,slots,"Corrupt offset");slots[0].Offset--;
    Reject(seed,slots.Concat(new[]{slots[0]}).ToArray(),"Overlapping declarations");
    Reject(seed[..^1],slots,"Truncated bundle");
    var saved=slots[0].Expected;slots[0].Expected=(byte[])saved.Clone();slots[0].Expected[0]^=1;Reject(seed,slots,"Mismatched seed");slots[0].Expected=saved;
    Console.WriteLine($"PASS {name} real D3DCompile, repeated rebuild, four malformed-file cases; compiler output trusted");
}
