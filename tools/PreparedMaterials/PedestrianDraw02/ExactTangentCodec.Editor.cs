using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class PedestrianExactTangentCodec
{
    [Serializable] public sealed class Sample {public int domain;public float[] position,source,coefficients;}
    [Serializable] public sealed class Evidence {public Sample[] samples;}
    [Serializable] public sealed class Packed {public int domain;public uint actual,source;public Vector3 evaluated,ideal;}
    [Serializable] public sealed class PackedReport {public Packed[] samples;}
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();string Arg(string n)=>args[Array.IndexOf(args,n)+1];
        string managed=Arg("-gameManaged");
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=>{string p=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");return File.Exists(p)?Assembly.LoadFrom(p):null;};
        var mathematics=Assembly.LoadFrom(Path.Combine(managed,"Unity.Mathematics.dll"));
        var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils");
        var encode=math.GetMethod("TangentToOctahedral",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
        object F4(Vector3 v)=>Activator.CreateInstance(mathematics.GetType("Unity.Mathematics.float4"),new object[]{v.x,v.y,v.z,-1f});
        var evidence=JsonUtility.FromJson<Evidence>(File.ReadAllText(Arg("-evidence")));
        int different=0;double maxEvaluation=0,maxStorage=0;
        var packed=new System.Collections.Generic.List<Packed>();
        foreach(var s in evidence.samples) {
            var c=s.coefficients;var p=s.position;
            var actual=new Vector3(c[0]*p[0]+c[3]*p[2]+c[6],c[1]*p[0]+c[4]*p[2]+c[7],c[2]*p[0]+c[5]*p[2]+c[8]);
            var source=new Vector3(s.source[0],s.source[1],s.source[2]);
            for(int j=0;j<3;j++) {
                double exact=(double)c[j]*p[0]+(double)c[3+j]*p[2]+c[6+j];
                maxEvaluation=Math.Max(maxEvaluation,Math.Abs(actual[j]-exact));
                maxStorage=Math.Max(maxStorage,Math.Abs(exact-source[j]));
            }
            uint a=Convert.ToUInt32(encode.Invoke(null,new[]{F4(actual)})),b=Convert.ToUInt32(encode.Invoke(null,new[]{F4(source)}));
            packed.Add(new Packed{domain=s.domain,actual=a,source=b,evaluated=actual,ideal=source});
            if(a!=b){different++;if(different<8)Debug.Log($"PED02 FIELD different domain={s.domain} position={new Vector3(p[0],p[1],p[2]):R} actual={actual:R} source={source:R} bits={a}/{b}");}
        }
        Debug.Log($"PED02 FIELD samples={evidence.samples.Length} nativePackedDifferences={different} maxFloatEvaluation={maxEvaluation:R} maxCoefficientStorageAndInputRounding={maxStorage:R}; raw rational fitting error is zero; no pre-normalization");
        File.WriteAllText(Arg("-evidence")+".packed.json",JsonUtility.ToJson(new PackedReport{samples=packed.ToArray()},true));
        if(different!=0)throw new Exception("Original native packed samples differ; inspect local quantization bound before acceptance.");
    }
}
