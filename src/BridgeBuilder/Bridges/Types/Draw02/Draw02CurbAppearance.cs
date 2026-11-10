using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Complete native curb receivers at actual Sidewalk components. The moving
// closure and fixed-platform curb have different measured prototype materials.
internal static class Draw02CurbAppearance
{
    internal static bool TryApply(Pose pose, int lod, MovableClosedRoad.Result road, ref MovableRoadGeometry.BoundTriangle[] faces, ref MovableExistingRoadSurface.Patch[] patches, ref MovableRoadAppearance.Type[] types,out string? error)
    {
        error=null;
        var output = new List<MovableExistingRoadSurface.Patch>(patches);
        var support = new List<MovableRoadAppearance.Type>(types);
        var sources = new MovableRoadRoute.Source? [road.Surfaces.Length];
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        for (int i = Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.I32[0]; i < faces.Length; i++)
        {
            var face = faces[i];
            int domain = (int)(face.Bone & ~MovableSurfaceAtlas.MovingSurface);
            // Reuse the already-resolved physical counterpart, not a bridge-wide
            // type declaration. Unmatched approach and shoulder domains remain Original.
            if ((types[domain] & MovableRoadAppearance.Type.Sidewalk) == (BridgeBuilder.Bridges.MovableRoadAppearance.Type)(Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.I32[1]))
                continue;
            var triangle = face.Geometry;
            var section = road.Occurrences[triangle.Occurrence].RootSection;
            if (section == null)
                continue;
            if (section.name != "Sidewalk" && !section.name.StartsWith("Sidewalk ", StringComparison.Ordinal))
                continue;
            if(!sources[triangle.Material].HasValue)
            {
                if(!MovableRoadRoute.TryRead(road.Surfaces[triangle.Material],out var read,out error))return false;
                sources[triangle.Material]=read;
            }
            var source=sources[triangle.Material]!.Value;
            if (source.Receiver(triangle) != MovableRoadAppearance.Type.Curb)
                continue;
            var a = transform.MultiplyPoint3x4(triangle.A.Position);
            var b = transform.MultiplyPoint3x4(triangle.B.Position);
            var c = transform.MultiplyPoint3x4(triangle.C.Position);
            if (Mathf.Max(Mathf.Abs(a.z), Mathf.Max(Mathf.Abs(b.z), Mathf.Abs(c.z))) > Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[0])
                continue;
            bool moving = (face.Bone & MovableSurfaceAtlas.MovingSurface) != (uint)(Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.I32[2]);
            float width = moving ? Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[1] : Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[2];
            var u = MovableSurfaceAtlas.OriginalProjection(triangle).U;
            u.w -= Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[3];
            u *= Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[4] / Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[5] / width;
            // Both native leaves use V decreasing away from their central seam.
            // The fixed curb's measured 82..86 m source interval has half a cycle
            // of phase relative to the moving closure's 40..44 m interval.
            float sign = a.z + b.z + c.z < Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[6] ? -Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[7] : Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[8];
            var v = projection * new Vector4(Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[9], Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[10], -sign / Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[11], moving ? Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[12] : Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values.F32[13]);
            var patch = new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one, u, v, BridgePreparedMaterials.RepeatUnit("Draw02", moving ? "MovingWalkEdge" : "FixedWalkEdge"), preparedUnit: true, repeatU: true, repeatV: true, projectTangent: true);
            faces[i] = new MovableRoadGeometry.BoundTriangle(triangle, (uint)output.Count | (face.Bone & MovableSurfaceAtlas.MovingSurface));
            output.Add(patch);
            support.Add(MovableRoadAppearance.Type.Curb);
        }

        patches = output.ToArray();
        types = support.ToArray();return true;
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02CurbAppearance_cs
internal static class Numeric_Types_Draw02_Draw02CurbAppearance_cs
{
    static Numeric_Types_Draw02_Draw02CurbAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02CurbAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02CurbAppearance_cs"));
}
