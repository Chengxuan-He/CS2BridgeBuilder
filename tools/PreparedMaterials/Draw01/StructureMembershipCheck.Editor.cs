using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
public static class Draw01StructureMembershipCheck {
 [Serializable] public class Row {public int lod;public int[] removed,restored;public Group[] groups;public Widen[] widen;}
 [Serializable] public class Group {public int role;public int[] faces;}
 [Serializable] public class Widen {public float weight;public int[] vertices;}
 [Serializable] public class Result {public Row[] rows;}
 public static void Run(){try{var args=Environment.GetCommandLineArgs();string A(string n)=>args[Array.IndexOf(args,n)+1];var managed=A("-gameManaged");AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};var asm=Assembly.LoadFrom(A("-candidate"));const BindingFlags F=BindingFlags.NonPublic|BindingFlags.Public|BindingFlags.Static|BindingFlags.Instance;Type T(string n)=>asm.GetType("BridgeBuilder.Bridges."+n,true);var rows=new Row[2];for(int lod=0;lod<2;lod++){var groups=((Array)T("Draw01UndersideData").GetField(lod==0?"Near":"Middle",F).GetValue(null)).Cast<object>().Select(g=>new Group{role=(int)g.GetType().GetField("Role",F).GetValue(g),faces=(int[])g.GetType().GetField("Faces",F).GetValue(g)}).ToArray();var widen=((Array)T("Draw01MovingWideningData").GetField(lod==0?"Near":"Middle",F).GetValue(null)).Cast<object>().Select(g=>new Widen{weight=(float)g.GetType().GetField("Item1").GetValue(g),vertices=(int[])g.GetType().GetField("Item2").GetValue(g)}).ToArray();rows[lod]=new Row{lod=lod,widen=widen,removed=(int[])T("Draw01MovingRoadData").GetMethod("RemovalFaces",F).Invoke(null,new object[]{lod}),restored=groups.SelectMany(g=>g.faces).ToArray(),groups=groups};}File.WriteAllText(A("-report"),JsonUtility.ToJson(new Result{rows=rows},true));Debug.Log("PASS membership extraction");EditorApplication.Exit(0);}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
