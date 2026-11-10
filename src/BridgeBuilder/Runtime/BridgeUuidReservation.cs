using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace BridgeBuilder.Runtime;

internal static class BridgeUuidReservation
{
    // Directory.CreateDirectory succeeds for an existing directory as well. The OS call
    // distinguishes a fresh claim from an existing name, even across threads/processes.
    [DllImport("kernel32.dll", ExactSpelling = true, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool CreateDirectoryW(string path, IntPtr securityAttributes);

    [DllImport("libc", EntryPoint = "mkdir", SetLastError = true)]
    private static extern int MakeDirectory(string path, uint mode);

    internal static string? Reserve(string gameRoot, Func<string, bool> occupied, Func<string> next)
    {
        var imported = Path.Combine(gameRoot, "ImportedData");
        Directory.CreateDirectory(BridgeFileAccess.Native(imported));
        BridgeRemovalFiles.RejectLink(gameRoot);
        BridgeRemovalFiles.RejectLink(imported);
        var locations = new[] { imported, Path.Combine(gameRoot, "BridgeBuilder"),
            Path.Combine(gameRoot, "StreamingData~", "VT") };
        for (int i = 0; i < 16; i++)
        {
            string owner = next();
            if (owner.Length != 37 || !BridgeAssetInfo.IsPrefabName(owner))
                throw new InvalidDataException("Invalid generated bridge UUID.");
            if (occupied(owner) || BridgeDeletionIntent.Contains(gameRoot, owner) || locations.Any(directory => HasOwnerEntry(directory, owner))
                || BridgeFileAccess.Exists(Path.Combine(gameRoot, "ModsData", "BridgeBuilder", "PendingRemoval", owner + ".ticket")))
                continue;
            var path = BridgeFileAccess.Native(Path.Combine(imported, owner));
            bool windows = Path.DirectorySeparatorChar == '\\';
            bool created = windows ? CreateDirectoryW(path, IntPtr.Zero) : MakeDirectory(path, 511) == 0;
            if (created) return owner;
            int error = Marshal.GetLastWin32Error();
            if (windows ? error == 183 || error == 80 : error == 17) continue;
            throw new IOException("Could not reserve a bridge directory.", new Win32Exception(error));
        }
        return null;
    }

    private static bool HasOwnerEntry(string directory, string owner)
    {
        if (!BridgeFileAccess.Exists(directory)) return false;
        BridgeRemovalFiles.RejectLink(directory);
        return Directory.EnumerateFileSystemEntries(BridgeFileAccess.Native(directory), "*" + owner + "*",
            SearchOption.TopDirectoryOnly).Any();
    }
}
