using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEditor;

public static class Draw01HeadCompositionProbe
{
    public static void Run()
    {
        try
        {
            var args = Environment.GetCommandLineArgs();
            string Arg(string name) => args[Array.IndexOf(args, name) + 1];
            AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
            {
                var file = Path.Combine(Arg("-gameManaged"), new AssemblyName(e.Name).Name + ".dll");
                return File.Exists(file) ? Assembly.LoadFrom(file) : null;
            };
            var mod = Assembly.LoadFrom(Arg("-modAssembly"));
            var burst = Assembly.LoadFrom(Path.Combine(Arg("-gameManaged"), "Unity.Burst.dll"));
            var compiler = burst.GetType("Unity.Burst.BurstCompiler", true);
            var options = compiler.GetField("Options", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null)
                ?? compiler.GetProperty("Options", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(null);
            options?.GetType().GetProperty("EnableBurstCompilation")?.SetValue(options, false);
            var type = mod.GetType("BridgeBuilder.Runtime.BridgePreviewComposition", true);
            using var native = (IDisposable)Activator.CreateInstance(type,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new object[] { true }, null);
            Debug.Log("PASS native composition ECS world initialization; no fixture generation asserted.");
            EditorApplication.Exit(0);
        }
        catch (Exception exception) { Debug.LogException(exception); EditorApplication.Exit(1); }
    }
}
