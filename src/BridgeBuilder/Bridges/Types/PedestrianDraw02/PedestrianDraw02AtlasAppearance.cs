using System;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Native finite strips and four continuous half-leaf repeat appearances.
// Retained contour folds belong to the real footprint, not atlas phase resets.
// No prototype faces are stored or intersected at generation time.
// Embedded numerical arrays: Types_PedestrianDraw02_AtlasAppearance
internal static class PedestrianDraw02AtlasAppearance
{
    private static readonly HardcodedNumbers Data = HardcodedNumbers.Load("Types_PedestrianDraw02_AtlasAppearance");
    // Embedded numerical arrays: Types_PedestrianDraw02_SourceTangentFields
    private static readonly HardcodedNumbers TangentFields = HardcodedNumbers.Load("Types_PedestrianDraw02_SourceTangentFields");

    internal static MovableRoadAppearance.Level Create(SurfaceAsset native, Pose roadToObject, float halfWidthDelta)
        => Prepared(native,
            BridgePreparedMaterials.RepeatUnit("PedestrianDraw02", "32b19a6ad3c9751659a935b6b819bcdf6c9e8307b42714b56b500392c22c2ce1"),
            BridgePreparedMaterials.RepeatUnit("PedestrianDraw02", "3bd3b389ccd3e506b219788ef22876c161e2d15582578ad50965312f2a99731d"),
            roadToObject, halfWidthDelta);

    internal static MovableRoadAppearance.Level Prepared(SurfaceAsset native, SurfaceAsset rightUnit,
        SurfaceAsset leftUnit, Pose roadToObject, float halfWidthDelta)
    {
        var integers = Data.I32;
        var values = Data.F32;
        if (integers[0] != 1 || !float.IsFinite(halfWidthDelta))
            throw new InvalidOperationException(nameof(PedestrianDraw02AtlasAppearance));
        int stations = integers[1], domains = integers[2], count = integers[3];
        int stationOffset = domains * 20, cellOffset = stationOffset + stations * 2;
        int cellIntegers = 4 + domains;
        var x = new float[stations];
        for (int i = 0; i < stations; i++)
        {
            x[i] = AffineGeometry.AddScaled(values[stationOffset + i * 2], values[stationOffset + i * 2 + 1], halfWidthDelta);
            if (i > 0 && x[i] <= x[i - 1])
                throw new InvalidOperationException(nameof(PedestrianDraw02AtlasAppearance));
        }
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        var patches = new Patch[count];
        var support = new MovableRoadAppearance.Type[count];
        for (int cell = count - 1; cell >= 0; cell--)
        {
            int slab = integers[cellIntegers + cell * 2], domain = integers[cellIntegers + cell * 2 + 1];
            int at = domain * 20, interval = cellOffset + cell * 4;
            float originalLeft = values[stationOffset + slab * 2];
            float originalRight = values[stationOffset + (slab + 1) * 2];
            float scale = (originalRight - originalLeft) / (x[slab + 1] - x[slab]);
            float offset = originalLeft - scale * x[slab];
            Vector4 Projection(int index) => pose.transpose * new Vector4(values[index] * scale, 0,
                values[index + 1], values[index + 2] + values[index] * offset);

            // Constant native frames remain unchanged. The ten proven affine
            // finite domains override this value below with their raw field.
            // Eight non-affine finite domains and four repeated domains still
            // use representative frames; their native equivalence is unresolved.
            float sourceX = (originalLeft + originalRight) * .5f;
            float sourceZ = (values[interval] + values[interval + 1] + values[interval + 2] + values[interval + 3]) * .25f;
            var sourceTangent = new Vector3(
                values[at + 10] * sourceX + values[at + 13] * sourceZ + values[at + 16],
                values[at + 11] * sourceX + values[at + 14] * sourceZ + values[at + 17],
                values[at + 12] * sourceX + values[at + 15] * sourceZ + values[at + 18]).normalized;
            var tangent = Quaternion.Inverse(roadToObject.rotation) * sourceTangent;
            // Repeating domains use continuous finished-unit coordinates. Finite
            // terminals retain their original atlas; no modulo adds period cuts.
            int prepared = integers[4 + domain];
            patches[cell] = prepared != 0
                ? PedestrianDraw02RepeatAppearance.Create(prepared == 1 ? rightUnit : leftUnit,
                    new Vector2(values[at], values[at + 1]), new Vector2(values[at + 2], values[at + 3]),
                    Projection(at + 4), Projection(at + 7),
                    new Vector4(tangent.x, tangent.y, tangent.z, values[at + 19]))
                : new Patch(new Vector2(values[at], values[at + 1]),
                new Vector2(values[at + 2], values[at + 3]), Projection(at + 4), Projection(at + 7),
                surface: native, sourceUv: true,
                tangent: new Vector4(tangent.x, tangent.y, tangent.z, values[at + 19]));
            if(prepared==0 && TangentFields.I32[domain]!=0)
            {
                var f=TangentFields.F32;int start=domain*9;
                var sourceField=Matrix4x4.zero;
                for(int component=0;component<3;component++)
                    sourceField.SetRow(component,new Vector4(f[start+component]*scale,0,
                        f[start+3+component],f[start+6+component]+f[start+component]*offset));
                // Position projection enters prototype space; direction returns
                // to receiver space. Preserve raw field magnitude before packing.
                var field=Matrix4x4.Rotate(Quaternion.Inverse(roadToObject.rotation))*sourceField*pose;
                patches[cell]=new Patch(new Vector2(values[at],values[at+1]),
                    new Vector2(values[at+2],values[at+3]),Projection(at+4),Projection(at+7),surface:native,sourceUv:true)
                    .WithSourceTangentField(field.GetRow(0),field.GetRow(1),field.GetRow(2),values[at+19]);
            }
            support[cell] = MovableRoadAppearance.Type.Pathway;
        }

        var partition = PedestrianDraw02LocalAppearance.Create(patches, roadToObject, halfWidthDelta, out var compact);
        support = new MovableRoadAppearance.Type[compact.Length];
        for(int i=0;i<support.Length;i++)support[i]=MovableRoadAppearance.Type.Pathway;
        return new MovableRoadAppearance.Level(native, compact, support, carriageway: partition, sidewalk: partition);
    }
}
