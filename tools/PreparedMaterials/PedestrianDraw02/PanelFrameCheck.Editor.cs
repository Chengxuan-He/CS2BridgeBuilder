using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Offline original native encoder evidence. No game world or bridge generation.
public static class PedestrianPanelFrameCheck
{
    public static void Run()
    {
        var args=Environment.GetCommandLineArgs();
        string managed=args[Array.IndexOf(args,"-gameManaged")+1];
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
        if(normal!=2147418112u)throw new Exception("Original panel normal frame differs.");
        foreach(float sign in new[]{-1f,1f})foreach(float hand in new[]{-1f,1f})
        {
            uint bits=Convert.ToUInt32(math.GetMethod("TangentToOctahedral",flags).Invoke(null,new[]{F4(sign,0,0,hand)}));
            Debug.Log($"PED02 original frame candidate X={sign}, handedness={hand}, bits={bits}");
        }
        Debug.Log("PASS original panel normal +Y encoded identically; tangent candidates reported.");
    }
}
