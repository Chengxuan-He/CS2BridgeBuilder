using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace BridgeBuilder.Bridges;

// Generation-time compiler. This does not install a runtime loader or claim that
// an owner-local bundle is discoverable by the game's native asset database.
internal static class MovableCustomShaderProgram
{
    // The authoring seed contains native reflection/pass records and complete HLSL
    // for each generated vertex adapter. Pixel programs retain the source shader's
    // HDRP implementation. Compilation replaces complete declared programs and
    // rebuilds their containers; an unknown layout is an input error, not a fallback.
    internal static bool TryCompilePackage(Stream input, out byte[] bundle, out string? error)
    {
        bundle = Array.Empty<byte>(); error = null;
        try
        {
            using var reader = new BinaryReader(input, Encoding.UTF8, leaveOpen: true);
            uint magic = reader.ReadUInt32(); int version = reader.ReadInt32();
            if (magic != 0x31534242u || (version != 1 && version != 2))
            { error = "Unsupported generated material program package."; return false; }
            byte[] ReadBytes(int maximum)
            {
                int count = reader.ReadInt32();
                if (count < 0 || count > maximum) throw new InvalidDataException("Invalid material package field length.");
                var bytes = reader.ReadBytes(count);
                if (bytes.Length != count) throw new EndOfStreamException("Incomplete material program package.");
                return bytes;
            }
            var candidate = ReadBytes(64 * 1024 * 1024);
            var replacements = new System.Collections.Generic.List<MovableShaderContainer.Replacement>();
            int programs = reader.ReadInt32();
            if (programs <= 0 || programs > 128)
            { error = "Invalid generated material program count."; return false; }
            for (int i = 0; i < programs; i++)
            {
                string source = Encoding.UTF8.GetString(ReadBytes(1024 * 1024));
                string entry = version == 1 ? "BBNetworkVertex" : Encoding.UTF8.GetString(ReadBytes(64));
                string profile = version == 1 ? "vs_5_0" : Encoding.UTF8.GetString(ReadBytes(16));
                if (!System.Text.RegularExpressions.Regex.IsMatch(entry, @"\A[A-Za-z_][A-Za-z0-9_]{0,63}\z") ||
                    (profile != "vs_5_0" && profile != "ps_5_0"))
                { error = "Unsupported generated material program entry or stage."; return false; }
                var expected = ReadBytes(1024 * 1024);
                if (!TryCompile(source, entry, profile, out var compiled, out error)) return false;
                int copies = reader.ReadInt32();
                if (copies <= 0 || copies > 1024)
                { error = "Invalid generated material program slot count."; return false; }
                for (int copy = 0; copy < copies; copy++)
                {
                    int offset = reader.ReadInt32();
                    if (offset < 0 || offset > candidate.Length - expected.Length)
                    { error = "Generated material program slot lies outside its container."; return false; }
                    for (int j = 0; j < expected.Length; j++)
                        if (candidate[offset + j] != expected[j])
                        { error = "Generated material program seed does not match its slot."; return false; }
                    replacements.Add(new MovableShaderContainer.Replacement { Offset = offset, Expected = expected, Compiled = compiled });
                }
            }
            if (input.ReadByte() != -1)
            { error = "Unexpected trailing material program data."; return false; }
            bundle = MovableShaderContainer.Rebuild(candidate, replacements); return true;
        }
        catch (Exception exception)
        {
            error = "Generated material package: " + exception.GetType().Name + ": " + exception.Message;
            return false;
        }
    }

    internal static bool TryCompile(string source, string entry, string profile,
        out byte[] bytecode, out string? error)
    {
        bytecode = Array.Empty<byte>(); error = null;
        IntPtr code = IntPtr.Zero, diagnostic = IntPtr.Zero;
        try
        {
            var bytes = Encoding.UTF8.GetBytes(source);
            // Preserve the source arithmetic order. No editor compiler or include
            // callback is used; the complete generated source is the input.
            int status = D3DCompile(bytes, (UIntPtr)bytes.Length, "BridgeBuilder.GeneratedMaterial",
                IntPtr.Zero, IntPtr.Zero, entry, profile, 1u << 15, 0, out code, out diagnostic);
            if (status < 0)
            {
                error = "Generated material compilation: " + (diagnostic == IntPtr.Zero
                    ? "HRESULT " + status : Encoding.UTF8.GetString(ReadBlob(diagnostic)));
                return false;
            }
            bytecode = ReadBlob(code);
            return true;
        }
        catch (Exception exception)
        {
            error = "Generated material compilation: " + exception;
            return false;
        }
        finally
        {
            ReleaseBlob(code); ReleaseBlob(diagnostic);
        }
    }

    private static byte[] ReadBlob(IntPtr pointer)
    {
        var table = Marshal.ReadIntPtr(pointer);
        var address = (BlobPointer)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, 3 * IntPtr.Size), typeof(BlobPointer));
        var size = (BlobSize)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, 4 * IntPtr.Size), typeof(BlobSize));
        var result = new byte[checked((int)size(pointer).ToUInt64())];
        Marshal.Copy(address(pointer), result, 0, result.Length);
        return result;
    }
    private static void ReleaseBlob(IntPtr pointer)
    {
        if (pointer == IntPtr.Zero) return;
        var table = Marshal.ReadIntPtr(pointer);
        ((BlobRelease)Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(table, 2 * IntPtr.Size), typeof(BlobRelease)))(pointer);
    }
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate IntPtr BlobPointer(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate UIntPtr BlobSize(IntPtr self);
    [UnmanagedFunctionPointer(CallingConvention.StdCall)] private delegate uint BlobRelease(IntPtr self);
    [DllImport("d3dcompiler_47.dll", CallingConvention = CallingConvention.StdCall)]
    private static extern int D3DCompile(byte[] source, UIntPtr size, string name, IntPtr defines,
        IntPtr include, string entry, string target, uint flags, uint effectFlags,
        out IntPtr code, out IntPtr error);
}
