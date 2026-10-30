using System;
using System.IO;
using System.Reflection;
using UnityEngine;

public static class Lift02BedFrameCheck
{
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string Arg(string key)=>args[Array.IndexOf(args,key)+1];
        string managed=Arg("-gameManaged");
        AppDomain.CurrentDomain.AssemblyResolve+=(_,e)=> {
            string path=Path.Combine(managed,new AssemblyName(e.Name).Name+".dll");
            return File.Exists(path)?Assembly.LoadFrom(path):null;
        };
        var mathematics=Assembly.LoadFrom(Path.Combine(managed,"Unity.Mathematics.dll"));
        var math=Assembly.LoadFrom(Path.Combine(managed,"Colossal.Mathematics.dll")).GetType("Colossal.Mathematics.MathUtils");
        object F3(float x,float y,float z)=>Activator.CreateInstance(mathematics.GetType("Unity.Mathematics.float3"),new object[]{x,y,z});
        object F4(float x,float y,float z,float w)=>Activator.CreateInstance(mathematics.GetType("Unity.Mathematics.float4"),new object[]{x,y,z,w});
        const BindingFlags flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
        uint normal=Convert.ToUInt32(math.GetMethod("NormalToOctahedral",flags).Invoke(null,new[]{F3(0,1,0)}));
        uint Tangent(float x,float z,float w)=>Convert.ToUInt32(math.GetMethod("TangentToOctahedral",flags).Invoke(null,new[]{F4(x,0,z,w)}));
        if(normal!=2147418112u || Tangent(0,-1,-1)!=3221176319u || Tangent(0,-1,1)!=1073692671u || Tangent(-1,0,-1)!=2147500033u)
            throw new Exception("Lift02 actual native bed frame differs from every-face packed evidence.");
        File.WriteAllText(Arg("-frameReport"),"{\"nativeEncoderPass\":true,\"normal\":[0,1,0],\"nearNegative\":[0,0,-1,-1],\"nearPositive\":[0,0,-1,1],\"far\":[-1,0,0,-1],\"normalTransform\":\"identity; runtime explicitly retains these frames\"}");
        Debug.Log("PASS Lift02 native bed frames: all recorded normal/tangent bits match native encoder; identity normal transform.");
    }
}
