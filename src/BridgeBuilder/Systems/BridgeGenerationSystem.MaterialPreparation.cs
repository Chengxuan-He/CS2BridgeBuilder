using System;
using System.Threading;
using System.Threading.Tasks;
using BridgeBuilder.Runtime;
using CS2Mods.Shared.Infrastructure;

namespace BridgeBuilder.Systems;

public partial class BridgeGenerationSystem
{
    private Task<BridgeMaterialFileSnapshot>? _materialRead;
    private CancellationTokenSource? _materialCancellation;
    private Action? _materialReady;
    private Action<Exception>? _materialFailed;
    private int? _materialPreviewRevision;

    private void PrepareMaterialFiles(string style, Action ready, Action<Exception> failed, int? revision = null)
    {
        if (_materialRead != null)
        {
            NotifyMaterialFailure(failed, new InvalidOperationException("Material preparation is already pending."), revision);
            return;
        }
        try
        {
            if(!BridgePreparedMaterials.TrySourceRoot(out var root,out var cause)){NotifyMaterialFailure(failed,new InvalidOperationException(cause),revision);return;}
            var cancellation = new CancellationTokenSource();
            _materialCancellation = cancellation;
            _materialReady = ready;
            _materialFailed = failed;
            _materialPreviewRevision = revision;
            // Only strings and a cancellation token enter the worker.
            _materialRead = Task.Run(() => BridgeMaterialFileSnapshot.Read(root, style, cancellation.Token));
        }
        catch (Exception exception) { NotifyMaterialFailure(failed, exception, revision); }
    }

    private bool UpdateMaterialPreparation()
    {
        if (_materialRead == null) return false;
        if (_materialPreviewRevision.HasValue && _materialPreviewRevision != BridgePreviewState.Revision)
        {
            CancelMaterialPreparation();
            return false;
        }
        if (!_materialRead.IsCompleted) return true;
        var task = _materialRead;
        var ready = _materialReady!;
        var failed = _materialFailed!;
        var revision = _materialPreviewRevision;
        _materialRead = null; _materialReady = null; _materialFailed = null; _materialPreviewRevision = null;
        var cancellation = _materialCancellation;
        _materialCancellation = null;
        try
        {
            cancellation?.Dispose();
            // IsCompleted above is mandatory: this retrieval never waits on the game thread.
            using var snapshot = task.GetAwaiter().GetResult();
            if(snapshot.Error!=null){NotifyMaterialFailure(failed,new System.IO.InvalidDataException(snapshot.Error),revision);return true;}
            Mod.Log.Info($"Prepared material IO: files={snapshot.FileCount}, bytes={snapshot.BytesHashed}, workerMs={snapshot.ElapsedMilliseconds}, workerThread={snapshot.WorkerThreadId}, applyThread={Thread.CurrentThread.ManagedThreadId}");
            BridgePreparedMaterials.RequestFiles = snapshot;
            try { ready(); }
            finally { BridgePreparedMaterials.RequestFiles = null; }
        }
        catch (Exception exception) { NotifyMaterialFailure(failed, exception, revision); }
        return true;
    }

    private void NotifyMaterialFailure(Action<Exception> failed, Exception failure, int? revision)
    {
        try { failed(failure); }
        catch (Exception callbackException)
        {
            Mod.Log.Error(failure, "Material preparation failed before its failure callback completed.");
            Mod.Log.Error(callbackException, "Material preparation failure callback failed; publishing terminal failure.");
            if (revision.HasValue) FailPreview(revision.Value, "PreviewFailed");
            else BridgeRuntimeRequests.Complete("CreateFailed");
        }
    }

    private void CancelMaterialPreparation()
    {
        var task = _materialRead;
        if (task == null) return;
        var cancellation = _materialCancellation!;
        _materialRead = null; _materialCancellation = null;
        _materialReady = null; _materialFailed = null; _materialPreviewRevision = null;
        cancellation.Cancel();
        // Observe failures and close leases even if the world/system has already been destroyed.
        // This continuation has no reference to the system or any engine-owned object.
        _ = task.ContinueWith(completed =>
        {
            if (completed.Status == TaskStatus.RanToCompletion) completed.GetAwaiter().GetResult().Dispose();
            else _ = completed.Exception;
            cancellation.Dispose();
        }, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default);
    }

    private void FailMaterialCreation(Exception exception)
    {
        var report = new ExportReport(logIssues: false);
        report.Failed("(prepared bridge materials)", exception);
        try { Finish(report, ExportStateStore.Load(), "Create runtime bridge", showMessage: false); }
        finally { BridgeRuntimeRequests.Complete("CreateFailed"); }
    }
}
