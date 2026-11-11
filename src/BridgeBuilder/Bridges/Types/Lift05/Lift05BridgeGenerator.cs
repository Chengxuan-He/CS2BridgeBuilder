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
    protected override System.Threading.Tasks.Task<MovableStraightSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift05RoadSampling.CreateAsync(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override async System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = await Lift05RoadPreparation.PrepareRenderLevelsAsync(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[0], Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[1], Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.F32[2])), material);
        if (levels == null) return null;
        var objects = road.Objects();
        var objectPartitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(objectPartitions, Lift05RoadPartitions.Carriageway);
        levels = factory.PrepareDirectRoadMeshes(levels, objects.Placements, road.RoadToObject, objectPartitions);
        levels = factory.PrepareBoneDecals(levels, new MovablePaintDeck(road.Road.LaneGeometry.Decals, road.RoadToObject, Lift05RoadPartitions.Carriageway,
            MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject)));
        return await Lift05RenderAssembly.CreateMechanismAsync(factory, owner + " Mechanism", levels, halfDelta);
    }

    protected override async System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = await Lift05SupportAssembly.CreateAsync(factory, target.name + " Column", roots[Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.I32[0]], false, halfDelta) ?? throw new InvalidOperationException("Lift05 support render graph creation failed.");
        var footing = await Lift05SupportAssembly.CreateAsync(factory, target.name + " Footing", roots[Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values.I32[1]], true, halfDelta) ?? throw new InvalidOperationException("Lift05 footing render graph creation failed.");
        var mechanism = await Lift05ObjectAssembly.CreateAsync(factory, target.name, target.name + " Moving Object", moving, column, footing, content, halfDelta, road, Lift05RoadPartitions.Carriageway);
        if(mechanism==null)return;
        Lift05NetworkAssembly.Apply(target, factory, Lift05SectionRoles.Resolve(target.m_Sections), mechanism);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05BridgeGenerator_cs
internal static class Numeric_Types_Lift05_Lift05BridgeGenerator_cs
{
    static Numeric_Types_Lift05_Lift05BridgeGenerator_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift05_Lift05BridgeGenerator_cs.Values", () => HardcodedNumbers.Load("Types_Lift05_Lift05BridgeGenerator_cs"));
}
