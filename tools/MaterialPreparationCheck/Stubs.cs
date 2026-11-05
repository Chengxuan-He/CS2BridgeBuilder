using BridgeBuilder.Runtime;
namespace BridgeBuilder.Runtime
{
    internal static class BridgePreparedMaterials { internal static string SourceRoot = ""; internal static BridgeMaterialFileSnapshot? RequestFiles; }
    internal static class BridgePreviewState { internal static int Revision; }
    internal static class BridgeRuntimeRequests { internal static void Complete(string text) { } }
}
namespace CS2Mods.Shared.Infrastructure
{
    internal sealed class ExportReport { internal ExportReport(bool logIssues) { } internal void Failed(string name, Exception exception) { } }
    internal sealed class ExportStateStore { internal static ExportStateStore Load() => new(); }
}
namespace BridgeBuilder
{
    internal static class Mod { internal static readonly Logger Log = new(); internal sealed class Logger { internal void Info(string text) => Console.WriteLine(text); } }
}
namespace BridgeBuilder.Systems
{
    public partial class BridgeGenerationSystem
    {
        internal bool Pending => _materialRead != null;
        internal void Begin(string style, Action ready, Action<Exception> failed, int? revision = null) => PrepareMaterialFiles(style, ready, failed, revision);
        internal bool Tick() => UpdateMaterialPreparation();
        internal void Stop() => CancelMaterialPreparation();
        internal void Inject(Task<BridgeMaterialFileSnapshot> task)
        { _materialRead = task; _materialCancellation = new(); _materialReady = () => { }; _materialFailed = _ => { }; }
        private void Finish(CS2Mods.Shared.Infrastructure.ExportReport report, CS2Mods.Shared.Infrastructure.ExportStateStore state, string operation, bool showMessage) { }
    }
}
