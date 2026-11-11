using System;
using System.Linq;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Draw03BridgeGenerator : MovableBridgeGeneratorBase<MovableStraightSample>
{
    internal Draw03BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Draw03";

    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, System.Collections.Generic.List<PrefabBase> created) => Draw03UnlockData.Apply(target, _report, created);
    protected override string NativeVariantName => "DrawBridge03";
    // Recorded native DrawBridge03 network identity and shared bridge aggregate.
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/DrawBridge03.svg";
    // Six 3 m drive strips and two 3.5 m sidewalks; outer 0.5 m strips excluded.
    protected override float NativeRoadWidth => BridgeMeasurements.Draw03RoadWidth;

    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Draw03SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override MovableStraightSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Draw03RoadSampling.Create(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override System.Threading.Tasks.Task<MovableStraightSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed) => Draw03RoadSampling.CreateAsync(owner, selectedRoad, roots, content, halfDelta, seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side);
    protected override async System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, MovableStraightSample road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        var layout = new MovableSelectedRoadLayout(road.Road, road.RoadToObject, "Draw03");
        var levels = await Draw03RoadPreparation.PrepareRenderLevelsAsync(road, nativeLevels, halfDelta, Matrix4x4.Translate(new Vector3(Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values.F32[0], Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values.F32[1], Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values.F32[2])), material, layout);
        if (levels == null) return null;
        var objects = MovableRoadLights.Place(road.Objects(), road.RoadToObject, Draw03MechanismData.RoadAttachments(_ => null!).Select(item => item.m_Position.z).Distinct().ToArray());
        var objectPartitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(objectPartitions, Draw03RoadPartitions.Carriageway);
        levels = factory.PrepareDirectRoadMeshes(levels, objects.Placements, road.RoadToObject, objectPartitions);
        levels = factory.PrepareBoneDecals(levels, new MovablePaintDeck(road.Road.LaneGeometry.Decals, road.RoadToObject, Draw03RoadPartitions.Carriageway,
            MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject)));
        return await Draw03RenderAssembly.CreateMechanismAsync(factory, owner + " Mechanism", levels, halfDelta, layout);
    }

    protected override async System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, MovableStraightSample road, RenderPrefab moving)
    {
        var column = await Draw03SupportAssembly.CreateAsync(factory, target.name + " Column", roots[Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values.I32[0]], false, halfDelta) ?? throw new InvalidOperationException("Draw03 support render graph creation failed.");
        var footing = await Draw03SupportAssembly.CreateAsync(factory, target.name + " Footing", roots[Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values.I32[1]], true, halfDelta) ?? throw new InvalidOperationException("Draw03 footing render graph creation failed.");
        var mechanism = await Draw03ObjectAssembly.CreateAsync(factory, target.name, target.name + " Moving Object", moving, column, footing, content, halfDelta, road);
        if(mechanism==null)return;
        var pillar = await Draw03PillarAssembly.CreateAsync(factory, target.name + " Pillar", factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content, halfDelta) ?? throw new InvalidOperationException("Draw03 ordinary pillar creation failed.");
        await Draw03ApproachAssembly.ApplyAsync(target, factory, content, halfDelta);
        Draw03NetworkAssembly.Apply(target, factory, pillar, mechanism);
    }
}

// Embedded numerical arrays: Types_Draw03_Draw03BridgeGenerator_cs
internal static class Numeric_Types_Draw03_Draw03BridgeGenerator_cs
{
    static Numeric_Types_Draw03_Draw03BridgeGenerator_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Draw03_Draw03BridgeGenerator_cs.Values", () => HardcodedNumbers.Load("Types_Draw03_Draw03BridgeGenerator_cs"));
}
