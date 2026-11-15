using System;
using System.Collections.Generic;
using System.IO;

namespace BridgeBuilder.Runtime;

/// <summary>One startup IO prerequisite; probes only uniquely owned temporary files.</summary>
internal static class BridgeInspectionPermissions
{
    private static bool _checked;
    internal static bool Ready { get; private set; }
    internal static void Start()
    {
        if (_checked) return;
        _checked = true;
        Ready = Check(new[] { BridgeAssetCatalog.Root,
            Path.Combine(UnityEngine.Application.persistentDataPath, "BridgeBuilder"),
            Path.Combine(UnityEngine.Application.persistentDataPath, "StreamingData~/VT") });
    }

    internal static bool Check(IEnumerable<string> roots)
    {
        foreach (var root in roots)
        {
            string first = Path.Combine(root, ".bb-permission-" + Guid.NewGuid().ToString("N"));
            string moved = first + ".moved";
            string operation = "write";
            string? pending = null;
            bool permissionFailed = false;
            try
            {
                Directory.CreateDirectory(BridgeFileAccess.Native(root));
                File.WriteAllText(BridgeFileAccess.Native(first), "Bridge Builder permission probe");
                pending = first;
                operation = "read";
                File.ReadAllText(BridgeFileAccess.Native(first));
                operation = "move";
                File.Move(BridgeFileAccess.Native(first), BridgeFileAccess.Native(moved));
                pending = moved;
                operation = "delete";
                File.Delete(BridgeFileAccess.Native(moved));
                pending = null;
            }
            catch (UnauthorizedAccessException error)
            { permissionFailed = true; throw new BridgeRequiredFilePermissionException(operation, root, error); }
            catch (Exception error)
            {
                BridgeInspectionLog.Error("Bridge inspection startup IO prerequisite unavailable: " + root + ": " + error);
                return false;
            }
            finally
            {
                if (pending != null)
                {
                    try { File.Delete(BridgeFileAccess.Native(pending)); }
                    catch (UnauthorizedAccessException error)
                    {
                        if (!permissionFailed) throw new BridgeRequiredFilePermissionException("delete probe", pending, error);
                        BridgeInspectionLog.Error("Permission probe cleanup also denied: " + pending + ": " + error);
                    }
                    catch (Exception error) { BridgeInspectionLog.Error("Inspection probe cleanup: " + pending + ": " + error); }
                }
            }
        }
        return true;
    }
}
