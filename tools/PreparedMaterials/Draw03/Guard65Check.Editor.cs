using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

// Runs compiled production Apply/Bands in a private Unity editor, without creating assets.
public static class Draw03Guard65Check
{
    const BindingFlags I=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    const BindingFlags S=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    static object Get(object value,string field)=>value.GetType().GetField(field,I).GetValue(value);
    static void Need(bool value,string reason){if(!value)throw new InvalidDataException(reason);}
    public static void Run()
    {
        try
        {
            var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
            var managed=Arg("-gameManaged");var dll=Arg("-modAssembly");
            AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
            var mod=Assembly.LoadFrom(dll);Type T(string n)=>mod.GetType("BridgeBuilder.Bridges."+n,true);
            var type=T("Draw03LaneAppearance");var apply=type.GetMethod("Apply",S);var bandsMethod=type.GetMethod("Bands",S);
            var patchType=apply.GetParameters()[0].ParameterType.GetElementType();
            var partition=T("MovableRoadGeometry").GetNestedType("Partition",BindingFlags.NonPublic);
            var leaf=partition.GetConstructor(I,null,new[]{typeof(int)},null);
            var branch=partition.GetConstructor(I,null,new[]{typeof(Matrix4x4),partition,partition},null);
            var bandType=type.GetNestedType("Band",BindingFlags.NonPublic);
            var mapType=apply.GetParameters()[5].ParameterType;
            // A tagged source value suffices: this test verifies routing, not resource loading.
            var source=Array.CreateInstance(patchType,2);
            var index=Expression.Parameter(typeof(int));var band=Expression.Parameter(bandType);
            var map=Expression.Lambda(mapType,Expression.ArrayIndex(Expression.Constant(source),index),index,band).Compile();
            int cases=0,empty=0,positive=0;
            foreach(var centers in new[]{Array.Empty<float>(),new[]{0f},new[]{-7.5f,-4.5f,-1.5f,1.5f,4.5f,7.5f},new[]{-100f,-3f,2f,7f,100f}})
            foreach(var span in new[]{new Vector2(0,0),new Vector2(4,4),new Vector2(8,-8),new Vector2(-9,9),new Vector2(-9,11)})
            foreach(bool right in new[]{false,true})
            foreach(var pose in new[]{new Pose(Vector3.zero,Quaternion.identity),new Pose(new Vector3(3,2,-5),Quaternion.Euler(0,37,0))})
            {
                var layout=FormatterServices.GetUninitializedObject(T("MovableSelectedRoadLayout"));
                layout.GetType().GetField("CarLaneCenters",I).SetValue(layout,centers);
                var a=leaf.Invoke(new object[]{0});var b=leaf.Invoke(new object[]{1});
                var root=branch.Invoke(new[]{(object)Matrix4x4.identity,a,b});
                var expected=(Array)bandsMethod.Invoke(null,new object[]{centers,span,right});
                object[] input={source,root,layout,pose,span,map,new Func<int,bool>(_=>right),null};
                var result=(Array)apply.Invoke(null,input);var flags=(bool[])input[7];
                Need(result.Length==2+2*expected.Length,"Patch count must match real emitted bands.");
                Need(flags.Length==result.Length&&!flags[0]&&!flags[1],"Original flags changed.");
                if(expected.Length==0)
                {
                    Need(ReferenceEquals(Get(input[1],"Negative"),a)&&ReferenceEquals(Get(input[1],"Positive"),b),"Empty span changed existing leaves.");
                    empty++;
                }
                else
                {
                    for(int side=0;side<2;side++)for(int i=0;i<expected.Length;i++)
                        Need(flags[2+side*expected.Length+i]==(bool)Get(expected.GetValue(i),"Wear"),"Wear routing changed.");
                    positive++;
                }
                cases++;
            }
            using var sha=SHA256.Create();var hash=BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(dll))).Replace("-","");
            var report=$"PASS Draw03 production Apply/Bands: {cases} cases; {empty} empty/reversed and {positive} positive intervals. Both sides, no lanes, centered/asymmetric/outside lanes; original leaves and wear flags retained. DLL SHA256 {hash}. No native asset publication or visual acceptance.";
            File.WriteAllText(Arg("-report"),report+Environment.NewLine);Debug.Log(report);EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
