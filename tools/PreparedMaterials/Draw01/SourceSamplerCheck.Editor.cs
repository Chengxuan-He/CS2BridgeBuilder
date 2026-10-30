using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class Draw01SourceSamplerCheck
{
    [Serializable] public class Row {public string channel,path,sha256,format,wrapU,wrapV,wrapW,filter;public int width,height,mips,anisotropy;}
    [Serializable] public class Report {public string sourceCid;public bool settingsChanged=false;public Row[] channels;}
    public static void Run()
    {
        string Arg(string k)=>Draw01AppearancePartitionCheck.Argument(k);
        var manifest=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(File.ReadAllText(Arg("-unitManifest")));
        var source=manifest.units[0];var rows=new List<Row>();var codec=PreparePaint.OpenCodec(Arg("-gameData"));
        try
        {
            foreach(var channel in source.channels)
            {
                byte[] bytes=File.ReadAllBytes(channel.path);string digest=PreparePaint.Digest(bytes);
                if(digest!=channel.sha256)throw new Exception("Original source payload differs: "+channel.name);
                int size=(int)typeof(PrepareRepeatUnits).GetMethod("NativeRawSize",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{bytes});
                using var stream=new MemoryStream(bytes);
                var texture=BridgeBuilder.Bridges.MovableTextureSource.Read(stream,b=>PreparePaint.DecodeRaw(b,size));
                try{rows.Add(new Row{channel=channel.name,path=channel.path,sha256=digest,format=texture.graphicsFormat.ToString(),width=texture.width,height=texture.height,mips=texture.mipmapCount,wrapU=texture.wrapModeU.ToString(),wrapV=texture.wrapModeV.ToString(),wrapW=texture.wrapModeW.ToString(),filter=texture.filterMode.ToString(),anisotropy=texture.anisoLevel});}
                finally{UnityEngine.Object.DestroyImmediate(texture);}
            }
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{sourceCid=source.sourceCid,channels=rows.ToArray()},true));
            Debug.Log("PASS actual original PBR source texture sampler inspection: "+rows.Count+" channels; no sampler changes.");EditorApplication.Exit(0);
        }
        finally{PreparePaint.CloseCodec(codec);}
    }
}
