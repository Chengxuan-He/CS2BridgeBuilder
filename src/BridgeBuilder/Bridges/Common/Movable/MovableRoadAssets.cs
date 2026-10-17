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
        internal readonly bool CompactRoad;

        // Carries the same compiled receivers and lane result into the existing appearance,
        // partition, skinning and owned-asset path. Decals remain subject to that path's checks.
        internal Level(RenderPrefab source, MovableClosedRoad.Result road,
            MovableRoadGeometry.Partition partition, Bounds3 renderBounds, Pose roadToObject,
            MovableStructureModel.Plan[]? structure = null, ProceduralAnimationProperties? skeleton = null,
            Matrix4x4? closedWorld = null, int appearancePageSize = 2048, float texelsPerMeter = 32f,
            IReadOnlyList<MovableRoadGeometry.Partition>? roadPartitions = null,
            IReadOnlyList<MovableRoadGeometry.Partition>? lanePartitions = null,
            IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? roadTrianglePartitions = null,
            Supplement? extraStructure = null, bool compactRoad = false)
            : this(source, road.Triangles, road.Surfaces, partition, renderBounds, roadToObject,
                structure, skeleton, road.LaneGeometry, closedWorld, appearancePageSize, texelsPerMeter,
                roadPartitions, lanePartitions, roadTrianglePartitions, extraStructure, compactRoad) { }

        internal Level(RenderPrefab source, IReadOnlyList<MovableRoadGeometry.Triangle> triangles,
            SurfaceAsset[] surfaces, MovableRoadGeometry.Partition partition, Bounds3 renderBounds, Pose roadToObject,
            MovableStructureModel.Plan[]? structure = null, ProceduralAnimationProperties? skeleton = null,
            MovableLaneGeometry.Result? lanes = null, Matrix4x4? closedWorld = null,
            int appearancePageSize = 2048, float texelsPerMeter = 32f,
            IReadOnlyList<MovableRoadGeometry.Partition>? roadPartitions = null,
            IReadOnlyList<MovableRoadGeometry.Partition>? lanePartitions = null,
            IReadOnlyDictionary<(int Occurrence, int SourceTriangle), MovableRoadGeometry.Partition>? roadTrianglePartitions = null,
            Supplement? extraStructure = null, bool compactRoad = false)
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
            CompactRoad = compactRoad;
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

    // Levels contain the root first and every reachable native LOD exactly once. Output resources
    // stay in this call's rollback list until the entire graph succeeds; no partial LOD is published.
    internal static bool TryCreate(string owner, string name, IReadOnlyList<Level> levels,
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
        var success = false;
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
                        AssetDataPath.Create("BridgeBuilder", levelName, EscapeStrategy.None), out _))
                { error = $"Movable render geometry already exists: '{levelName}'."; return false; }
                var roadTriangles = level.Triangles;
                var roadSurfaces = level.Surfaces;
                var roadModels = Array.Empty<ModelImporter.Model>();
                if (level.HasRoad)
                {
                    foreach (var surface in roadSurfaces)
                    {
                        if (surface == null) throw new InvalidOperationException($"Moving road LOD {i} has a null surface.");
                        surface.LoadHeader();
                    }
                    foreach (var surface in roadSurfaces)
                    {
                        if (surface == null)
                        { error = $"Moving road LOD {i} has an unresolved surface reference."; return false; }
                        surface.LoadHeader();
                        // Measured SG_DefaultShader templates in game 1.6.2f1 support procedural
                        // object skinning. This guard checks representation, not appearance equality.
                        // Network inputs must have been assigned existing object materials by the caller.
                        var template = surface.materialTemplateHash;
                        if (template != 0 && template != 1 && template != 13 && template != 14 &&
                            template != 35 && template != 36)
                        { error = $"Moving road LOD {i} surface '{surface.name}' requires an unimplemented object conversion (template {template})."; return false; }
                    }
                    // Appearance is evaluated in the selected-road frame. Only evaluated geometry
                    // moves into the measured mechanism frame before partitioning and bone binding.
                    roadTriangles = MovableRoadGeometry.InFrame(roadTriangles, level.RoadToObject);
                    if (!MovableRoadGeometry.TryBind(roadTriangles, level.Partition!,
                            skeleton!.m_Bones.Length, roadSurfaces.Length, out var triangles, out error,
                            occurrencePartitions: level.RoadPartitions, trianglePartitions: level.RoadTrianglePartitions))
                    { error = $"Moving road LOD {i}: {error}"; return false; }
                    // Atlas clipping and partitioning interpolate new vertices. Include
                    // the emitted positions, not only pre-conversion input extrema.
                    foreach (var triangle in triangles)
                    {
                        Include(ref bounds, triangle.Geometry.A.Position);
                        Include(ref bounds, triangle.Geometry.B.Position);
                        Include(ref bounds, triangle.Geometry.C.Position);
                    }
                    if (level.Lanes != null && level.Lanes.Decals.Length != 0)
                        triangles = MovableRoadPaint.Apply(triangles, level.Lanes.Decals,
                            level.RoadToObject, level.TexelsPerMeter, ref roadSurfaces);
                    // Appearance and paint boundaries are already encoded in vertex attributes.
                    // Prototype face IDs are provenance, not mandatory seams on a flat receiver.
                    if (level.CompactRoad) triangles = MovableRoadLod.Compact(triangles);
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
                        if (loaded.Any(mesh => mesh == null) || originals.Any(surface => surface == null) ||
                            originals.Length != loaded.Sum(mesh => mesh.subMeshCount))
                        { error = $"Moving road LOD {i} has unresolved structural material slots."; return false; }
                        for (var part = 0; part < loaded.Length; part++)
                        {
                            if (!MovableStructureModel.TryBuild(levelName + " Structure " + part, loaded[part],
                                    level.Structure[part], boneCount, out var structureModel, out error)) return false;
                            models.Add(structureModel!); batch.Add(structureModel!);
                            // A static support retains native authored stacking bounds. Native
                            // decoded vertices can slightly exceed those bounds through compression;
                            // the model builder computes accurate submesh bounds independently.
                            // Procedural renders also enclose the generated closed-pose structure.
                            if (skeleton != null)
                                foreach (var position in level.Structure[part].Positions)
                                    Include(ref bounds, position);
                        }
                        surfaces.AddRange(originals);
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
                                extra.Plan, boneCount, out var model, out error, compact: true)) return false;
                        models.Add(model!); batch.Add(model!);
                        var slot = loaded.Take(extra.Mesh).Sum(mesh => mesh.subMeshCount);
                        surfaces.Add(extra.Source.GetSurfaceAsset(slot));
                        for (var slotIndex = 0; slotIndex < extra.Plan.SourceIndices.Length; slotIndex++)
                            for (var face = 0; face < extra.Plan.RemoveTriangles[slotIndex].Length; face++)
                                if (!extra.Plan.RemoveTriangles[slotIndex][face])
                                    for (var corner = 0; corner < 3; corner++)
                                        Include(ref bounds, extra.Plan.Positions[extra.Plan.SourceIndices[slotIndex][face * 3 + corner]]);
                    }
                    finally { PrivateGeometryReader.Release(loaded); }
                }
                // Original slots retain their order across every structural mesh; converted road
                // slots follow them. No native structural surface is cloned or reassigned.
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
                        converted = MovableSurfaceAssets.ExistingObject(source, levels[0].Source.GetSurfaceAsset(0));
                        laneMaterials.Add(source, converted);
                    }
                    levelSurfaces[i][slot] = converted;
                }

            for (var i = 0; i < levels.Count; i++)
            {
                var level = levels[i];
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
                    if (component != null && component is not ProceduralAnimationProperties) prefab.AddComponentFrom(component);
                if (level.Skeleton != null) prefab.AddComponentFrom(level.Skeleton);
                prefab.surfaceAssets = levelSurfaces[i];
                prefab.bounds = renderBounds[i];
                prefab.vertexCount = levelModels[i].Sum(model => model.vertexCount);
                prefab.indexCount = levelModels[i].Sum(model => model.indexCount);
                prefab.meshCount = levelModels[i].Length;
                // The temporary Geometry wrapper only reads the model for this calculation.
                prefab.surfaceArea = new Geometry(levelModels[i]).CalcSurfaceArea();
                if (!math.isfinite(prefab.surfaceArea))
                { error = $"Moving road LOD {i} has non-finite surface area."; return false; }
            }
            for (var i = 0; i < levels.Count; i++)
            {
                var lods = prefabs[i].GetComponent<LodProperties>();
                if (lods?.m_LodMeshes != null)
                    lods.m_LodMeshes = lods.m_LodMeshes.Select(source => prefabs[indices[source]]).ToArray();
            }

            for (var i = 0; i < levels.Count; i++)
            {
                if (preview != null)
                {
                    prefabs[i].hideFlags = HideFlags.HideAndDontSave;
                    if (!preview.Capture(prefabs[i], levelModels[i], levelSurfaces[i]))
                    { error = $"Could not capture moving road LOD {i} preview geometry."; return false; }
                    continue;
                }
                var asset = AssetDatabase.user.AddAsset(
                    AssetDataPath.Create("BridgeBuilder", prefabs[i].name, EscapeStrategy.None),
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
        { error = "Moving road asset construction failed: " + exception.Message; return false; }
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
        foreach (var group in triangles.GroupBy(t => t.Geometry.Material).OrderBy(g => g.Key))
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
