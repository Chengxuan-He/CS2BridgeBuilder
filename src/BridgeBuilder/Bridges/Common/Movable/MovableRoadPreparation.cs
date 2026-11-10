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
        Func<int, MovableRoadAppearance.Type, MovableRoadAppearance.Level?>? appearanceForReceiver = null) =>
        GenerationWork.Synchronous(() => PrepareAsync(sample, sources, halfDelta, closedWorld, material, carriageway, sidewalk, carriagewayAppearance, sidewalkAppearance, patches, boneCount, layout, appearanceForLod, combinePartitions, assignedAppearance, captureAppearance, appearanceForReceiver));

    internal static System.Threading.Tasks.Task<MovableRoadAssets.Level[]?> PrepareAsync(MovableStraightSample sample,
        IReadOnlyList<RenderPrefab> sources, float halfDelta, Matrix4x4 closedWorld,
        MovableRoadMaterial material, Partition carriageway, Partition sidewalk,
        Partition carriagewayAppearance, Partition sidewalkAppearance,
        MovableExistingRoadSurface.Patch[] patches, int boneCount, MovableSelectedRoadLayout layout,
        Func<int, MovableRoadAppearance.Level>? appearanceForLod = null, bool combinePartitions = false,
        AssignedAppearance? assignedAppearance = null,
        Action<int, IReadOnlyList<MovableRoadAppearanceBinding>>? captureAppearance = null,
        Func<int, MovableRoadAppearance.Type, MovableRoadAppearance.Level?>? appearanceForReceiver = null)
        => PrepareAsync(sample.Road, sample.RoadToObject, sample.CompileLodAsync, sources, halfDelta,
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
        Func<int, MovableRoadAppearance.Type, MovableRoadAppearance.Level?>? appearanceForReceiver = null) =>
        GenerationWork.Synchronous(() => PrepareAsync(road, pose, lod => System.Threading.Tasks.Task.FromResult(compileLod(lod)), sources, halfDelta, closedWorld, material, carriageway, sidewalk, carriagewayAppearance, sidewalkAppearance, patches, boneCount, layout, appearanceForLod, combinePartitions, assignedAppearance, captureAppearance, appearanceForReceiver));

    internal static async System.Threading.Tasks.Task<MovableRoadAssets.Level[]?> PrepareAsync(MovableClosedRoad.Result road,
        Pose pose, Func<int, System.Threading.Tasks.Task<MovableClosedRoad.Result>> compileLod,
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
        var stageCache = new GenerationStageCache();
        var canonicalLevels=new List<MovableRoadAppearance.Level>();
        MovableRoadAppearance.Level? CanonicalLevel(MovableRoadAppearance.Level? value)
        {
            if(value==null)return null;
            foreach(var previous in canonicalLevels)if(MovableAppearanceReuse.SameLevel(previous,value))return previous;
            canonicalLevels.Add(value);return value;
        }
        var canonicalPartitions=new List<Partition>();
        Partition Canonical(Partition value)
        {
            foreach(var previous in canonicalPartitions)if(MovableRoadLodCache.Tree(previous,value))return previous;
            canonicalPartitions.Add(value);return value;
        }
        var movingBones=sources[0].GetComponent<ProceduralAnimationProperties>().m_Bones.Select(b=>b.m_ConnectionID!=0).ToArray();
        var conversionCache=new Dictionary<MovableClosedRoad.Result,bool[]>();
        for (var lod = 0; lod < sources.Count; lod++)
        {
            var appearanceLevel = appearanceForLod?.Invoke(lod);
            if(appearanceLevel?.Error!=null){material.Fail("appearance-level",appearanceLevel.Error);return null;}
            await GenerationWork.Next("road appearance LOD " + lod);
            var source = await compileLod(lod);
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
            lanes[lod] = new MovableLaneGeometry.Result(source.LaneGeometry.Triangles,
                source.LaneGeometry.Surfaces, Array.Empty<MovableLaneVisuals.Batch>());
            var sourceTriangles=source.Triangles;var laneTriangles=source.LaneGeometry.Triangles;
            var selectedBounds = await stageCache.Get("selected-road-bounds",new object[]{source.Triangles,source.LaneGeometry.Triangles},
                ()=>GenerationWork.Background("selected road and lane bounds",()=>
                    FramedBounds(sourceTriangles,laneTriangles,pose)));
            if(!conversionCache.TryGetValue(source,out var converted))
            {
                converted=new bool[source.Surfaces.Length];
                if(!combinePartitions)for(int i=0;i<converted.Length;i++)
                { source.Surfaces[i].LoadHeader();converted[i]=!MovableSurfaceAssets.IsObjectSurface(source.Surfaces[i]); }
                conversionCache.Add(source,converted);
            }
            var assignedGroups = new List<(MovableRoadGeometry.BoundTriangle[] Faces,uint Offset)>();
            var allPatches = new List<MovableExistingRoadSurface.Patch>();
            var allTypes = new List<MovableRoadAppearance.Type>();
            var allNatives = new List<Colossal.IO.AssetDatabase.SurfaceAsset>();
            // Preserve the default path's face order and single family, including an
            // empty road. Alternatives are grouped by the returned immutable level.
            (MovableRoadAppearance.Level? Level,MovableRoadGeometry.Triangle[] Triangles)[] families;
            if(appearanceForReceiver==null)families=new[]{(CanonicalLevel(appearanceLevel),sourceTriangles)};
            else
            {
                var identities=source.Surfaces.Select(surface=>routeSources[surface]).ToArray();
                var receivers=await stageCache.Get("appearance-receiver-kinds",new object[]{sourceTriangles},
                    ()=>GenerationWork.Background("road receiver classification",()=>
                        sourceTriangles.Select(t=>identities[t.Material].Receiver(t)).ToArray()));
                var kinds=receivers.Distinct().ToArray();
                var choices=kinds.Select(kind=>CanonicalLevel(appearanceForReceiver(lod,kind)??appearanceLevel)).ToArray();
                var uniqueChoices=new List<MovableRoadAppearance.Level?>();var familyIndices=new Dictionary<MovableRoadAppearance.Type,int>();
                for(int i=0;i<kinds.Length;i++)
                {
                    int index=uniqueChoices.FindIndex(level=>ReferenceEquals(level,choices[i]));
                    if(index<0){index=uniqueChoices.Count;uniqueChoices.Add(choices[i]);}
                    familyIndices.Add(kinds[i],index);
                }
                var groupKey=new List<object>{sourceTriangles};groupKey.AddRange(choices.Select(choice=>(object)choice!));
                int familyCount=uniqueChoices.Count;
                var grouped=await stageCache.Get("appearance-family-groups",groupKey.ToArray(),
                    ()=>GenerationWork.Background("road receiver grouping",()=>
                    {
                        var lists=Enumerable.Range(0,familyCount).Select(_=>new List<MovableRoadGeometry.Triangle>()).ToArray();
                        for(int i=0;i<sourceTriangles.Length;i++)lists[familyIndices[receivers[i]]].Add(sourceTriangles[i]);
                        return lists.Select(list=>list.ToArray()).ToArray();
                    }));
                families=grouped.Select((triangles,index)=>(uniqueChoices[index],triangles)).ToArray();
            }
            foreach (var family in families)
            {
                var familyLevel = family.Level;
                if(familyLevel?.Error!=null){material.Fail("appearance-family",familyLevel.Error);return null;}
                var familyTriangles = family.Triangles;
                var familyPatches = familyLevel?.Patches ?? patches;
                var levelCarriageway = familyLevel?.Carriageway ?? carriagewayAppearance;
                var levelSidewalk = familyLevel?.Sidewalk ?? sidewalkAppearance;
                var levelAppearance = source.Sidewalks.Partitions(levelCarriageway, levelSidewalk);
                var roles=source.Sidewalks;var surfaceCount=source.Surfaces.Length;var domainCount=familyPatches.Length;
                var familyAssigned = await stageCache.Get("appearance-partition-bind",
                    new object[]{familyTriangles,Canonical(levelCarriageway),Canonical(levelSidewalk),domainCount,converted},
                    ()=>GenerationWork.Background("road appearance partition and bind",()=>
                    {
                var computed = Array.Empty<MovableRoadGeometry.BoundTriangle>();
                if(combinePartitions)
                {
                    var transform=Matrix4x4.TRS(pose.position,pose.rotation,Vector3.one);
                    var fallback=MovableRoadGeometry.CombinePartitions(carriageway,levelCarriageway,
                        transform,movingBones,domainCount);
                    var walking=MovableRoadGeometry.CombinePartitions(sidewalk,levelSidewalk,
                        transform,movingBones,domainCount);
                    var joint=roles.Partitions(fallback,walking);
                    if(!MovableRoadGeometry.TryBind(familyTriangles,fallback,checked(domainCount*2),
                        surfaceCount,out var combined,out var jointError,trianglePartitions:joint))
                        throw new InvalidOperationException(jointError);
                    computed=combined.Select(face=>new MovableRoadGeometry.BoundTriangle(face.Geometry,
                        face.Bone>=(uint)domainCount
                            ?(face.Bone-(uint)domainCount)|MovableSurfaceAtlas.MovingSurface:face.Bone)).ToArray();
                }
                else
                {
                    if (!MovableRoadGeometry.TryBind(MovableRoadGeometry.InFrame(familyTriangles, pose), carriageway, boneCount, surfaceCount,
                        out var receivers, out var error, trianglePartitions: physical))
                        throw new InvalidOperationException(error);
                    bool Original(MovableRoadGeometry.BoundTriangle face) => movingBones[face.Bone];
                    var original = receivers.Where(Original).ToArray();
                    var prepared = MovableRoadTemplate.Prepare(receivers.Where(face => !Original(face)).ToArray(), converted);
                    var selected = MovableRoadGeometry.InFrame(prepared, inverse);
                    if (selected.Length != 0 && !MovableRoadGeometry.TryBind(selected, levelCarriageway, domainCount,
                        surfaceCount, out computed, out error, trianglePartitions: levelAppearance))
                        throw new InvalidOperationException(error);
                    var moving = MovableRoadGeometry.InFrame(original.Select(face => face.Geometry).ToArray(), inverse);
                    var movingAppearance = Array.Empty<MovableRoadGeometry.BoundTriangle>();
                    if (moving.Length != 0 && !MovableRoadGeometry.TryBind(moving, levelCarriageway, domainCount,
                        surfaceCount, out movingAppearance, out error, trianglePartitions: levelAppearance))
                        throw new InvalidOperationException(error);
                    computed = computed.Concat(movingAppearance.Select(face => new MovableRoadGeometry.BoundTriangle(
                        face.Geometry, face.Bone | MovableSurfaceAtlas.MovingSurface))).ToArray();
                }
                        return computed;
                    }));
                var familyTypes = familyLevel?.SupportedTypes ?? Array.Empty<MovableRoadAppearance.Type>();
                var domainOffset = checked((uint)allPatches.Count);
                assignedGroups.Add((familyAssigned,domainOffset));
                allPatches.AddRange(familyPatches);
                for (var domain = 0; domain < familyPatches.Length; domain++)
                {
                    allTypes.Add(domain < familyTypes.Length ? familyTypes[domain] : MovableRoadAppearance.Type.None);
                    if (appearanceForReceiver != null)
                        allNatives.Add(familyLevel?.Native ?? sources[0].GetSurfaceAsset(0));
                }
            }
            var assignedKey=new List<object>();foreach(var group in assignedGroups){assignedKey.Add(group.Faces);assignedKey.Add(group.Offset);}
            var assigned = await stageCache.Get("appearance-domain-offsets",assignedKey.ToArray(),
                ()=>GenerationWork.Background("road appearance domain offsets",()=>
            {
                var output=new List<MovableRoadGeometry.BoundTriangle>();
                foreach(var group in assignedGroups)foreach(var face in group.Faces)
                    output.Add(new MovableRoadGeometry.BoundTriangle(face.Geometry,
                        checked((face.Bone&~MovableSurfaceAtlas.MovingSurface)+group.Offset)|
                        (face.Bone&MovableSurfaceAtlas.MovingSurface)));
                return output.ToArray();
            }));
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
            bounds[lod].min=math.min(bounds[lod].min,selectedBounds.min);
            bounds[lod].max=math.max(bounds[lod].max,selectedBounds.max);
        }
        var composed = await MovableSurfaceAtlas.ComposeAsync(inputs, material,
            Array.Empty<MovableNativeDecals.Region>(), captureAppearance);
        var visible = composed.Triangles; var surfaces = composed.Materials;
        if (visible == null) return null;
        var result = new MovableRoadAssets.Level[sources.Count];
        for (var lod = 0; lod < result.Length; lod++)
            result[lod] = new MovableRoadAssets.Level(sources[lod], visible[lod], surfaces[lod],
                carriageway, bounds[lod], pose, lanes: lanes[lod], closedWorld: closedWorld,
                roadTrianglePartitions: physicalByLod[lod], lanePartitions: lanePartitions);
        Mod.Log.Info("Selected-road preparation stages: "+stageCache.Report());
        return result;
    }

    // Same position operation order as InFrame, without constructing unused normals/UVs/triangles.
    private static Bounds3 FramedBounds(IReadOnlyList<MovableRoadGeometry.Triangle> road,
        IReadOnlyList<MovableRoadGeometry.Triangle> lanes,Pose pose)
    {
        var offset=pose.position;var q=pose.rotation;
        var lengthSquared=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
        if(!float.IsFinite(offset.x)||!float.IsFinite(offset.y)||!float.IsFinite(offset.z)||
            !float.IsFinite(lengthSquared)||lengthSquared<=0f)
            throw new InvalidOperationException("The measured road-to-object pose is invalid.");
        q=q.normalized;
        var bounds=new Bounds3(new float3(float.PositiveInfinity),new float3(float.NegativeInfinity));
        void Include(IReadOnlyList<MovableRoadGeometry.Triangle> triangles)
        {
            foreach(var t in triangles)
            {
                var a=(float3)(q*t.A.Position+offset);var b=(float3)(q*t.B.Position+offset);var c=(float3)(q*t.C.Position+offset);
                bounds.min=math.min(bounds.min,math.min(a,math.min(b,c)));
                bounds.max=math.max(bounds.max,math.max(a,math.max(b,c)));
            }
        }
        Include(road);Include(lanes);return bounds;
    }

}
