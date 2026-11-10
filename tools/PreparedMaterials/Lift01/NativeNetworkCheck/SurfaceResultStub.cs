// Control-flow seam; native SurfaceAsset behavior is covered by MaterialResult125Check.
namespace BridgeBuilder.Bridges;
internal static class MovableExistingRoadSurface
{
    internal readonly struct SurfaceResult
    {
        internal readonly Colossal.IO.AssetDatabase.SurfaceAsset? Value;
        internal readonly string? Error;
        internal bool Success=>Error==null;
        internal SurfaceResult(Colossal.IO.AssetDatabase.SurfaceAsset? value,string? error=null){Value=error==null?value:null;Error=error;}
        public static implicit operator SurfaceResult(Colossal.IO.AssetDatabase.SurfaceAsset value)=>new(value);
    }
}
