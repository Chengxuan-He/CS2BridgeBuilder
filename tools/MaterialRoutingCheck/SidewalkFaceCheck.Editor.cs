using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Validate measured source-face roles in the compiled candidate.
public static class SidewalkFaceCheck
{
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();
            string Arg(string key) => args[Array.IndexOf(args,key)+1];
            string managed=Arg("-gameManaged");
            AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=>{
                var path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
                return File.Exists(path)?Assembly.LoadFrom(path):null;
            };
            var mod=Assembly.LoadFrom(Arg("-candidate"));
            const BindingFlags f=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
            var roleType=mod.GetType("BridgeBuilder.Bridges.MovableSidewalkFaces",true);
            var family=roleType.GetMethod("Family",f);var classify=roleType.GetMethod("Classify",f);
            var identity=mod.GetType("BridgeBuilder.Bridges.MovableRoadLayerIdentity",true).GetMethod("WholeReceiver",f);
            var kind=mod.GetType("BridgeBuilder.Bridges.MovableRoadAppearance+Type",true);
            object Kind(string n)=>Enum.Parse(kind,n);
            string Role(string section, object source, bool ped,bool mix,float ny) => classify.Invoke(null,new[]{family.Invoke(null,new object[]{section}),source,ped,mix,ny,ny,ny}).ToString();
            int walk=0,curb=0,gutter=0;
            using(var file=new BinaryReader(File.OpenRead(Path.Combine(Arg("-laneFixtures"),"curb.bin"))))
            while(file.BaseStream.Position<file.BaseStream.Length)
            {
                var u=new[]{file.ReadSingle(),file.ReadSingle(),file.ReadSingle()};var ny=file.ReadSingle();file.ReadSingle();
                var source=identity.Invoke(null,new object[]{"1d7dad9f64c9fe55a809f1080c39fb92",true,"9a6506aaede935752b5c4c0362c95f6e","a142c18b42577de52815d1afd054660b","37ceea0e8c2bd24599e237236e6ae338",Kind("Asphalt"),u.Min(),u.Max()});
                var actual=Role("Sidewalk 3.5",source,true,true,ny);
                if(u.Min()>=.32763671875f && u.Max()<=.6650390625f){if(actual!="Walk")throw new Exception("Paving lost");walk++;}
                else if(u.Min()>=.28173828125f && u.Max()<=.29638671875f){if(actual!="Curb")throw new Exception("Curb promoted");curb++;}
                else {if(actual=="Walk")throw new Exception("Gutter promoted");gutter++;}
            }
            foreach(float ny in new[]{0f,.45f,1f})
                if(Role("Curb 0.5",Kind("Asphalt"),true,false,ny)!="Curb" || Role("Pathway",Kind("Pathway"),true,false,ny)!="Other" ||
                   Role("Sidewalk 3.5",Kind("Curb"),true,true,ny)!="Curb")throw new Exception("Section/face role mismatch");
            if(walk*curb*gutter==0)throw new Exception("Incomplete native fixture");
            Debug.Log($"PASS actual native sidewalk 3.5/6 faces paving={walk} curb={curb} gutter/other={gutter}; separate Pathway and sloped/side/top curb.");
            EditorApplication.Exit(0);
        }
        catch(Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }
}
