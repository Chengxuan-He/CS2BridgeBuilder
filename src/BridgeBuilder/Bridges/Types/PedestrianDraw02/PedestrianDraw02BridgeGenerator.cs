using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed class PedestrianDraw02BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal PedestrianDraw02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "PedestrianDraw02";
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,
        System.Collections.Generic.List<PrefabBase> created) => PedestrianDraw02UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "PedestrianDrawBridge02";
    // Native 1.6.2f1 root 118834, aggregate 120422 and UIObject 70066.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/PedestrianDrawBridge02.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.PedestrianDraw02RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new PedestrianDraw02SourceRenders(
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"),
            factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) =>
        PedestrianDraw02RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);

    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner,
        MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = PedestrianDraw02RoadPreparation.Prepare(road, nativeLevels, halfDelta,
            Matrix4x4.Translate(new Vector3(0f, 10f, 0f)), material);
        return PedestrianDraw02RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw02 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = PedestrianDraw02RenderAssembly.CreateColumn(factory, target.name,
            target.name + " Column", roots[1], halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw02 support render graph creation failed.");
        var footing = PedestrianDraw02RenderAssembly.CreateFooting(factory, target.name,
            target.name + " Footing", roots[2], halfDelta)
            ?? throw new InvalidOperationException("PedestrianDraw02 footing render graph creation failed.");
        var mechanism = PedestrianDraw02ObjectAssembly.Create(factory, target.name,
            target.name + " Moving Object", moving, column, footing, content,
            factory.ResolveOriginal<ObjectPrefab>, halfDelta, road);
        // The native pathway uses ordinary approach sections, not a custom approach mesh.
        PedestrianDraw02NetworkAssembly.Apply(target, factory, mechanism);
    }
}
