using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Lift05BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Lift05BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Lift05";

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, System.Collections.Generic.List<PrefabBase> created) => Lift05UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "LiftBridge05";
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/DoubleSubwayTrackLiftBridge.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.Lift05RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Lift05SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift05RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = Lift05RoadPreparation.PrepareRenderLevels(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[0], Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[1], Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[2])), material);
        return Lift05RenderAssembly.CreateMechanism(factory, owner + " Mechanism", levels, halfDelta) ?? throw new InvalidOperationException("Lift05 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = Lift05SupportAssembly.Create(factory, target.name + " Column", roots[Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.I32[0]], false, halfDelta) ?? throw new InvalidOperationException("Lift05 support render graph creation failed.");
        var footing = Lift05SupportAssembly.Create(factory, target.name + " Footing", roots[Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.I32[1]], true, halfDelta) ?? throw new InvalidOperationException("Lift05 footing render graph creation failed.");
        var mechanism = Lift05ObjectAssembly.Create(factory, target.name, target.name + " Moving Object", moving, column, footing, content, halfDelta, road, Lift05RoadPartitions.Carriageway);
        Lift05NetworkAssembly.Apply(target, factory, Lift05SectionRoles.Resolve(target.m_Sections), mechanism);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05BridgeGenerator_cs
internal static class Numeric_Types_Lift05_Lift05BridgeGenerator_cs
{
    static Numeric_Types_Lift05_Lift05BridgeGenerator_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift05_Lift05BridgeGenerator_cs");
}
