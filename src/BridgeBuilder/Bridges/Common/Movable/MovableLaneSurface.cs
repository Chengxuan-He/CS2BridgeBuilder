using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
namespace BridgeBuilder.Bridges;
internal static class MovableLaneSurface
{
    internal static bool TryCreate(SurfaceAsset source, MovableMaterialConversion.GeometryInput geometry,
        string owner, string styleId, PreviewGeometry? preview, List<AssetData> assets,
        out SurfaceAsset material, out string? error)
        => MovableMaterialConversion.TryCreate(source, geometry, owner, styleId, preview, assets, out material, out error);
}
