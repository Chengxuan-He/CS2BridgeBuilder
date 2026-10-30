using System;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Appearance support belongs to an individual prototype domain and LOD. These
// are independent layers, not section names or permission to replace a whole road.
internal static class MovableRoadAppearance
{
    [Flags]
    internal enum Type
    {
        None = 0,
        Asphalt = 1,
        Sidewalk = 2,
        WheelTrack = 4,
        Pathway = 8,
        Ballast = 16,
        TrackBed = 32
    }

    internal sealed class Level
    {
        internal readonly SurfaceAsset Native;
        internal readonly MovableExistingRoadSurface.Patch[] Patches;
        internal readonly Type[] SupportedTypes;
        internal readonly MovableRoadGeometry.Partition? Carriageway;
        internal readonly MovableRoadGeometry.Partition? Sidewalk;

        internal Level(SurfaceAsset native, MovableExistingRoadSurface.Patch[] patches,
            Type[] supportedTypes, MovableRoadGeometry.Partition? carriageway = null,
            MovableRoadGeometry.Partition? sidewalk = null)
        {
            if (patches.Length != supportedTypes.Length)
                throw new ArgumentException("Road appearance support must match the LOD's physical domains.");
            Native = native;
            Patches = patches;
            SupportedTypes = supportedTypes;
            Carriageway = carriageway;
            Sidewalk = sidewalk;
        }
    }
}
