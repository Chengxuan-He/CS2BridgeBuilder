using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.Mathematics;
using Game.Common;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Transform = Game.Objects.Transform;

namespace BridgeBuilder.Bridges;

// Draw01's straight closed-pose sampling arrangement. The approaches supply native
// connectivity; only the two central-node halves become the mechanism's road input.
// This is not a city-construction request and does not persist the sampling entities.
internal sealed class Draw01ClosedRoad : IDisposable
{
    internal readonly MovableNetworkContext Network;
    internal readonly MovableClosedRoad.Result Road;
    internal readonly Pose RoadToObject;
    private readonly Entity _sourceTheme;
    private readonly float3 _roadOrigin;
    private Draw01SamplingMechanism? _samplingMechanism;

    internal static Draw01ClosedRoad Sample(string owner, Entity selectedRoad,
        IReadOnlyList<RenderPrefab> nativeRenderRoots, ContentPrefab content,
        float halfWidthDelta, RandomSeed seed,
        Upgraded? upgrade = null)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        // The recorded native bridge's water elevation is 10 m. This private
        // arrangement uses the same elevation over flat zero-height terrain.
        // Calculation and visual extraction complete synchronously before the
        // height arrays are disposed; the returned context retains its own entities.
        var mechanism = new Draw01SamplingMechanism(owner, nativeRenderRoots, content, halfWidthDelta);
        try
        {
            var result = new Draw01ClosedRoad(owner, selectedRoad, mechanism.Prefab,
                Draw01NetworkData.MechanismPlacement(mechanism.Prefab),
                new float3(0f, 10f, 0f), new Elevation(new float2(10f)), environment.Terrain,
                environment.Theme, environment.LeftHandTraffic, seed,
                environment.BuildingConfiguration, upgrade);
            result._samplingMechanism = mechanism;
            return result;
        }
        catch
        {
            mechanism.Dispose();
            throw;
        }
    }

    internal Draw01ClosedRoad(string owner, Entity selectedRoad, StaticObjectPrefab mechanism,
        NetSubObjectInfo placement, float3 roadOrigin, Elevation elevation,
        TerrainHeightData terrain, Entity sourceTheme, bool leftHandTraffic,
        RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration,
        Upgraded? upgrade = null)
    {
        _sourceTheme = sourceTheme;
        _roadOrigin = roadOrigin;
        // Native DrawBridge01 Bridge.m_SegmentLength is 144 m. These outer nodes
        // bound the sampling approaches, NOT the road cut or the mechanism extent.
        // The staged NetObject's 90 m offset supplies the fixed-node extent to the
        // native GeometrySystem on both incident edges.
        var positions = new[]
        {
            roadOrigin + new float3(0f, 0f, -144f), roadOrigin,
            roadOrigin + new float3(0f, 0f, 144f)
        };
        var random = randomSeed.GetRandom(0);
        var nodes = new MovableNetworkContext.NodeInput[positions.Length];
        for (var i = 0; i < nodes.Length; i++)
            nodes[i] = new MovableNetworkContext.NodeInput(selectedRoad,
                new Node { m_Position = positions[i], m_Rotation = quaternion.identity },
                new PseudoRandomSeed(ref random), elevation);
        var edges = new MovableNetworkContext.EdgeInput[2];
        for (var i = 0; i < edges.Length; i++)
            edges[i] = new MovableNetworkContext.EdgeInput(selectedRoad, i, i + 1,
                NetUtils.StraightCurve(positions[i], positions[i + 1]),
                new PseudoRandomSeed(ref random), elevation, upgrade);
        var centralNode = new MovableNetworkContext.OwnerIndex(false, 1);
        var mechanisms = new[]
        {
            new MovableNetworkContext.MechanismInput(mechanism, centralNode,
                new Transform { m_Position = roadOrigin, m_Rotation = quaternion.identity },
                placement, new TrafficLights
                {
                    m_State = TrafficLightState.Ongoing, m_CurrentSignalGroup = 1,
                    m_NextSignalGroup = 1, m_SignalGroupCount = 2
                }, new PseudoRandomSeed(ref random))
        };
        Network = new MovableNetworkContext(owner, nodes, edges, mechanisms,
            Array.Empty<MovableNetworkContext.MiddleInput>(),
            Array.Empty<MovableNetworkContext.ParentInput>(), terrain, sourceTheme,
            leftHandTraffic, randomSeed, buildingConfiguration);
        try
        {
            Road = Network.CompileRoad(new[]
            {
                (0, MovableNetworkContext.SurfacePart.EndNode),
                (1, MovableNetworkContext.SurfacePart.StartNode)
            }, new[] { centralNode }, roadOrigin,
                replacedStructureLayers: NetPieceLayerMask.Bottom | NetPieceLayerMask.Side,
                trackSourceFaces: false);
            RoadToObject = Network.RoadToMechanism(0, roadOrigin);
        }
        catch
        {
            Network.Dispose();
            throw;
        }
    }

    // Resolve native node objects while the private lanes and signal needs still exist.
    // Their positions share Road's origin-relative frame, before mechanism bone binding.
    internal MovableNodeObjects.Result Objects()
        => Network.NodeObjects(1, _sourceTheme, _roadOrigin);

    // The local render bounds describe the closed geometry. Native StandingObject
    // and StackProperties extend the object's culling bounds separately; a swept
    // animation volume must not be substituted for authored stacking metadata.
    internal MovableRoadAssets.Level[] PrepareRenderLevels(IReadOnlyList<RenderPrefab> sources,
        float halfWidthDelta, Matrix4x4 closedWorld)
    {
        if (sources.Count != 3 || !math.isfinite(halfWidthDelta))
            throw new ArgumentException("Draw01 road preparation requires three render sources and a finite width delta.");
        var partition = Draw01RoadPartitions.Carriageway;
        var roadPartitions = new MovableRoadGeometry.Partition[Road.Occurrences.Length];
        var appearancePartitions = new MovableRoadGeometry.Partition[Road.Occurrences.Length];
        var carriagewayAppearance = Draw01RoadPartitions.MaterialRegions(false, RoadToObject);
        var sidewalkAppearance = Draw01RoadPartitions.MaterialRegions(true, RoadToObject);
        for (var i = 0; i < Road.Occurrences.Length; i++)
        {
            // Native composition provenance identifies the section that owns this occurrence.
            // Curbs may contain pedestrian lanes; those lanes do not make a curb a sidewalk.
            var section = Road.Occurrences[i].RootSection
                ?? throw new InvalidOperationException("Draw01 road occurrence has no source section provenance.");
            var sidewalk = section.name == "Sidewalk" ||
                section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            roadPartitions[i] = Draw01RoadPartitions.RoadSurface(sidewalk);
            appearancePartitions[i] = sidewalk ? sidewalkAppearance : carriagewayAppearance;
        }
        var lanePartitions = new MovableRoadGeometry.Partition[Road.LaneVisuals.Length];
        for (var i = 0; i < lanePartitions.Length; i++)
            lanePartitions[i] = Road.LaneVisuals[i].SourceLane.Has<Game.Prefabs.PedestrianLane>()
                ? Draw01RoadPartitions.Sidewalk : Draw01RoadPartitions.Carriageway;
        // Native mesh 0 UV gradients: asphalt faces 1512/1513, grating 1819/1820,
        // fixed carriageway concrete 2674/2675. Use their physical metre scale and
        // axis directions, never the selected network shader's UV convention.
        var frame = Matrix4x4.TRS(RoadToObject.position, RoadToObject.rotation, Vector3.one).transpose;
        MovableExistingRoadSurface.Patch Project(Vector2 min, Vector2 max, Vector4 u, Vector4 v)
            => new(min, max, u, v);
        // Original UV min lands at texel X=50.94, inside the pale gutter (X<78).
        // Keep a measured interior margin; retain the native UV-per-metre density.
        const float asphaltMin = 96f / 4096f;
        var asphaltWidth = (0.18f - asphaltMin) * 3.3660278f /
            (0.2003173828125f - 0.0124359130859375f);
        var concreteWidth = (0.93f - 0.72705078125f) * 3.4000244f /
            (0.94873046875f - 0.72705078125f);
        var positive = new[]
            {
                Project(new Vector2(asphaltMin,0.67822265625f), new Vector2(0.18f,0.85693359375f),
                    new Vector4(1f/asphaltWidth,0,0,3.3660278f/asphaltWidth), new Vector4(0,0,.25f,-73.96094f/4f)),
                Project(new Vector2(0.240478515625f,0.6611328125f),new Vector2(0.409423828125f,0.88427734375f),
                    new Vector4(-1f/3.0269777f,0,0,(-3.3234863f-halfWidthDelta)/3.0269777f),
                    new Vector4(0,0,-.25f,53.95996f/4f)),
                Project(new Vector2(0.486083984375f,0.01238250732421875f),new Vector2(0.67529296875f,0.2734375f),
                    new Vector4(1f/2.9000244f,0,0,0),new Vector4(0,0,-.25f,86f/4f)),
                Project(new Vector2(0.72705078125f,0.01238250732421875f),new Vector2(0.93f,0.2734375f),
                    new Vector4(1f/concreteWidth,0,0,3.4000244f/concreteWidth),new Vector4(0,0,-.25f,90f/4f)),
                // Opposite sidewalk faces 2367/2368 keep the same U direction but
                // have a different authored origin; widening moves that origin with the side.
                Project(new Vector2(0.240478515625f,0.6611328125f),new Vector2(0.409423828125f,0.88427734375f),
                    new Vector4(-1f/3.0269777f,0,0,(6.350464f+halfWidthDelta)/3.0269777f),
                    new Vector4(0,0,-.25f,53.95996f/4f))
            };
        // Native right carriageway faces 2183/2184 reverse both U and V;
        // fixed-cap faces 2682/2683 reverse U only. The negative leaf mirrors Z
        // (9406/9407, 9713/9714, 10077/10078, 10261/10262, 10568/10569).
        var patches = new MovableExistingRoadSurface.Patch[14];
        Array.Copy(positive, patches, positive.Length);
        var asphalt = positive[0]; var concrete = positive[3];
        var rightU = asphalt.U; rightU.x = -rightU.x;
        var rightV = -asphalt.V; rightV.w += 1f;
        patches[5] = Project(asphalt.Min, asphalt.Max, rightU, rightV);
        rightU = concrete.U; rightU.x = -rightU.x;
        patches[6] = Project(concrete.Min, concrete.Max, rightU, concrete.V);
        for (var i = 0; i < 7; i++)
        {
            var p = patches[i]; var u = p.U; var v = p.V;
            u.z = -u.z; v.z = -v.z;
            patches[i + 7] = Project(p.Min, p.Max, u, v);
            patches[i] = Project(p.Min, p.Max, frame * p.U, frame * p.V);
            patches[i + 7] = Project(p.Min,p.Max,frame * u,frame * v);
        }
        var road = MovableRoadGeometry.InFrame(Road.Triangles, RoadToObject);
        var lanes = MovableRoadGeometry.InFrame(Road.LaneGeometry.Triangles, RoadToObject);
        if (!MovableRoadGeometry.TryBind(road, partition, 3, Road.Surfaces.Length,
                out var receivers, out var error, occurrencePartitions: roadPartitions))
            throw new InvalidOperationException(error);
        var converted = new bool[Road.Surfaces.Length];
        for (var i = 0; i < converted.Length; i++)
        {
            Road.Surfaces[i].LoadHeader();
            converted[i] = Road.Surfaces[i].materialTemplateHash is not (0 or 1 or 13 or 14 or 35 or 36);
        }
        var inverseRotation = Quaternion.Inverse(RoadToObject.rotation);
        var objectToRoad = new Pose(inverseRotation * -RoadToObject.position, inverseRotation);
        // The recorded seam frames above already define the complete fixed/leaf domains.
        // Prepare once; no prototype triangle intersection is required at any LOD.
        var prepared = MovableRoadTemplate.Prepare(receivers, converted);
        if (!MovableRoadGeometry.TryBind(prepared, partition, 3, Road.Surfaces.Length,
                out var paintReceivers, out error, occurrencePartitions: roadPartitions))
            throw new InvalidOperationException(error);
        var selectedMaterials = (Colossal.IO.AssetDatabase.SurfaceAsset[])Road.Surfaces.Clone();
        // Native mechanism paint is part of its surface appearance, not an independent
        // decal object. Inlay selected paint before road atlas repetition can fragment it.
        var painted = MovableRoadPaint.Apply(paintReceivers, Road.LaneGeometry.Decals,
            RoadToObject, 32f, ref selectedMaterials);
        var selected = MovableRoadGeometry.InFrame(painted.Select(t => t.Geometry).ToArray(), objectToRoad);
        // Decals have been consumed exactly once above. Keep every non-decal lane mesh
        // and surface for the later native lane assembly, without repeating paint per LOD.
        var remainingLanes = new MovableLaneGeometry.Result(Road.LaneGeometry.Triangles.ToList(),
            Road.LaneGeometry.Surfaces.ToList(), new List<MovableLaneVisuals.Batch>());
        var levels = new MovableRoadAssets.Level[3];
        for (var i = 0; i < levels.Length; i++)
        {
            var levelPatches = (MovableExistingRoadSurface.Patch[])patches.Clone();
            if (i == 2) Draw01RoadLodProjection.Far(levelPatches, frame, sources[i].GetSurfaceAsset(0));
            // Reconstruct from the selected closed geometry for each native LOD;
            // never pass the already tiled near surface into a reduced level.
            var visible = MovableExistingRoadSurface.Apply(selected, selectedMaterials,
                carriagewayAppearance, sources[Math.Min(i, 1)].GetSurfaceAsset(0),
                levelPatches, out var materials, occurrenceRegions: appearancePartitions);
            var bounds = sources[i].bounds;
            bounds.min.x -= halfWidthDelta;
            bounds.max.x += halfWidthDelta;
            Include(road, ref bounds);
            Include(lanes, ref bounds);
            levels[i] = new MovableRoadAssets.Level(sources[i], visible, materials,
                partition, bounds, RoadToObject, lanes: remainingLanes, closedWorld: closedWorld,
                roadPartitions: roadPartitions, lanePartitions: lanePartitions);
        }
        return levels;
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

    public void Dispose()
    {
        try { Network.Dispose(); }
        finally { _samplingMechanism?.Dispose(); }
    }
}
