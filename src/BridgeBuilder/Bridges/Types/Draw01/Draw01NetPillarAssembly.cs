using System;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal static class Draw01NetPillarAssembly
{
    // Build the complete ordinary support in dependency order. Each graph joins
    // factory.Created before the object, including partial results on failure,
    // so the enclosing generation/preview transaction owns the entire lifetime.
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab nativeTop, RenderPrefab nativeColumn, RenderPrefab nativeFooting, ContentPrefab content, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, owner, name, nativeTop, nativeColumn, nativeFooting, content, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string owner, string name, RenderPrefab nativeTop, RenderPrefab nativeColumn, RenderPrefab nativeFooting, ContentPrefab content, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw01NetPillarAssembly.Create");
        var top = await CreateTopAsync(factory, owner, name + " Top", nativeTop, halfWidthDelta) ?? throw new InvalidOperationException("Draw01 ordinary cap render creation failed.");
        var column = await CreateColumnAsync(factory, owner, name + " Column", nativeColumn) ?? throw new InvalidOperationException("Draw01 ordinary column render creation failed.");
        var footing = await CreateFootingAsync(factory, owner, name + " Footing", nativeFooting, halfWidthDelta) ?? throw new InvalidOperationException("Draw01 ordinary footing render creation failed.");
        // Render components, including stack/base and all LOD references, have
        // already been copied by the graph builder. Do not add duplicate copies.
        return Draw01PillarData.Create(factory, name, top, column, footing, content);
    }

    internal static RenderPrefab? CreateTop(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateTopAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateTopAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw01NetPillarAssembly.CreateTop");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[1]],
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[2]]
        };
        var names = new[]
        {
            "DrawBridge01NetPillarTop Mesh",
            "DrawBridge01NetPillarTop_LOD1 Mesh",
            "DrawBridge01NetPillarTop_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[4]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                var supportInput0 = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[9]].vertices;
                var supportInput1 = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[10]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[11]);
                var plan = await GenerationWork.Background("support structure plan", () => Draw01NetTopData.CreatePlan(lod, supportInput0, supportInput1, halfWidthDelta));
                var bounds = sources[lod].bounds;
                foreach (var position in plan.Positions)
                {
                    bounds.min.x = Math.Min(bounds.min.x, position.x);
                    bounds.max.x = Math.Max(bounds.max.x, position.x);
                }

                levels[lod] = new MovableRoadAssets.Level(sources[lod], bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }

    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta) =>
        GenerationWork.Synchronous(() => CreateFootingAsync(factory, owner, name, source, halfWidthDelta));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateFootingAsync(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        await GenerationWork.Next("support Draw01NetPillarAssembly.CreateFooting");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[13]],
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[14]]
        };
        var names = new[]
        {
            "DrawBridge01NetPillarBase Mesh",
            "DrawBridge01NetPillarBase_LOD1 Mesh",
            "DrawBridge01NetPillarBase_LOD2 Mesh"
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[16]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                var supportInput0 = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[21]].vertices;
                var supportInput1 = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[22]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[23]);
                var plan = await GenerationWork.Background("support structure plan", () => Draw01NetFootingData.CreatePlan(lod, supportInput0, supportInput1, halfWidthDelta));
                // Preserve native vertical/longitudinal reach and encompass every widened
                // far vertex, including its authored simplification beyond the near profile.
                var bounds = sources[lod].bounds;
                foreach (var position in plan.Positions)
                {
                    bounds.min.x = Math.Min(bounds.min.x, position.x);
                    bounds.max.x = Math.Max(bounds.max.x, position.x);
                }

                levels[lod] = new MovableRoadAssets.Level(sources[lod], bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }

    // Native 1.6.2f1: one centred cylindrical column at all three LODs. Columns
    // translate with their authored centre; this centre is zero, so the displacement
    // is zero. Do not stretch its cross-section with the road or infer it at runtime.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source) =>
        GenerationWork.Synchronous(() => CreateColumnAsync(factory, owner, name, source));

    internal static async System.Threading.Tasks.Task<RenderPrefab?> CreateColumnAsync(TowerFactory factory, string owner, string name, RenderPrefab source)
    {
        await GenerationWork.Next("support Draw01NetPillarAssembly.CreateColumn");
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        var sources = new[]
        {
            source,
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[25]],
            lods[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[26]]
        };
        var names = new[]
        {
            "DrawBridge01NetPillar Mesh",
            "DrawBridge01NetPillar_LOD1 Mesh",
            "DrawBridge01NetPillar_LOD2 Mesh"
        };
        var vertexCounts = new[]
        {
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[27],
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[28],
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[29]
        };
        var triangleCounts = new[]
        {
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[30],
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[31],
            Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[32]
        };
        var imported = Path.Combine(Application.persistentDataPath, "ImportedData");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[34]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                var positions = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[40]].vertices;
                var indices = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[41]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[42]);
                var plan = await GenerationWork.Background("support structure plan", () => new MovableStructureModel.Plan(positions, (Vector3[])positions.Clone(), new[] { indices }, new[] { new bool[triangleCounts[lod]] }));
                levels[lod] = new MovableRoadAssets.Level(sources[lod], sources[lod].bounds, new[] { plan });
            }
            finally
            {
                PrivateGeometryReader.Release(meshes);
            }
        }

        // Allocate owned geometry/renders even for this zero-displacement column.
        // The shared writer retains raw attributes, native surfaces and stack metadata,
        // repoints both LOD references and rolls back incomplete resource creation.
        return await factory.CreateMovableRenderGraphAsync(name, levels);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01NetPillarAssembly_cs
internal static class Numeric_Types_Draw01_Draw01NetPillarAssembly_cs
{
    static Numeric_Types_Draw01_Draw01NetPillarAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Draw01_Draw01NetPillarAssembly_cs"));
}
