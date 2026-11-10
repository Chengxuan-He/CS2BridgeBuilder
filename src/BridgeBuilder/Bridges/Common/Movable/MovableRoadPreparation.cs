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
    // Runs after physical/appearance binding, before source-aware material routing.
    // Faces remain in the compiled road frame. Bone is an appearance domain here,
    // including MovingSurface; preserve that flag and every Geometry field. The
    // callback may reassign domains and extend patches/types, never split faces,
    // alter motion, infer source class from a root name, or bypass Compose gates.
    internal delegate void AssignedAppearance(int lod, MovableClosedRoad.Result source,
        ref MovableRoadGeometry.BoundTriangle[] assigned,
        ref MovableExistingRoadSurface.Patch[] patches,
        ref MovableRoadAppearance.Type[] supportedTypes);

    // appearanceForReceiver is optional per-LOD selection by measured source category.
    // Return stable Level instances with independent appearance partitions and Native
    // sources. Null inherits appearanceForLod; physical partitions and lane bones are
    // never selected by this callback. Compose still checks correspondence per face.

    internal static MovableRoadAssets.Level[]? Prepare(MovableStraightSample sample,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout,
        Func<int, MovableRoadAppearance.Level>? appearanceForLod = null, bool combinePartitions = false,
        AssignedAppearance? assignedAppearance = null,
        Action<int, IReadOnlyList<MovableRoadAppearanceBinding>>? captureAppearance = null,
        Func<int, MovableRoadAppearance.Type, MovableRoadAppearance.Level?>? appearanceForReceiver = null)
        => Prepare(sample.Road, sample.RoadToObject, sample.CompileLod, sources, halfDelta,
            closedWorld, material, carriageway, sidewalk, carriagewayAppearance,
            sidewalkAppearance, patches, boneCount, layout, appearanceForLod, combinePartitions, assignedAppearance, captureAppearance, appearanceForReceiver);

    // A coordinated dual-network sample supplies a separate road frame and LOD
    // compiler for each deck. Never reuse the main deck's frame for the auxiliary.
    internal static MovableRoadAssets.Level[]? Prepare(MovableClosedRoad.Result road,
        Pose pose, Func<int, MovableClosedRoad.Result> compileLod,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout,
        Func<int, MovableRoadAppearance.Level>? appearanceForLod = null, bool combinePartitions = false,
        AssignedAppearance? assignedAppearance = null,
        Action<int, IReadOnlyList<MovableRoadAppearanceBinding>>? captureAppearance = null,
        Func<int, MovableRoadAppearance.Type, MovableRoadAppearance.Level?>? appearanceForReceiver = null)
    {
        if(road.Error!=null){material.Fail("native-source",road.Error);return null;}
        var physicalByLod = new Dictionary<(int,int),Partition>[sources.Count];
        var lanePartitions = road.LaneVisuals.Select(lane =>
            lane.SourceLane.Has<PedestrianLane>() ? sidewalk : carriageway).ToArray();
        var rotation = Quaternion.Inverse(pose.rotation);
        var inverse = new Pose(rotation * -pose.position, rotation);
        var inputs = new MovableSurfaceAtlas.Input[sources.Count];
        var lanes = new MovableLaneGeometry.Result[sources.Count];
        var bounds = new Bounds3[sources.Count];
        var routeSources = new Dictionary<Colossal.IO.AssetDatabase.SurfaceAsset, MovableRoadRoute.Source>();
        for (var lod = 0; lod < sources.Count; lod++)
        {
            var appearanceLevel = appearanceForLod?.Invoke(lod);
            if(appearanceLevel?.Error!=null){material.Fail("appearance-level",appearanceLevel.Error);return null;}
            var source = compileLod(lod);
            if(source.Error!=null){material.Fail("native-source",source.Error);return null;}
            if(source.Sidewalks.Error!=null){material.Fail("receiver-classification",source.Sidewalks.Error);return null;}
            if(appearanceForReceiver!=null)foreach(var face in source.Triangles)
            {
                var surface=source.Surfaces[face.Material];
                if(routeSources.ContainsKey(surface))continue;
                if(!MovableRoadRoute.TryRead(surface,out var identity,out var error)){material.Fail("appearance-classification",error);return null;}
                routeSources.Add(surface,identity);
            }
            var physical = physicalByLod[lod] = source.Sidewalks.Partitions(carriageway, sidewalk);
            lanes[lod] = new MovableLaneGeometry.Result(source.LaneGeometry.Triangles.ToList(),
                source.LaneGeometry.Surfaces.ToList(), new List<MovableLaneVisuals.Batch>());
            var geometry = MovableRoadGeometry.InFrame(source.Triangles, pose);
            var bones = sources[0].GetComponent<ProceduralAnimationProperties>().m_Bones;
            // Classification chooses an appearance family, never a different road or
            // motion partition. Unknown receivers keep the ordinary original route.
            MovableRoadAppearance.Level? Family(MovableRoadGeometry.Triangle triangle)
            {
                if (appearanceForReceiver == null) return appearanceLevel;
                var surface = source.Surfaces[triangle.Material];
                var identity=routeSources[surface];
                return appearanceForReceiver(lod, identity.Receiver(triangle)) ?? appearanceLevel;
            }
            var allAssigned = new List<MovableRoadGeometry.BoundTriangle>();
            var allPatches = new List<MovableExistingRoadSurface.Patch>();
            var allTypes = new List<MovableRoadAppearance.Type>();
            var allNatives = new List<Colossal.IO.AssetDatabase.SurfaceAsset>();
            // Preserve the default path's face order and single family, including an
            // empty road. Alternatives are grouped by the returned immutable level.
            var families = appearanceForReceiver == null
                ? new[] { (Level: appearanceLevel, Triangles: source.Triangles.ToArray()) }
                : source.Triangles.GroupBy(Family).Select(group =>
                    (Level: group.Key, Triangles: group.ToArray())).ToArray();
            foreach (var family in families)
            {
                var familyLevel = family.Level;
                if(familyLevel?.Error!=null){material.Fail("appearance-family",familyLevel.Error);return null;}
                var familyTriangles = family.Triangles;
                var familyPatches = familyLevel?.Patches ?? patches;
                var levelCarriageway = familyLevel?.Carriageway ?? carriagewayAppearance;
                var levelSidewalk = familyLevel?.Sidewalk ?? sidewalkAppearance;
                var levelAppearance = source.Sidewalks.Partitions(levelCarriageway, levelSidewalk);
                var familyAssigned = Array.Empty<MovableRoadGeometry.BoundTriangle>();
                if(combinePartitions)
                {
                    var movingBones=bones.Select(b=>b.m_ConnectionID!=0).ToArray();
                    var transform=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
                    var fallback=MovableRoadGeometry.CombinePartitions(carriageway,levelCarriageway,
                        transform,movingBones,familyPatches.Length);
                    var walking=MovableRoadGeometry.CombinePartitions(sidewalk,levelSidewalk,
                        transform,movingBones,familyPatches.Length);
                    var joint=source.Sidewalks.Partitions(fallback,walking);
                    if(!MovableRoadGeometry.TryBind(familyTriangles,fallback,checked(familyPatches.Length*2),
                        source.Surfaces.Length,out var combined,out var jointError,trianglePartitions:joint))
                        throw new InvalidOperationException(jointError);
                    familyAssigned=combined.Select(face=>new MovableRoadGeometry.BoundTriangle(face.Geometry,
                        face.Bone>=(uint)familyPatches.Length
                            ?(face.Bone-(uint)familyPatches.Length)|MovableSurfaceAtlas.MovingSurface:face.Bone)).ToArray();
                }
                else
                {
                    if (!MovableRoadGeometry.TryBind(MovableRoadGeometry.InFrame(familyTriangles, pose), carriageway, boneCount, source.Surfaces.Length,
                        out var receivers, out var error, trianglePartitions: physical))
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
                    if (selected.Length != 0 && !MovableRoadGeometry.TryBind(selected, levelCarriageway, familyPatches.Length,
                        source.Surfaces.Length, out familyAssigned, out error, trianglePartitions: levelAppearance))
                        throw new InvalidOperationException(error);
                    var moving = MovableRoadGeometry.InFrame(original.Select(face => face.Geometry).ToArray(), inverse);
                    var movingAppearance = Array.Empty<MovableRoadGeometry.BoundTriangle>();
                    if (moving.Length != 0 && !MovableRoadGeometry.TryBind(moving, levelCarriageway, familyPatches.Length,
                        source.Surfaces.Length, out movingAppearance, out error, trianglePartitions: levelAppearance))
                        throw new InvalidOperationException(error);
                    familyAssigned = familyAssigned.Concat(movingAppearance.Select(face => new MovableRoadGeometry.BoundTriangle(
                        face.Geometry, face.Bone | MovableSurfaceAtlas.MovingSurface))).ToArray();
                }
                var familyTypes = familyLevel?.SupportedTypes ?? Array.Empty<MovableRoadAppearance.Type>();
                var domainOffset = checked((uint)allPatches.Count);
                foreach (var face in familyAssigned)
                    allAssigned.Add(new MovableRoadGeometry.BoundTriangle(face.Geometry,
                        checked((face.Bone & ~MovableSurfaceAtlas.MovingSurface) + domainOffset)
                        | (face.Bone & MovableSurfaceAtlas.MovingSurface)));
                allPatches.AddRange(familyPatches);
                for (var domain = 0; domain < familyPatches.Length; domain++)
                {
                    allTypes.Add(domain < familyTypes.Length ? familyTypes[domain] : MovableRoadAppearance.Type.None);
                    if (appearanceForReceiver != null)
                        allNatives.Add(familyLevel?.Native ?? sources[0].GetSurfaceAsset(0));
                }
            }
            var assigned = allAssigned.ToArray();
            var levelPatches = allPatches.ToArray();
            var supportedTypes = appearanceForReceiver == null
                ? appearanceLevel?.SupportedTypes ?? Array.Empty<MovableRoadAppearance.Type>()
                : allTypes.ToArray();
            assignedAppearance?.Invoke(lod, source, ref assigned, ref levelPatches, ref supportedTypes);
            inputs[lod] = new MovableSurfaceAtlas.Input(selectedInput: true)
            {
                PrototypeRoad = layout.Prototype is "Draw02" or "Draw03" or "Lift03 Main",
                Faces = assigned, Materials = source.Surfaces, Patches = levelPatches,
                Native = appearanceLevel?.Native ?? sources[0].GetSurfaceAsset(0),
                SupportedTypes = supportedTypes,
                DomainNatives = appearanceForReceiver == null ? null : allNatives.ToArray(),
                SourceFrame = closedWorld * Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one)
            };
            inputs[lod].SetReceiverRoles(source);
            bounds[lod] = sources[lod].bounds;
            bounds[lod].min.x -= halfDelta;
            bounds[lod].max.x += halfDelta;
            Include(geometry, ref bounds[lod]);
            Include(MovableRoadGeometry.InFrame(source.LaneGeometry.Triangles, pose), ref bounds[lod]);
        }
        var visible = MovableSurfaceAtlas.Compose(inputs, material,
            Array.Empty<MovableNativeDecals.Region>(), out var surfaces, captureAppearance);
        if (visible == null) return null;
        var result = new MovableRoadAssets.Level[sources.Count];
        for (var lod = 0; lod < result.Length; lod++)
            result[lod] = new MovableRoadAssets.Level(sources[lod], visible[lod], surfaces[lod],
                carriageway, bounds[lod], pose, lanes: lanes[lod], closedWorld: closedWorld,
                roadTrianglePartitions: physicalByLod[lod], lanePartitions: lanePartitions);
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
