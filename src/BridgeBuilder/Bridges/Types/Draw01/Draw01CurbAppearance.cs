using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Only the measured selected-road curb within a prototype sidewalk edge is supported.
internal static class Draw01CurbAppearance
{
    internal static bool TryApply(MovableClosedRoad.Result road, Pose pose, ref MovableRoadGeometry.BoundTriangle[] faces, ref MovableExistingRoadSurface.Patch[] patches, ref MovableRoadAppearance.Type[] types,out string? error)
    {
        error=null;
        var output = new List<MovableExistingRoadSurface.Patch>(patches);
        var support = new List<MovableRoadAppearance.Type>(types);
        var sources = new MovableRoadRoute.Source? [road.Surfaces.Length];
        var frame = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one).transpose;
        for (int i = Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[0]; i < faces.Length; i++)
        {
            var face = faces[i];
            var triangle = face.Geometry;
            uint domain = face.Bone & ~MovableSurfaceAtlas.MovingSurface;
            int part = (int)domain % Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[1];
            if (part != Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[2] && part != Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[3] && part != Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[4])
                continue;
            var section = road.Occurrences[triangle.Occurrence].RootSection;
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
            bool moving = part != Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[5];
            string key = moving ? "17426aa290b6e326ec517916b4e1db8bb0fefb240e34feae2fb214dfa1ccf1bf" : "d469e9228140bbb52175544326369c6cacd9a6ab1c5dee3aa490ff4bbc3e25b5";
            float periodX = moving ? Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[0] : Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[1];
            float periodZ = moving ? Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[2] : Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[3];
            // Preserve the native cross-section traversal on top, slope and side.
            // RoadEU's measured 0.200012207 m section spans 0.0146484375 UV.
            var u = MovableSurfaceAtlas.OriginalProjection(triangle).U;
            u.w -= Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[4];
            u *= Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[5] / Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.F32[6] / periodX;
            var v = frame * new Vector4(Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[6], Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[7], -Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[8] / periodZ, Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values.I32[9]);
            var patch = new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one, u, v, BridgePreparedMaterials.RepeatUnit("Draw01", key), preparedUnit: true, repeatU: true, repeatV: true, projectTangent: true);
            faces[i] = new MovableRoadGeometry.BoundTriangle(triangle, (uint)output.Count | (face.Bone & MovableSurfaceAtlas.MovingSurface));
            output.Add(patch);
            support.Add(MovableRoadAppearance.Type.Curb);
        }

        patches = output.ToArray();
        types = support.ToArray();return true;
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01CurbAppearance_cs
internal static class Numeric_Types_Draw01_Draw01CurbAppearance_cs
{
    static Numeric_Types_Draw01_Draw01CurbAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01CurbAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01CurbAppearance_cs"));
}
