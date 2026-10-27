namespace Colossal.IO.AssetDatabase
{
    internal sealed class SurfaceAsset(string cid, byte[] data, bool builtin=true)
    {
        internal sealed class Identity(string value) { public object guid=value; }
        public Identity id=new(cid);
        public bool isBuiltin=builtin;
        public int Reads;
        public Stream GetReadStream(){Reads++;return new MemoryStream(data,false);}
    }
}
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
