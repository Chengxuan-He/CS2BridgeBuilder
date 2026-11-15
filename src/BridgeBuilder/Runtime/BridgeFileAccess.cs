using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace BridgeBuilder.Runtime;

// Keep logical paths unprefixed for ownership comparisons; use extended paths only at the IO boundary.
// File.Exists/Directory.Exists hide access errors, which must never authorize asset retirement.
internal static class BridgeFileAccess
{
    internal static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
    internal static StringComparer PathComparer => Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    internal static string Logical(string path) => path.StartsWith(@"\\?\UNC\", PathComparison)
        ? @"\\" + path.Substring(8) : path.StartsWith(@"\\?\", StringComparison.Ordinal) ? path.Substring(4) : path;
    internal static string Native(string path)
    {
        var full = Path.GetFullPath(path);
        if (Path.DirectorySeparatorChar != '\\' || full.StartsWith(@"\\?\", StringComparison.Ordinal)) return full;
        return full.StartsWith(@"\\", StringComparison.Ordinal) ? @"\\?\UNC\" + full.Substring(2) : @"\\?\" + full;
    }

    [ThreadStatic] internal static int ReadFailures;

    internal static bool Exists(string path)
    {
        try { File.GetAttributes(Native(path)); return true; }
        catch (FileNotFoundException) { return false; }
        catch (DirectoryNotFoundException) { return false; }
        catch (UnauthorizedAccessException error) { throw new BridgeRequiredFilePermissionException("read", path, error); }
        catch { ReadFailures++; throw; }
    }
    internal static string ReadText(string path)
    { try { return File.ReadAllText(Native(path)); } catch (UnauthorizedAccessException error) { throw new BridgeRequiredFilePermissionException("read", path, error); }
        catch { ReadFailures++; throw; } }
    internal static byte[] ReadBytes(string path)
    { try { return File.ReadAllBytes(Native(path)); } catch (UnauthorizedAccessException error) { throw new BridgeRequiredFilePermissionException("read", path, error); }
        catch { ReadFailures++; throw; } }
    internal static FileStream OpenRead(string path)
    { try { return File.OpenRead(Native(path)); } catch (UnauthorizedAccessException error) { throw new BridgeRequiredFilePermissionException("read", path, error); }
        catch { ReadFailures++; throw; } }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(string path, uint access, uint share, IntPtr security, uint creation, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(SafeFileHandle handle, StringBuilder name, uint size, uint flags);
    [DllImport("libc", SetLastError = true)] private static extern IntPtr realpath(string path, IntPtr buffer);
    [DllImport("libc")] private static extern void free(IntPtr pointer);

    // Resolve existing ancestors too, so destinations below a mapped parent compare correctly.
    internal static string Physical(string path)
    {
        var full = Path.GetFullPath(Logical(path));
        if (!Exists(full))
        {
            var parent = Path.GetDirectoryName(full);
            if (parent == null) return full;
            return Path.Combine(Physical(parent), Path.GetFileName(full));
        }
        if (Path.DirectorySeparatorChar == '\\')
        {
            using var handle = CreateFileW(Native(full), 0, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero);
            if (handle.IsInvalid) ThrowNativePathError(full);
            var buffer = new StringBuilder(512);
            uint size = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
            if (size == 0) ThrowNativePathError(full);
            if (size >= buffer.Capacity)
            {
                buffer = new StringBuilder(checked((int)size + 1));
                if (GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0) == 0) ThrowNativePathError(full);
            }
            return Logical(buffer.ToString());
        }
        var resolved = realpath(full, IntPtr.Zero);
        if (resolved == IntPtr.Zero) throw new IOException("Cannot resolve mapped path: " + full, new Win32Exception(Marshal.GetLastWin32Error()));
        try { return Marshal.PtrToStringAnsi(resolved)!; } finally { free(resolved); }
    }

    private static void ThrowNativePathError(string path)
    {
        int code = Marshal.GetLastWin32Error(); var cause = new Win32Exception(code);
        if (code == 5) throw new BridgeRequiredFilePermissionException("read", path, cause);
        throw new IOException("Cannot resolve mapped path: " + path, cause);
    }
    internal static bool SamePath(string a, string b) => string.Equals(Physical(a).TrimEnd('\\','/'), Physical(b).TrimEnd('\\','/'), PathComparison);
    internal static bool WithinPhysical(string path, string root)
    {
        string a = Physical(path).TrimEnd('\\','/'), b = Physical(root).TrimEnd('\\','/');
        return a.Equals(b, PathComparison) || a.StartsWith(b + Path.DirectorySeparatorChar, PathComparison);
    }
    internal static string LogicalUnder(string path, string root)
    {
        string full = Path.GetFullPath(Logical(path)), logicalRoot = Path.GetFullPath(Logical(root)).TrimEnd('\\','/');
        if (full.Equals(logicalRoot, PathComparison) || full.StartsWith(logicalRoot + Path.DirectorySeparatorChar, PathComparison)) return full;
        string physical = Physical(full), physicalRoot = Physical(logicalRoot).TrimEnd('\\','/');
        if (physical.Equals(physicalRoot, PathComparison)) return logicalRoot;
        if (physical.StartsWith(physicalRoot + Path.DirectorySeparatorChar, PathComparison)) return logicalRoot + physical.Substring(physicalRoot.Length);
        if (Directory.Exists(Native(logicalRoot)))
        {
            string? match = null;
            // Resolve only asset namespaces when the caller supplies the game root.
            var roots = Directory.Exists(Native(Path.Combine(logicalRoot, "ImportedData")))
                ? new[] { Path.Combine(logicalRoot, "ImportedData"), Path.Combine(logicalRoot, "BridgeBuilder"), Path.Combine(logicalRoot, "StreamingData~", "VT") }.Where(p => Directory.Exists(Native(p)))
                : new[] { logicalRoot }.AsEnumerable();
            foreach (string directory in roots.SelectMany(Directories))
            {
                string target = Physical(directory).TrimEnd('\\','/');
                if (!physical.Equals(target, PathComparison) && !physical.StartsWith(target + Path.DirectorySeparatorChar, PathComparison)) continue;
                string candidate = directory + physical.Substring(target.Length);
                if (match != null && !match.Equals(candidate, PathComparison))
                    throw new IOException("Ambiguous mapped asset path: " + path);
                match = candidate;
            }
            if (match != null) return match;
        }
        return full;
    }
    internal static IEnumerable<string> Directories(string root) => Directories(root, new HashSet<string>(PathComparer));
    private static IEnumerable<string> Directories(string root, HashSet<string> ancestors)
    {
        if (!Directory.Exists(Native(root))) yield break;
        string physical;
        try { physical = Physical(root); }
        catch (IOException error) when (MissingTarget(error) && (File.GetAttributes(Native(root)) & FileAttributes.ReparsePoint) != 0) { yield break; }
        Enter(root, ancestors);
        try
        {
            yield return root;
            foreach (string child in Directory.GetDirectories(Native(root)))
                foreach (string directory in Directories(Logical(child), ancestors)) yield return directory;
        }
        finally { ancestors.Remove(physical); }
    }
    // Inventory only directory mapping boundaries, not payload files. Unrelated inaccessible
    // descendants are not evidence against a selected owner; its own traversal remains strict.
    internal static IEnumerable<string> MappedBoundaries(string root)
    {
        var result = new List<string>();
        Inventory(root, true, new HashSet<string>(PathComparer), result);
        return result;
    }
    private static void Inventory(string directory, bool root, HashSet<string> ancestors, List<string> result)
    {
        string physical;
        try
        {
            if (!Directory.Exists(Native(directory))) return;
            physical = Physical(directory);
            if (!ancestors.Add(physical)) return;
        }
        catch (UnauthorizedAccessException) { return; }
        catch (IOException) { return; }
        try
        {
            if (root || (File.GetAttributes(Native(directory)) & FileAttributes.ReparsePoint) != 0) result.Add(directory);
            foreach (string child in Directory.GetDirectories(Native(directory))) Inventory(Logical(child), false, ancestors, result);
        }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
        finally { ancestors.Remove(physical); }
    }
    internal static bool WithinAssetStorage(string path, string gameRoot)
    {
        if (WithinPhysical(path, gameRoot)) return true;
        foreach (string root in new[] { Path.Combine(gameRoot, "ImportedData"), Path.Combine(gameRoot, "BridgeBuilder"), Path.Combine(gameRoot, "StreamingData~", "VT") })
            foreach (string boundary in MappedBoundaries(root))
                if (WithinPhysical(path, boundary)) return true;
        return false;
    }
    internal static bool WithinTree(string path, string root) => Directory.Exists(Native(root))
        ? Directories(root).Any(directory => WithinPhysical(path, directory)) : WithinPhysical(path, root);
    private static bool MissingTarget(IOException error) => error.InnerException is Win32Exception native && (native.NativeErrorCode == 2 || native.NativeErrorCode == 3);
    private static void Enter(string directory, HashSet<string> ancestors)
    {
        if (!ancestors.Add(Physical(directory))) throw new IOException("Directory mapping cycle: " + directory);
    }
    internal static IEnumerable<string> EnumerateFiles(string directory) => EnumerateFiles(directory, new HashSet<string>(PathComparer));
    private static IEnumerable<string> EnumerateFiles(string directory, HashSet<string> ancestors)
    {
        if (!Directory.Exists(Native(directory))) yield break;
        string physical = Physical(directory); Enter(directory, ancestors);
        try
        {
            foreach (var file in Directory.GetFiles(Native(directory))) yield return Logical(file);
            foreach (var child in Directory.GetDirectories(Native(directory)))
                foreach (var file in EnumerateFiles(Logical(child), ancestors)) yield return file;
        }
        finally { ancestors.Remove(physical); }
    }

    internal static void DeletePayload(string path, Action? changed = null)
    {
        if (Directory.Exists(Native(path))) _ = Directories(path).ToArray(); // Validate cycles before mutation.
        DeletePayload(path, new HashSet<string>(PathComparer), changed);
    }
    private static void DeletePayload(string path, HashSet<string> ancestors, Action? changed)
    {
        if (!Exists(path)) return;
        var attributes = File.GetAttributes(Native(path));
        bool link = (attributes & FileAttributes.ReparsePoint) != 0;
        // A sibling logical route can point at a payload already removed by this operation.
        if (link && !Directory.Exists(Native(path)) && !File.Exists(Native(path)))
        {
            if ((attributes & FileAttributes.Directory) != 0) Directory.Delete(Native(path), false); else File.Delete(Native(path));
            changed?.Invoke(); return;
        }
        string physical;
        try { physical = Physical(path); }
        catch (IOException error) when (link && MissingTarget(error))
        {
            if ((attributes & FileAttributes.Directory) != 0) Directory.Delete(Native(path), false); else File.Delete(Native(path));
            changed?.Invoke(); return;
        }
        if (Directory.Exists(Native(path)))
        {
            Enter(path, ancestors);
            try
            {
                foreach (string child in Directory.GetFileSystemEntries(Native(path))) DeletePayload(Logical(child), ancestors, changed);
                Directory.Delete(Native(physical), false); changed?.Invoke();
            }
            finally { ancestors.Remove(physical); }
            if (link) Directory.Delete(Native(path), false);
        }
        else
        {
            File.Delete(Native(physical)); changed?.Invoke();
            if (link) File.Delete(Native(path));
        }
    }

    // Copy a complete independent payload before any source deletion. No links enter a backup.
    internal static void MovePayload(string source, string destination, Action? changed = null)
    {
        if (SamePath(source, destination)) return;
        if (Directory.Exists(Native(source)) && WithinTree(destination, source))
            throw new IOException("Move destination is inside the source payload: " + destination);
        if (Exists(destination)) throw new IOException("Move destination already exists: " + destination);
        Directory.CreateDirectory(Native(Path.GetDirectoryName(destination)!));
        string from = Physical(source), to = Physical(destination);
        bool directory = Directory.Exists(Native(source));
        bool linked = (File.GetAttributes(Native(source)) & FileAttributes.ReparsePoint) != 0;
        // Native rename is retained for trees without nested mappings on the same resolved volume.
        bool nested = directory && HasNestedLinks(source, new HashSet<string>(PathComparer));
        if (!nested && string.Equals(Path.GetPathRoot(from), Path.GetPathRoot(to), PathComparison))
        {
            try
            {
                if (directory) Directory.Move(Native(from), Native(to)); else File.Move(Native(from), Native(to));
                changed?.Invoke();
                if (linked) { if (directory) Directory.Delete(Native(source), false); else File.Delete(Native(source)); }
                return;
            }
            catch (IOException error) when ((error.HResult & 0xffff) == (Path.DirectorySeparatorChar == '\\' ? 17 : 18)) { } // ERROR_NOT_SAME_DEVICE (mounted volume).

        }
        CopyPayload(source, destination, new HashSet<string>(PathComparer));
        DeletePayload(source, changed);
    }
    private static bool HasNestedLinks(string directory, HashSet<string> ancestors)
    {
        string physical = Physical(directory); Enter(directory, ancestors);
        try
        {
            foreach (string child in Directory.GetFileSystemEntries(Native(directory)))
            {
                if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) != 0) return true;
                if (Directory.Exists(child) && HasNestedLinks(Logical(child), ancestors)) return true;
            }
            return false;
        }
        finally { ancestors.Remove(physical); }
    }
    private static void CopyPayload(string source, string destination, HashSet<string> ancestors)
    {
        if (!Directory.Exists(Native(source))) { File.Copy(Native(source), Native(destination), false); return; }
        string physical = Physical(source); Enter(source, ancestors);
        try
        {
            Directory.CreateDirectory(Native(destination));
            foreach (string child in Directory.GetFileSystemEntries(Native(source)))
                CopyPayload(Logical(child), Path.Combine(destination, Path.GetFileName(child)), ancestors);
            Directory.SetLastWriteTimeUtc(Native(destination), Directory.GetLastWriteTimeUtc(Native(source)));
        }
        finally { ancestors.Remove(physical); }
    }

}

internal sealed class BridgeRequiredFilePermissionException : UnauthorizedAccessException
{
    internal BridgeRequiredFilePermissionException(string operation, string path, Exception cause)
        : base("Bridge Builder requires " + operation + " permission: " + path + "; " + cause.Message, cause) { }
}
