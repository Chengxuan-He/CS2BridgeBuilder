using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using UnityEngine;
using Unity.Mathematics;

internal static class CoreCases
{
    private const BindingFlags Static=BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    private const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    internal static void Run(Assembly before,Assembly after,string report)
    {
        using var parameterScope = LifecycleCases.Begin(after);
        var numbers=after.GetType("BridgeBuilder.Bridges.HardcodedNumbers",true);
        var load=numbers.GetMethod("Load",Static);
        var data=load.Invoke(null,new object[]{"StructurePrograms"});
        var program=after.GetType("BridgeBuilder.Bridges.StructureCoordinateProgram",true);
        var constructor=program.GetConstructors(Instance).Single();
        int decoded=0;
        foreach(string resource in after.GetManifestResourceNames().Where(n=>n.StartsWith("BridgeBuilder.Numbers.") && n.EndsWith(".bin")))
        {
            string key=resource.Substring("BridgeBuilder.Numbers.".Length);key=key.Substring(0,key.Length-4);
            var table=load.Invoke(null,new object[]{key});var words=(int[])numbers.GetField("I32",Instance).GetValue(table);
            if(words.Length!=0 && words[0]==0x31465041)throw new Exception("Legacy APF1 resource shipped.");
            if(words.Length!=0 && words[0]==0x32465041){constructor.Invoke(new[]{table});decoded++;}
        }
        if(decoded==0)throw new Exception("No packed programs decoded.");
        var execute=after.GetType("BridgeBuilder.Bridges.AffineGeometry",true).GetMethod("Execute",Static).MakeGenericMethod(typeof(Vector3));
        string oraclePath=Environment.GetEnvironmentVariable("BB_AFFINE_SCALAR_ORACLE");
        if(!string.IsNullOrEmpty(oraclePath))before=Assembly.Load(File.ReadAllBytes(oraclePath));
        var original=before.GetType("BridgeBuilder.Bridges.AffineGeometry",true).GetMethod("AddScaled",Static);
        void Exec(int entry,Vector3[][] coordinates,float[][] columns,float[] parameters,int count,int[][] rows)
            =>execute.Invoke(null,new object[]{data,entry,coordinates,columns,parameters,0,count,rows});
        float Scalar(float a,float b,float c)=>(float)original.Invoke(null,new object[]{a,b,c});
        int cases=0;
        float[] values={0f,BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)),float.Epsilon,-float.Epsilon,
            BitConverter.Int32BitsToSingle(0x00800000),-BitConverter.Int32BitsToSingle(0x00800000),float.MaxValue,-float.MaxValue,1f,-1f,1.00000012f,-.99999994f};
        void Equal(Vector3[] expected,Vector3[] actual,string label)
        {
            for(int i=0;i<expected.Length;i++)for(int axis=0;axis<3;axis++)
                if(BitConverter.SingleToInt32Bits(expected[i][axis])!=BitConverter.SingleToInt32Bits(actual[i][axis]))
                    throw new Exception("Core kernel "+label+" row="+i+" axis="+axis);
            cases++;
        }
        for(int count=0;count<16;count++)foreach(bool alias in new[]{false,true})
        {
            var source=new Vector3[16];for(int i=0;i<source.Length;i++)source[i]=new Vector3(values[i%values.Length],i-.5f,values[(i+3)%values.Length]);
            var a=Enumerable.Range(0,count).Select(i=>(i*3)%16).ToArray();
            var b=Enumerable.Range(0,count).Select(i=>(i*7+1)%16).ToArray();
            var output=Enumerable.Range(0,count).Select(i=>(i*5)%11).ToArray();
            var fractions=Enumerable.Range(0,count).Select(i=>new[]{0f,.25f,.5f,1f}[i%4]).ToArray();
            var destination=alias?source:new Vector3[16];var expected=(Vector3[])destination.Clone();var snapshot=(Vector3[])source.Clone();
            for(int i=0;i<count;i++)expected[output[i]]=Vector3.LerpUnclamped(snapshot[a[i]],snapshot[b[i]],fractions[i]);
            Exec(7,new[]{source,destination},new[]{fractions},Array.Empty<float>(),count,new[]{a,b,output});
            Equal(expected,destination,"edge XYZ alias="+alias+" count="+count);
            foreach(float delta in new[]{BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)),8.5f,float.MaxValue})
            {
                source=(Vector3[])snapshot.Clone();destination=alias?source:new Vector3[16];expected=(Vector3[])destination.Clone();
                var weights=Enumerable.Range(0,count).Select(i=>values[(i+4)%values.Length]).ToArray();
                for(int i=0;i<count;i++)expected[a[i]].x=Scalar(snapshot[a[i]].x,weights[i],delta);
                Exec(12,new[]{source,destination},new[]{weights},new[]{delta},count,new[]{a});
                Equal(expected,destination,"row delta alias="+alias+" count="+count);
            }
        }
        var repeated=new[]{2,1,2,2,1,0,2};var coefficient=new[]{.25f,-.5f,1f};
        var sequential=new[]{new Vector3(-0f,1,2),new Vector3(3,4,5),new Vector3(6,7,8)};
        var expectation=(Vector3[])sequential.Clone();foreach(int row in repeated)expectation[row].x=Scalar(expectation[row].x,coefficient[row],8.5f);
        var stages=(int[][])after.GetType("BridgeBuilder.Bridges.StructureRows",true).GetMethod("Stages",Static).Invoke(null,new object[]{repeated});
        foreach(var stage in stages)Exec(1,new[]{sequential,sequential},new[]{coefficient},new[]{8.5f},stage.Length,new[]{stage});
        Equal(expectation,sequential,"sequential duplicate destination stages");
        for(int count=0;count<17;count++)
        {
            float negativeZero=BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
            float payload=BitConverter.Int32BitsToSingle(unchecked((int)0x7fc01234));
            var source=Enumerable.Range(0,24).Select(i=>new Vector3(i,payload,negativeZero)).ToArray();
            var destination=(Vector3[])source.Clone();var expected=(Vector3[])source.Clone();
            var weights=Enumerable.Repeat(.25f,24).ToArray();
            for(int i=3;i<3+count;i++)expected[i].x=Scalar(source[i].x,weights[i],8f);
            execute.Invoke(null,new object[]{data,15,new[]{source,destination},new[]{weights},new[]{8f},3,count,null});
            Equal(expected,destination,"first offset and untouched NaN/signed-zero axes count="+count);
        }
        var originalWords=(int[])numbers.GetField("I32",Instance).GetValue(data);int first=originalWords[3];int invalid=0;
        foreach(var change in new[]{(0,0),(0,0x31465041),(3,-1),(first,-2),(first+1,0),(first+3,2),(first+4,-1),(first+5+1,-1),(first+5+2,-1)})
        {
            var bad=FormatterServices.GetUninitializedObject(numbers);
            foreach(var field in numbers.GetFields(Instance))field.SetValue(bad,field.GetValue(data));
            var words=(int[])originalWords.Clone();words[change.Item1]=change.Item2;numbers.GetField("I32",Instance).SetValue(bad,words);
            try{constructor.Invoke(new[]{bad});throw new Exception("Malformed numerical program accepted.");}
            catch(TargetInvocationException error)when(error.InnerException is InvalidDataException){invalid++;}
        }
        File.AppendAllText(report,"PASS common kernels="+cases+" tails=0..15 signedZero/subnormal/extreme/cancellation/multilane/alias/duplicate; decodedPrograms="+decoded+" malformedRejected="+invalid+"\n");
        // This historical family is not present in the installed game's native
        // archives. Exercise every recorded LOD map, but label its coordinates synthetic.
        int historical=0;
        var oldType=before.GetType("BridgeBuilder.Bridges.TrussArch01Geometry",true);
        var newType=after.GetType(oldType.FullName,true);
        foreach(var selection in new[]{("SectionMaps","TryWidenSection"),("PierMaps","TryWidenPier")})
        foreach(System.Collections.DictionaryEntry item in (System.Collections.IDictionary)oldType.GetField(selection.Item1,Static).GetValue(null))
        {
            int count=(int)item.Value.GetType().GetField("_vertices",Instance).GetValue(item.Value);
            var source=new float3[count];for(int i=0;i<count;i++)source[i]=new float3(values[i%values.Length],i%7,-i%11);
            foreach(float delta in new[]{-2f,BitConverter.Int32BitsToSingle(unchecked((int)0x80000000)),0f,8.5f,float.MaxValue})
            {
                object[] a={item.Key,source,delta,null},b={item.Key,source,delta,null};
                if(!(bool)oldType.GetMethod(selection.Item2,Static).Invoke(null,a)||!(bool)newType.GetMethod(selection.Item2,Static).Invoke(null,b))throw new Exception("Historical map not recognized.");
                var x=(float3[])a[3];var y=(float3[])b[3];
                for(int i=0;i<count;i++)for(int axis=0;axis<3;axis++)
                    if(BitConverter.SingleToInt32Bits(x[i][axis])!=BitConverter.SingleToInt32Bits(y[i][axis]))throw new Exception("Historical map bits differ: "+item.Key+"/"+i);
                historical++;
            }
        }
        File.AppendAllText(report,"PASS historical TrussArch01 recorded maps="+historical+" synthetic-coordinate cases; native payload unavailable in installed archives, no native-geometry acceptance claimed\n");
    }
}
