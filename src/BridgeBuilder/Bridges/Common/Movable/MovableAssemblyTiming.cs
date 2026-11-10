using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace BridgeBuilder.Bridges;

// Observational only: no per-vertex timestamps, retained assets or global request state.
internal sealed class MovableAssemblyTiming
{
    private sealed class Row
    {
        internal readonly Dictionary<string,long> Ticks = new();
        internal readonly Dictionary<string,long> Counts = new();
    }
    private readonly Dictionary<int,Row> _rows = new();
    private readonly long _start = Stopwatch.GetTimestamp();
    private long _last;
    private int _lod = -1;
    private string _stage = "setup";
    private readonly int[] _gc = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
    private readonly long _allocated = Allocated();
    private static readonly Func<long>? AllocationCounter = FindAllocationCounter();
    private bool _finished;
    internal MovableAssemblyTiming() { _last=_start; }
    private Row Current => _rows.TryGetValue(_lod,out var row)?row:(_rows[_lod]=new Row());
    internal void Stage(string stage,int? lod=null)
    {
        long now=Stopwatch.GetTimestamp(); var row=Current;
        row.Ticks.TryGetValue(_stage,out long prior);row.Ticks[_stage]=prior+now-_last;
        _last=now;_stage=stage;if(lod.HasValue)_lod=lod.Value;
    }
    internal void Count(string name,long count=1)
    {var row=Current;row.Counts.TryGetValue(name,out long prior);row.Counts[name]=prior+count;}
    internal void Encoding(long ticks) => Count("encodingTicks",ticks);
    private static string Ms(long ticks) => (ticks*1000.0/Stopwatch.Frequency).ToString("F3",CultureInfo.InvariantCulture);
    internal void Finish(string owner,string style,bool success)
    {
        if(_finished)return;_finished=true;
        Stage("finished");
        long end=_last,allocated=Allocated();
        int gc0=GC.CollectionCount(0)-_gc[0],gc1=GC.CollectionCount(1)-_gc[1],gc2=GC.CollectionCount(2)-_gc[2];
        // Logging itself is outside the measured interval and cannot fail generation.
        try
        {
            foreach(var entry in _rows.OrderBy(x=>x.Key))
            {
                var row=entry.Value;
                string stages=string.Join("; ",row.Ticks.Select(x=>x.Key+"Ms="+Ms(x.Value)));
                string counts=string.Join("; ",row.Counts.Where(x=>x.Key!="encodingTicks").Select(x=>x.Key+"="+x.Value));
                row.Counts.TryGetValue("encodingTicks",out long encoding);
                Mod.Log.Info($"Assembly detail: graph={owner}; style={style}; lod={entry.Key}; {stages}; {counts}; encodingSubsetMs={Ms(encoding)}; rowTotalMs={Ms(row.Ticks.Values.Sum())}.");
            }
            long measured=_rows.Values.Sum(r=>r.Ticks.Values.Sum());
            Mod.Log.Info($"Assembly detail total: graph={owner}; style={style}; success={success}; totalMs={Ms(end-_start)}; stageSumMs={Ms(measured)}; unaccountedMs={Ms(end-_start-measured)}; threadAllocatedBytes={(_allocated<0||allocated<0?-1:allocated-_allocated)}; processGc0={gc0}; processGc1={gc1}; processGc2={gc2}; encodingSubsetIncluded=true; loggingExcluded=true.");
        }
        catch { /* Diagnostics must not change native construction results. */ }
    }
    private static Func<long>? FindAllocationCounter()
    {
        try
        {
            var method=typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread",Type.EmptyTypes);
            if(method==null)return null;
            var counter=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),method);
            long before=counter();var probe=new byte[1024];long after=counter();GC.KeepAlive(probe);
            // Some game/Unity Mono versions expose a stub returning zero.
            return after-before>=1024?counter:null;
        }
        catch {return null;}
    }
    private static long Allocated(){try{return AllocationCounter?.Invoke()??-1;}catch{return -1;}}
}
