using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Pedestrian02Truss81Check {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static T Field<T>(object p,string n)=>(T)p.GetType().GetField(n,Flags).GetValue(p);
 static bool Equal32(float a,float b)=>BitConverter.ToInt32(BitConverter.GetBytes(a),0)==BitConverter.ToInt32(BitConverter.GetBytes(b),0);
 static int[] Ints(BinaryReader r){int n=r.ReadInt32();var a=new int[n];for(int i=0;i<n;i++)a[i]=r.ReadInt32();return a;}
 sealed class Member {public float Centre;public int[] Vertices,Faces;}
 public static void Run(){try{Check();EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
 static void Check(){
 var dll=Environment.GetEnvironmentVariable("BB81_DLL");var fixture=Environment.GetEnvironmentVariable("BB81_FIXTURE");var managed=Environment.GetEnvironmentVariable("BB81_MANAGED");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 var mod=Assembly.LoadFrom(dll);var create=mod.GetType("BridgeBuilder.Bridges.PedestrianDraw02RenderAssembly").GetMethod("Structure",BindingFlags.Static|BindingFlags.NonPublic);
 int cases=0,faceChecks=0,frameChecks=0;
 using(var r=new BinaryReader(File.OpenRead(fixture))){int levels=r.ReadInt32();for(int level=0;level<levels;level++){
 int lod=r.ReadInt32();float h=r.ReadSingle();var source=new Vector3[r.ReadInt32()];for(int i=0;i<source.Length;i++)source[i]=new Vector3(r.ReadSingle(),r.ReadSingle(),r.ReadSingle());
 var indices=Ints(r);var assembly=new HashSet<int>(Ints(r));var assemblyFaces=Ints(r);var members=new Member[r.ReadInt32()];for(int i=0;i<members.Length;i++)members[i]=new Member{Centre=r.ReadSingle(),Vertices=Ints(r),Faces=Ints(r)};
 foreach(float delta in new[]{-6f,-h,-1f,0f,2f,8f,24f}){
 var plan=create.Invoke(null,new object[]{lod,0,source,indices,delta});var target=Field<Vector3[]>(plan,"Positions");var removed=Field<bool[][]>(plan,"RemoveTriangles");var frames=Field<Array>(plan,"FrameTransforms");var scale32=new float[]{(h+delta)/h};float k=scale32[0];var selected=new HashSet<int>(assembly);var sf=new HashSet<int>(assemblyFaces);
 foreach(int i in assembly)if(!Equal32(target[i].x,new float[]{source[i].x*scale32[0]}[0])||target[i].y!=source[i].y||target[i].z!=source[i].z)throw new Exception("Assembly mapping mismatch");
 foreach(var member in members){float c=member.Centre;var ids=member.Vertices;foreach(int i in ids){selected.Add(i);if(!Equal32(target[i].x,new float[]{source[i].x+c/4f*delta}[0])||target[i].y!=source[i].y||target[i].z!=source[i].z)throw new Exception("Rib mapping mismatch");}if(Math.Abs((ids.Max(i=>target[i].x)-ids.Min(i=>target[i].x))-(ids.Max(i=>source[i].x)-ids.Min(i=>source[i].x)))>4e-6f)throw new Exception("Rib widened");foreach(int f in member.Faces)sf.Add(f);}
 foreach(int f in sf){if(removed[0][f])throw new Exception("Structural face removed");faceChecks++;}
 foreach(var f in Field<Array>(plan,"Fragments"))if(sf.Contains(Field<int>(f,"Triangle")))throw new Exception("Structural face fragmented");
 var keys=new HashSet<int>();foreach(var p in frames){int i=(int)p.GetType().GetProperty("Key").GetValue(p);var m=(Matrix4x4)p.GetType().GetProperty("Value").GetValue(p);if(!keys.Add(i)||!assembly.Contains(i)||!Equal32(m.m00,k)||m.m11!=1||m.m22!=1)throw new Exception("Frame mismatch");frameChecks++;}if(!keys.SetEquals(assembly))throw new Exception("Incomplete frames");
 foreach(var e in Field<Array>(plan,"AddedVertices"))if(selected.Contains(Field<int>(e,"A"))||selected.Contains(Field<int>(e,"B")))throw new Exception("Selected construction edge not handled");cases++;
 }}}
 Debug.Log($"PASS Pedestrian02 {cases} actual final plans; {faceChecks} complete source-face checks; {frameChecks} unique assembly Jacobians; rigid rib profiles; Y/Z preserved; no selected constructed edges.");
 }
}
