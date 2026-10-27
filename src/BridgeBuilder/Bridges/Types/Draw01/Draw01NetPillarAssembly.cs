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
    internal static StaticObjectPrefab Create(TowerFactory factory, string owner, string name, RenderPrefab nativeTop, RenderPrefab nativeColumn, RenderPrefab nativeFooting, ContentPrefab content, float halfWidthDelta)
    {
        if (content == null || !float.IsFinite(halfWidthDelta))
            throw new ArgumentException("Draw01 ordinary pillar requires native content and a finite width change.");
        var top = CreateTop(factory, owner, name + " Top", nativeTop, halfWidthDelta) ?? throw new InvalidOperationException("Draw01 ordinary cap render creation failed.");
        var column = CreateColumn(factory, owner, name + " Column", nativeColumn) ?? throw new InvalidOperationException("Draw01 ordinary column render creation failed.");
        var footing = CreateFooting(factory, owner, name + " Footing", nativeFooting, halfWidthDelta) ?? throw new InvalidOperationException("Draw01 ordinary footing render creation failed.");
        // Render components, including stack/base and all LOD references, have
        // already been copied by the graph builder. Do not add duplicate copies.
        return Draw01PillarData.Create(factory, name, top, column, footing, content);
    }

    internal static RenderPrefab? CreateTop(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[0])
            throw new InvalidOperationException("Draw01 ordinary cap requires both native LODs.");
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
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[3]; lod < sources.Length; lod++)
            if (sources[lod] == null || !string.Equals(sources[lod].name, names[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(sources[lod].asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 ordinary cap LOD {lod} is not its permitted native source '{names[lod]}'.");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[4]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                if (meshes.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[5] || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[6]] == null || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[7]].subMeshCount != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[8])
                    throw new InvalidOperationException($"Draw01 ordinary cap LOD {lod} requires its native mesh and surface slot.");
                var plan = Draw01NetTopData.CreatePlan(lod, meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[9]].vertices, meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[10]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[11]), halfWidthDelta);
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

        return factory.CreateMovableRenderGraph(name, levels);
    }

    internal static RenderPrefab? CreateFooting(TowerFactory factory, string owner, string name, RenderPrefab source, float halfWidthDelta)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[12])
            throw new InvalidOperationException("Draw01 ordinary footing requires both native LODs.");
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
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[15]; lod < sources.Length; lod++)
            if (sources[lod] == null || !string.Equals(sources[lod].name, names[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(sources[lod].asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 ordinary footing LOD {lod} is not its permitted native source '{names[lod]}'.");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[16]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                if (meshes.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[17] || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[18]] == null || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[19]].subMeshCount != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[20])
                    throw new InvalidOperationException($"Draw01 ordinary footing LOD {lod} requires its native mesh and surface slot.");
                var plan = Draw01NetFootingData.CreatePlan(lod, meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[21]].vertices, meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[22]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[23]), halfWidthDelta);
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

        return factory.CreateMovableRenderGraph(name, levels);
    }

    // Native 1.6.2f1: one centred cylindrical column at all three LODs. Columns
    // translate with their authored centre; this centre is zero, so the displacement
    // is zero. Do not stretch its cross-section with the road or infer it at runtime.
    internal static RenderPrefab? CreateColumn(TowerFactory factory, string owner, string name, RenderPrefab source)
    {
        var lods = source.GetComponent<LodProperties>()?.m_LodMeshes;
        if (lods == null || lods.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[24])
            throw new InvalidOperationException("Draw01 ordinary column requires both native LODs.");
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
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[33]; lod < sources.Length; lod++)
            if (sources[lod] == null || !string.Equals(sources[lod].name, names[lod], StringComparison.Ordinal) || BridgeAssetInfo.IsOtherBridgePrefab(sources[lod].asset?.path, owner, imported))
                throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} is not its permitted native source '{names[lod]}'.");
        var levels = new MovableRoadAssets.Level[sources.Length];
        for (var lod = Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[34]; lod < sources.Length; lod++)
        {
            Mesh[]? meshes = null;
            try
            {
                meshes = PrivateGeometryReader.Read(sources[lod]);
                if (meshes.Length != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[35] || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[36]] == null || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[37]].subMeshCount != Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[38] || meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[39]].vertexCount != vertexCounts[lod])
                    throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} differs from its recorded mesh layout.");
                var positions = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[40]].vertices;
                var indices = meshes[Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[41]].GetIndices(Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[42]);
                if (indices.Length != triangleCounts[lod] * Numeric_Types_Draw01_Draw01NetPillarAssembly_cs.Values.I32[43])
                    throw new InvalidOperationException($"Draw01 ordinary column LOD {lod} differs from its recorded topology.");
                var plan = new MovableStructureModel.Plan(positions, (Vector3[])positions.Clone(), new[] { indices }, new[] { new bool[triangleCounts[lod]] });
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
        return factory.CreateMovableRenderGraph(name, levels);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01NetPillarAssembly_cs
internal static class Numeric_Types_Draw01_Draw01NetPillarAssembly_cs
{
    static Numeric_Types_Draw01_Draw01NetPillarAssembly_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01NetPillarAssembly_cs");
}
