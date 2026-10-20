using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed class Lift01BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Lift01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Lift01";
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant,
        System.Collections.Generic.List<PrefabBase> created) => Lift01UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "LiftBridge01";
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/LiftBridge01.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.Lift01RoadWidth;


    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Lift01SourceRenders(
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"),
            factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) =>
        Lift01RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed,
            NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);

    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner,
        MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = Lift01RoadPreparation.PrepareRenderLevels(road, nativeLevels, halfDelta,
            Matrix4x4.Translate(new Vector3(0f, 10f, 0f)), material);
        return Lift01RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta)
            ?? throw new InvalidOperationException("Lift01 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = Lift01SupportAssembly.CreateColumn(factory, target.name + " Column", roots[1])
            ?? throw new InvalidOperationException("Lift01 support render graph creation failed.");
        var footing = Lift01SupportAssembly.CreateFooting(factory, target.name + " Footing", roots[2], halfDelta)
            ?? throw new InvalidOperationException("Lift01 footing render graph creation failed.");
        var mechanism = Lift01ObjectAssembly.Create(factory, target.name, target.name + " Moving Object",
            moving, column, footing, content, halfDelta, road, Lift01RoadPartitions.Carriageway);
        var pillar = Lift01PillarAssembly.Create(factory, target.name + " Pillar",
            factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content, halfDelta)
            ?? throw new InvalidOperationException("Lift01 ordinary pillar creation failed.");
        Lift01ApproachAssembly.Apply(target, factory, content, halfDelta);
        Lift01NetworkAssembly.Apply(target, factory, Lift01SectionRoles.Resolve(target.m_Sections), pillar, mechanism);
    }
}
