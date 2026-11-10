using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
public static class Network133Check
{
 const BindingFlags F=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static;
 static void Need(bool b,string s){if(!b)throw new Exception(s);}
 public static void Run(){try{
 var args=Environment.GetCommandLineArgs();string Arg(string k)=>args[Array.IndexOf(args,k)+1];string managed=Arg("-gameManaged");
 AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{var p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
 bool record=args.Contains("-recordBaseline");string baseline=args.Contains("-baselineRows")?Arg("-baselineRows"):"E:/CS2BridgeBuilder/tools/NetworkStateCheck/Native132States.tsv";
 var evidence=new System.Collections.Generic.List<string>();var expected=record?null:File.ReadAllLines(baseline).ToDictionary(x=>x.Split('\t')[0],x=>x.Split('\t')[1]);
 var mod=Assembly.LoadFrom(Arg("-modAssembly"));var game=Assembly.LoadFrom(Path.Combine(managed,"Game.dll"));Type T(string n)=>game.GetType("Game.Prefabs."+n,true);
 var req=T("NetPieceRequirements");Array States(params string[] names){var a=Array.CreateInstance(req,names.Length);for(int i=0;i<names.Length;i++)a.SetValue(Enum.Parse(req,names[i]),i);return a;}
 object Rule(string kind,string appearance){var x=Activator.CreateInstance(T("Net"+kind+"StateInfo"));foreach(string f in new[]{"m_RequireAll","m_RequireAny","m_RequireNone"})x.GetType().GetField(f).SetValue(x,States());x.GetType().GetField("m_SetState").SetValue(x,States(appearance));return x;}
 Array Single(object rule){var a=Array.CreateInstance(rule.GetType(),1);a.SetValue(rule,0);return a;}
 string[] Values(object rule)=>((Array)rule.GetType().GetField("m_SetState").GetValue(rule)).Cast<object>().Select(x=>x.ToString()).ToArray();
 using((IDisposable)mod.GetType("BridgeBuilder.Bridges.GenerationParameters",true).GetMethod("Begin",F).Invoke(null,null)){
 int cases=0;
 foreach(var style in new[]{"Draw01","Draw02","Draw03","PedestrianDraw01","PedestrianDraw02","Lift03"})foreach(var appearance in new[]{"Tiles","Gravel","Pavement"}){
 var target=ScriptableObject.CreateInstance(T("RoadPrefab"));var edge=Rule("Edge",appearance);var node=Rule("Node",appearance);
 T("NetGeometryPrefab").GetField("m_EdgeStates").SetValue(target,Single(edge));T("NetGeometryPrefab").GetField("m_NodeStates").SetValue(target,Single(node));
 mod.GetType("BridgeBuilder.Bridges."+style+"NetworkData",true).GetMethod(style=="Lift03"?"AddMainStates":"AddMechanismStates",F).Invoke(null,new object[]{target});
 foreach(var item in new[]{("Edge",edge),("Node",node)}){
 var rules=(Array)T("NetGeometryPrefab").GetField("m_"+item.Item1+"States").GetValue(target);
 Need(ReferenceEquals(rules.GetValue(0),item.Item2),"Selected rule replaced");Need(Values(item.Item2).SequenceEqual(new[]{appearance}),"Selected appearance mutated");
 foreach(var rule in rules.Cast<object>().Skip(1))if(!record)Need(!Values(rule).Any(x=>x=="Tiles"||x=="Gravel"||x=="Pavement"),style+" prototype appearance leaked");
 string Key(object rule,string field)=>string.Join(",",((Array)rule.GetType().GetField(field).GetValue(rule)).Cast<object>().Select(x=>x.ToString()));
 var retained=rules.Cast<object>().Skip(1).Select(rule=>new{Rule=rule,States=Values(rule).Where(x=>!record||!(x=="Tiles"||x=="Gravel"||x=="Pavement")).ToArray()}).Where(x=>x.States.Length!=0);
 string signature=string.Join("|",retained.Select(x=>string.Join(",",x.States)+"/"+Key(x.Rule,"m_RequireAll")+"/"+Key(x.Rule,"m_RequireAny")+"/"+Key(x.Rule,"m_RequireNone")+"/"+(item.Item1=="Node"?x.Rule.GetType().GetField("m_MatchType").GetValue(x.Rule).ToString():"edge")));
 string key=style+":"+appearance+":"+item.Item1;
 if(record)evidence.Add(key+"\t"+signature);else Need(expected[key]==signature,"Non-appearance mechanism rule changed: "+key);
 }
 UnityEngine.Object.DestroyImmediate(target);cases++;
 }
 if(record){File.WriteAllLines(baseline,evidence);Debug.Log("PASS recorded original native132 state rules; rows="+evidence.Count);EditorApplication.Exit(0);return;}
 // Conditions, node match type and non-appearance mechanism states survive the common merge.
 var helper=mod.GetType("BridgeBuilder.Bridges.MovableNetworkStates",true);
 foreach(string kind in new[]{"Edge","Node"}){
 var source=Rule(kind,"Tiles");var extra=Rule(kind,"Pavement");extra.GetType().GetField("m_SetState").SetValue(extra,States("Pavement","Opening","Gravel","Tiles","FixedNodeSize"));extra.GetType().GetField("m_RequireAll").SetValue(extra,States("Elevated"));
 var method=helper.GetMethods(F).Single(m=>m.Name=="Append"&&m.GetParameters()[0].ParameterType.GetElementType()==source.GetType());
 var result=(Array)method.Invoke(null,new object[]{Single(source),Single(extra)});Need(result.Length==2,"Lost mixed rule");Need(Values(result.GetValue(1)).SequenceEqual(new[]{"Opening","FixedNodeSize"}),"Motion semantics changed");Need(Values(extra).Length==5,"Original rule mutated");
 Need(((Array)extra.GetType().GetField("m_RequireAll").GetValue(result.GetValue(1))).GetValue(0).ToString()=="Elevated","Condition lost");
 }
 Debug.Log("PASS "+cases+" actual generated state methods (six affected styles x three selected appearances), edge/node, source identity, retained mechanism state, mixed conditions and immutability; dll="+mod.Location);
 }
 EditorApplication.Exit(0);
 }catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
