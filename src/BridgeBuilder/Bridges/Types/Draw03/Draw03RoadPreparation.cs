using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw03RoadPreparation
{
    internal static MovableRoadAssets.Level[] PrepareRenderLevels(MovableStraightSample sample, RenderPrefab[] nativeLevels, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial material, MovableSelectedRoadLayout layout)
    {
        // Appearance domains must cover actual selected geometry at every LOD,
        // including a far-LOD vertex outside the LOD0 layout's lateral span.
        // Retain only this invocation's evaluated inputs; Prepare consumes each
        // once rather than recompiling them after collecting their bounds.
        var roads = new MovableClosedRoad.Result[nativeLevels.Length];
        var span = layout.SurfaceSpan;
        var pose = Matrix4x4.TRS(sample.RoadToObject.position, sample.RoadToObject.rotation, Vector3.one);
        void Include(Vector3 point)
        {
            var x = pose.MultiplyPoint3x4(point).x;
            span.x = Mathf.Min(span.x, x);
            span.y = Mathf.Max(span.y, x);
        }
        for (int lod = 0; lod < roads.Length; lod++)
        {
            roads[lod] = sample.CompileLod(lod);
            foreach (var triangle in roads[lod].Triangles)
            { Include(triangle.A.Position); Include(triangle.B.Position); Include(triangle.C.Position); }
        }
        var carriageway = Draw03RoadAppearance.Regions(false, sample.RoadToObject);
        var sidewalk = Draw03RoadAppearance.Regions(true, sample.RoadToObject);
        var patches = Draw03RoadWearDomains.Apply(Draw03RoadAppearance.Patches(layout, sample.RoadToObject, span),
            ref carriageway, ref sidewalk);
        return MovableRoadPreparation.Prepare(sample.Road, sample.RoadToObject, lod => roads[lod],
            nativeLevels, halfWidthDelta, closedWorld, material,
            Draw03RoadPartitions.Carriageway, Draw03RoadPartitions.Sidewalk, carriageway, sidewalk,
            patches, Numeric_Types_Draw03_Draw03RoadPreparation_cs.Values.I32[0], layout);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadPreparation_cs
internal static class Numeric_Types_Draw03_Draw03RoadPreparation_cs
{
    static Numeric_Types_Draw03_Draw03RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadPreparation_cs");
}
