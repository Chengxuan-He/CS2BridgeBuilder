using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Lift02BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Lift02BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Lift02";

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, System.Collections.Generic.List<PrefabBase> created) => Lift02UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "LiftBridge02";
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/LiftBridge02.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.Lift02RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Lift02SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift02RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = Lift02RoadPreparation.PrepareRenderLevels(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[0], Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[1], Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[2])), material);
        return Lift02RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta) ?? throw new InvalidOperationException("Lift02 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = Lift02ColumnAssembly.Create(factory, target.name + " Column", roots[Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.I32[0]], halfDelta) ?? throw new InvalidOperationException("Lift02 support render graph creation failed.");
        var footing = Lift02FootingAssembly.Create(factory, target.name + " Footing", roots[Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.I32[1]], halfDelta) ?? throw new InvalidOperationException("Lift02 footing render graph creation failed.");
        var mechanism = Lift02ObjectAssembly.Create(factory, target.name + " Moving Object", moving, column, footing, content, halfDelta, road);
        using var pillarSources = new Lift02PillarSources(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content);
        var pillar = Lift02NetPillarAssembly.Create(factory, target.name + " Pillar", pillarSources, content, halfDelta) ?? throw new InvalidOperationException("Lift02 ordinary pillar creation failed.");
        Lift02NetworkAssembly.Apply(target, factory, Lift02SectionRoles.Resolve(target.m_Sections), pillar, mechanism);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02BridgeGenerator_cs
internal static class Numeric_Types_Lift02_Lift02BridgeGenerator_cs
{
    static Numeric_Types_Lift02_Lift02BridgeGenerator_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift02_Lift02BridgeGenerator_cs");
}
