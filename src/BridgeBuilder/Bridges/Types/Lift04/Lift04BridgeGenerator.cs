using System;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Lift04BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Lift04BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Lift04";

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, System.Collections.Generic.List<PrefabBase> created) => Lift04UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "LiftBridge04";
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/DoubleTramTrackLiftBridge.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.Lift04RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Lift04SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift04RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override System.Threading.Tasks.Task<MovableStraightSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift04RoadSampling.CreateAsync(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override async System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = await Lift04RoadPreparation.PrepareRenderLevelsAsync(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.F32[0], Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.F32[1], Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.F32[2])), material);
        if (levels == null) return null;
        return await Lift04RenderAssembly.CreateMechanismAsync(factory, owner + " Mechanism", levels, halfDelta);
    }

    protected override async System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = await Lift04SupportAssembly.CreateAsync(factory, target.name + " Column", roots[Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.I32[0]], false, halfDelta) ?? throw new InvalidOperationException("Lift04 support render graph creation failed.");
        var footing = await Lift04SupportAssembly.CreateAsync(factory, target.name + " Footing", roots[Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values.I32[1]], true, halfDelta) ?? throw new InvalidOperationException("Lift04 footing render graph creation failed.");
        var mechanism = await Lift04ObjectAssembly.CreateAsync(factory, target.name, target.name + " Moving Object", moving, column, footing, content, halfDelta, road);
        if(mechanism==null)return;
        Lift04NetworkAssembly.Apply(target, factory, Lift04SectionRoles.Resolve(target.m_Sections), mechanism);
    }
}

// Embedded numerical arrays: Types_Lift04_Lift04BridgeGenerator_cs
internal static class Numeric_Types_Lift04_Lift04BridgeGenerator_cs
{
    static Numeric_Types_Lift04_Lift04BridgeGenerator_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift04_Lift04BridgeGenerator_cs.Values", () => HardcodedNumbers.Load("Types_Lift04_Lift04BridgeGenerator_cs"));
}
