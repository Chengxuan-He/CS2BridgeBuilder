using System.IO.Compression;
using System.Security.Cryptography;

internal static class NativeNeutralControl
{
    // Every BC7 block, including tail mips, decodes to RGBA(0,0,0,0).
    internal const string Cid = "09426c88de2bf8983a7e1943ea8a21ad";
    internal static string Verify(string gameData)
    {
        using var archive = ZipFile.OpenRead(Path.Combine(gameData, "Content/Game/Blob_FreeUpdate02.cok"));
        const string name = "PlaygroundDecal02_ControlMask_a2f87c008035cf70f1a89c75cac39a0d.Texture";
        using var sidecar = new StreamReader(archive.GetEntry(name + ".cid")!.Open());
        using var payload = archive.GetEntry(name)!.Open();
        if (sidecar.ReadToEnd().Trim() != Cid || Convert.ToHexString(SHA256.HashData(payload)) != "F7E9F3EAC0C772313C57AE87B35229DF152835C7181B6A86F3BE287BA0CA483F")
            throw new InvalidDataException("Review the native neutral ControlMask before regenerating descriptors.");
        return Cid;
    }
}
