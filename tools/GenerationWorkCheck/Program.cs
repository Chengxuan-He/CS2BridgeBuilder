using System;
using System.Threading;
using System.Threading.Tasks;
using BridgeBuilder.Bridges;
namespace BridgeBuilder { internal static class Mod { internal static readonly LogStub Log = new(); } internal sealed class LogStub { internal void Warn(string text) { Console.WriteLine(text); } internal void Info(string text) { Console.WriteLine(text); } } }
public static class GenerationWorkCheck
{
    private static int _checks;
    private static void Check(bool value,string message) { _checks++; if(!value) throw new Exception(message); }
    public static string Run()
    {
        int thread=Thread.CurrentThread.ManagedThreadId;
        var outside = new SynchronizationContext(); SynchronizationContext.SetSynchronizationContext(outside);
        var context = new AsyncLocal<string?>(); context.Value="outside";
        GenerationParameters.Session? session=null; int progress=0; bool disposed=false;
        var work=new GenerationWork("nested",()=>true);
        async Task Child()
        {
            Check(context.Value=="inside","child context");
            await GenerationWork.Next("child one"); progress++;
            Check(Thread.CurrentThread.ManagedThreadId==thread,"native thread");
            Check(GenerationParameters.Capture()==session,"session resumed");
            Check(context.Value=="inside","resumed context");
            await GenerationWork.Next("child two"); progress++;
        }
        work.Start(async()=>
        {
            using var lease=GenerationParameters.Begin(); session=GenerationParameters.Capture();context.Value="inside";
            try { await Child(); await GenerationWork.Next("parent"); progress++; }
            finally { disposed=true; }
        });
        Check(progress==0&&!work.IsComplete,"initial yield");
        Check(context.Value=="outside"&&GenerationParameters.Capture()==null,"start context leaked");
        for(int i=0;i<20&&!work.IsComplete;i++)
        {
            work.Tick();Check(GenerationWork.Current==null&&GenerationParameters.Capture()==null,"pump leaked scope");
            Check(ReferenceEquals(SynchronizationContext.Current,outside),"pump leaked synchronization context");
        }
        work.Completion!.GetAwaiter().GetResult();Check(progress==3&&disposed&&session!.Closed,"complete disposal");work.Report();
        foreach(bool failure in new[]{false,true})
        {
            disposed=false;var cancelled=new GenerationWork("cancel",()=>true);
            cancelled.Start(async()=> { using var lease=GenerationParameters.Begin();session=GenerationParameters.Capture();
                try { await GenerationWork.Next("before failure"); if(failure)throw new InvalidOperationException("injected"); await Child(); }
                finally{disposed=true;} });
            if(failure)cancelled.Tick();else cancelled.Cancel();
            for(int i=0;i<20&&!cancelled.IsComplete;i++)cancelled.Tick();
            Check(cancelled.IsComplete&&disposed&&session!.Closed,"cancel/fault cleanup");
            try{cancelled.Completion!.GetAwaiter().GetResult();throw new Exception("Expected failure");}
            catch(OperationCanceledException) { Check(!failure,"wrong cancellation"); }
            catch(InvalidOperationException) { Check(failure,"wrong fault"); }
            Check(GenerationParameters.Capture()==null&&context.Value=="outside","failure context leaked");
        }
        bool valid=true, published=false; disposed=false;
        var stale=new GenerationWork("stale revision",()=>valid);
        stale.Start(async()=>{try{await GenerationWork.Next("stale output");published=true;}finally{disposed=true;}});
        valid=false;stale.Tick();Check(stale.IsComplete&&disposed&&!published,"stale selection published");
        try{stale.Completion!.GetAwaiter().GetResult();throw new Exception("Expected stale cancellation");}catch(OperationCanceledException){}
        var fresh=new GenerationWork("new revision",()=>true);
        fresh.Start(async()=>{await GenerationWork.Next("new output");published=true;});
        stale.Cancel();Check(!published&&!fresh.IsComplete,"old cancellation changed new request");
        fresh.Tick();fresh.Completion!.GetAwaiter().GetResult();Check(published,"new request blocked by old cancellation");
        int sync=GenerationWork.Synchronous(async()=>{await GenerationWork.Next("sync");return 42;});
        Check(sync==42,"sync wrapper");
        // Task continuations posted from a worker must enter the explicit pump, never execute native work there.
        var signal=new TaskCompletionSource<bool>();var posted=new GenerationWork("posted",()=>true);int resumed=0;
        posted.Start(async()=>{await signal.Task;Check(Thread.CurrentThread.ManagedThreadId==thread,"posted thread");resumed++;});
        Task.Run(()=>signal.SetResult(true)).GetAwaiter().GetResult();
        Check(resumed==0,"worker bypassed pump");posted.Tick();posted.Completion!.GetAwaiter().GetResult();Check(resumed==1,"posted resume");
        // Ready continuations share one budget, but polling is explicitly next-frame.
        int ready=0;var readyWork=new GenerationWork("ready budget",()=>true);
        readyWork.Start(async()=>{for(int i=0;i<4;i++){await GenerationWork.Next("ready");ready++;}});
        readyWork.Tick();Check(ready==4&&readyWork.IsComplete,"ready work needlessly spread across frames");
        int polled=0;var polling=new GenerationWork("next frame",()=>true);
        polling.Start(async()=>{for(int i=0;i<3;i++){await GenerationWork.NextFrame("poll");polled++;}});
        polling.Tick();Check(polled==1,"busy polled in same frame");polling.Tick();Check(polled==2,"poll cadence");polling.Tick();Check(polled==3,"poll completion");
        int budgetSteps=0;var budget=new GenerationWork("aggregate budget",()=>true);
        budget.Start(async()=>{for(int i=0;i<10;i++){await GenerationWork.Next("cost");Thread.Sleep(3);budgetSteps++;}});
        budget.Tick();Check(budgetSteps>0&&budgetSteps<10,"budget reset per invocation");budget.Cancel();
        foreach(bool fault in new[]{false,true})
        {
            using var entered=new ManualResetEventSlim();using var release=new ManualResetEventSlim();using var finished=new ManualResetEventSlim();
            var inputArray=new int[]{3,5,7};int observedSum=0;
            GenerationParameters.Session? workerSession=null;int workerThread=0;bool isolated=false,publishedWorker=false;
            var background=new GenerationWork("background cancellation",()=>true);
            background.Start(async()=>{using var scope=GenerationParameters.Begin();workerSession=GenerationParameters.Capture();
                GenerationParameters.Get("data",()=>new int[]{1,2,3});
                await GenerationWork.Background("detached",()=>{workerThread=Thread.CurrentThread.ManagedThreadId;
                    isolated=GenerationWork.Current==null&&SynchronizationContext.Current==null&&context.Value==null&&GenerationWork.IsBackground;
                    entered.Set();release.Wait();try {observedSum=inputArray[0]+inputArray[1]+inputArray[2];if(fault)throw new InvalidOperationException("worker injected");return 7;}finally{finished.Set();}});
                publishedWorker=true;});
            Check(entered.Wait(5000),"worker not scheduled");
            background.Cancel();Check(background.IsComplete&&!publishedWorker,"cancel waited for worker or published result");
            Check(workerSession!=null&&!workerSession.Closed&&workerSession.Values.Count==1,"worker input released early");
            Check(workerThread!=thread&&isolated,"worker inherited native scope/context");
            release.Set();Check(finished.Wait(5000),"worker did not finish");
            // No further Tick: context-free observer must release the abandoned session itself.
            var deadline=DateTime.UtcNow.AddSeconds(5);while(!workerSession!.Closed&&DateTime.UtcNow<deadline)Thread.Sleep(1);
            Check(workerSession.Closed&&workerSession.Values.Count==0,"abandoned worker scope leaked");
            Check(observedSum==15&&inputArray[0]==3,"cancel changed detached input arrays");
            try{background.Completion!.GetAwaiter().GetResult();throw new Exception("Expected cancellation");}catch(OperationCanceledException){}
        }
        int workerResult=0;var successful=new GenerationWork("background success",()=>true);
        successful.Start(async()=>{using var scope=GenerationParameters.Begin();workerResult=await GenerationWork.Background("sum",()=>42);
            Check(Thread.CurrentThread.ManagedThreadId==thread,"worker result published off main thread");});
        var until=DateTime.UtcNow.AddSeconds(5);while(!successful.IsComplete&&DateTime.UtcNow<until){successful.Tick();Thread.Sleep(1);}
        successful.Completion!.GetAwaiter().GetResult();Check(workerResult==42,"worker result lost");successful.Report();
        var stages=new GenerationStageCache();var immutableInput=new object();int executions=0;
        var pending=new TaskCompletionSource<int>();
        Task<int> Create(){executions++;return pending.Task;}
        var first=stages.Get("decode",new object[]{immutableInput,7},Create);
        var second=stages.Get("decode",new object[]{immutableInput,7},Create);
        var third=stages.Get("decode",new object[]{immutableInput,7},Create);
        Check(ReferenceEquals(first,second)&&ReferenceEquals(second,third)&&executions==1,"three LODs duplicated in-flight stage");
        pending.SetResult(123);
        Check(first.Result==123&&stages.Report().Contains("executions=1, hits=2"),"stage result/accounting missing");
        var distinct=stages.Get("decode",new object[]{new object(),7},()=>Task.FromResult(++executions));
        Check(distinct.Result==2,"distinct source incorrectly reused");
        var failed=stages.Get<int>("failure",new object[]{immutableInput},()=>throw new InvalidOperationException("injected stage"));
        Check(failed.IsFaulted,"failure stage published success");_ = failed.Exception;
        stages.Clear();
        Check(stages.Get("decode",new object[]{immutableInput,7},()=>Task.FromResult(++executions)).Result==3,"cleared request retained stage");
        SynchronizationContext.SetSynchronizationContext(null);
        return "PASS GenerationWork checks="+_checks+" runtime="+Environment.Version;
    }
}
