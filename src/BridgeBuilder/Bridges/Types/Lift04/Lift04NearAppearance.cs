using System;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Minimal measured phase-gradient domains; ordinary period resets do not cut receivers.
internal static class Lift04NearAppearance
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_Lift04_ContinuousAppearance");
    internal static MovableRoadAppearance.Level Create(int lod, RenderPrefab native, Pose roadToObject) => Prepared(lod, native.GetSurfaceAsset(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[0]), BridgePreparedMaterials.RepeatUnit("Lift04", "128604022d6d3d98bf5940187aa449ef214e75095ad81c04b10279e8b8275d0c"), BridgePreparedMaterials.RepeatUnit("Lift04", "2bc06fd00989705f14f9bc1c57c4295e38ae2292dfcb776d49aec1dfee80b746"), roadToObject);
    internal static MovableRoadAppearance.Level Prepared(int lod, SurfaceAsset native, SurfaceAsset repeating, SurfaceAsset finite, Pose roadToObject)
    {
        if (Data.I32[Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[1]] != Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[2] || lod < Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[3] || lod >= Data.I32[Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[4]])
            throw new InvalidOperationException("Invalid Lift04 continuous appearance schema or LOD.");
        var count = Data.I32[Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[5]];
        var stride = Data.I32[Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[6]];
        var patches = new Patch[count];
        var support = new MovableRoadAppearance.Type[count];
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var regions = Partition.Gap;
        Partition Cut(float station, Partition negative, Partition positive)
        {
            var frame = Matrix4x4.identity;
            frame.m23 = -station;
            return new Partition(frame * pose, negative, positive);
        }

        for (int pair = count - Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[7]; pair >= Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[8]; pair--)
        {
            int first = (lod * count + pair) * stride;
            {
                int at = first;
                float F(int offset) => Data.F32[at + offset];
                bool repeatU = F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[9]) != Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[10];
                var u = new Vector4(F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[11]), Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[12], F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[13]), F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[14]));
                var v = new Vector4(F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[15]), Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[16], F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[17]), F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[18]));
                var min = repeatU ? Vector2.zero : new Vector2(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.F32[0], Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[19]);
                var max = repeatU ? Vector2.one : new Vector2(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.F32[1], Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[20]);
                if (!repeatU)
                {
                    u *= max.x - min.x;
                    u.w += min.x;
                }

                var tangent = Quaternion.Inverse(roadToObject.rotation) * new Vector3(F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[21]), F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[22]), F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[23]));
                // Both offline products contain valid source samples over their entire texture.
                // Content bounds above still define phase and finite-axis density.
                patches[pair] = new Patch(min, max, pose.transpose * u, pose.transpose * v, repeatU ? repeating : finite, preparedUnit: true, repeatU: repeatU, repeatV: true, tangent: new Vector4(tangent.x, tangent.y, tangent.z, F(Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[24])))
                    .WithFiniteSampling(Vector2.zero, Vector2.one);
                support[pair] = MovableRoadAppearance.Type.Ballast;
            }

            var sides = new Partition(pair);
            regions = Cut(Data.F32[first], Partition.Gap, Cut(Data.F32[first + Numeric_Types_Lift04_Lift04NearAppearance_cs.Values.I32[25]], sides, regions));
        }

        return new MovableRoadAppearance.Level(native, patches, support, regions, regions);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04NearAppearance_cs
internal static class Numeric_Types_Lift04_Lift04NearAppearance_cs
{
    static Numeric_Types_Lift04_Lift04NearAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift04_Lift04NearAppearance_cs");
}
