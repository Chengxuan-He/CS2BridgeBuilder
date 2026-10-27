using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Execute the production DLL's loader on Unity's Mono runtime. No game world
// or Prefab is created. Baseline files are the byte-exact pre-migration sources.
public static class HardcodedNumbersNativeCheck
{
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();
            string Arg(string key)=>args[Array.IndexOf(args,key)+1];
            var assembly=Assembly.Load(File.ReadAllBytes(Arg("-numbersDll")));
            var type=assembly.GetType("BridgeBuilder.Bridges.HardcodedNumbers",true);
            var load=type.GetMethod("Load",BindingFlags.Static|BindingFlags.NonPublic);
            var names=assembly.GetManifestResourceNames().Where(n=>n.StartsWith("BridgeBuilder.Numbers.")).ToArray();
            if(names.Length==0)throw new Exception("No numerical resources embedded.");
            var baseline=Directory.GetFiles(Path.GetTempPath(),"BridgeBuilder-Numbers-*.bin").ToHashSet();
            long values=0;
            foreach(var name in names)
            {
                if(!name.EndsWith(".bin"))throw new Exception("Unexpected resource encoding.");
                string id=name.Substring("BridgeBuilder.Numbers.".Length,name.Length-"BridgeBuilder.Numbers.".Length-4);
                var data=load.Invoke(null,new object[]{id});
                using(var stream=File.OpenRead(Path.Combine(Arg("-numbersBaseline"),id+".bin")))
                using(var reader=new BinaryReader(stream))
                {
                    stream.Position=32;
                    foreach(var tag in new[]{"I32","U32","I64","U64","F32","F64"})
                    {
                        var array=(Array)type.GetField(tag,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(data);
                        // BlockCopy compares the actual native array representation,
                        // including negative zero and floating-point payload bits.
                        var bytes=new byte[Buffer.ByteLength(array)];Buffer.BlockCopy(array,0,bytes,0,bytes.Length);
                        if(!reader.ReadBytes(bytes.Length).SequenceEqual(bytes))throw new Exception("Numerical bits changed: "+id+"/"+tag);
                        values+=array.Length;
                    }
                    if(stream.Position!=stream.Length-32)throw new Exception("Array length changed: "+id);
                }
            }
            if(Directory.GetFiles(Path.GetTempPath(),"BridgeBuilder-Numbers-*.bin").Any(p=>!baseline.Contains(p)))
                throw new Exception("Temporary extraction leaked.");
            Debug.Log("PASS Unity Mono numerical resources: "+names.Length+" resources; "+values+" bit-exact values; no temporary extraction remains.");
            EditorApplication.Exit(0);
        }
        catch(Exception error){Debug.LogException(error);EditorApplication.Exit(1);}
    }
}
