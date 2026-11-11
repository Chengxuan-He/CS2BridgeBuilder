using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace BridgeBuilder.Bridges;

// Explicit main-thread continuation queue. It never pumps the player loop or runs native work on a worker.
internal sealed class GenerationWork
{
    [ThreadStatic] private static bool _background;
    internal static bool IsBackground => _background;
    private int _lastWorkerThread;
    private static readonly AsyncLocal<GenerationWork?> Active = new();
    internal static GenerationWork? Current => Active.Value;
    private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
    private readonly Func<bool> _valid;
    private readonly string _identity;
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly System.Collections.Generic.Queue<Action> _pending = new();
    private readonly PumpContext _context;
    private readonly SynchronizationContext? _hostContext;
    private int _dispatchPosted;
    private bool _draining;
    private readonly System.Collections.Generic.Queue<Action> _nextFrame = new();
    private long _maximumFrameTicks, _workerTicks;
    private int _frames, _workerJobs;
    private long _workerQueueTicks, _readyQueueTicks, _frameQueueTicks;
    private readonly System.Collections.Generic.HashSet<Action> _workerWaits=new();
    private sealed class PumpContext : SynchronizationContext
    {
        private readonly GenerationWork _owner;
        internal PumpContext(GenerationWork owner) { _owner = owner; }
        public override void Post(SendOrPostCallback callback, object? state)
        {
            long queued=Stopwatch.GetTimestamp();
            lock (_owner._pending) _owner._pending.Enqueue(() =>
            { _owner._readyQueueTicks+=Stopwatch.GetTimestamp()-queued;callback(state); });
            _owner.WakeNativeDispatcher();
        }
        public override SynchronizationContext CreateCopy() => this;
    }
    private string _stage="start", _maximumStage="start";
    private bool _cancelled, _reported;
    private long _activeTicks, _maximumTicks;
    private int _steps;
    internal Task? Completion {get;private set;}
    internal bool IsComplete=>Completion?.IsCompleted??true;
    internal GenerationWork(string identity,Func<bool> valid)
    {
        _identity=identity;_valid=valid;_context=new PumpContext(this);
        var host=SynchronizationContext.Current;
        if(host!=null && host.GetType()!=typeof(SynchronizationContext) && host is not PumpContext) _hostContext=host;
    }
    private void WakeNativeDispatcher()
    {
        if(_hostContext==null || Interlocked.Exchange(ref _dispatchPosted,1)!=0)return;
        using(ExecutionContext.SuppressFlow()) _hostContext.Post(_=>
        {
            Interlocked.Exchange(ref _dispatchPosted,0);
            DrainReady();
        },null);
    }
    internal void Start(Func<Task> build)
    {
        if(Completion!=null)throw new InvalidOperationException("Generation work already started.");
        Invoke(()=>Completion=build());
    }
    internal void Tick()
    {
        CheckThread(); _frames++;
        lock (_pending) while (_nextFrame.Count != 0) _pending.Enqueue(_nextFrame.Dequeue());
        DrainReady();
    }
    private void DrainReady()
    {
        CheckThread();
        if(_draining)return;
        _draining=true;
        long dispatchStarted=Stopwatch.GetTimestamp();
        try
        {
            while(!IsComplete)
            {
                Action step;
                lock(_pending){if(_pending.Count==0)break;step=_pending.Dequeue();}
                Invoke(step);
            }
        }
        finally
        {
            _draining=false;
            _maximumFrameTicks=Math.Max(_maximumFrameTicks,Stopwatch.GetTimestamp()-dispatchStarted);
        }
    }
    internal void Cancel()
    {
        CheckThread();_cancelled=true;
        foreach(var cancel in new System.Collections.Generic.List<Action>(_workerWaits))cancel();
        // Resume only the suspended state machine's cancellation/finally path, not native generation.
        // Only posted completion/finally continuations remain after cancellation;
        // each generation await observes the cancellation before doing native work.
        while (!IsComplete)
        {
            lock (_pending) { if (_pending.Count == 0 && _nextFrame.Count == 0) break; }
            Tick();
        }
    }
    private void Invoke(Action action)
    {
        CheckThread();var previous=Active.Value;Active.Value=this;long start=Stopwatch.GetTimestamp();var stage=_stage;
        var previousContext = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(_context);
        try{action();}
        finally
        {
            long elapsed=Stopwatch.GetTimestamp()-start;_activeTicks+=elapsed;_steps++;
            if(elapsed>_maximumTicks){_maximumTicks=elapsed;_maximumStage=stage;}
            SynchronizationContext.SetSynchronizationContext(previousContext);
            Active.Value=previous;
        }
    }
    private void CheckThread()
    {if(Thread.CurrentThread.ManagedThreadId!=_thread)throw new InvalidOperationException("Generation native continuation left the game thread.");}
    private void Check()
    {CheckThread();if(_cancelled||!_valid())throw new OperationCanceledException("Preview generation superseded or world unavailable.");}
    internal static Step Next(string stage)=>new(Current,stage,false);
    internal static Step NextFrame(string stage)=>new(Current,stage,true);
    internal static Step Checkpoint(string stage)=>new(Current,stage,false);
    internal readonly struct Step : ICriticalNotifyCompletion
    {
        private readonly GenerationWork? _work;private readonly string _stage;private readonly bool _nextFrame;
        internal Step(GenerationWork? work,string stage,bool nextFrame){_work=work;_stage=stage;_nextFrame=nextFrame;}
        public Step GetAwaiter()=>this;
        public bool IsCompleted=>_work==null||!_nextFrame;
        public void GetResult(){if(_work!=null){_work._stage=_stage;_work.Check();}}
        public void OnCompleted(Action continuation)=>UnsafeOnCompleted(continuation);
        public void UnsafeOnCompleted(Action continuation)
        {
            var work=_work??throw new InvalidOperationException("Completed generation step cannot be queued.");
            work.CheckThread();
            work._stage=_stage;
            long queued=Stopwatch.GetTimestamp();bool nextFrame=_nextFrame;
            lock (work._pending) (_nextFrame ? work._nextFrame : work._pending).Enqueue(()=>
            {
                long waited=Stopwatch.GetTimestamp()-queued;
                if(nextFrame)work._frameQueueTicks+=waited;else work._readyQueueTicks+=waited;
                continuation();
            });
        }
    }
    // Only detached managed-data functions may use this entry. Never pass Unity objects or ECS state.
    internal static async Task<T> Background<T>(string stage, Func<T> calculate)
    {
        var work=Current;
        if(work==null)return calculate();
        work.Check();
        var parameters=GenerationParameters.Capture();
        // Acquire before scheduling: cancellation may dispose the parent scope immediately.
        var retained=GenerationParameters.Attach(parameters);
        Task<T> task;
        long queued=Stopwatch.GetTimestamp();
        try
        {
            using(ExecutionContext.SuppressFlow())
                task=Task.Run(() =>
                {
                    long started=Stopwatch.GetTimestamp();
                    Interlocked.Add(ref work._workerQueueTicks,started-queued);
                    _background=true;
                    Interlocked.Exchange(ref work._lastWorkerThread,Thread.CurrentThread.ManagedThreadId);
                    using var attached=GenerationParameters.Attach(parameters);
                    try { return calculate(); }
                    finally { _background=false; Interlocked.Add(ref work._workerTicks,Stopwatch.GetTimestamp()-started);Interlocked.Increment(ref work._workerJobs); }
                });
        }
        catch { retained?.Dispose(); throw; }
        // The scheduling lease is released in its original context only after the worker has attached.
        // Keep it through completion, with a private worker-observer for abandoned requests.
        try
        {
            if(!task.IsCompleted)
            {
                var ready=new TaskCompletionSource<bool>();
                Action cancel=()=>ready.TrySetCanceled();
                work._workerWaits.Add(cancel);work._stage=stage;
                try
                {
                    using(ExecutionContext.SuppressFlow())
                    _=task.ContinueWith(completed=>
                    {
                        // Wake the native dispatcher immediately after detached computation.
                        // No native API or request publication runs on the worker.
                        work._context.Post(_=>ready.TrySetResult(true),null);
                    },CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
                    await ready.Task;
                }
                finally{work._workerWaits.Remove(cancel);}
            }
            work.Check();return task.GetAwaiter().GetResult();
        }
        finally
        {
            if(task.IsCompleted)retained?.Dispose();
            else
            {
                // Transfer only the refcount, not a context-bound Lease, to the worker observer.
                var owner=parameters;
                if(owner!=null)lock(owner.Values)owner.Leases++;
                retained?.Dispose();
                using(ExecutionContext.SuppressFlow())
                _=task.ContinueWith(completed =>
                {
                    _=completed.Exception;
                    if(owner!=null)lock(owner.Values){if(--owner.Leases==0){owner.Values.Clear();owner.Closed=true;}}
                },CancellationToken.None,TaskContinuationOptions.ExecuteSynchronously,TaskScheduler.Default);
            }
        }
    }
    internal static T Synchronous<T>(Func<Task<T>> action)
    {
        var previous=Active.Value;Active.Value=null;
        try{var task=action();if(!task.IsCompleted)throw new InvalidOperationException("Synchronous generation attempted deferred work.");return task.GetAwaiter().GetResult();}
        finally{Active.Value=previous;}
    }
    internal void Report()
    {
        if(_reported)return;_reported=true;
        double Ms(long value)=>value*1000.0/Stopwatch.Frequency;
        Mod.Log.Info($"Preview generation scheduling: request={_identity}; dispatcher={_hostContext?.GetType().FullName ?? "update-fallback"}; steps={_steps}; cancelled={_cancelled}; elapsedMs={Ms(Stopwatch.GetTimestamp()-_started):F3}; activeMainThreadMs={Ms(_activeTicks):F3}; maxMainThreadStepMs={Ms(_maximumTicks):F3}; maxStage={_maximumStage}; frames={_frames}; maxPumpMs={Ms(_maximumFrameTicks):F3}; workerJobs={_workerJobs}; mainThread={_thread}; lastWorkerThread={_lastWorkerThread}; workerMs={Ms(Interlocked.Read(ref _workerTicks)):F3}; workerQueueMs={Ms(Interlocked.Read(ref _workerQueueTicks)):F3}; readyQueueAggregateMs={Ms(_readyQueueTicks):F3}; explicitFrameQueueAggregateMs={Ms(_frameQueueTicks):F3}.");
    }
}
