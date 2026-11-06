using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

internal static class LifecycleCases
{
 private const BindingFlags Flags=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static;
 private const BindingFlags Instance=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance;
 internal static bool Capture;
 private static Dictionary<WeakReference,string> _labels=new Dictionary<WeakReference,string>();
 private static long _rawBytes;private static int _resources;
 private static List<WeakReference> _last=new List<WeakReference>();
 private sealed class Boundary:IDisposable
 {
  private IDisposable _lease;private object _session;
  internal Boundary(Type type){_lease=(IDisposable)type.GetMethod("Begin",Flags).Invoke(null,null);_session=type.GetMethod("Capture",Flags).Invoke(null,null);}
  [MethodImpl(MethodImplOptions.NoInlining)]public void Dispose()
  {
   var values=(IDictionary)_session.GetType().GetField("Values",Instance).GetValue(_session);
   if(Capture)_last=References(values);
   _lease.Dispose();
   if(values.Count!=0)throw new Exception("Completed request retained parameter cache entries.");
   _lease=null;_session=null;
  }
 }
 internal static object InvokeCaptured(Assembly assembly,Func<object> invoke)
 {
  if(!Capture)return invoke();
  object output=null;Exception failure=null;
  // Game Mono uses conservative stack scanning. Exit the producer thread before
  // asserting collection, so dead numerical locals cannot masquerade as owners.
  var thread=new Thread(()=>{try{assembly.GetType("BridgeBuilder.Bridges.AffineGeometry",true).GetField("_worker",Flags).SetValue(null,true);output=invoke();}catch(Exception error){failure=error;}});
  thread.Start();thread.Join();if(failure!=null)throw failure;return output;
 }
 internal static IDisposable Begin(Assembly assembly){var type=assembly.GetType("BridgeBuilder.Bridges.GenerationParameters");return type==null?null:new Boundary(type);}
 [MethodImpl(MethodImplOptions.NoInlining)]private static List<WeakReference> References(IDictionary values)
 {
  _labels.Clear();var result=new List<WeakReference>();var resources=new HashSet<object>();_rawBytes=0;_resources=0;
  foreach(DictionaryEntry entry in values)
  {
   object value=entry.Value;
   if(value==null||value.GetType().IsValueType||value is string||value is Array a&&a.Length==0)continue;
   var weak=new WeakReference(value);result.Add(weak);_labels[weak]=entry.Key.ToString();
   if(value.GetType().Name=="HardcodedNumbers"&&resources.Add(value))
   {
    _resources++;
    foreach(var field in value.GetType().GetFields(Instance))if(field.GetValue(value) is Array data&&data.Length!=0){var raw=new WeakReference(data);result.Add(raw);_labels[raw]=entry.Key+":"+field.Name+":"+data.Length;_rawBytes+=Buffer.ByteLength(data);}
   }
  }
  return result;
 }
 internal static void AssertReleased(object output,string report,string label)
 {
  GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
  var live=_last.Where(w=>w.IsAlive).Select(w=>w.Target.GetType().FullName+" "+_labels[w]).ToArray();
  GC.KeepAlive(output);
  if(live.Length!=0)throw new Exception("Parameters survive completed request with output alive: "+label+" "+string.Join(",",live.Distinct()));
  File.AppendAllText(report,"LIFETIME "+label+" released="+_last.Count+" cached roots/raw arrays; resources="+_resources+" typedArrayBytes="+_rawBytes+"; output alive\n");_last.Clear();
 }
 internal static void Run(Assembly assembly,string report)
 {
  var type=assembly.GetType("BridgeBuilder.Bridges.GenerationParameters");if(type==null)return;
  object Load()=>assembly.GetType("BridgeBuilder.Bridges.HardcodedNumbers",true).GetMethod("Load",Flags).Invoke(null,new object[]{"BridgeMeasurements_cs"});
  // Independent nested requests must not invalidate their outer request.
  using(var first=(IDisposable)type.GetMethod("Begin",Flags).Invoke(null,null))
  {
   var a=Load();if(!ReferenceEquals(a,Load()))throw new Exception("No request-local reuse.");
   using(var second=(IDisposable)type.GetMethod("Begin",Flags).Invoke(null,null))if(ReferenceEquals(a,Load()))throw new Exception("Independent request reused another request's data.");
   if(!ReferenceEquals(a,Load()))throw new Exception("Nested completion invalidated parent.");
  }
  Capture=true;object catalogue;
  using(var scope=Begin(assembly))catalogue=assembly.GetType("BridgeBuilder.Bridges.BridgeStyleCatalog",true).GetMethod("CreateNamedStyles",Flags).Invoke(null,null);
  AssertReleased(catalogue,report,"catalogue metadata");Capture=false;
  var parent=(IDisposable)type.GetMethod("Begin",Flags).Invoke(null,null);var session=type.GetMethod("Capture",Flags).Invoke(null,null);
  object expected=Load();var attached=new ManualResetEvent(false);var finish=new ManualResetEvent(false);Exception failure=null;
  var worker=new Thread(()=>{try{using(var lease=(IDisposable)type.GetMethod("Attach",Flags).Invoke(null,new[]{session})){if(!ReferenceEquals(expected,Load()))throw new Exception("Worker did not inherit request.");attached.Set();finish.WaitOne();if(!ReferenceEquals(expected,Load()))throw new Exception("Parent completion invalidated active worker.");}}catch(Exception e){failure=e;attached.Set();}});
  worker.Start();attached.WaitOne();parent.Dispose();var values=(IDictionary)session.GetType().GetField("Values",Instance).GetValue(session);
  if(values.Count==0)throw new Exception("Released active worker parameters.");finish.Set();worker.Join();if(failure!=null)throw failure;if(values.Count!=0)throw new Exception("Last worker retained parameters.");
  Capture=true;try{using(var scope=Begin(assembly)){Load();throw new InvalidOperationException("controlled failure");}}catch(InvalidOperationException){}AssertReleased(new object(),report,"exception");Capture=false;
  Capture=true;try{using(var scope=Begin(assembly)){Load();throw new OperationCanceledException("controlled cancellation");}}catch(OperationCanceledException){}AssertReleased(new object(),report,"cancellation");Capture=false;
  File.AppendAllText(report,"PASS request lifetime: nested isolation, cache reuse, cross-thread overlap, last-worker release, exception release; GC only in test\n");
 }
}
