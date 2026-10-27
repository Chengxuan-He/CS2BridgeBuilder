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
    internal static Draw01ClosedRoad Sample(string owner, Entity selectedRoad, IReadOnlyList<RenderPrefab> nativeRenderRoots, ContentPrefab content, float halfWidthDelta, RandomSeed seed, Upgraded? upgrade = null)
    {
        using var environment = new MovableSamplingEnvironment(World.DefaultGameObjectInjectionWorld);
        // The recorded native bridge's water elevation is 10 m. This private
        // arrangement uses the same elevation over flat zero-height terrain.
        // Calculation and visual extraction complete synchronously before the
        // height arrays are disposed; the returned context retains its own entities.
        var mechanism = new Draw01SamplingMechanism(owner, nativeRenderRoots, content, halfWidthDelta);
        try
        {
            var result = new Draw01ClosedRoad(owner, selectedRoad, mechanism.Prefab, Draw01NetworkData.MechanismPlacement(mechanism.Prefab), new float3(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[0], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[1], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[2]), new Elevation(new float2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[3])), environment.Terrain, environment.Theme, environment.LeftHandTraffic, seed, environment.BuildingConfiguration, upgrade);
            result._samplingMechanism = mechanism;
            return result;
        }
        catch
        {
            mechanism.Dispose();
            throw;
        }
    }

    internal Draw01ClosedRoad(string owner, Entity selectedRoad, StaticObjectPrefab mechanism, NetSubObjectInfo placement, float3 roadOrigin, Elevation elevation, TerrainHeightData terrain, Entity sourceTheme, bool leftHandTraffic, RandomSeed randomSeed, BuildingConfigurationData buildingConfiguration, Upgraded? upgrade = null)
    {
        _sourceTheme = sourceTheme;
        _roadOrigin = roadOrigin;
        // Native DrawBridge01 Bridge.m_SegmentLength is 144 m. These outer nodes
        // bound the sampling approaches, NOT the road cut or the mechanism extent.
        // The staged NetObject's 90 m offset supplies the fixed-node extent to the
        // native GeometrySystem on both incident edges.
        var positions = new[]
        {
            roadOrigin + new float3(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[4], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[5], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[6]),
            roadOrigin,
            roadOrigin + new float3(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[7], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[8], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[9])
        };
        var random = randomSeed.GetRandom(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[0]);
        var nodes = new MovableNetworkContext.NodeInput[positions.Length];
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[1]; i < nodes.Length; i++)
            nodes[i] = new MovableNetworkContext.NodeInput(selectedRoad, new Node { m_Position = positions[i], m_Rotation = quaternion.identity }, new PseudoRandomSeed(ref random), elevation);
        var edges = new MovableNetworkContext.EdgeInput[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[2]];
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[3]; i < edges.Length; i++)
            edges[i] = new MovableNetworkContext.EdgeInput(selectedRoad, i, i + Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[4], NetUtils.StraightCurve(positions[i], positions[i + Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[5]]), new PseudoRandomSeed(ref random), elevation, upgrade);
        var centralNode = new MovableNetworkContext.OwnerIndex(false, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[6]);
        var mechanisms = new[]
        {
            new MovableNetworkContext.MechanismInput(mechanism, centralNode, new Transform { m_Position = roadOrigin, m_Rotation = quaternion.identity }, placement, new TrafficLights { m_State = TrafficLightState.Ongoing, m_CurrentSignalGroup = (byte)(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[7]), m_NextSignalGroup = (byte)(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[8]), m_SignalGroupCount = (byte)(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[9]) }, new PseudoRandomSeed(ref random))
        };
        Network = new MovableNetworkContext(owner, nodes, edges, mechanisms, Array.Empty<MovableNetworkContext.MiddleInput>(), Array.Empty<MovableNetworkContext.ParentInput>(), terrain, sourceTheme, leftHandTraffic, randomSeed, buildingConfiguration);
        try
        {
            Road = Network.CompileRoad(new[] { (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[10], MovableNetworkContext.SurfacePart.EndNode), (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[11], MovableNetworkContext.SurfacePart.StartNode) }, new[] { centralNode }, roadOrigin, replacedStructureLayers: NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, trackSourceFaces: false);
            RoadToObject = Network.RoadToMechanism(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[12], roadOrigin);
        }
        catch
        {
            Network.Dispose();
            throw;
        }
    }

    // Resolve native node objects while the private lanes and signal needs still exist.
    // Their positions share Road's origin-relative frame, before mechanism bone binding.
    internal MovableNodeObjects.Result Objects() => Network.NodeObjects(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[13], _sourceTheme, _roadOrigin);
    // The local render bounds describe the closed geometry. Native StandingObject
    // and StackProperties extend the object's culling bounds separately; a swept
    // animation volume must not be substituted for authored stacking metadata.
    internal MovableRoadAssets.Level[] PrepareRenderLevels(IReadOnlyList<RenderPrefab> sources, float halfWidthDelta, Matrix4x4 closedWorld, MovableRoadMaterial roadMaterial)
    {
        if (sources.Count != Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[14] || !math.isfinite(halfWidthDelta))
            throw new ArgumentException("Draw01 road preparation requires three render sources and a finite width delta.");
        var partition = Draw01RoadPartitions.Carriageway;
        var roadPartitions = new MovableRoadGeometry.Partition[Road.Occurrences.Length];
        var appearancePartitions = new MovableRoadGeometry.Partition[Road.Occurrences.Length];
        var carriagewayAppearance = Draw01RoadPartitions.MaterialRegions(false, RoadToObject);
        var sidewalkAppearance = Draw01RoadPartitions.MaterialRegions(true, RoadToObject);
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[15]; i < Road.Occurrences.Length; i++)
        {
            // Native composition provenance identifies the section that owns this occurrence.
            // Curbs may contain pedestrian lanes; those lanes do not make a curb a sidewalk.
            var section = Road.Occurrences[i].RootSection ?? throw new InvalidOperationException("Draw01 road occurrence has no source section provenance.");
            var sidewalk = section.name == "Sidewalk" || section.name.StartsWith("Sidewalk ", StringComparison.Ordinal);
            roadPartitions[i] = Draw01RoadPartitions.RoadSurface(sidewalk);
            appearancePartitions[i] = sidewalk ? sidewalkAppearance : carriagewayAppearance;
        }

        var lanePartitions = new MovableRoadGeometry.Partition[Road.LaneVisuals.Length];
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[16]; i < lanePartitions.Length; i++)
            lanePartitions[i] = Road.LaneVisuals[i].SourceLane.Has<Game.Prefabs.PedestrianLane>() ? Draw01RoadPartitions.Sidewalk : Draw01RoadPartitions.Carriageway;
        // Native mesh 0 UV gradients: asphalt faces 1512/1513, grating 1819/1820,
        // fixed carriageway concrete 2674/2675. Use their physical metre scale and
        // axis directions, never the selected network shader's UV convention.
        var frame = Matrix4x4.TRS(RoadToObject.position, RoadToObject.rotation, Vector3.one).transpose;
        MovableExistingRoadSurface.Patch Project(Vector2 min, Vector2 max, Vector4 u, Vector4 v) => new(min, max, u, v);
        float asphaltMin = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[10] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[11];
        var asphaltWidth = (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[12] - asphaltMin) * Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[13] / (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[14] - Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[15]);
        var concreteWidth = (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[16] - Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[17]) * Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[18] / (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[19] - Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[20]);
        var positive = new[]
        {
            Project(new Vector2(asphaltMin, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[21]), new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[22], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[23]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[24] / asphaltWidth, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[17], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[18], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[25] / asphaltWidth), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[19], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[20], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[26], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[27] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[28])),
            Project(new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[29], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[30]), new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[31], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[32]), new Vector4(-Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[33] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[34], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[21], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[22], (-Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[35] - halfWidthDelta) / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[36]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[23], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[24], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[37], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[38] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[39])),
            Project(new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[40], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[41]), new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[42], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[43]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[44] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[45], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[25], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[26], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[27]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[28], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[29], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[46], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[47] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[48])),
            Project(new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[49], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[50]), new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[51], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[52]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[53] / concreteWidth, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[30], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[31], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[54] / concreteWidth), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[32], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[33], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[55], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[56] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[57])),
            // Opposite sidewalk faces 2367/2368 keep the same U direction but
            // have a different authored origin; widening moves that origin with the side.
            Project(new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[58], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[59]), new Vector2(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[60], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[61]), new Vector4(-Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[62] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[63], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[34], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[35], (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[64] + halfWidthDelta) / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[65]), new Vector4(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[36], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[37], -Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[66], Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[67] / Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[68]))
        };
        // Native right carriageway faces 2183/2184 reverse both U and V;
        // fixed-cap faces 2682/2683 reverse U only. The negative leaf mirrors Z
        // (9406/9407, 9713/9714, 10077/10078, 10261/10262, 10568/10569).
        var patches = new MovableExistingRoadSurface.Patch[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[38]];
        Array.Copy(positive, patches, positive.Length);
        var asphalt = positive[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[39]];
        var concrete = positive[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[40]];
        var rightU = asphalt.U;
        rightU.x = -rightU.x;
        var rightV = -asphalt.V;
        rightV.w += Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.F32[69];
        patches[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[41]] = Project(asphalt.Min, asphalt.Max, rightU, rightV);
        rightU = concrete.U;
        rightU.x = -rightU.x;
        patches[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[42]] = Project(concrete.Min, concrete.Max, rightU, concrete.V);
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[43]; i < Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[44]; i++)
        {
            var p = patches[i];
            var u = p.U;
            var v = p.V;
            u.z = -u.z;
            v.z = -v.z;
            patches[i + Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[45]] = Project(p.Min, p.Max, u, v);
            patches[i] = Project(p.Min, p.Max, frame * p.U, frame * p.V);
            patches[i + Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[46]] = Project(p.Min, p.Max, frame * u, frame * v);
        }

        var road = MovableRoadGeometry.InFrame(Road.Triangles, RoadToObject);
        var lanes = MovableRoadGeometry.InFrame(Road.LaneGeometry.Triangles, RoadToObject);
        var inverseRotation = Quaternion.Inverse(RoadToObject.rotation);
        var objectToRoad = new Pose(inverseRotation * -RoadToObject.position, inverseRotation);
        foreach (var region in new[]
        {
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[47],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[48],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[49],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[50]
        }

        )
        {
            var patch = patches[region];
            patches[region] = new MovableExistingRoadSurface.Patch(patch.Min, patch.Max, patch.U, patch.V, roadMaterial.Asset);
        }

        foreach (var region in new[]
        {
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[51],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[52],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[53],
            Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[54]
        }

        )
        {
            var patch = patches[region];
            patches[region] = new MovableExistingRoadSurface.Patch(patch.Min, patch.Max, patch.U, patch.V, repeat: true);
        }

        var inputs = new MovableSurfaceAtlas.Input[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[55]];
        var laneLevels = new MovableLaneGeometry.Result[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[56]];
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[57]; i < inputs.Length; i++)
        {
            // Each level starts with the selected road's corresponding native mesh.
            // Native pieces without a reduced mesh retain their authored last level.
            var levelRoad = i == Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[58] ? Road : Network.CompileRoad(new[] { (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[59], MovableNetworkContext.SurfacePart.EndNode), (Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[60], MovableNetworkContext.SurfacePart.StartNode) }, new[] { new MovableNetworkContext.OwnerIndex(false, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[61]) }, _roadOrigin, replacedStructureLayers: NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, trackSourceFaces: false, lod: i);
            laneLevels[i] = new MovableLaneGeometry.Result(levelRoad.LaneGeometry.Triangles.ToList(), levelRoad.LaneGeometry.Surfaces.ToList(), new List<MovableLaneVisuals.Batch>());
            var levelGeometry = MovableRoadGeometry.InFrame(levelRoad.Triangles, RoadToObject);
            if (!MovableRoadGeometry.TryBind(levelGeometry, partition, Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[62], levelRoad.Surfaces.Length, out var receivers, out var error, occurrencePartitions: roadPartitions))
                throw new InvalidOperationException(error);
            var converted = new bool[levelRoad.Surfaces.Length];
            for (var j = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[63]; j < converted.Length; j++)
            {
                levelRoad.Surfaces[j].LoadHeader();
                converted[j] = !MovableSurfaceAssets.IsObjectSurface(levelRoad.Surfaces[j]);
            }

            var bones = sources[0].GetComponent<ProceduralAnimationProperties>().m_Bones;
            bool Original(MovableRoadGeometry.BoundTriangle face) => bones[face.Bone].m_ConnectionID != 0;
            var original = receivers.Where(Original).ToArray();
            var prepared = MovableRoadTemplate.Prepare(receivers.Where(face => !Original(face)).ToArray(), converted);
            var selected = MovableRoadGeometry.InFrame(prepared, objectToRoad);
            var assigned = Array.Empty<MovableRoadGeometry.BoundTriangle>();
            if (selected.Length != 0 && !MovableRoadGeometry.TryBind(selected, carriagewayAppearance, patches.Length, levelRoad.Surfaces.Length, out assigned, out error, occurrencePartitions: appearancePartitions))
                throw new InvalidOperationException(error);
            var moving = MovableRoadGeometry.InFrame(original.Select(face => face.Geometry).ToArray(), objectToRoad);
            var movingAppearance = Array.Empty<MovableRoadGeometry.BoundTriangle>();
            if (moving.Length != 0 && !MovableRoadGeometry.TryBind(moving, carriagewayAppearance, patches.Length,
                levelRoad.Surfaces.Length, out movingAppearance, out error))
                throw new InvalidOperationException(error);
            else if (moving.Length != 0)
                assigned = assigned.Concat(movingAppearance.Select(face => new MovableRoadGeometry.BoundTriangle(
                    face.Geometry, face.Bone | MovableSurfaceAtlas.MovingSurface))).ToArray();
            inputs[i] = new MovableSurfaceAtlas.Input
            {
                Faces = assigned,
                PrototypeRoad = true,
                SourceFrame = closedWorld * Matrix4x4.TRS(RoadToObject.position, RoadToObject.rotation, Vector3.one),
                Materials = levelRoad.Surfaces,
                Patches = patches,
                // The measured appearance patches address the native near atlas; its
                // composed mip chain supplies far appearance without geometry tiling.
                Native = sources[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[64]].GetSurfaceAsset(Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[65])
            };
        }

        var visibleLevels = MovableSurfaceAtlas.Compose(inputs, roadMaterial, Array.Empty<MovableNativeDecals.Region>(), out var materialLevels);
        var levels = new MovableRoadAssets.Level[Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[66]];
        for (var i = Numeric_Types_Draw01_Draw01ClosedRoad_cs.Values.I32[67]; i < levels.Length; i++)
        {
            var bounds = sources[i].bounds;
            bounds.min.x -= halfWidthDelta;
            bounds.max.x += halfWidthDelta;
            Include(road, ref bounds);
            Include(lanes, ref bounds);
            levels[i] = new MovableRoadAssets.Level(sources[i], visibleLevels[i], materialLevels[i], partition, bounds, RoadToObject, lanes: laneLevels[i], closedWorld: closedWorld, roadPartitions: roadPartitions, lanePartitions: lanePartitions);
        }

        return levels;
    }

    private static void Include(IReadOnlyList<MovableRoadGeometry.Triangle> triangles, ref Bounds3 bounds)
    {
        foreach (var triangle in triangles)
        {
            bounds.min = math.min(bounds.min, math.min((float3)triangle.A.Position, math.min((float3)triangle.B.Position, (float3)triangle.C.Position)));
            bounds.max = math.max(bounds.max, math.max((float3)triangle.A.Position, math.max((float3)triangle.B.Position, (float3)triangle.C.Position)));
        }
    }

    public void Dispose()
    {
        try
        {
            Network.Dispose();
        }
        finally
        {
            _samplingMechanism?.Dispose();
        }
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01ClosedRoad_cs
internal static class Numeric_Types_Draw01_Draw01ClosedRoad_cs
{
    static Numeric_Types_Draw01_Draw01ClosedRoad_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01ClosedRoad_cs");
}
