using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed class Draw03BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Draw03BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Draw03";
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,
        System.Collections.Generic.List<PrefabBase> created) => Draw03UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "DrawBridge03";
    // Recorded native DrawBridge03 network identity and shared bridge aggregate.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/DrawBridge03.svg";
    // Six 3 m drive strips and two 3.5 m sidewalks; outer 0.5 m strips excluded.
    protected override float NativeRoadWidth => BridgeMeasurements.Draw03RoadWidth;


    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Draw03SourceRenders(
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"),
            factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) =>
        Draw03RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);

    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner,
        MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(road.Road, road.RoadToObject);
        var levels = Draw03RoadPreparation.PrepareRenderLevels(road, nativeLevels, halfDelta,
            Matrix4x4.Translate(new Vector3(0f, 10f, 0f)), material, layout);
        return Draw03RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta, layout)
            ?? throw new InvalidOperationException("Draw03 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = Draw03SupportAssembly.Create(factory, target.name + " Column", roots[1], false, halfDelta)
            ?? throw new InvalidOperationException("Draw03 support render graph creation failed.");
        var footing = Draw03SupportAssembly.Create(factory, target.name + " Footing", roots[2], true, halfDelta)
            ?? throw new InvalidOperationException("Draw03 footing render graph creation failed.");
        var mechanism = Draw03ObjectAssembly.Create(factory, target.name, target.name + " Moving Object",
            moving, column, footing, content, halfDelta, road);
        var pillar = Draw03PillarAssembly.Create(factory, target.name + " Pillar",
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content, halfDelta)
            ?? throw new InvalidOperationException("Draw03 ordinary pillar creation failed.");
        Draw03ApproachAssembly.Apply(target, factory, content, halfDelta);
        Draw03NetworkAssembly.Apply(target, factory, pillar, mechanism);
    }
}
