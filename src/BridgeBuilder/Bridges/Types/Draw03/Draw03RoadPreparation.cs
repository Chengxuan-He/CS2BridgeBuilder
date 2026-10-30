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
        var appearances = new MovableRoadAppearance.Level[nativeLevels.Length];
        for (int lod = 0; lod < appearances.Length; lod++)
            appearances[lod] = lod < 2
                ? Draw03PreparedRoad.Near(nativeLevels[lod].GetSurfaceAsset(0), layout, sample.RoadToObject, span)
                : Draw03PreparedRoad.Far(nativeLevels[lod].GetSurfaceAsset(0), layout, sample.RoadToObject, span);
        var near = appearances[0];
        return MovableRoadPreparation.Prepare(sample.Road, sample.RoadToObject, lod => roads[lod],
            nativeLevels, halfWidthDelta, closedWorld, material,
            Draw03RoadPartitions.Carriageway, Draw03RoadPartitions.Sidewalk, near.Carriageway!, near.Sidewalk!,
            near.Patches, Numeric_Types_Draw03_Draw03RoadPreparation_cs.Values.I32[0], layout, lod => appearances[lod]);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03RoadPreparation_cs
internal static class Numeric_Types_Draw03_Draw03RoadPreparation_cs
{
    static Numeric_Types_Draw03_Draw03RoadPreparation_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw03_Draw03RoadPreparation_cs");
}
