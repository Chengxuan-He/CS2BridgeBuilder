using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Builds the native render graph for evaluated road and marking triangles. The caller supplies
/// measured partitions and evaluated road geometry. Native network surfaces enter the existing-material
/// resolver before this call; this class never bakes materials. Prefabs join the owned
/// PrefabAssetWriter plan, so dependencies use the ordinary per-bridge persistence path.
/// </summary>
internal static class MovableRoadAssets
{
    // A measured part from another LOD, appended to the same render asset. It is
    // structural geometry, so road paint must never be projected onto its underside.
    internal sealed class Supplement
    {
        internal readonly RenderPrefab Source;
        internal readonly int Mesh;
        internal readonly MovableStructureModel.Plan Plan;
        internal Supplement(RenderPrefab source, int mesh, MovableStructureModel.Plan plan)
        { Source = source; Mesh = mesh; Plan = plan; }
    }

    internal sealed class Level
    {
        internal readonly RenderPrefab Source;
        internal readonly IReadOnlyList<MovableRoadGeometry.Triangle> Triangles;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly MovableRoadGeometry.Partition? Partition;
        // Road occurrences index composition pieces; lane occurrences index visual batches.
        // They are different domains and must never share an array by index coincidence.
        internal readonly MovableRoadGeometry.Partition[]? RoadPartitions, LanePartitions;
        internal readonly IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? RoadTrianglePartitions;
        internal readonly bool HasRoad;
        // Local render metadata, not the final object culling envelope. Native
        // StandingObject/StackData contribute the latter during object initialization.
        internal readonly Bounds3 RenderBounds;
        internal readonly MovableStructureModel.Plan[]? Structure;
        internal readonly ProceduralAnimationProperties? Skeleton;
        internal readonly MovableLaneGeometry.Result? Lanes;
        internal readonly Matrix4x4 ClosedWorld;
        internal readonly Pose RoadToObject;
        internal readonly int AppearancePageSize;
        internal readonly float TexelsPerMeter;
        internal readonly Supplement? ExtraStructure;

        // Carries the same compiled receivers and lane result into the existing appearance,
        // partition, skinning and owned-asset path. Decals remain subject to that path's checks.
        internal Level(RenderPrefab source, MovableClosedRoad.Result road,
            MovableRoadGeometry.Partition partition, Bounds3 renderBounds, Pose roadToObject,
            MovableStructureModel.Plan[]? structure = null, ProceduralAnimationProperties? skeleton = null,
            Matrix4x4? closedWorld = null, int appearancePageSize = 2048, float texelsPerMeter = 32f,
            IReadOnlyList<MovableRoadGeometry.Partition>? roadPartitions = null,
            IReadOnlyList<MovableRoadGeometry.Partition>? lanePartitions = null,
            IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? roadTrianglePartitions = null,
            Supplement? extraStructure = null)
            : this(source, road.Triangles, road.Surfaces, partition, renderBounds, roadToObject,
                structure, skeleton, road.LaneGeometry, closedWorld, appearancePageSize, texelsPerMeter,
                roadPartitions, lanePartitions, roadTrianglePartitions, extraStructure) { }

