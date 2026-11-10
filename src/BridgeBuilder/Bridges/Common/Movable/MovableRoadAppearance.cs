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
        TrackBed = 32,
        Curb = 64
    }

    internal sealed class Level
    {
        internal readonly SurfaceAsset Native;
        internal readonly string? Error;
        internal readonly MovableExistingRoadSurface.Patch[] Patches;
        internal readonly Type[] SupportedTypes;
        // Null inherits the caller's partition; it does not mean no correspondence.
        // Retained unmatched geometry uses an explicit valid leaf with Type.None.
        // Partition.Gap removes geometry and must not represent a material no-match.
        internal readonly MovableRoadGeometry.Partition? Carriageway;
        internal readonly MovableRoadGeometry.Partition? Sidewalk;

        internal Level(SurfaceAsset native, MovableExistingRoadSurface.Patch[] patches,
            Type[] supportedTypes, MovableRoadGeometry.Partition? carriageway = null,
            MovableRoadGeometry.Partition? sidewalk = null)
        {
            if (patches.Length != supportedTypes.Length)
                Error="Road appearance support must match the LOD's physical domains.";
            Native = native;
            Patches = patches;
            SupportedTypes = supportedTypes;
            Carriageway = carriageway;
            Sidewalk = sidewalk;
        }
        internal Level(MovableExistingRoadSurface.SurfaceResult native,MovableExistingRoadSurface.Patch[] patches,
            Type[] supportedTypes,MovableRoadGeometry.Partition? carriageway=null,MovableRoadGeometry.Partition? sidewalk=null)
            :this(native.Value!,patches,supportedTypes,carriageway,sidewalk){Error=native.Error??Error;}
    }
}
