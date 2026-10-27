namespace BridgeBuilder.Bridges
{
    internal static class MovableStructureKey
    {
        internal static string Digest(byte[] b)=>Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(b)).ToLowerInvariant();
        internal static string For(string digest,bool triangle,object bounds,object a,object b,object c)=>digest;
    }
    internal static class MovableStructurePacking
    {
        // Boundary test doubles only: containment/gradient arithmetic is not under test here.
        internal sealed class Unit { internal bool CanUseSource,Triangle;internal object Bounds=new(),A=new(),B=new(),C=new(); }
    }
    internal static class MovableStructureTiles
    {
        internal sealed class Slot { internal Colossal.IO.AssetDatabase.SurfaceAsset Surface=null!;internal MovableStructurePacking.Unit? Original; }
    }
}
