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
    protected override System.Threading.Tasks.Task<MovableStraightSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Lift02RoadSampling.CreateAsync(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override async System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var levels = await Lift02RoadPreparation.PrepareRenderLevelsAsync(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[0], Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[1], Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.F32[2])), material);
        if (levels == null) return null;
        var objects = road.Objects();
        var objectPartitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(objectPartitions, Lift02RoadPartitions.Carriageway);
        levels = factory.PrepareDirectRoadMeshes(levels, objects.Placements, road.RoadToObject, objectPartitions);
        levels = factory.PrepareBoneDecals(levels, new MovablePaintDeck(road.Road.LaneGeometry.Decals, road.RoadToObject, Lift02RoadPartitions.Carriageway,
            MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject)));
        return await Lift02RenderAssembly.CreateMechanismAsync(factory, owner + " Mechanism", levels, halfDelta);
    }

    protected override async System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = await Lift02ColumnAssembly.CreateAsync(factory, target.name + " Column", roots[Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.I32[0]], halfDelta) ?? throw new InvalidOperationException("Lift02 support render graph creation failed.");
        var footing = await Lift02FootingAssembly.CreateAsync(factory, target.name + " Footing", roots[Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values.I32[1]], halfDelta) ?? throw new InvalidOperationException("Lift02 footing render graph creation failed.");
        var mechanism = await Lift02ObjectAssembly.CreateAsync(factory, target.name + " Moving Object", moving, column, footing, content, halfDelta, road);
        if(mechanism==null)return;
        using var pillarSources = new Lift02PillarSources(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content);
        var pillar = await Lift02NetPillarAssembly.CreateAsync(factory, target.name + " Pillar", pillarSources, content, halfDelta) ?? throw new InvalidOperationException("Lift02 ordinary pillar creation failed.");
        Lift02NetworkAssembly.Apply(target, factory, Lift02SectionRoles.Resolve(target.m_Sections), pillar, mechanism);
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02BridgeGenerator_cs
internal static class Numeric_Types_Lift02_Lift02BridgeGenerator_cs
{
    static Numeric_Types_Lift02_Lift02BridgeGenerator_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02BridgeGenerator_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02BridgeGenerator_cs"));
}
