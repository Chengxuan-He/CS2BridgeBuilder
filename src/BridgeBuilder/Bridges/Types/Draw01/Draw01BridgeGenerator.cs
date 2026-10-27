using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Draw01BridgeGenerator : MovableBridgeGeneratorBase<Draw01ClosedRoad>
{
    internal Draw01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Draw01";
    protected override string NativeVariantName => "DrawBridge01";
    // Native 1.6.2f1 root 118584, aggregate 120422 and UIObject 70051.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/DrawBridge01.svg";
    // Two 3 m drive pieces and two 3.5 m sidewalks; outer 0.5 m sections excluded.
    protected override float NativeRoadWidth => Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.F32[0];

    protected override RenderSources ResolveRenderRoots(TowerFactory factory) => new(new[] { factory.ResolveOriginal<RenderPrefab>("DrawBridge01 Mesh"), factory.ResolveOriginal<RenderPrefab>("DrawBridge01Pillar Mesh"), factory.ResolveOriginal<RenderPrefab>("DrawBridge01PillarBase Mesh") });
    protected override Draw01ClosedRoad SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Draw01ClosedRoad.Sample(owner, selectedRoad, roots, content, halfDelta, seed);
    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner, Draw01ClosedRoad road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        // The recorded closed sampling pose has identity rotation and 10 m elevation.
        var levels = road.PrepareRenderLevels(nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.F32[1], Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.F32[2], Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.F32[3])), material);
        return Draw01RenderAssembly.CreateMechanism(factory, owner, owner + " Mechanism", levels, halfDelta, new Draw01UndersideLayout(road.Road, road.RoadToObject)) ?? throw new InvalidOperationException("Draw01 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, Draw01ClosedRoad road, RenderPrefab moving)
    {
        var support = Draw01RenderAssembly.CreateColumn(factory, target.name, target.name + " Column", roots[Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.I32[0]], halfDelta) ?? throw new InvalidOperationException("Draw01 support render graph creation failed.");
        var foundation = Draw01RenderAssembly.CreateFooting(factory, target.name, target.name + " Footing", roots[Numeric_Types_Draw01_Draw01BridgeGenerator_cs.Values.I32[1]], halfDelta) ?? throw new InvalidOperationException("Draw01 footing render graph creation failed.");
        var mechanism = Draw01ObjectAssembly.Create(factory, target.name, target.name + " Moving Object", moving, support, foundation, content, factory.ResolveOriginal<ObjectPrefab>, halfDelta, road);
        var pillar = Draw01NetPillarAssembly.Create(factory, target.name, target.name + " Pillar", factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillarTop Mesh"), factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillar Mesh"), factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillarBase Mesh"), content, halfDelta);
        Draw01FixedBase.Apply(target, factory, halfDelta);
        Draw01NetworkAssembly.Apply(target, factory, pillar, mechanism);
    }
}

// Embedded numerical arrays: Types_Draw01_Draw01BridgeGenerator_cs
internal static class Numeric_Types_Draw01_Draw01BridgeGenerator_cs
{
    static Numeric_Types_Draw01_Draw01BridgeGenerator_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Draw01_Draw01BridgeGenerator_cs");
}
