using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;

// Real game binary readers; deliberately does not claim in-game rendering acceptance.
public static class Draw02RepeatDescriptorCheck
{
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string name)=>args[Array.IndexOf(args,name)+1];
        string managed=Arg("-gameManaged");
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{
            var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        try
        {
            var db=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.AssetDatabase.dll"));
            var io=Assembly.LoadFrom(Path.Combine(managed,"Colossal.IO.dll"));
            var core=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Core.dll"));
            var type=db.GetType("Colossal.IO.AssetDatabase.SurfaceAsset",true);
            var binary=io.GetType("Colossal.IO.BinaryReaderExtensions",true);
            var hash=core.GetType("Colossal.IO.BinaryReaderExtensions",true).GetMethod("ReadHash");
            int count=0,vt=0;
            foreach(var path in Directory.GetFiles(Arg("-materials"),"*.Surface",SearchOption.AllDirectories))
            {
                var asset=FormatterServices.GetUninitializedObject(type);
                using var reader=new BinaryReader(File.OpenRead(path));
                type.GetMethod("ReadHeader",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(asset,new object[]{reader});
                bool virtualTexture=(bool)type.GetProperty("isVTMaterial").GetValue(asset);
                if(virtualTexture)throw new Exception("Repeat must be non-VT."); if((int)type.GetProperty("materialTemplateHash").GetValue(asset)!=14)throw new Exception("Cutout double-sided template required.");
                // 1 and 14 are distinct native single-/double-sided cutout
                // templates. Validation must accept both without rewriting them.
                if(!new[]{0,1,14,35}.Contains((int)type.GetProperty("materialTemplateHash").GetValue(asset)))throw new Exception("Not a supported object material: "+path);
                var floats=(IDictionary)binary.GetMethod("ReadStringFloatDictionary").Invoke(null,new object[]{reader}); if(!floats.Contains("_Snow")||(float)floats["_Snow"]!=0f)throw new Exception("Snow override missing."); foreach(var name in new[]{"ReadStringIntDictionary","ReadStringVectorDictionary","ReadStringColorDictionary"})binary.GetMethod(name).Invoke(null,new object[]{reader});
                if(reader.ReadByte()!=255)throw new Exception("Missing native texture map.");
                for(int n=reader.ReadInt32();n>0;n--){reader.ReadString();byte present=reader.ReadByte();if(present==255)hash.Invoke(null,new object[]{reader});else if(present!=0)throw new Exception("Texture marker.");}
                var keywords=(IEnumerable)binary.GetMethod("ReadStringHashSet").Invoke(null,new object[]{reader});
                if(!keywords.Cast<string>().Contains("_TANGENTSPACE_OCTO"))throw new Exception("Missing packed tangent keyword.");
                foreach(var required in new[]{"_ALPHATEST_ON","_DOUBLESIDED_ON","_DISABLE_SSR_TRANSPARENT"})if(!keywords.Cast<string>().Contains(required))throw new Exception("Required semantic keyword: "+required); if(reader.BaseStream.Position!=reader.BaseStream.Length)throw new Exception("Native reader did not consume the descriptor.");
                if(Path.GetFileName(path)=="SkinnedPaint.Surface"&&!virtualTexture)throw new Exception("Paint lost its source VT mip chain.");
                count++;
            }
            if(count==0)throw new Exception("No descriptors tested.");
            Debug.Log($"PASS native source descriptors: {count} descriptors, {vt} VT; installed game reader; complete consumption; renderer acceptance pending.");
        }
        catch(Exception e){Debug.LogException(e);UnityEditor.EditorApplication.Exit(1);}
    }
}
