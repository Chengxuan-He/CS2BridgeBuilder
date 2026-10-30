using System;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Appearance = BridgeBuilder.Bridges.MovableRoadAppearance;

namespace BridgeBuilder.Bridges;

// Offline-composed native road assets. Structural source Surfaces remain untouched.
internal static class Draw03PreparedRoad
{
    private static float[] Data => Numeric_Types_Draw03_Draw03PreparedRoad_cs.Values.F32;
    private static int NativeRows => Numeric_Types_Draw03_Draw03PreparedRoad_cs.Values.I32[0];
    private static int NearDomains => Numeric_Types_Draw03_Draw03RoadAppearance_cs.Values.I32[0];
    private static int Units => NativeRows * 8;
    private static SurfaceAsset Unit(string name) => BridgePreparedMaterials.RepeatUnit("Draw03", name);

    private static Patch Prepared(Patch source, string unit, Vector4 u, Vector4 v, int guard)
    {
        v *= Data[Units + guard + 1];
        v.w += Data[Units + guard];
        return new Patch(Vector2.zero, Vector2.one, u, v, Unit(unit), repeat: true,
            preparedUnit: true, repeatU: true, repeatV: false, projectTangent: true);
    }

    internal static Patch Band(Patch source, int index)
    {
        var reduced = Draw03RoadWearUnits.ForBand(source, index);
        var band = source.Bands![index];
        var numbers = Numeric_Types_Draw03_Draw03RoadWear_cs.Values.F32;
        bool gap = band.Min.y < numbers[3] / numbers[4];
        var v = reduced.V;
        // The gap's original transverse region is finite. Its old repeatY
        // multiplier must not survive in the prepared finite-axis mapping.
        if (gap) v /= band.RepeatY;
        return Prepared(reduced, gap ? "FullPeriodLaneGap" : "FullPeriodLaneWear",
            reduced.U * Data[Units + 8], v, gap ? 2 : 0);
    }

    private static Vector2 Span(MovableSelectedRoadLayout layout, Vector2 span, bool walk, bool right)
    {
        var sidewalk = right ? layout.RightSidewalk : layout.LeftSidewalk;
        if (walk) return sidewalk;
        bool present = sidewalk.y > sidewalk.x;
        return right ? new Vector2(0, present ? sidewalk.x : span.y)
            : new Vector2(present ? sidewalk.y : span.x, 0);
    }

    internal static Appearance.Level Near(SurfaceAsset native, MovableSelectedRoadLayout layout,
        Pose pose, Vector2 span)
    {
        var carriageway = Draw03RoadAppearance.Regions(false, pose);
        var sidewalk = Draw03RoadAppearance.Regions(true, pose);
        var patches = Draw03RoadWearDomains.Apply(Draw03RoadAppearance.Patches(layout, pose, span),
            ref carriageway, ref sidewalk, prepared: true);
        var types = new Appearance.Type[patches.Length];
        var frame = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one).transpose;
        for (int i = 0; i < patches.Length; i++)
        {
            bool walk = i < NearDomains && i % 4 >= 2;
            types[i] = walk ? Appearance.Type.Sidewalk : Appearance.Type.Asphalt | Appearance.Type.WheelTrack;
            if (i >= NearDomains || patches[i].Bands != null) continue;
            bool right = i % 2 != 0;
            var interval = Span(layout, span, walk, right);
            float width = interval.y - interval.x;
            // Empty sidewalk domains are never assigned by the occurrence tree.
            var v = width > 0 ? frame * new Vector4((right ? -1 : 1) / width, 0, 0,
                (right ? interval.y : -interval.x) / width) : Vector4.zero;
            // One offline native source period; continuous U never adds a
            // geometry wrap seam. Filtering differences remain recorded in the
            // preparation report and are not accepted by this mapping choice.
            patches[i] = Prepared(patches[i], walk ? "FullPeriodGrating" : "FullPeriodCleanAsphalt",
                patches[i].U, v, walk ? 6 : 4);
        }
        return new Appearance.Level(native, patches, types, carriageway, sidewalk);
    }

    internal static Appearance.Level Far(SurfaceAsset native, MovableSelectedRoadLayout layout,
        Pose pose, Vector2 span)
    {
        var source = Unit("FarRoadAtlas");
        var patches = new Patch[NativeRows * 2];
        var types = new Appearance.Type[patches.Length];
        var objectFrame = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var frame = objectFrame.transpose;
        for (int walk = 0; walk < 2; walk++)
        for (int row = 0; row < NativeRows; row++)
        {
            bool right = row % 2 != 0;
            var interval = Span(layout, span, walk != 0, right);
            float inner = walk != 0 ? Data[Units + 9] : 0;
            float outer = walk != 0 ? Data[Units + 10] : Data[Units + 9];
            float low = right ? inner : -outer, high = right ? outer : -inner;
            float width = interval.y - interval.x;
            float scale = width > 0 ? (high - low) / width : 0;
            float offset = low - scale * interval.x;
            int at = row * 8, index = walk * NativeRows + row;
            var u = frame * new Vector4(Data[at + 2] * scale, 0, Data[at + 3], Data[at + 4] + Data[at + 2] * offset);
            var v = frame * new Vector4(Data[at + 5] * scale, 0, Data[at + 6], Data[at + 7] + Data[at + 5] * offset);
            patches[index] = new Patch(Vector2.zero, Vector2.one, u, v, source, sourceUv: true);
            types[index] = walk != 0 ? Appearance.Type.Sidewalk : Appearance.Type.Asphalt | Appearance.Type.WheelTrack;
        }
        Partition Tree(int start, int end, int offset)
        {
            if (end - start == 1)
            {
                var seam = Matrix4x4.identity;
                seam.m00 = 0; seam.m02 = -1; seam.m20 = 1; seam.m22 = 0;
                return new Partition(seam * objectFrame, new Partition(offset + start * 2), new Partition(offset + start * 2 + 1));
            }
            int middle = (start + end) / 2;
            // Original finite atlas quad boundaries, including native resets;
            // never period-driven subdivision or a near-LOD tile at far LOD.
            float boundary = (Data[(middle * 2 - 2) * 8 + 1] + Data[middle * 2 * 8]) * 0.5f;
            var cut = Matrix4x4.identity; cut.m23 = -boundary;
            return new Partition(cut * objectFrame, Tree(start, middle, offset), Tree(middle, end, offset));
        }
        return new Appearance.Level(native, patches, types, Tree(0, NativeRows / 2, 0), Tree(0, NativeRows / 2, NativeRows));
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03PreparedRoad_cs
internal static class Numeric_Types_Draw03_Draw03PreparedRoad_cs
{
    static Numeric_Types_Draw03_Draw03PreparedRoad_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03PreparedRoad_cs");
}
