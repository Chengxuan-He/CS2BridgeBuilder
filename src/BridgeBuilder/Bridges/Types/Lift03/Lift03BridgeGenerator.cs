using System;
using System.Collections.Generic;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;
internal sealed class Lift03BridgeGenerator : MovableBridgeGeneratorBase<Lift03RoadSampling>
{
    internal Lift03BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers)
    {
    }

    internal override string StyleId => "Lift03";
    internal override bool BuildsAuxiliaryDeck => true;
    protected override string NativeVariantName => "LiftBridge03";
    protected override string NativeAggregateName => "Bridge";
    protected override string NativeIcon => "Media/Game/Icons/LiftBridge03.svg";
    protected override float NativeRoadWidth => BridgeMeasurements.Lift03RoadWidth;
    private AuxiliaryDeckInput Lower => AuxiliaryDeck ?? throw new InvalidOperationException("Lift03 requires its coordinated auxiliary network.");

    private float LowerDelta
    {
        get
        {
            var width = WidthOf(Lower.Source, Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.F32[0]);
            if (!(width > Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.F32[1]) || !float.IsFinite(width))
                throw new InvalidOperationException("Lift03 auxiliary road has no initialized surface width.");
            return (width - BridgeMeasurements.Lift03AuxiliaryRoadWidth) * Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.F32[2];
        }
    }

    // The shared frame must clear both independently sized decks.
    private float StructureDelta(float upperDelta) => Math.Max(upperDelta, LowerDelta);
    internal override bool ApplyUnlocks(NetGeometryPrefab target, BridgeStyleVariant variant, List<PrefabBase> created) => Lift03UnlockData.Apply(target, _report, created) && Lift03TrackUnlockData.Apply(Lower.Target, _report, created);
    protected override RenderSources ResolveRenderRoots(TowerFactory factory)
    {
        var sources = new Lift03SourceRenders(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), factory.ResolveOriginal<ContentPrefab>("BridgesAndPorts"));
        return new RenderSources(sources.Roots, sources);
    }

    protected override Lift03RoadSampling SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots, ContentPrefab content, float halfDelta, RandomSeed seed)
    {
        var prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        if (!prefabs.TryGetEntity(Lower.Source, out var auxiliary))
            throw new InvalidOperationException("Lift03 auxiliary road is not registered with the native prefab system.");
        return Lift03RoadSampling.Create(owner, selectedRoad, auxiliary, roots, content, StructureDelta(halfDelta), seed, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, NetPieceLayerMask.Bottom | NetPieceLayerMask.Side, auxiliaryOpposite: Lower.Opposite);
    }

    protected override RenderPrefab BuildMechanism(TowerFactory factory, string owner, Lift03RoadSampling road, RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material)
    {
        // Recover the shared mechanism world frame from the sampled main road frame.
        var pose = road.MainToObject;
        var closedWorld = Matrix4x4.Translate(new Vector3(Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.F32[3], Lift03NetworkData.MainElevation, Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.F32[4])) * Matrix4x4.TRS(pose.position, pose.rotation, Vector3.one).inverse;
        MovableRoadAssets.Level[] Prepare(Lift03RoadSampling.Deck deck, float delta, Pose frame)
        {
            var appearance = Lift03RoadAppearance.Regions(frame);
            return road.PrepareDeck(deck, nativeLevels, delta, closedWorld, material, Lift03RoadPartitions.Carriageway, appearance, appearance, Lift03RoadAppearance.Patches(deck, frame));
        }

        var upper = Prepare(Lift03RoadSampling.Deck.Main, halfDelta, road.MainToObject);
        var lower = Prepare(Lift03RoadSampling.Deck.Auxiliary, LowerDelta, road.AuxiliaryToObject);
        return Lift03RenderAssembly.CreateMechanism(factory, owner + " Mechanism", upper, lower, StructureDelta(halfDelta), halfDelta, LowerDelta) ?? throw new InvalidOperationException("Lift03 moving render graph creation failed.");
    }

    protected override void AssembleBridge(NetGeometryPrefab target, TowerFactory factory, RenderPrefab[] roots, ContentPrefab content, float halfDelta, Lift03RoadSampling road, RenderPrefab moving)
    {
        var delta = StructureDelta(halfDelta);
        var column = Lift03ColumnAssembly.Create(factory, target.name + " Column", roots[Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.I32[0]], delta) ?? throw new InvalidOperationException("Lift03 support render graph creation failed.");
        var footing = Lift03FootingAssembly.Create(factory, target.name + " Footing", roots[Numeric_Types_Lift03_Lift03BridgeGenerator_cs.Values.I32[1]], delta) ?? throw new InvalidOperationException("Lift03 footing render graph creation failed.");
        var mechanism = Lift03ObjectAssembly.Create(factory, target.name, target.name + " Moving Object", moving, column, footing, content, delta, road);
        using var pillarSources = new Lift03PillarSources(factory.ResolveOriginal<RenderPrefab>("Default_Base Mesh"), content);
        var pillar = Lift03NetPillarAssembly.Create(factory, target.name + " Pillar", pillarSources, content, delta) ?? throw new InvalidOperationException("Lift03 ordinary pillar creation failed.");
        Lift03NetworkAssembly.Apply(target, Lower.Target, factory, pillar, mechanism, Lower.Opposite);
    }
}

// Embedded numerical arrays: Types_Lift03_Lift03BridgeGenerator_cs
internal static class Numeric_Types_Lift03_Lift03BridgeGenerator_cs
{
    static Numeric_Types_Lift03_Lift03BridgeGenerator_cs() {}
    internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Types_Lift03_Lift03BridgeGenerator_cs");
}
