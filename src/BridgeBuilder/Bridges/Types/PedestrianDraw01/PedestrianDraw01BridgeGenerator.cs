using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed class PedestrianDraw01BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal PedestrianDraw01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "PedestrianDraw01";
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,
        System.Collections.Generic.List<PrefabBase> created) => PedestrianDraw01UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "PedestrianDrawBridge01";
    // Native 1.6.2f1 pathway root 118833; independent reconstructed mechanism inputs.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/PedestrianDrawBridge01.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.PedestrianDraw01RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new PedestrianDraw01SourceRenders(
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"),
            factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) =>
        PedestrianDraw01RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);

    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner,
        MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = PedestrianDraw01RoadPreparation.Prepare(road, nativeLevels, halfDelta,
            Matrix4x4.Translate(new Vector3(0f, 10f, 0f)), material);
        return PedestrianDraw01RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw01 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = PedestrianDraw01SupportAssembly.Create(factory,
            target.name + " Column", roots[1], false, halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw01 support render graph creation failed.");
        var footing = PedestrianDraw01SupportAssembly.Create(factory,
            target.name + " Footing", roots[2], true, halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw01 footing render graph creation failed.");
        var mechanism = PedestrianDraw01ObjectAssembly.Create(factory, target.name,
            target.name + " Moving Object", moving, column, footing, content,
            factory.ResolveOriginal<ObjectPrefab>, halfDelta, road);
        // The native pathway uses ordinary approach sections, not a custom approach mesh.
        PedestrianDraw01NetworkAssembly.Apply(target, factory, mechanism);
    }
}
