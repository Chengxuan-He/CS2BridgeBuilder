using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace BridgeBuilder.Bridges;

// One request owns this table. Reference operands are immutable snapshots, not mutable values.
internal sealed class GenerationStageCache
{
    private sealed class Key : IEquatable<Key>
    {
        private readonly object[] _parts;
        private readonly int _hash;
        internal Key(object[] parts)
        {
            _parts=(object[])parts.Clone();
            unchecked { int h=17;foreach(var p in _parts)h=h*31+(p==null?0:Value(p)?p.GetHashCode():RuntimeHelpers.GetHashCode(p));_hash=h; }
        }
        private static bool Value(object p)=>p is string || p.GetType().IsValueType;
        public bool Equals(Key? other)
        {
            if(other==null || _parts.Length!=other._parts.Length)return false;
            for(int i=0;i<_parts.Length;i++)
            {
                var a=_parts[i];var b=other._parts[i];
                if(ReferenceEquals(a,b))continue;
                if(a==null || b==null || !Value(a) || !a.Equals(b))return false;
            }
            return true;
        }
        public override bool Equals(object? value)=>value is Key key && Equals(key);
        public override int GetHashCode()=>_hash;
    }
    private long _lookupTicks;
    private readonly Dictionary<Key,object> _entries=new();
    private readonly Dictionary<string,(int Executions,int Hits,long Wall)> _counts=new();
    internal Task<T> Get<T>(string stage,object[] input,Func<Task<T>> create)
    {
        long lookupStarted=Stopwatch.GetTimestamp();
        var all=new object[input.Length+2];all[0]=stage;all[1]=typeof(T);Array.Copy(input,0,all,2,input.Length);
        var key=new Key(all);TaskCompletionSource<T> pending;
        lock(_entries)
        {
            _counts.TryGetValue(stage,out var count);
            if(_entries.TryGetValue(key,out var found))
            { _counts[stage]=(count.Executions,count.Hits+1,count.Wall);_lookupTicks+=Stopwatch.GetTimestamp()-lookupStarted;return (Task<T>)found; }
            pending=new TaskCompletionSource<T>();_entries.Add(key,pending.Task);
            _counts[stage]=(count.Executions+1,count.Hits,count.Wall);
            _lookupTicks+=Stopwatch.GetTimestamp()-lookupStarted;
        }
        _=Complete();return pending.Task;
        async Task Complete()
        {
            long started=Stopwatch.GetTimestamp();
            T result=default!;Exception? failure=null;bool cancelled=false;
            try{result=await create();}
            catch(OperationCanceledException){cancelled=true;}
            catch(Exception exception){failure=exception;}
            lock(_entries){var c=_counts[stage];_counts[stage]=(c.Executions,c.Hits,c.Wall+Stopwatch.GetTimestamp()-started);}
            // Publish after accounting: a synchronous consumer may dispose/report immediately.
            if(cancelled)pending.TrySetCanceled();
            else if(failure!=null)pending.TrySetException(failure);
            else pending.TrySetResult(result);
        }
    }
    internal string Report()
    {
        lock(_entries)
        {
            var rows=new List<string>();foreach(var pair in _counts)
                rows.Add($"{pair.Key}: executions={pair.Value.Executions}, hits={pair.Value.Hits}, stageWallMs={pair.Value.Wall*1000.0/Stopwatch.Frequency:F3}");
            return string.Join("; ",rows)+$"; keyLookupMs={_lookupTicks*1000.0/Stopwatch.Frequency:F3}; misses=first-or-distinct-consumed-input";
        }
    }
    internal void Clear(){lock(_entries)_entries.Clear();}
}
