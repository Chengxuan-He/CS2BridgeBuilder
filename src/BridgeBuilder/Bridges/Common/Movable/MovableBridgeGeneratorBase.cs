using System;
using System.Diagnostics;
using System.Linq;
using Colossal.IO.AssetDatabase;
using CS2Mods.Shared.Infrastructure;
using Game.Common;
using Game.Prefabs;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

// Owns the movable generation lifecycle; measured mechanism rules belong to the style.
// The sample type keeps each style's disposable sampling state strongly typed.
internal abstract class MovableBridgeGeneratorBase<TSample> : BridgeGeneratorBase
    where TSample : IDisposable
{
    protected MovableBridgeGeneratorBase(ExportReport report, TowerFactory? towers) : base(report, towers) { }

    protected abstract string NativeVariantName { get; }
    protected abstract float NativeRoadWidth { get; }
    // Main mechanism first, followed by the style's sampling supports.
    protected abstract RenderPrefab[] ResolveRenderRoots(TowerFactory factory);
    protected abstract TSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots,
        ContentPrefab content, float halfDelta, RandomSeed seed);
    protected abstract RenderPrefab BuildMechanism(TowerFactory factory, string owner, TSample road,
        RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material);
    protected abstract void AssembleBridge(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, TSample road, RenderPrefab moving);

    internal sealed override BridgeStyleVariant? Apply(NetGeometryPrefab target, BridgeStyle style,
        float roadWidth, BridgeOptions options, NetGeometryPrefab? measure = null,
        Func<BridgeStyleVariant, bool>? allow = null)
    {
        if (style.Id != StyleId) throw new InvalidOperationException($"{StyleId} generator received another style.");
        var factory = _towers ?? throw new InvalidOperationException($"{StyleId} requires the owned geometry factory.");
        var variant = style.Variants.FirstOrDefault(v => v.Name == NativeVariantName && v.IsAvailable && (allow == null || allow(v)))
            ?? throw new InvalidOperationException($"{StyleId} native content source is unavailable.");
        var selected = measure ?? target;
        var width = WidthOf(selected, roadWidth);
        if (!(width > 0f) || !float.IsFinite(width))
            throw new InvalidOperationException($"{StyleId} selected road has no initialized surface width.");
        var halfDelta = (width - NativeRoadWidth) * 0.5f;
        factory.BeginBridge(StyleId, bridgeName: target.name);
        var roots = ResolveRenderRoots(factory);
        var main = roots[0];
        var content = main.GetComponent<ContentPrerequisite>()?.m_ContentPrerequisite
            ?? throw new InvalidOperationException($"{StyleId} mechanism content prerequisite is missing.");
        var prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        if (!prefabs.TryGetEntity(selected, out var selectedEntity))
            throw new InvalidOperationException($"{StyleId} selected road is not registered with the native prefab system.");

        var clock = Stopwatch.StartNew();
        using var road = SampleRoad(target.name, selectedEntity, roots, content, halfDelta, RandomSeed.Next());
        var samplingMs = clock.ElapsedMilliseconds;
        // Shared native tileable asphalt source, independent of each structural atlas.
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse("1d7dad9f64c9fe55a809f1080c39fb92"), out SurfaceAsset surface))
            throw new InvalidOperationException("The native Road surface is unavailable.");
        using var material = factory.CreateMovableRoadMaterial(surface);
        var nativeLevels = new[] { main }.Concat(main.GetComponent<LodProperties>().m_LodMeshes).ToArray();
        var moving = BuildMechanism(factory, target.name, road, nativeLevels, halfDelta, material);
        var mechanismMs = clock.ElapsedMilliseconds - samplingMs;
        AssembleBridge(target, factory, roots, content, halfDelta, road, moving);
        Mod.Log.Info($"{target.name}: {StyleId} generation samplingMs={samplingMs}; mechanismMs={mechanismMs}; " +
            $"supports/objects/networkMs={clock.ElapsedMilliseconds - samplingMs - mechanismMs}; totalMs={clock.ElapsedMilliseconds}.");
        // Transfer material ownership only after the complete native graph exists.
        material.Keep();
        return variant;
    }
}
