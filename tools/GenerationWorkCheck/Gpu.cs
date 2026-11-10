using System;
using System.Threading;
using BridgeBuilder.Bridges;
using UnityEngine;
public sealed class GenerationGpuCheck
{
 private readonly GenerationWork _work;
 private readonly GenerationGpuLease _lease = new();
 private readonly int[] _output = new int[1048576];
 private readonly bool _cancel;
 private bool _released;
 private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
 private int _ticks;
 public GenerationGpuCheck(bool cancel)
 {
  _cancel=cancel;
  var input = new int[_output.Length]; for(int i=0;i<input.Length;i++)input[i]=i^0x12345;
  var buffer=new ComputeBuffer(input.Length,4);buffer.SetData(input);_lease.Buffers.Add(buffer);
  _lease.ReleaseShaders=()=>{if(Thread.CurrentThread.ManagedThreadId!=_thread)throw new Exception("GPU release left main thread");_released=true;};
  _work=new GenerationWork("gpu-cancel="+cancel,()=>true);
  _work.Start(async()=>{try{await _lease.Read(buffer,_output);}finally{_lease.Dispose();}});
  if(cancel)
  {
   _work.Cancel();
   if(!_work.IsComplete)throw new Exception("Cancellation waited for GPU");
   try{_work.Completion!.GetAwaiter().GetResult();throw new Exception("Expected cancellation");}
   catch(OperationCanceledException){}
  }
 }
 public bool Tick()
 {
  _ticks++;
  // Cancel case deliberately never pumps again, simulating destroyed owning ECS system.
  if(!_cancel)_work.Tick();
  if(!_released)return false;
  if(!_work.IsComplete)throw new Exception("Released before work completed");
  if(_lease.Buffers.Count!=0)throw new Exception("GPU buffers retained");
  if(!_cancel)
  {
   _work.Completion!.GetAwaiter().GetResult();
   for(int i=0;i<_output.Length;i++)if(_output[i]!=(i^0x12345))throw new Exception("Readback mismatch");
  }
  else for(int i=0;i<_output.Length;i++)if(_output[i]!=0)throw new Exception("Cancelled GPU published data");
  _work.Report();Console.WriteLine("PASS GPU lease cancel="+_cancel+" ticks="+_ticks+" bytes="+_output.Length*4);return true;
 }
}