        internal Level(RenderPrefab source, IReadOnlyList<MovableRoadGeometry.Triangle> triangles,
            SurfaceAsset[] surfaces, MovableRoadGeometry.Partition partition, Bounds3 renderBounds, Pose roadToObject,
            MovableStructureModel.Plan[]? structure = null, ProceduralAnimationProperties? skeleton = null,
            MovableLaneGeometry.Result? lanes = null, Matrix4x4? closedWorld = null,
            int appearancePageSize = 2048, float texelsPerMeter = 32f,
            IReadOnlyList<MovableRoadGeometry.Partition>? roadPartitions = null,
            IReadOnlyList<MovableRoadGeometry.Partition>? lanePartitions = null,
            IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? roadTrianglePartitions = null,
            Supplement? extraStructure = null)
        {
            HasRoad = true;
            Source = source; Triangles = triangles; Surfaces = (SurfaceAsset[])surfaces.Clone();
            Partition = partition; RenderBounds = renderBounds;
            RoadPartitions = roadPartitions?.ToArray(); LanePartitions = lanePartitions?.ToArray();
            RoadTrianglePartitions = roadTrianglePartitions?.ToDictionary(entry => entry.Key, entry => entry.Value);
            Structure = structure == null ? null : (MovableStructureModel.Plan[])structure.Clone();
            Skeleton = skeleton ?? source.GetComponent<ProceduralAnimationProperties>();
            Lanes = lanes;
            ClosedWorld = closedWorld ?? Matrix4x4.identity;
            RoadToObject = roadToObject;
            AppearancePageSize = appearancePageSize; TexelsPerMeter = texelsPerMeter;
            ExtraStructure = extraStructure;
        }

        // Fixed support renders share geometry ownership, LOD remapping and rollback with
        // the moving render graph. They contain no selected-road batch or fabricated bone.
        internal Level(RenderPrefab source, Bounds3 bounds, MovableStructureModel.Plan[] structure)
        {
            Source = source; RenderBounds = bounds;
            Structure = (MovableStructureModel.Plan[])structure.Clone();
            Triangles = Array.Empty<MovableRoadGeometry.Triangle>();
            Surfaces = Array.Empty<SurfaceAsset>();
            Skeleton = source.GetComponent<ProceduralAnimationProperties>();
            ClosedWorld = Matrix4x4.identity;
            RoadToObject = new Pose(Vector3.zero, Quaternion.identity);
        }
    }

    internal static bool NativeNetworkGraph(IReadOnlyList<Level> levels)
        =>levels.Count!=0 && levels[0]?.Source is NetPiecePrefab &&
            levels.All(level=>level!=null && !level.HasRoad && level.Skeleton==null);

