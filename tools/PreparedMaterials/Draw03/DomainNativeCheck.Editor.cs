using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Executes the source-linked production clipper inside Unity, including native
// Matrix4x4 validation. No texture bake or generated asset write is performed.
public static class Draw03DomainNativeCheck
{
    [Serializable] public sealed class Unit { public string key, sourceDigest; }
    [Serializable] public sealed class Inventory { public Unit[] units; }
    public static void Run()
    {
        var args = Environment.GetCommandLineArgs();
        string Arg(string name) => args[Array.IndexOf(args, name) + 1];
        string managed = Arg("-gameManaged");
        ResolveEventHandler resolve = (_, e) => {
            string file = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(file) ? Assembly.LoadFrom(file) : null;
        };
        AppDomain.CurrentDomain.AssemblyResolve += resolve;
        try
        {
            var inventory = JsonUtility.FromJson<Inventory>(File.ReadAllText(Arg("-repeatInventory")));
            var assembly = Assembly.LoadFrom(Arg("-checkAssembly"));
            var result = (string)assembly.GetType("NativeEntry", true).GetMethod("Run")
                .Invoke(null, new object[] { inventory.units.Select(u => u.key).ToArray(), inventory.units[0].sourceDigest });
            File.WriteAllText(Arg("-checkReport"), result + Environment.NewLine);
            Debug.Log(result);
        }
        finally { AppDomain.CurrentDomain.AssemblyResolve -= resolve; }
    }
}
