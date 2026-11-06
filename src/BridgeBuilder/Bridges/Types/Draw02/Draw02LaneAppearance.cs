using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;
using Type = BridgeBuilder.Bridges.MovableRoadAppearance.Type;
using Kind = BridgeBuilder.Bridges.MovableRoadLaneLayout.Kind;

namespace BridgeBuilder.Bridges;

// A complete measured two-grating lane pattern, translated to actual native lanes.
// The same high-detail-derived road unit is shared at all three geometry LODs.
internal static class Draw02LaneAppearance
{
    internal static MovableRoadAppearance.Level Apply(MovableRoadAppearance.Level source,
        MovableSelectedRoadLayout layout, Pose pose)
    {
        var d = Numeric_Types_Draw02_Draw02LaneAppearance_cs.Values.F32;
        var width = d[0]; var period = d[1]; var visualU = d[2]; var backgroundWidth = d[3];
        var motion = Numeric_Types_Draw02_Draw02RoadPartitions_cs.Values.F32;
        var leafEnd = motion[1];
        var transform = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one);
        var projection = transform.transpose;
        var patches = new List<Patch>(source.Patches);
        var support = new List<Type>(source.SupportedTypes);
        var laneSurface = BridgePreparedMaterials.RepeatUnit("Draw02", "LanePattern");
        var background = BridgePreparedMaterials.RepeatUnit("Draw02", "LaneBackground");
        Partition X(float x, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m00 = 0; frame.m02 = -1;
            frame.m20 = 1; frame.m22 = 0; frame.m23 = -x;
            return new Partition(frame * transform, negative, positive);
        }
        Partition Z(float z, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity; frame.m23 = -z;
            return new Partition(frame * transform, negative, positive);
        }
        int Add(float center, bool pattern, float sign)
        {
            var u = projection * (pattern
                ? new Vector4(-1 / width, 0, 0, visualU + center / width)
                : new Vector4(-1 / backgroundWidth, 0, 0, 0));
            var v = projection * new Vector4(0, 0, -sign / period, 1);
            int id = patches.Count;
            patches.Add(new Patch(Vector2.zero, Vector2.one, u, v,
                pattern ? laneSurface : background, preparedUnit: true,
                repeatU: true, repeatV: true, projectTangent: true));
            support.Add(Type.Asphalt);
            return id;
        }
        Partition Half(float low, float high, float sign)
        {
            int backgroundId = Add(0, false, sign);
            var stations = new SortedSet<float> { low, high };
            foreach (float z in layout.LaneStations) if (z > low && z < high) stations.Add(z);
            var zs = new List<float>(stations);
            Partition result = new Partition(backgroundId);
            for (int station = zs.Count - 2; station >= 0; station--)
            {
                float z = (zs[station] + zs[station + 1]) * .5f;
                var lanes = new List<MovableRoadLaneLayout.Lane>();
                foreach (var lane in layout.Lanes)
                    if (lane.Type == Kind.Car && lane.StartZ <= z && lane.EndZ >= z)
                    {
                        // Evaluate the existing station mapping for every selected lane;
                        // curvature is not a generation admission condition.
                        lanes.Add(lane);
                    }
                lanes.Sort((a,b) => a.LeftAt(z).CompareTo(b.LeftAt(z)));
                Partition strip = new Partition(backgroundId);
                for (int i = lanes.Count - 1; i >= 0; i--)
                {
                    var lane = lanes[i]; float center = lane.CenterAt(z);
                    float left = Mathf.Max(lane.LeftAt(z), center + (visualU - 1) * width);
                    float right = Mathf.Min(lane.RightAt(z), center + visualU * width);
                    if (i > 0 && lane.LeftAt(z) == lanes[i - 1].LeftAt(z) && lane.RightAt(z) == lanes[i - 1].RightAt(z)) continue;
                    if (right <= left) continue;
                    int id = Add(center, true, sign);
                    strip = X(left, new Partition(backgroundId), X(right, new Partition(id), strip));
                }
                result = Z(zs[station + 1], strip, result);
            }
            return result;
        }
        var original = source.Carriageway ?? throw new InvalidOperationException("Draw02 lane appearance requires its existing part routing.");
        var moving = Z(0, Half(-leafEnd, 0, -1), Half(0, leafEnd, 1));
        var car = Z(-leafEnd, original, Z(leafEnd, moving, original));
        return new MovableRoadAppearance.Level(source.Native, patches.ToArray(), support.ToArray(), car, source.Sidewalk);
    }
}

// Embedded numerical arrays: Types_Draw02_Draw02LaneAppearance_cs
internal static class Numeric_Types_Draw02_Draw02LaneAppearance_cs
{
    static Numeric_Types_Draw02_Draw02LaneAppearance_cs() { }
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw02_Draw02LaneAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Draw02_Draw02LaneAppearance_cs"));
}
