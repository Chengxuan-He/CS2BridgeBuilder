using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// The measured Lift03 road grain and complete wear profile are independent of
// selected road width. Physical source trees still delimit prototype support.
internal static class Lift03CleanRoadAppearance
{
    private static float[] Data => Numbers.Values.F32;

    internal static MovableRoadAppearance.Level Apply(MovableRoadAppearance.Level source, MovableSelectedRoadLayout layout, Pose pose)
    {
        var background = BridgePreparedMaterials.RepeatUnit("Lift03", "CleanRoadBackground");
        var wear = BridgePreparedMaterials.RepeatUnit("Lift03", "CleanLaneWear");
        var frame = Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one).transpose;
        var patches = new List<Patch>(source.Patches);
        var types = new List<MovableRoadAppearance.Type>(source.SupportedTypes);
        Patch Map(bool worn, float center)
        {
            float width = worn ? Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[0]] : Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[1]];
            // The original source's visual wear centre, not the crop midpoint,
            // is placed on the actual native lane centre. No width scaling.
            float phase = worn ? (Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[2]] - Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[3]] - center * Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[4]]) / width : Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.F32[0];
            return new Patch(Vector2.zero, Vector2.one, frame * new Vector4(Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[5]] / width, Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[6], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[7], phase), frame * new Vector4(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[8], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[9], -Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[10]] / Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[11]], Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[12]]), worn ? wear : background, repeat: true, preparedUnit: true, repeatU: !worn, repeatV: true, projectTangent: true);
        }

        var replacements = new Dictionary<int, Partition>();
        Partition Expand(Partition node)
        {
            if (node.IsGap)
                return node;
            if (node.Bone < Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[13])
                return new Partition(node.ObjectToSeam, Expand(node.Negative!), Expand(node.Positive!));
            if ((source.SupportedTypes[node.Bone] & MovableRoadAppearance.Type.Asphalt) == (BridgeBuilder.Bridges.MovableRoadAppearance.Type)(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[14]))
                return node;
            if (replacements.TryGetValue(node.Bone, out var existing))
                return existing;
            int backgroundIndex = patches.Count;
            patches.Add(Map(false, Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[15]));
            types.Add(MovableRoadAppearance.Type.Asphalt);
            Partition result = new Partition(backgroundIndex);
            var centers = layout.CarLaneCenters;
            // Adjacent lanes may overlap the fixed-width donor. Only coverage is
            // clipped at their midpoint; the donor UV gradient remains unchanged.
            for (int i = centers.Length - Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[16]; i >= Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[17]; i--)
            {
                float low = centers[i] + (Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[18]] - Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[19]]) / Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[20]];
                float high = centers[i] + (Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[21]] + Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[22]] - Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[23]]) / Data[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[24]];
                if (i > Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[25])
                    low = Mathf.Max(low, (centers[i - Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[26]] + centers[i]) * Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.F32[1]);
                if (i + Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[27] < centers.Length)
                    high = Mathf.Min(high, (centers[i] + centers[i + Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[28]]) * Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.F32[2]);
                if (high <= low)
                    continue;
                int index = patches.Count;
                patches.Add(Map(true, centers[i]));
                types.Add(MovableRoadAppearance.Type.Asphalt | MovableRoadAppearance.Type.WheelTrack);
                Partition Cut(float x, Partition negative, Partition positive)
                {
                    var seam = Matrix4x4.identity;
                    seam.SetRow(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[29], frame * new Vector4(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[30], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[31], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[32], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[33]));
                    seam.SetRow(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[34], frame * new Vector4(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[35], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[36], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[37], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[38]));
                    seam.SetRow(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[39], frame * new Vector4(Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[40], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[41], Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[42], -x));
                    return new Partition(seam, negative, positive);
                }

                result = Cut(low, new Partition(backgroundIndex), Cut(high, new Partition(index), result));
            }

            replacements.Add(node.Bone, result);
            return result;
        }

        var carriageway = Expand(source.Carriageway!);
        var sidewalk = Expand(source.Sidewalk!);
        return new MovableRoadAppearance.Level(source.Native, patches.ToArray(), types.ToArray(), carriageway, sidewalk);
    }

    private static class Numbers
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_CleanRoadAppearance");
        static Numbers()
        {
            if (Values.I32.Length != Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[43] || Values.I32[Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[44]] != Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[45] || Values.F32.Length != Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs.Values.I32[46])
                throw new InvalidOperationException("Invalid Lift03 clean road source profile.");
        }
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03CleanRoadAppearance_cs
internal static class Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs
{
    static Numeric_Types_Lift03_Lift03CleanRoadAppearance_cs()
    {
    }

    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03CleanRoadAppearance_cs");
}
