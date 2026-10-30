using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;

// Converts evaluated native road LODs using style-owned motion and appearance data.
// Structure plans and skinned paint are attached by the mechanism assembly afterwards.
internal static class MovableRoadPreparation
{
    internal static MovableRoadAssets.Level[] Prepare(MovableStraightSample sample,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout,
        Func<int, MovableRoadAppearance.Level>? appearanceForLod = null, bool combinePartitions = false)
        => Prepare(sample.Road, sample.RoadToObject, sample.CompileLod, sources, halfDelta,
            closedWorld, material, carriageway, sidewalk, carriagewayAppearance,
            sidewalkAppearance, patches, boneCount, layout, appearanceForLod, combinePartitions);

    // A coordinated dual-network sample supplies a separate road frame and LOD
    // compiler for each deck. Never reuse the main deck's frame for the auxiliary.
    internal static MovableRoadAssets.Level[] Prepare(MovableClosedRoad.Result road,
        Pose pose, Func<int, MovableClosedRoad.Result> compileLod,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout,
        Func<int, MovableRoadAppearance.Level>? appearanceForLod = null, bool combinePartitions = false)
    {
        var physical = new Partition[road.Occurrences.Length];
        for (var i = 0; i < physical.Length; i++)
        {
            var walk = layout.SidewalkOccurrences[i];
            physical[i] = walk ? sidewalk : carriageway;
        }
        var lanePartitions = road.LaneVisuals.Select(lane =>
            lane.SourceLane.Has<PedestrianLane>() ? sidewalk : carriageway).ToArray();
        var rotation = Quaternion.Inverse(pose.rotation);
        var inverse = new Pose(rotation * -pose.position, rotation);
        var inputs = new MovableSurfaceAtlas.Input[sources.Count];
        var lanes = new MovableLaneGeometry.Result[sources.Count];
        var bounds = new Bounds3[sources.Count];
        for (var lod = 0; lod < sources.Count; lod++)
        {
            var appearanceLevel = appearanceForLod?.Invoke(lod);
            var levelPatches = appearanceLevel?.Patches ?? patches;
            var levelCarriageway = appearanceLevel?.Carriageway ?? carriagewayAppearance;
            var levelSidewalk = appearanceLevel?.Sidewalk ?? sidewalkAppearance;
            var levelAppearance = layout.SidewalkOccurrences.Select(walk => walk ? levelSidewalk : levelCarriageway).ToArray();
            var source = compileLod(lod);
            lanes[lod] = new MovableLaneGeometry.Result(source.LaneGeometry.Triangles.ToList(),
                source.LaneGeometry.Surfaces.ToList(), new List<MovableLaneVisuals.Batch>());
            var geometry = MovableRoadGeometry.InFrame(source.Triangles, pose);
            var bones = sources[0].GetComponent<ProceduralAnimationProperties>().m_Bones;
            var assigned = Array.Empty<MovableRoadGeometry.BoundTriangle>();
            if(combinePartitions)
            {
                var movingBones=bones.Select(b=>b.m_ConnectionID!=0).ToArray();
                var transform=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
                var joint=physical.Select((tree,index)=>MovableRoadGeometry.CombinePartitions(
                    tree,levelAppearance[index],transform,movingBones,levelPatches.Length)).ToArray();
                var fallback=MovableRoadGeometry.CombinePartitions(carriageway,levelCarriageway,
                    transform,movingBones,levelPatches.Length);
                if(!MovableRoadGeometry.TryBind(source.Triangles,fallback,checked(levelPatches.Length*2),
                    source.Surfaces.Length,out var combined,out var jointError,occurrencePartitions:joint))
                    throw new InvalidOperationException(jointError);
                assigned=combined.Select(face=>new MovableRoadGeometry.BoundTriangle(face.Geometry,
                    face.Bone>=(uint)levelPatches.Length
                        ?(face.Bone-(uint)levelPatches.Length)|MovableSurfaceAtlas.MovingSurface:face.Bone)).ToArray();
            }
            else
            {
                if (!MovableRoadGeometry.TryBind(geometry, carriageway, boneCount, source.Surfaces.Length,
                    out var receivers, out var error, occurrencePartitions: physical))
                    throw new InvalidOperationException(error);
                var converted = new bool[source.Surfaces.Length];
                for (var i = 0; i < converted.Length; i++)
                {
                    source.Surfaces[i].LoadHeader();
                    converted[i] = !MovableSurfaceAssets.IsObjectSurface(source.Surfaces[i]);
                }
                bool Original(MovableRoadGeometry.BoundTriangle face) => bones[face.Bone].m_ConnectionID != 0;
                var original = receivers.Where(Original).ToArray();
                var prepared = MovableRoadTemplate.Prepare(receivers.Where(face => !Original(face)).ToArray(), converted);
                var selected = MovableRoadGeometry.InFrame(prepared, inverse);
                if (selected.Length != 0 && !MovableRoadGeometry.TryBind(selected, levelCarriageway, levelPatches.Length,
                    source.Surfaces.Length, out assigned, out error, occurrencePartitions: levelAppearance))
                    throw new InvalidOperationException(error);
                var moving = MovableRoadGeometry.InFrame(original.Select(face => face.Geometry).ToArray(), inverse);
                var movingAppearance = Array.Empty<MovableRoadGeometry.BoundTriangle>();
                if (moving.Length != 0 && !MovableRoadGeometry.TryBind(moving, levelCarriageway, levelPatches.Length,
                    source.Surfaces.Length, out movingAppearance, out error, occurrencePartitions: levelAppearance))
                    throw new InvalidOperationException(error);
                assigned = assigned.Concat(movingAppearance.Select(face => new MovableRoadGeometry.BoundTriangle(
                    face.Geometry, face.Bone | MovableSurfaceAtlas.MovingSurface))).ToArray();
            }
            inputs[lod] = new MovableSurfaceAtlas.Input(selectedInput: true)
            {
                PrototypeRoad = layout.Prototype is "Draw02" or "Draw03" or "Lift03 Main",
                Faces = assigned, Materials = source.Surfaces, Patches = levelPatches,
                Native = appearanceLevel?.Native ?? sources[0].GetSurfaceAsset(0),
                SupportedTypes = appearanceLevel?.SupportedTypes ?? Array.Empty<MovableRoadAppearance.Type>(),
                SourceFrame = closedWorld * Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one)
            };
            bounds[lod] = sources[lod].bounds;
            bounds[lod].min.x -= halfDelta;
            bounds[lod].max.x += halfDelta;
            Include(geometry, ref bounds[lod]);
            Include(MovableRoadGeometry.InFrame(source.LaneGeometry.Triangles, pose), ref bounds[lod]);
        }
        var visible = MovableSurfaceAtlas.Compose(inputs, material,
            Array.Empty<MovableNativeDecals.Region>(), out var surfaces);
        var result = new MovableRoadAssets.Level[sources.Count];
        for (var lod = 0; lod < result.Length; lod++)
            result[lod] = new MovableRoadAssets.Level(sources[lod], visible[lod], surfaces[lod],
                carriageway, bounds[lod], pose, lanes: lanes[lod], closedWorld: closedWorld,
                roadPartitions: physical, lanePartitions: lanePartitions);
        return result;
    }

    private static void Include(IReadOnlyList<MovableRoadGeometry.Triangle> triangles, ref Bounds3 bounds)
    {
        foreach (var triangle in triangles)
        {
            bounds.min = math.min(bounds.min, math.min((float3)triangle.A.Position,
                math.min((float3)triangle.B.Position, (float3)triangle.C.Position)));
            bounds.max = math.max(bounds.max, math.max((float3)triangle.A.Position,
                math.max((float3)triangle.B.Position, (float3)triangle.C.Position)));
        }
    }
}
