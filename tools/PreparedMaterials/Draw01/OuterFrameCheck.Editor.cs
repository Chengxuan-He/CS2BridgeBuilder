using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
public static class Draw01OuterFrameCheck {
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static object Get(object o,string n)=>o.GetType().GetField(n,F).GetValue(o);
 public static void Run(){try{var args=Environment.GetCommandLineArgs();string A(string n)=>args[Array.IndexOf(args,n)+1];var managed=A("-gameManaged");AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var asm=Assembly.LoadFrom(A("-candidate"));Type T(string n)=>asm.GetType("BridgeBuilder.Bridges."+n,true);int cases=0,checkedFaces=0;using var reader=new BinaryReader(File.OpenRead(A("-fixture")));int levels=reader.ReadInt32();
 for(int lod=0;lod<levels;lod++){int nv=reader.ReadInt32();var source=new Vector3[nv];for(int i=0;i<nv;i++)source[i]=new Vector3(reader.ReadSingle(),reader.ReadSingle(),reader.ReadSingle());int ni=reader.ReadInt32();var indices=Enumerable.Range(0,ni).Select(_=>reader.ReadInt32()).ToArray();int nf=reader.ReadInt32();var faces=Enumerable.Range(0,nf).Select(_=>reader.ReadInt32()).ToArray();var ids=faces.SelectMany(f=>indices.Skip(f*3).Take(3)).Distinct().ToArray();
 foreach(float delta in new[]{0f,7f,2.25f})foreach(int layoutKind in new[]{0,1,2,3}){
  var layout=FormatterServices.GetUninitializedObject(T("Draw01UndersideLayout"));var spans=layoutKind==0?new[]{new Vector2(-13.5f,13.5f),Vector2.zero,Vector2.zero}:layoutKind==1?new[]{new Vector2(-13.5f,10.6f),Vector2.zero,new Vector2(10.6f,13.5f)}:layoutKind==2?new[]{new Vector2(-10.6f,13.5f),new Vector2(-14,-10.6f),Vector2.zero}:new[]{new Vector2(-10.6f,10.6f),new Vector2(-14,-10.6f),new Vector2(10.6f,13.5f)};layout.GetType().GetField("Spans",F).SetValue(layout,spans);
  var plan=T("Draw01RoadReplacement").GetMethod("CreateMainMeshPlan",F).Invoke(null,new object[]{lod,source,delta,new[]{indices},layout});var removed=((bool[][])Get(plan,"RemoveTriangles"))[0];var positions=(Vector3[])Get(plan,"Positions");var fragments=((Array)Get(plan,"Fragments")).Cast<object>().Select(x=>(int)Get(x,"Triangle")).ToHashSet();var corrections=((KeyValuePair<int,Vector2>[])Get(plan,"UvCorrections")).Select(x=>x.Key).ToHashSet();
  foreach(int f in faces){if(removed[f]||fragments.Contains(f))throw new Exception("Final Plan loses or duplicates frame face "+lod+"/"+f);checkedFaces++;}foreach(int id in ids){var expected=source[id];expected.x+=Math.Sign(expected.x)*delta;if(positions[id]!=expected||corrections.Contains(id))throw new Exception("Final Plan changed profile/UV "+id);}
  if(lod==1){var mapped=(Vector3[])source.Clone();var omit=Enumerable.Repeat(true,ni/3).ToArray();T("Draw01UndersideData").GetMethod("Apply",F).Invoke(null,new object[]{1,layout,mapped,omit});T("Draw01OuterFrameData").GetMethod("Apply",F).Invoke(null,new object[]{1,delta,source,mapped,omit});foreach(int f in faces)if(omit[f])throw new Exception("Far supplement omitted outer frame");foreach(int id in ids)if(mapped[id]!=positions[id])throw new Exception("Far frame differs from middle rigid mapping");}
  cases++;
 }
 }
 File.WriteAllText(A("-report"),"PASS final production Plans: "+cases+" scenarios, "+checkedFaces+" original face checks; no/left/right/asymmetric sidewalks; delta0/7/2.25; original YZ/rigid X; zero Fragment and UvCorrections overlap; middle-based far supplement parity. Full native Retile and game rendering not asserted.");Debug.Log("PASS Draw01 final outer-frame Plans "+cases);EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
