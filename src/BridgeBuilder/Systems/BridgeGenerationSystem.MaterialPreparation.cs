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
        if (_materialRead != null) throw new InvalidOperationException("Material preparation is already pending.");
        string root;
        try { root = BridgePreparedMaterials.SourceRoot; }
        catch (Exception exception) { failed(exception); return; }
        var cancellation = new CancellationTokenSource();
        _materialCancellation = cancellation;
        _materialReady = ready;
        _materialFailed = failed;
        _materialPreviewRevision = revision;
        // Only strings and a cancellation token enter the worker; continuations stay in OnUpdate.
        _materialRead = Task.Run(() => BridgeMaterialFileSnapshot.Read(root, style, cancellation.Token));
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
        _materialRead = null; _materialReady = null; _materialFailed = null; _materialPreviewRevision = null;
        _materialCancellation!.Dispose(); _materialCancellation = null;
        try
        {
            // IsCompleted above is mandatory: this retrieval never waits on the game thread.
            using var snapshot = task.GetAwaiter().GetResult();
            Mod.Log.Info($"Prepared material IO: files={snapshot.FileCount}, bytes={snapshot.BytesHashed}, workerMs={snapshot.ElapsedMilliseconds}, workerThread={snapshot.WorkerThreadId}, applyThread={Thread.CurrentThread.ManagedThreadId}");
            BridgePreparedMaterials.RequestFiles = snapshot;
            try { ready(); }
            finally { BridgePreparedMaterials.RequestFiles = null; }
        }
        catch (Exception exception) { failed(exception); }
        return true;
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
        Finish(report, ExportStateStore.Load(), "Create runtime bridge", showMessage: false);
        BridgeRuntimeRequests.Complete("CreateFailed");
    }
}
