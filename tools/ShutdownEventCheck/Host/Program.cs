using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
class Host {
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate void SetPath([MarshalAs(UnmanagedType.LPStr)]string path);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Init([MarshalAs(UnmanagedType.LPStr)]string name,[MarshalAs(UnmanagedType.LPStr)]string version);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Open(IntPtr domain,[MarshalAs(UnmanagedType.LPStr)]string path);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Image(IntPtr assembly);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Klass(IntPtr image,[MarshalAs(UnmanagedType.LPStr)]string ns,[MarshalAs(UnmanagedType.LPStr)]string name);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Method(IntPtr klass,[MarshalAs(UnmanagedType.LPStr)]string name,int args);
 [UnmanagedFunctionPointer(CallingConvention.Cdecl)] delegate IntPtr Invoke(IntPtr method,IntPtr instance,IntPtr args,out IntPtr exception);
 static void Main(string[] args){
 var lib=NativeLibrary.Load(Path.Combine(args[0],"MonoBleedingEdge/EmbedRuntime/mono-2.0-bdwgc.dll"));
 T Fn<T>(string name) where T:Delegate => Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(lib,name));
 Fn<SetPath>("mono_set_assemblies_path")(Path.Combine(args[0],"Cities2_Data/Managed"));
 var domain=Fn<Init>("mono_jit_init_version")("shutdown-event-check","v4.0.30319");
 foreach(var file in new[]{args[1],args[2]}){var assembly=Fn<Open>("mono_domain_assembly_open")(domain,Path.GetFullPath(file));if(assembly==IntPtr.Zero)throw new Exception("Cannot load "+file);if(file==args[2]){
 var image=Fn<Image>("mono_assembly_get_image")(assembly);var type=Fn<Klass>("mono_class_from_name")(image,"","GameMonoProbe");var method=Fn<Method>("mono_class_get_method_from_name")(type,"Run",0);Fn<Invoke>("mono_runtime_invoke")(method,IntPtr.Zero,IntPtr.Zero,out var exception);if(exception!=IntPtr.Zero)throw new Exception("Managed probe failed");}}
 Console.WriteLine("Game Mono probe completed.");
 }
}
