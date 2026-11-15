using System;

namespace BridgeBuilder.Runtime;

/// <summary>Nonrecursive diagnostic boundary for inspection, including failed log sinks.</summary>
internal static class BridgeInspectionLog
{
    private static Colossal.Logging.ILog? _log;
    private static Colossal.Logging.ILog Log => _log ??= Colossal.Logging.LogManager.GetLogger("BridgeBuilder.SelfCheck").SetShowsErrorsInUI(false);
    internal static void Info(string message) { try { Log.Info(message); } catch (Exception) { } }
    internal static void Warn(string message) { try { Log.Warn(message); } catch (Exception) { } }
    internal static void Error(string message)
    {
        try { Log.Error(message); }
        catch (Exception)
        {
            try { System.Diagnostics.Trace.WriteLine(message); } catch (Exception) { }
        }
    }
    internal static void Run(string stage, Action action)
    {
        try { action(); }
        catch (Exception error) when (error is not UnauthorizedAccessException) { Error("Bridge inspection " + stage + ": " + error); }
    }
}
