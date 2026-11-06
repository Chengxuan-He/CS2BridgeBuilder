using System;
using System.IO;
using System.Runtime.InteropServices;

// Embed the installed game's Mono and core library without starting Cities2 or
// changing JIT options. Unity editor Mono has different float evaluation rules.
internal static class Host
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void SetPath([MarshalAs(UnmanagedType.LPStr)] string path);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Init([MarshalAs(UnmanagedType.LPStr)] string name,[MarshalAs(UnmanagedType.LPStr)] string version);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Open(IntPtr domain,[MarshalAs(UnmanagedType.LPStr)] string path);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Image(IntPtr assembly);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Klass(IntPtr image,[MarshalAs(UnmanagedType.LPStr)] string ns,[MarshalAs(UnmanagedType.LPStr)] string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Method(IntPtr klass,[MarshalAs(UnmanagedType.LPStr)] string name,int count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr Invoke(IntPtr method,IntPtr instance,IntPtr args,out IntPtr exception);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void AddCall([MarshalAs(UnmanagedType.LPStr)] string name,IntPtr function);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate float Determinant(IntPtr matrix);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr JitInfo(IntPtr domain,IntPtr address);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int CodeSize(IntPtr info);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr CodeStart(IntPtr info);
    private static readonly Determinant ValidationDeterminant = pointer => Marshal.PtrToStructure<System.Numerics.Matrix4x4>(pointer).GetDeterminant();
    private static void Main(string[] args)
    {
        if(args.Length!=6)throw new ArgumentException("GAME_DIRECTORY PROBE_DLL BASELINE_DLL CANDIDATE_DLL MANIFEST_TSV REPORT");
        string game=Path.GetFullPath(args[0]),managed=Path.Combine(game,"Cities2_Data/Managed");
        string[] names={"BB_AFFINE_BASELINE","BB_AFFINE_CANDIDATE","BB_AFFINE_MANIFEST","BB_AFFINE_REPORT"};
        for(int i=0;i<names.Length;i++)Environment.SetEnvironmentVariable(names[i],Path.GetFullPath(args[i+2]));
        var library=NativeLibrary.Load(Path.Combine(game,"MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll"));
        NativeLibrary.Load(Path.Combine(game,"MonoBleedingEdge/EmbedRuntime/MonoPosixHelper.dll"));
        T Function<T>(string name) where T:Delegate=>Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library,name));
        Function<SetPath>("mono_set_assemblies_path")(managed);
        var domain=Function<Init>("mono_jit_init_version")("affine-native-check","v4.0.30319");
        // Only Plan.Validate's finite-determinant gate is supplied by this host.
        // This is not Unity engine validation or a replacement for native mesh tests.
        Function<AddCall>("mono_add_internal_call")("UnityEngine.Matrix4x4::GetDeterminant_Injected",Marshal.GetFunctionPointerForDelegate(ValidationDeterminant));
        Environment.SetEnvironmentVariable("BB_AFFINE_VALIDATION_ADAPTER","System.Numerics.Matrix4x4 determinant; Plan finite-validation only; no geometry arithmetic replaced");
        var assembly=Function<Open>("mono_domain_assembly_open")(domain,Path.GetFullPath(args[1]));
        if(assembly==IntPtr.Zero)throw new Exception("Cannot load the managed probe.");
        var image=Function<Image>("mono_assembly_get_image")(assembly);
        var type=Function<Klass>("mono_class_from_name")(image,"","GameMonoProbe");
        var method=Function<Method>("mono_class_get_method_from_name")(type,"Run",0);
        Function<Invoke>("mono_runtime_invoke")(method,IntPtr.Zero,IntPtr.Zero,out var error);
        if(error!=IntPtr.Zero)throw new Exception("Managed comparison failed; see "+args[5]);
        var pointer=new IntPtr(long.Parse(File.ReadAllText(args[5]+".pointer")));
        var info=Function<JitInfo>("mono_jit_info_table_find")(domain,pointer);
        if(info==IntPtr.Zero)throw new Exception("Packed arithmetic JIT extent unavailable.");
        int size=Function<CodeSize>("mono_jit_info_get_code_size")(info);
        var code=new byte[size];Marshal.Copy(Function<CodeStart>("mono_jit_info_get_code_start")(info),code,0,size);
        File.WriteAllBytes(args[5]+".machine-code.bin",code);
        File.AppendAllText(args[5],"actual packed Arithmetic JIT body bytes="+size+"; inspect paired machine-code artifact\n");
        Console.WriteLine(File.ReadAllText(args[5]));
    }
}