    // Levels contain the root first and every reachable native LOD exactly once. Output resources
    // stay in this call's rollback list until the entire graph succeeds; no partial LOD is published.
    internal static bool TryCreate(string owner, string name, string styleId, IReadOnlyList<Level> levels,
        PreviewGeometry? preview, out RenderPrefab[] created, out string? error)
    {
        created = Array.Empty<RenderPrefab>(); error = null;
        name = TowerPrefabNaming.Safe(name);
        if (!MovableAssetOwnership.Matches(name, owner, preview) ||
            levels == null || levels.Count == 0)
        { error = "Moving road assets require their generation owner and a complete measured LOD graph."; return false; }

        var models = new List<ModelImporter.Model>();
        var levelModels = new List<ModelImporter.Model[]>();
        var levelSurfaces = new List<SurfaceAsset[]>();
        var levelNames = new List<string>();
        var renderBounds = new List<Bounds3>();
        var laneSlotStarts = new List<int>();
        var prefabs = new List<RenderPrefab>();
        var assets = new List<AssetData>();
        var nativeNetworkGraph = NativeNetworkGraph(levels);
        var structureTiles = new MovableStructureTiles(styleId,nativeNetworkGraph);
        var success = false;
        // Validation is immutable during this graph build; no cross-owner cache.
        var validatedRoadSurfaces = new HashSet<SurfaceAsset>();
        try
        {
            var indices = new Dictionary<RenderPrefab, int>();
            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
                if (level?.Source == null || indices.ContainsKey(level.Source))
                { error = $"Moving road LOD {i} has a missing or duplicate source."; return false; }
                indices.Add(level.Source, i);
                var sourcePath = level.Source.asset?.path;
                if (BridgeAssetInfo.IsOtherBridgePrefab(sourcePath, owner,
                        System.IO.Path.Combine(Application.persistentDataPath, "ImportedData")))
                { error = $"Moving road LOD {i} belongs to another generated bridge."; return false; }
                if (!ValidBounds(level.RenderBounds))
                { error = $"Moving road LOD {i} has invalid render bounds."; return false; }
                var bounds = level.RenderBounds;
                var skeleton = level.Skeleton;
                if ((level.HasRoad || skeleton != null) && !ValidSkeleton(skeleton))
                { error = $"Moving road LOD {i} has no valid native procedural skeleton."; return false; }
                var boneCount = skeleton?.m_Bones.Length ?? 0;
                if (level.HasRoad && level.Surfaces.Length == 0)
                { error = $"Moving road LOD {i} has no resolved object surfaces."; return false; }
                var levelName = i == 0 ? name : name + " LOD" + i;
                if (preview == null && AssetDatabase.user.Exists<GeometryAsset>(
                        BridgeBuilder.Runtime.BridgePrefabPersistence.ResourcePath(levelName, "Geometry"), out _))
                { error = $"Movable render geometry already exists: '{levelName}'."; return false; }
                var roadTriangles = level.Triangles;
                var roadSurfaces = level.Surfaces;
                var roadModels = Array.Empty<ModelImporter.Model>();
                if (level.HasRoad)
                {
                    foreach (var surface in roadSurfaces)
                    {
                        if (surface == null)
                        { error = $"Moving road LOD {i} has an unresolved surface reference."; return false; }
                        if (validatedRoadSurfaces.Contains(surface)) continue;
                        surface.LoadHeader();
                        // Measured native object templates in game 1.6.2f1 support procedural
                        // object skinning. This guard checks representation, not appearance equality.
                        // Network inputs must have been assigned existing object materials by the caller.
                        var template = surface.materialTemplateHash;
                        if (!MovableSurfaceAssets.IsObjectSurface(surface))
                        { error = $"Moving road LOD {i} surface '{surface.name}' requires an unimplemented object conversion (template {template})."; return false; }
                        validatedRoadSurfaces.Add(surface);
                    }
                    // Appearance is evaluated in the selected-road frame. Only evaluated geometry
                    // moves into the measured mechanism frame before partitioning and bone binding.
                    roadTriangles = MovableRoadGeometry.InFrame(roadTriangles, level.RoadToObject);
                    if (!MovableRoadGeometry.TryBind(roadTriangles, level.Partition!,
                            skeleton!.m_Bones.Length, roadSurfaces.Length, out var triangles, out error,
                            occurrencePartitions: level.RoadPartitions, trianglePartitions: level.RoadTrianglePartitions))
                    { error = $"Moving road LOD {i}: {error}"; return false; }
                    // Motion partitioning interpolates seam vertices. Include
                    // the emitted positions, not only pre-conversion input extrema.
                    foreach (var triangle in triangles)
                    {
                        Include(ref bounds, triangle.Geometry.A.Position);
                        Include(ref bounds, triangle.Geometry.B.Position);
                        Include(ref bounds, triangle.Geometry.C.Position);
                    }
                    if (level.Lanes != null && level.Lanes.Decals.Length != 0)
                        throw new InvalidOperationException("Moving-road paint must be composed before render assembly; geometry inlay is prohibited.");
                    // Preserve partitioned road triangles; do not coalesce or retriangulate them.
                    roadModels = BuildMaterialMeshes(levelName, triangles, roadSurfaces,
                        skeleton.m_Bones.Length, models, out roadSurfaces);
                }
                var batch = new List<ModelImporter.Model>();
                var surfaces = new List<SurfaceAsset>();
                if (level.Structure != null)
                {
                    Mesh[]? loaded = null;
                    try
                    {
                        loaded = PrivateGeometryReader.Read(level.Source);
                        if (loaded == null || loaded.Length == 0 || loaded.Length != level.Structure.Length)
                        { error = $"Moving road LOD {i} requires a measured replacement plan for every source mesh."; return false; }
                        var originals = level.Source.surfaceAssets?.ToArray() ?? Array.Empty<SurfaceAsset>();
                        if (loaded.Any(mesh => mesh == null || mesh.subMeshCount != 1) || originals.Any(surface => surface == null) ||
                            originals.Length != loaded.Length)
                        { error = $"Moving road LOD {i} has unresolved structural material slots."; return false; }
                        for (var part = 0; part < loaded.Length; part++)
                        {
                            if (!MovableStructureModel.TryBuild(levelName + " Structure " + part, loaded[part],
                                    level.Structure[part], boneCount, out var structureModel, out error)) return false;
                            models.Add(structureModel!);
                            MovableStructureRetile.Append(levelName, level.Source.name, part,
                                level.Structure[part], structureModel!, originals[part], structureTiles,
                                models, batch, surfaces);
                            // A static support retains native authored stacking bounds. Native
                            // decoded vertices can slightly exceed those bounds through compression;
                            // the model builder computes accurate submesh bounds independently.
                            // Procedural renders also enclose the generated closed-pose structure.
                            if (skeleton != null)
                                foreach (var position in level.Structure[part].Positions)
                                    Include(ref bounds, position);
                        }
                        // Rigid domains preserve original VT references; widened rectangular
                        // domains use owner-local crops with the same native shader properties.
                    }
                    finally { PrivateGeometryReader.Release(loaded); }
                }
                if (level.ExtraStructure is { } extra)
                {
                    if (BridgeAssetInfo.IsOtherBridgePrefab(extra.Source.asset?.path, owner,
                            System.IO.Path.Combine(Application.persistentDataPath, "ImportedData")))
                        throw new InvalidOperationException("Supplemental structure belongs to another bridge.");
                    Mesh[]? loaded = null;
                    try
                    {
                        loaded = PrivateGeometryReader.Read(extra.Source);
                        if (extra.Mesh < 0 || extra.Mesh >= loaded.Length || loaded[extra.Mesh].subMeshCount != 1)
                            throw new InvalidOperationException("Supplemental structure requires its measured single-surface mesh.");
                        if (!MovableStructureModel.TryBuild(levelName + " Underside", loaded[extra.Mesh],
                                extra.Plan, boneCount, out var model, out error)) return false;
                        models.Add(model!);
                        var slot = loaded.Take(extra.Mesh).Sum(mesh => mesh.subMeshCount);
                        var extraSurface = extra.Source.GetSurfaceAsset(slot);
                        MovableStructureRetile.Append(levelName + " Underside", extra.Source.name, extra.Mesh,
                            extra.Plan, model!, extraSurface, structureTiles, models, batch, surfaces);
                        for (var slotIndex = 0; slotIndex < extra.Plan.SourceIndices.Length; slotIndex++)
                            for (var face = 0; face < extra.Plan.RemoveTriangles[slotIndex].Length; face++)
                                if (!extra.Plan.RemoveTriangles[slotIndex][face])
                                    for (var corner = 0; corner < 3; corner++)
                                        Include(ref bounds, extra.Plan.Positions[extra.Plan.SourceIndices[slotIndex][face * 3 + corner]]);
                    }
                    finally { PrivateGeometryReader.Release(loaded); }
                }
                // Original slots retain their order across every structural mesh; converted road
                // slots follow them. Native structural descriptors, textures and UVs
                // remain unchanged across preview and persisted geometry.
                if (roadModels.Length != 0) { batch.AddRange(roadModels); surfaces.AddRange(roadSurfaces); }
                laneSlotStarts.Add(surfaces.Count);
                var lanes = level.Lanes;
                if (lanes != null)
                {
                    if (i != 0 && lanes.Decals.Length != 0 && !ReferenceEquals(lanes, levels[0].Lanes))
                        throw new InvalidOperationException("Moving road levels must share their compiled marking snapshot.");
                    if (lanes.Triangles.Length != 0)
                    {
                        if ((level.RoadPartitions != null || level.RoadTrianglePartitions != null) && level.LanePartitions == null)
                            throw new InvalidOperationException("Occurrence-partitioned roads require an explicit lane visual partition mapping.");
                        var laneTriangles = MovableRoadGeometry.InFrame(lanes.Triangles, level.RoadToObject);
                        if (!MovableRoadGeometry.TryBind(laneTriangles, level.Partition!,
                                boneCount, lanes.Surfaces.Length, out var boundLanes, out error, allowEmpty: true,
                                occurrencePartitions: level.LanePartitions))
                        { error = $"Moving lane LOD {i}: {error}"; return false; }
                        foreach (var triangle in boundLanes)
                        {
                            Include(ref bounds, triangle.Geometry.A.Position);
                            Include(ref bounds, triangle.Geometry.B.Position);
                            Include(ref bounds, triangle.Geometry.C.Position);
                        }
                        // A marking wholly inside an authored gap has no receiver. Do not
                        // fabricate an empty render mesh or retain unused material slots.
                        if (boundLanes.Length != 0)
                        {
                            batch.AddRange(BuildMaterialMeshes(levelName + " Lanes", boundLanes,
                                lanes.Surfaces, boneCount, models, out var laneSurfaces));
                            surfaces.AddRange(laneSurfaces);
                        }
                    }
                }
                levelModels.Add(batch.ToArray()); levelSurfaces.Add(surfaces.ToArray()); levelNames.Add(levelName);
                renderBounds.Add(bounds);
            }

            // RequiredBatchesSystem creates at most 16 batches across the root and its LODs.
            // Publishing more silently drops all later surfaces in the native renderer.
            for (var i = 0; i < levels.Count; i++)
                if (levelModels[i].Length != levelSurfaces[i].Length)
                    throw new InvalidOperationException("Each native material batch must have exactly one model and Surface slot.");

            var visited = new HashSet<RenderPrefab>();
            if (!Visit(levels[0].Source, indices, visited, new HashSet<RenderPrefab>()) ||
                visited.Count != levels.Count)
            { error = "Moving road levels must cover the complete acyclic native LOD graph without unrelated entries."; return false; }

            // Conversion is shared across this bridge's LOD graph, never across bridge owners.
            // It changes only curved-lane material slots; structural and prepared road slots keep
            // their exact references. Every geometry model is already validated before this write.
            var laneMaterials = new Dictionary<SurfaceAsset, SurfaceAsset>();
            for (var i = 0; i < levels.Count; i++)
                for (var slot = laneSlotStarts[i]; slot < levelSurfaces[i].Length; slot++)
                {
                    var source = levelSurfaces[i][slot];
                    if (source == null) throw new InvalidOperationException($"Moving lane LOD {i} has an unresolved material slot {slot}.");
                    if (!laneMaterials.TryGetValue(source, out var converted))
                    {
                        converted = MovableLaneSurface.Create(source, name, styleId, preview, assets);
                        laneMaterials.Add(source, converted);
                    }
                    levelSurfaces[i][slot] = converted;
                }

            // Far LODs can have fewer material units than the number of groups
            // needed by near geometry. Partition existing triangle streams, never
            // texture-period cuts or duplicated/missing far faces.
            if (BalanceAppearanceBatches(levelModels, levelSurfaces, models))
                for (int i=0;i<laneSlotStarts.Count;i++) laneSlotStarts[i]=0;
            var batchCount = levelModels.Sum(level => level.Length);
            var renderGroups = MovableMaterialSlots.RenderGroups(
                levelModels.Select(level => level.Length).ToArray(), laneSlotStarts);

            // Every extra object mesh has an independent native LOD budget. Each
            // group keeps the complete original LOD links, skeleton and bounds.
            var originalModels = levelModels.ToArray();
            var originalSurfaces = levelSurfaces.ToArray();
            var originalNames = levelNames.ToArray();
            var originalBounds = renderBounds.ToArray();
            levelModels.Clear(); levelSurfaces.Clear(); levelNames.Clear(); renderBounds.Clear();
            for (var group = 0; group < renderGroups.Length; group++)
                for (var i = 0; i < levels.Count; i++)
                {
                    var slots = renderGroups[group][i];
                    levelModels.Add(slots.Select(slot => originalModels[i][slot]).ToArray());
                    levelSurfaces.Add(slots.Select(slot => originalSurfaces[i][slot]).ToArray());
                    levelNames.Add(originalNames[i] + (group == 0 ? "" : " Appearance Group " + group));
                    renderBounds.Add(originalBounds[i]);
                }
            Mod.Log.Info($"Moving bridge native batches: total={batchCount}; groups={renderGroups.Length}; batches/group={string.Join(",", renderGroups.Select(group => group.Sum(slots => slots.Length)))}; limit/group=16; textures unchanged.");

            for (var i = 0; i < levelModels.Count; i++)
            {
                var level = levels[i % levels.Count];
                // Network structure must retain NetPiece fields as well as render data.
                // LOD-only render sources stay RenderPrefab; the root keeps native network semantics.
                RenderPrefab prefab;
                if (level.Source is NetPiecePrefab sourcePiece)
                {
                    var piece = ScriptableObject.CreateInstance<NetPiecePrefab>();
                    piece.m_Layer = sourcePiece.m_Layer;
                    piece.m_Width = sourcePiece.m_Width;
                    piece.m_Length = sourcePiece.m_Length;
                    piece.m_HeightRange = sourcePiece.m_HeightRange;
                    piece.m_WidthOffset = sourcePiece.m_WidthOffset;
                    piece.m_NodeOffset = sourcePiece.m_NodeOffset;
                    piece.m_SideConnectionOffset = sourcePiece.m_SideConnectionOffset;
                    piece.m_SurfaceHeights = sourcePiece.m_SurfaceHeights;
                    prefab = piece;
                }
                else prefab = ScriptableObject.CreateInstance<RenderPrefab>();
                prefabs.Add(prefab);
                prefab.name = levelNames[i];
                foreach (var component in level.Source.components)
                    if (component != null && component is not ProceduralAnimationProperties &&
                        !(i>=levels.Count && component is PlaceableNetPiece)) prefab.AddComponentFrom(component);
                if (level.Skeleton != null) prefab.AddComponentFrom(level.Skeleton);
                prefab.surfaceAssets = levelSurfaces[i];
                // Generated atlas receivers need full native VT requests. Otherwise
                // ManagedBatchSystem's alpha-test fallback caps requests at 8 texels;
                // empty atlas space can then erase opaque small charts in coarse mips.
                // Persist the native flag so cold loads work without this mod as well.
                prefab.manualVTRequired = level.Source.manualVTRequired ||
                    (level.HasRoad && levelSurfaces[i].Any(surface => surface.isVTMaterial));
                prefab.bounds = renderBounds[i];
                prefab.vertexCount = levelModels[i].Sum(model => model.vertexCount);
                prefab.indexCount = levelModels[i].Sum(model => model.indexCount);
                prefab.meshCount = levelModels[i].Length;
                // The temporary Geometry wrapper only reads the model for this calculation.
                prefab.surfaceArea = new Geometry(levelModels[i]).CalcSurfaceArea();
                if (!math.isfinite(prefab.surfaceArea))
                { error = $"Moving road LOD {i} has non-finite surface area."; return false; }
            }
            for (var i = 0; i < levelModels.Count; i++)
            {
                var lods = prefabs[i].GetComponent<LodProperties>();
                if (lods?.m_LodMeshes != null)
                    lods.m_LodMeshes = lods.m_LodMeshes.Select(source => prefabs[indices[source] + (i / levels.Count) * levels.Count]).ToArray();
            }

            for (var i = 0; i < levelModels.Count; i++)
            {
                if (preview != null)
                {
                    prefabs[i].hideFlags = HideFlags.HideAndDontSave;
                    if (!preview.Capture(prefabs[i], levelModels[i], levelSurfaces[i]))
                    { error = $"Could not capture moving road LOD {i} preview geometry."; return false; }
                    continue;
                }
                var asset = AssetDatabase.user.AddAsset(
                    BridgeBuilder.Runtime.BridgePrefabPersistence.ResourcePath(prefabs[i].name, "Geometry"),
                    new Geometry(levelModels[i]));
                assets.Add(asset);
                // AddAsset copies model buffers into native asset data (CreateDataFromGeometry).
                // The input importer models remain ours to dispose in both preview and save paths.
                prefabs[i].geometryAsset = asset;
                asset.Save();
                asset.Unload();
            }
            created = prefabs.ToArray();

            success = true;
            return true;
        }
        catch (Exception exception)
        {
            Mod.Log.Error(exception, $"Moving road asset construction failed: style={styleId}; graph={name}; nativeNetwork={nativeNetworkGraph}.");
            error = "Moving road asset construction failed: " + exception.Message; return false;
        }
        finally
        {
            foreach (var model in models) model.Dispose();
            if (!success)
            {
                foreach (var asset in assets)
                {
                    // Delete only resources allocated by this failed call, including converted
                    // lane surfaces. Original materials/textures and existing assets are untouched.
                    try
                    {
                        if (MovableAssetOwnership.Matches(asset.path, owner, preview)
                            && (preview == null || ReferenceEquals(asset.database, preview.NativeAssets)))
                            asset.database.DeleteAsset(asset);
                        else error += " Moving resource rollback refused an unowned path: " + asset.path;
                    }
                    catch (Exception exception) { error += " Moving resource rollback failed: " + exception.Message; }
                }
                foreach (var prefab in prefabs)
                {
                    foreach (var component in prefab.components)
                        if (component != null) UnityEngine.Object.DestroyImmediate(component);
                    UnityEngine.Object.DestroyImmediate(prefab);
                }
            }
        }
    }

    // Native MeshSystem interprets RenderPrefab.meshCount as the number of material
    // draw batches. Match the native importer: one geometry model per material slot.
    // A multi-submesh road model would count as one and hide the remaining surfaces.
    private static ModelImporter.Model[] BuildMaterialMeshes(string name,
        IReadOnlyList<MovableRoadGeometry.BoundTriangle> triangles, SurfaceAsset[] surfaces,
        int boneCount, List<ModelImporter.Model> owned, out SurfaceAsset[] usedSurfaces)
    {
        var meshes = new List<ModelImporter.Model>();
        var materials = new List<SurfaceAsset>();
        var canonical = MovableMaterialSlots.Canonical(surfaces);
        foreach (var group in triangles.GroupBy(t => canonical[t.Geometry.Material]).OrderBy(g => g.Key))
        {
            var selected = group.Select(t => new MovableRoadGeometry.BoundTriangle(
                new MovableRoadGeometry.Triangle(t.Geometry.A, t.Geometry.B, t.Geometry.C,
                    0, t.Geometry.Occurrence, t.Geometry.SourceTriangle), t.Bone)).ToArray();
            if (!MovableRoadModel.TryBuild(name + " Material " + group.Key, selected, 1,
                    boneCount, out var mesh, out var error))
                throw new InvalidOperationException(error);
            owned.Add(mesh!);
            meshes.Add(mesh!);
            materials.Add(surfaces[group.Key]);
        }
        usedSurfaces = materials.ToArray();
        return meshes.ToArray();
    }

    private static bool Visit(RenderPrefab source, Dictionary<RenderPrefab, int> levels,
        HashSet<RenderPrefab> visited, HashSet<RenderPrefab> active)
    {
        if (source == null || !levels.ContainsKey(source) || active.Contains(source)) return false;
        if (!visited.Add(source)) return true;
        active.Add(source);
        foreach (var lod in source.GetComponent<LodProperties>()?.m_LodMeshes ?? Array.Empty<RenderPrefab>())
            if (!Visit(lod, levels, visited, active)) return false;
        active.Remove(source);
        return true;
    }

    private static bool BalanceAppearanceBatches(List<ModelImporter.Model[]> levels,
        List<SurfaceAsset[]> materials, List<ModelImporter.Model> owned)
    {
        var target=MovableMaterialSlots.MinimumLodSlots(levels.Select(level=>level.Length).ToArray());
        bool changed=false;
        for(int lod=0;lod<levels.Count;lod++)
        {
            if(levels[lod].Length>=target)continue;
            var models=levels[lod].ToList();var surfaces=materials[lod].ToList();
            while(models.Count<target)
            {
                int slot=Enumerable.Range(0,models.Count).OrderByDescending(i=>models[i].indexCount).First();
                var source=models[slot];
                if(source.subMeshCount!=1 || source.indexCount<6 || source.indexCount%3!=0)
                    throw new InvalidOperationException("Native appearance groups require more far triangles than this LOD supplies.");
                int split=(source.indexCount/6)*3;
                ModelImporter.Model Slice(int start,int count)
                {
                    var attributes=new List<ModelImporter.Model.VertexData>();NativeArray<int> indices=default;
                    try
                    {
                        foreach(var a in source)attributes.Add(new ModelImporter.Model.VertexData(a.attribute,a.format,a.dimension,
                            new NativeArray<byte>(a.data,Allocator.Persistent),a.isPacked));
                        indices=new NativeArray<int>(source.indices.GetSubArray(start,count),Allocator.Persistent);
                        var submesh=source.subMeshes[0];submesh.indexStart=0;submesh.indexCount=count;
                        var result=new ModelImporter.Model(source.name+" Batch "+owned.Count,source.transform,source.vertexCount,
                            indices,attributes.ToArray(),new[]{submesh},source.rootBoneIndex,source.bones);
                        owned.Add(result);return result;
                    }
                    catch {if(indices.IsCreated)indices.Dispose();foreach(var a in attributes)a.Dispose();throw;}
                }
                models[slot]=Slice(0,split);models.Insert(slot+1,Slice(split,source.indexCount-split));
                surfaces.Insert(slot+1,surfaces[slot]);changed=true;
            }
            levels[lod]=models.ToArray();materials[lod]=surfaces.ToArray();
        }
        return changed;
    }

    private static bool ValidSkeleton(ProceduralAnimationProperties? skeleton)
    {
        var bones = skeleton?.m_Bones;
        if (bones == null || bones.Length == 0) return false;
        for (var i = 0; i < bones.Length; i++)
        {
            var bone = bones[i];
            if (bone == null || !math.all(math.isfinite((float3)bone.position)) ||
                !math.all(math.isfinite((float3)bone.scale))) return false;
            for (var k = 0; k < 16; k++) if (!math.isfinite(bone.bindPose[k])) return false;
            for (var k = 0; k < 4; k++) if (!math.isfinite(bone.rotation[k])) return false;
            var chain = new HashSet<int>();
            for (var parent = i; parent != -1; parent = bones[parent].parentId)
                if (parent < 0 || parent >= bones.Length || bones[parent] == null || !chain.Add(parent)) return false;
        }
        return true;
    }

    private static bool ValidBounds(Bounds3 bounds) => math.all(math.isfinite(bounds.min)) &&
        math.all(math.isfinite(bounds.max)) && math.all(bounds.min <= bounds.max);

    private static void Include(ref Bounds3 bounds, Vector3 position)
    {
        bounds.min = math.min(bounds.min, (float3)position);
        bounds.max = math.max(bounds.max, (float3)position);
    }
}
