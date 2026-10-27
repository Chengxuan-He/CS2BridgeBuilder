using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Lift03RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, IReadOnlyList<MovableRoadAssets.Level> mainRoadLevels, IReadOnlyList<MovableRoadAssets.Level> auxiliaryRoadLevels, float structureDelta, float upperDelta, float lowerDelta)
    {
        IReadOnlyList<MovableRoadAssets.Level> roadLevels = Lift03RoadLevels.Combine(mainRoadLevels, auxiliaryRoadLevels);
        if (roadLevels.Count != Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[0])
            throw new ArgumentException("Lift03 mechanism requires all three prepared road LODs.");
        var expected = new[]
        {
            "LiftBridge03 Mesh",
            "LiftBridge03_LOD1 Mesh",
            "LiftBridge03_LOD2 Mesh"
        };
        for (var lod = Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[1]; lod < roadLevels.Count; lod++)
        {
            var source = roadLevels[lod].Source;
            if (source == null || source.asset != null || !string.Equals(source.name, expected[lod], StringComparison.Ordinal))
                throw new InvalidOperationException($"Lift03 LOD {lod} requires its reconstructed native source.");
        }

        var levels = new[]
        {
            roadLevels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[2]],
            roadLevels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[3]],
            roadLevels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[4]]
        };
        var far = levels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[5]];
        if (far.ExtraStructure != null)
            throw new InvalidOperationException("Lift03 far support supplement has already been attached.");
        Mesh[]? meshes = null;
        try
        {
            meshes = PrivateGeometryReader.Read(levels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[6]].Source);
            if (meshes.Length != Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[7] || meshes[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[8]].subMeshCount != Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[9])
                throw new InvalidOperationException("Lift03 support supplement requires its recorded middle main surface.");
            var supportPlan = Lift03FarSupports.CreateSupplement(meshes[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[10]].vertices, meshes[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[11]].GetIndices(Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[12]), structureDelta, upperDelta, lowerDelta);
            var extra = new MovableRoadAssets.Supplement(levels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[13]].Source, Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[14], supportPlan);
            levels[Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[15]] = new MovableRoadAssets.Level(far.Source, far.Triangles, far.Surfaces, far.Partition!, far.RenderBounds, far.RoadToObject, far.Structure, far.Skeleton, far.Lanes, far.ClosedWorld, far.AppearancePageSize, far.TexelsPerMeter, far.RoadPartitions, far.LanePartitions, far.RoadTrianglePartitions, extra);
        }
        finally
        {
            PrivateGeometryReader.Release(meshes);
        }

        return MovableRenderAssembly.CreateMechanism(factory, name, levels, new[] { Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[16], Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[17], Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[18] }, (lod, mesh, source, indices) => mesh == Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[19] ? Lift03Glass.Create(lod, source, indices, structureDelta) : lod == Numeric_Types_Lift03_Lift03RenderAssembly_cs.Values.I32[20] ? Lift03FarStructure.Create(source, indices, structureDelta) : Lift03DetailedStructure.Create(lod, source, indices, structureDelta, upperDelta, lowerDelta));
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03RenderAssembly_cs
internal static class Numeric_Types_Lift03_Lift03RenderAssembly_cs
{
    static Numeric_Types_Lift03_Lift03RenderAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03RenderAssembly_cs");
}
