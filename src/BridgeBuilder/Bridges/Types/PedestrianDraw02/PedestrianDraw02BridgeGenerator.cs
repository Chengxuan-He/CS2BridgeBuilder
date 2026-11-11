using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class PedestrianDraw02BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal PedestrianDraw02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "PedestrianDraw02";

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, System.Collections.Generic.List<PrefabBase> created) => PedestrianDraw02UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "PedestrianDrawBridge02";
    // Native 1.6.2f1 root 118834, aggregate 120422 and UIObject 70066.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/PedestrianDrawBridge02.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.PedestrianDraw02RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new PedestrianDraw02SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => PedestrianDraw02RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override System.Threading.Tasks.Task<MovableStraightSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => PedestrianDraw02RoadSampling.CreateAsync(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override async System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = await PedestrianDraw02RoadPreparation.PrepareAsync(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values.F32[0], Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values.F32[1], Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values.F32[2])), material);
        if (levels == null) return null;
        var objects = road.Objects();
        var objectPartitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(objectPartitions, PedestrianDraw02RoadPartitions.Create(halfDelta));
        levels = factory.PrepareDirectRoadMeshes(levels, objects.Placements, road.RoadToObject, objectPartitions);
        levels = factory.PrepareBoneDecals(levels, new MovablePaintDeck(road.Road.LaneGeometry.Decals, road.RoadToObject, PedestrianDraw02RoadPartitions.Create(halfDelta),
            MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject)));
        return await PedestrianDraw02RenderAssembly.CreateMechanismAsync(factory, owner + " Mechanism", levels, halfDelta);
    }

    protected override async System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = await PedestrianDraw02RenderAssembly.CreateColumnAsync(factory, target.name, target.name + " Column", roots[Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values.I32[0]], halfDelta) ?? throw new InvalidOperationException("PedestrianDraw02 support render graph creation failed.");
        var footing = await PedestrianDraw02RenderAssembly.CreateFootingAsync(factory, target.name, target.name + " Footing", roots[Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values.I32[1]], halfDelta) ?? throw new InvalidOperationException("PedestrianDraw02 footing render graph creation failed.");
        var mechanism = await PedestrianDraw02ObjectAssembly.CreateAsync(factory, target.name, target.name + " Moving Object", moving, column, footing, content, factory.ResolveOriginal<ObjectPrefab>, halfDelta, road);
        if(mechanism==null)return;
        // The native pathway uses ordinary approach sections, not a custom approach mesh.
        PedestrianDraw02NetworkAssembly.Apply(target, factory, mechanism);
    }
}

// Embedded numerical arrays: Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs
internal static class Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs
{
    static Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs.Values", () => HardcodedNumbers.Load("Types_PedestrianDraw02_PedestrianDraw02BridgeGenerator_cs"));
}
