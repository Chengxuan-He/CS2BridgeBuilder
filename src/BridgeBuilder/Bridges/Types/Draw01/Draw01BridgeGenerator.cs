using System;
using System.Linq;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal sealed class Draw01BridgeGenerator : BridgeGeneratorBase
{
    internal Draw01BridgeGenerator(ExportReport report, TowerFactory? towers) : base(report, towers) { }
    internal override string StyleId => "Draw01";

    internal override BridgeStyleVariant? Apply(NetGeometryPrefab target, BridgeStyle style,
        float roadWidth, BridgeOptions options, NetGeometryPrefab? measure = null,
        Func<BridgeStyleVariant, bool>? allow = null)
    {
        if (style.Id != StyleId) throw new InvalidOperationException("Draw01 generator received another style.");
        var factory = _towers ?? throw new InvalidOperationException("Draw01 requires the owned geometry factory.");
        var variant = style.Variants.FirstOrDefault(v => v.Name == "DrawBridge01" && v.IsAvailable && (allow == null || allow(v)))
            ?? throw new InvalidOperationException("Draw01 native content source is unavailable.");
        var selected = measure ?? target;
        var width = WidthOf(selected, roadWidth);
        if (!(width > 0f) || !float.IsFinite(width))
            throw new InvalidOperationException("Draw01 selected road has no initialized surface width.");
        // Native elevated surface: two 3 m drive pieces and two 3.5 m sidewalks.
        // The two 0.5 m side sections are outside this surface-width basis.
        const float nativeRoadWidth = 13f;
        var halfDelta = (width - nativeRoadWidth) * 0.5f;
        factory.BeginBridge(StyleId, bridgeName: target.name);
        var main = factory.ResolveOriginal<RenderPrefab>("DrawBridge01 Mesh");
        var column = factory.ResolveOriginal<RenderPrefab>("DrawBridge01Pillar Mesh");
        var footing = factory.ResolveOriginal<RenderPrefab>("DrawBridge01PillarBase Mesh");
        var content = main.GetComponent<ContentPrerequisite>()?.m_ContentPrerequisite
            ?? throw new InvalidOperationException("Draw01 mechanism content prerequisite is missing.");
        var prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        if (!prefabs.TryGetEntity(selected, out var selectedEntity))
            throw new InvalidOperationException("Draw01 selected road is not registered with the native prefab system.");
        var clock = System.Diagnostics.Stopwatch.StartNew();
        using var road = Draw01ClosedRoad.Sample(target.name, selectedEntity,
            new[] { main, column, footing }, content, halfDelta, RandomSeed.Next());
        var samplingMs = clock.ElapsedMilliseconds;
        var lods = main.GetComponent<LodProperties>().m_LodMeshes;
        // Sampling uses identity rotation at the recorded 10 m closed deck elevation.
        var levels = road.PrepareRenderLevels(new[] { main, lods[0], lods[1] }, halfDelta,
            Matrix4x4.Translate(new Vector3(0f, 10f, 0f)));
        var moving = Draw01RenderAssembly.CreateMechanism(factory, target.name, target.name + " Mechanism", levels, halfDelta,
            new Draw01UndersideLayout(road.Road, road.RoadToObject))
            ?? throw new InvalidOperationException("Draw01 moving render graph creation failed.");
        var mechanismMs = clock.ElapsedMilliseconds - samplingMs;
        var support = Draw01RenderAssembly.CreateColumn(factory, target.name, target.name + " Column", column, halfDelta)
            ?? throw new InvalidOperationException("Draw01 support render graph creation failed.");
        var foundation = Draw01RenderAssembly.CreateFooting(factory, target.name, target.name + " Footing", footing, halfDelta)
            ?? throw new InvalidOperationException("Draw01 footing render graph creation failed.");
        var mechanism = Draw01ObjectAssembly.Create(factory, target.name, target.name + " Moving Object",
            moving, support, foundation, content, factory.ResolveOriginal<ObjectPrefab>, halfDelta, road);
        var pillar = Draw01NetPillarAssembly.Create(factory, target.name, target.name + " Pillar",
            factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillarTop Mesh"),
            factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillar Mesh"),
            factory.ResolveOriginal<RenderPrefab>("DrawBridge01NetPillarBase Mesh"), content, halfDelta);
        Draw01FixedBase.Apply(target, factory, halfDelta);
        Draw01NetworkAssembly.Apply(target, factory, pillar, mechanism);
        Mod.Log.Info($"{target.name}: Draw01 generation samplingMs={samplingMs}; mechanismMs={mechanismMs}; " +
            $"supports/objects/networkMs={clock.ElapsedMilliseconds - samplingMs - mechanismMs}; totalMs={clock.ElapsedMilliseconds}.");
        return variant;
    }
}
