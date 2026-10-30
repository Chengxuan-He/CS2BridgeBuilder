using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class RepeatPanelCheck
{
    public static void Run()
    {
        try
        {
            var fixtureRoot="C:/Users/admin/Documents/Codex/CS2BridgeBuilder/artifacts/material-remap/PedestrianDraw02/";
            var authored=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(System.IO.File.ReadAllText(fixtureRoot+"piecewise-repeat-manifest.json"));
            var guards=JsonUtility.FromJson<PrepareRepeatUnits.Manifest>(System.IO.File.ReadAllText(fixtureRoot+"finite-guard-recipe.json"));
            var guardMethod=typeof(PrepareRepeatUnits).GetMethod("Guard",BindingFlags.Static|BindingFlags.NonPublic);
            for(int i=0;i<authored.units.Length;i++)
            {
                var unit=authored.units[i];var guard=guards.units[i].guardPanels[0];
                guardMethod.Invoke(null,new object[]{unit,guard});
                var wrong=new PrepareRepeatUnits.GuardPanel{adjacentPanel=guard.adjacentPanel,target=guard.target,source=(double[])guard.source.Clone()};
                wrong.source[0]+=.001;
                try{guardMethod.Invoke(null,new object[]{unit,wrong});throw new Exception("Invalid guard source accepted.");}
                catch(TargetInvocationException e)when(e.InnerException is System.IO.InvalidDataException){}
            }
            var source=new Texture2D(16,16,TextureFormat.RGBA32,true,true){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            for(int mip=0;mip<source.mipmapCount;mip++)
            {
                int n=Math.Max(1,16>>mip);var pixels=new Color32[n*n];
                for(int y=0;y<n;y++)for(int x=0;x<n;x++)pixels[y*n+x]=new Color32((byte)(x*13),(byte)(y*13),(byte)(mip*40),255);
                source.SetPixels32(pixels,mip);
            }
            source.Apply(false,false);
            PrepareRepeatUnits.Panel P(double[] t,double[] s)=>new PrepareRepeatUnits.Panel{target=t,source=s};
            var panels=new[]{P(new[]{0d,0,.5,0,.5,1},new[]{0d,0,.25,0,.25,1}),
                P(new[]{0d,0,.5,1,0,1},new[]{0d,0,.25,1,0,1}),
                P(new[]{.5,0,1,0,1,1},new[]{.25,0,1,0,1,1}),
                P(new[]{.5,0,1,1,.5,1},new[]{.25,0,1,1,.25,1})};
            var method=typeof(PrepareRepeatUnits).GetMethod("Extract",BindingFlags.Static|BindingFlags.NonPublic);
            var shader=AssetDatabase.LoadAssetAtPath<ComputeShader>("Assets/RepeatUnit.compute");
            Texture2D Bake(PrepareRepeatUnits.Panel[] ps)=>(Texture2D)method.Invoke(null,new object[]{shader,source,0d,0d,16d,16d,48,16,1,1,null,false,true,false,false,null,true,ps});
            foreach(var unit in authored.units)
            {
                unit.guardPanels=guards.units[Array.FindIndex(authored.units,u=>u.id==unit.id)].guardPanels;
                var sampling=typeof(PrepareRepeatUnits).GetMethod("SamplingPanels",BindingFlags.Static|BindingFlags.NonPublic);
                var withGuard=(PrepareRepeatUnits.Panel[])sampling.Invoke(null,new object[]{unit});
                UnityEngine.Object.DestroyImmediate(Bake(withGuard));
            }
            var result=Bake(panels);var actual=result.GetPixels32(0);var original=source.GetPixels32(0);
            for(int y=0;y<16;y++)for(int x=0;x<48;x++)
            {
                double q=Math.Max(0,Math.Min(1,2*(x+.5)/48-.5));
                double u=q<=.5?q*.5:.25+(q-.5)*1.5;
                var expected=original[y*16+Math.Min(15,(int)(u*16))];
                if(!actual[y*48+x].Equals(expected))throw new Exception("Piecewise mapping or finite guard sampling differs at "+x+","+y);
            }
            if(result.mipmapCount!=6)throw new Exception("Incomplete panel unit mip chain.");
            try{var invalid=Bake(new[]{panels[0]});UnityEngine.Object.DestroyImmediate(invalid);throw new Exception("Uncovered panel output silently sampled an unrelated crop.");}
            catch(TargetInvocationException e) when(e.InnerException is System.IO.InvalidDataException) { }
            UnityEngine.Object.DestroyImmediate(result);UnityEngine.Object.DestroyImmediate(source);
            Debug.Log("PASS offline piecewise panels: four authored triangles, two distinct Jacobians, finite guards, native complete mips and uncovered-domain failure; no runtime receiver geometry.");
            EditorApplication.Exit(0);
        }
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
