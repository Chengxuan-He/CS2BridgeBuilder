using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class PedestrianDraw02RenderAssembly
{
    internal static RenderPrefab? CreateMechanism(TowerFactory factory, string name, MovableRoadAssets.Level[] roadLevels, float halfWidthDelta)
        => GenerationWork.Synchronous(()=>CreateMechanismAsync(factory,name,roadLevels,halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateMechanismAsync(TowerFactory factory, string name, MovableRoadAssets.Level[] roadLevels, float halfWidthDelta)
    {
        return await MovableRenderAssembly.CreateMechanismAsync(factory, name, roadLevels, new[] { Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[1], Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[2] }, (lod, mesh, source, indices) => Structure(lod, mesh, source, indices, halfWidthDelta));
    }

    private static MovableStructureModel.Plan Structure(int lod, int mesh, Vector3[] source, int[] indices, float delta)
    {
        var positions = (Vector3[])source.Clone();
        if (mesh == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[3])
        {
            PedestrianDraw02RigidParts.Apply(lod, source, positions, delta);
            PedestrianDraw02TransverseParts.Apply(lod, source, positions, delta);
            PedestrianDraw02Bases.Apply(lod, source, positions, delta);
            var structure = PedestrianDraw02JoinedSupports.Complete(lod, source, positions, indices, delta);
            return PedestrianDraw02TrussMapping.Apply(lod, PedestrianDraw02RoadReplacement.WithTopRemoved(lod, structure), delta);
        }
        var rightStart = lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[9] ? Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[10] : Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[11];
        var rightEnd = lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[12] ? Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[13] : Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[14];
        var rightGlass = new List<int>();
        var leftGlass = new List<int>();
        for (int i = 0; i < positions.Length; i++)
        {
            if (i >= rightStart && i <= rightEnd) rightGlass.Add(i); else leftGlass.Add(i);
        }
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.TranslateX, new[] { positions, positions },
            Array.Empty<float[]>(), new[] { delta }, count: rightGlass.Count, indices: new[] { rightGlass.ToArray() });
        AffineGeometry.Execute(StructurePrograms.Data, StructurePrograms.NegativeTranslateX, new[] { positions, positions },
            Array.Empty<float[]>(), new[] { delta }, count: leftGlass.Count, indices: new[] { leftGlass.ToArray() });
        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[15]] });
    }

    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateColumnAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateColumnAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support PedestrianDraw02RenderAssembly.CreateColumn");
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var sources = new[]
        {
            source,
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[17]],
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[18]]
        };
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[19]; lod < levels.Length; lod++)
        {
            var original = sources[lod];
            var expected = "PedestrianDrawBridge02Pillar" + (lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[20] ? "" : "_LOD" + lod) + " Mesh";
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(original);
                var mesh = meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[25]];
                var supportInput0 = mesh.vertices;
                var supportInput1 = mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[27]);
                var supportInput2 = mesh.vertices;
                var supportInput3 = mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[28]);
                var plan = await GenerationWork.Background("support structure plan", () => lod == Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[26] ? PedestrianDraw02FarColumn.CreatePlan(supportInput0, supportInput1, halfWidthDelta) : PedestrianDraw02ColumnWideningData.CreateDetailedPlan(lod, supportInput2, supportInput3, halfWidthDelta));
                var bounds = original.bounds;
                bounds.min.x -= halfWidthDelta;
                bounds.max.x += halfWidthDelta;
                levels[lod] = new MovableRoadAssets.Level(original, bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }

    // The native stacked footing is a separate render graph from the column and mechanism.
    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateFootingAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateFootingAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support PedestrianDraw02RenderAssembly.CreateFooting");
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var sources = new[]
        {
            source,
            lods[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[30]]
        };
        var levels = new MovableRoadAssets.Level[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[34]];
        for (var lod = Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[35]; lod < levels.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                var original = sources[lod];
                meshes = PrivateGeometryReader.Read(original);
                var mesh = meshes[Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[40]];
                var supportInput0 = mesh.vertices;
                var supportInput1 = mesh.GetIndices(Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values.I32[41]);
                var plan = await GenerationWork.Background("support structure plan", () => PedestrianDraw02FootingWideningData.CreatePlan(lod, supportInput0, halfWidthDelta, new[] { supportInput1 }));
                // Preserve native vertical stacking reach and longitudinal bounds.
                var bounds = original.bounds;
                var widened = new Bounds3(new float3(bounds.min.x - halfWidthDelta, bounds.min.y, bounds.min.z), new float3(bounds.max.x + halfWidthDelta, bounds.max.y, bounds.max.z));
                levels[lod] = new MovableRoadAssets.Level(original, widened, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs.Values", () => HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02RenderAssembly_cs"));
}
