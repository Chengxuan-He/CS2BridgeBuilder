using System;
using System.IO;
using System.Linq;
using UnityEngine;
using BridgeBuilder.Bridges;

public static class Lift02NativeBindingCheck
{
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        var inventory=JsonUtility.FromJson<PrepareStructure.Inventory>(File.ReadAllText(Arg("-structureInventory")));
        var units=inventory.units.Where(u=>u.style=="Lift02").ToArray();int accepted=0,preciseRejected=0,expandedRejected=0;
        foreach(var u in units){
            if(!Lift02NativeStructureCatalogue.SamplerSources.TryGetValue(u.key,out var cid)||cid!=u.sourceCid)throw new Exception("Sampler identity differs.");
            var unit=u.Sampler();unit.Points.Add(Vector2.zero);unit.Points.Add(new Vector2(.25f,0));unit.Points.Add(new Vector2(0,.25f));
            var original=new object();
            try{var result=Lift02NativeStructureBinding.Bind(original,u.sourceDigest,unit);if(!ReferenceEquals(result,original))throw new Exception("Cloned original source.");accepted++;}
            catch(NotSupportedException){if(unit.CanUseSource)throw;preciseRejected++;}
            unit.Points.Clear();unit.Points.Add(Vector2.zero);unit.Points.Add(new Vector2(2,0));unit.Points.Add(new Vector2(0,2));
            try{Lift02NativeStructureBinding.Bind(original,u.sourceDigest,unit);throw new Exception("Unsafe expanded receiver was accepted.");}
            catch(NotSupportedException){expandedRejected++;}
        }
        if(units.Length!=Lift02NativeStructureCatalogue.SamplerSources.Count)throw new Exception("Uncovered catalogue entries.");
        string resultText=$"PASS Lift02 native binder: {units.Length} exact sampler identities; {accepted} bounded original-object bindings; {preciseRejected} precision-rejected interiors; {expandedRejected} expanded receivers explicitly rejected; no material/texture creation. This is code validation, not persisted shader or gameplay acceptance.";
        File.WriteAllText(Arg("-bindingReport"),resultText+Environment.NewLine);Debug.Log(resultText);
    }
}
