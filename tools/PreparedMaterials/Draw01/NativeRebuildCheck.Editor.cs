using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

// Uses the installed game reader; does not claim scene rendering or cold-load acceptance.
public static class Draw01NativeRebuildCheck
{
    [Serializable] public sealed class Entry { public string path; public int template, bytes; public bool vt; }
    [Serializable] public sealed class Report { public string status="PASS"; public bool nativeReaderVerified=true, nativeRenderingVerified=false; public Entry[] surfaces; }
    static void Need(bool ok,string message) { if(!ok)throw new InvalidDataException(message); }
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string root=Arg("-candidateRoot"),managed=Path.Combine(Arg("-gameData"),"Managed");
        ResolveEventHandler resolve=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        AppDomain.CurrentDomain.AssemblyResolve+=resolve;
        try
        {
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var hashes=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            var type=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var entries=new List<Entry>();
            var outputFiles=Directory.GetFiles(root,"*.Surface",SearchOption.AllDirectories);
            string originals=Arg("-originals");
            var originalFiles=Directory.GetFiles(originals,"*.Surface",SearchOption.TopDirectoryOnly);
            Need(originalFiles.Length==13,"Expected all 13 original source descriptors");
            // Original VT headers with a VTSurface AssetData reference require the game's
            // initialized database. Preserve those inputs and verify their archive hashes
            // separately; this standalone reader checks the three non-VT source descriptors.
            var standaloneOriginals=originalFiles.Where(f=>!BitConverter.ToBoolean(File.ReadAllBytes(f),6)).ToArray();
            Need(standaloneOriginals.Length==3,"Expected three non-VT original sources");
            foreach(string file in outputFiles.Concat(standaloneOriginals).OrderBy(x=>x))
            {
                bool original=originalFiles.Contains(file);
                var bytes=File.ReadAllBytes(file);using var r=new BinaryReader(new MemoryStream(bytes));
                var surface=FormatterServices.GetUninitializedObject(type);
                type.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(surface,new object[]{r});
                int template=(int)type.GetProperty("materialTemplateHash").GetValue(surface);
                bool vt=(bool)type.GetProperty("isVTMaterial").GetValue(surface);
                Need((original?new[]{0,1,17}:new[]{0,1,35}).Contains(template),"Unsupported native template: "+file);
                Need(template==BitConverter.ToInt32(bytes,2),"Native source header disagrees with serialized template");
                var floats=(IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null,new object[]{r});
                foreach(string method in new[]{"ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})
                    binary.GetMethod(method).Invoke(null,new object[]{r});
                Need(r.ReadByte()==255,"Missing texture dictionary");
                var textures=new Dictionary<string,string>();
                for(int n=r.ReadInt32();n>0;n--){string name=r.ReadString();byte marker=r.ReadByte();Need(marker==0||marker==255,"Texture marker");textures[name]=marker==0?null:hashes.Invoke(null,new object[]{r}).ToString();}
                var keys=((IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{r})).Cast<string>().ToArray();
                if(!original)Need(keys.Contains("_TANGENTSPACE_OCTO"),"Native packed tangent encoding missing");
                Need(r.BaseStream.Position==r.BaseStream.Length,"Unconsumed native descriptor data");
                if(file.EndsWith("SkinnedPaint.Surface"))
                {
                    Need(vt&&template==0,"Paint must preserve native VT backing");
                    Need((float)floats["_CullMode"]==2&&(float)floats["_CullModeForward"]==2,"Paint must remain single-sided");
                    Need((float)floats["_Snow"]==0&&keys.Contains("_DISABLE_DECALS"),"Paint appearance defaults");
                    Need(textures.Count==0,"Paint must not refer to deleted authoring textures");
                }
                if(file.Contains("NativeSources"))
                {
                    Need(!vt&&template==0,"Converted network object template");
                    Need((float)floats["_Snow"]==0&&(float)floats["_EmissiveIntensity"]==1&&(float)floats["_EmissiveExposureWeight"]==1,"Template 17 defaults lost");
                    Need(textures["_BaseColorMap"]=="d99de72b9a3e4442445ad6720119e4fc"&&textures["_NormalMap"]=="dae791c517af749a9c6be7b4f422db9c"&&textures["_MaskMap"]=="2ae97c5ed8cb4ef6561cba57795d0966","Source channels changed");
                }
                entries.Add(new Entry{path=original?"originals/"+Path.GetFileName(file):file.Substring(root.Length+1).Replace('\\','/'),template=template,vt=vt,bytes=bytes.Length});
            }
            Need(entries.Count==33,"Expected 30 candidate and three non-VT original Surface descriptors");
            File.WriteAllText(Arg("-report"),JsonUtility.ToJson(new Report{surfaces=entries.ToArray()},true));
            Debug.Log("PASS Draw01 native rebuild: 30 candidate and three non-VT original native descriptors; Paint native VT header and template-17 defaults verified; original VT database load and rendering pending.");
        }
        finally{AppDomain.CurrentDomain.AssemblyResolve-=resolve;}
    }
}
