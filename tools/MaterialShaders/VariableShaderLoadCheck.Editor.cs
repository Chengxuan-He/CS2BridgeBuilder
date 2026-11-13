using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
public static class VariableShaderLoadCheck
{
    public static void Run()
    {
        var root=Environment.GetEnvironmentVariable("BB_SHADER_CHECK_OUTPUT");
        var report=new StringBuilder();
        try
        {
            foreach(var name in new[]{"BoneDecal","WorldLayeredPbr"})
            foreach(var suffix in new[]{"actual","compiler-0","compiler-4","compiler-short"})
            {
                var file=Path.Combine(root,name+"-"+suffix+".bundle");
                var bundle=AssetBundle.LoadFromMemory(File.ReadAllBytes(file));
                if(bundle==null)throw new Exception("Load failed: "+file);
                try
                {
                    var shaders=bundle.LoadAllAssets<Shader>();
                    if(shaders.Length!=1)throw new Exception("Shader count: "+shaders.Length);
                    var shader=shaders[0];
                    foreach(var message in ShaderUtil.GetShaderMessages(shader))
                        if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
                    var material=new Material(shader);
                    report.AppendLine("PASS "+Path.GetFileName(file)+" shader="+shader.name+" supported="+shader.isSupported+" passes="+material.passCount);
                    UnityEngine.Object.DestroyImmediate(material);
                }
                finally { bundle.Unload(true); }
            }
            File.WriteAllText(Path.Combine(root,"unity-load-check.txt"),report.ToString());EditorApplication.Exit(0);
        }
        catch(Exception e){File.WriteAllText(Path.Combine(root,"unity-load-check.txt"),report+"\nFAIL "+e);Debug.LogException(e);EditorApplication.Exit(1);}
    }
}
