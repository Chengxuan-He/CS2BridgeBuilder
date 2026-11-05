using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Lift02Truss69Dump
{
    public static void Run()
    {
        try
        {
            const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
            var cli=Environment.GetCommandLineArgs();string Arg(string key)=>cli[Array.IndexOf(cli,key)+1];
            string managed=Arg("-gameManaged"), output=Arg("-dumpOutput");Directory.CreateDirectory(output);
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(Arg("-candidate"));var type=mod.GetType("BridgeBuilder.Bridges.Lift02StructurePlan",true);
            for(int lod=0;lod<3;lod++)
            {
                using var r=new BinaryReader(File.OpenRead(Arg("-trussFixtures")+"/lod"+lod+"-fixture.bin"));
                var p=new Vector3[r.ReadInt32()];var ix=new int[r.ReadInt32()];
                for(int i=0;i<p.Length;i++)p[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
                for(int i=0;i<ix.Length;i++)ix[i]=r.ReadInt32();
                foreach(float delta in new[]{-8f,-3f,0f,2f,8.5f,24f})
                {
                    var plan=type.GetMethod("Create",F).Invoke(null,new object[]{lod,p,ix,delta});
                    var positions=(Vector3[])plan.GetType().GetField("Positions",F).GetValue(plan);
                    using var w=new BinaryWriter(File.Create(Path.Combine(output,"lod"+lod+"-delta"+delta.ToString(System.Globalization.CultureInfo.InvariantCulture)+".bin")));
                    w.Write(positions.Length);foreach(var v in positions){w.Write(v.x);w.Write(v.y);w.Write(v.z);}
                }
            }
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
