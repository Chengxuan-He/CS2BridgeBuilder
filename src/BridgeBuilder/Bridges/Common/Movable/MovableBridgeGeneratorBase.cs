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
internal interface IMovableRoadSample : IDisposable { string? MaterialError {get;} }

internal abstract class MovableBridgeGeneratorBase<TSample> : BridgeGeneratorBase
    where TSample : IMovableRoadSample
{
    protected MovableBridgeGeneratorBase(ExportReport report, TowerFactory? towers) : base(report, towers) { }

    protected abstract string NativeVariantName { get; }
    protected abstract string NativeAggregateName { get; }
    protected abstract string NativeIcon { get; }
    protected abstract float NativeRoadWidth { get; }
    // Main mechanism first, followed by the style's sampling supports.
    protected abstract RenderSources ResolveRenderRoots(TowerFactory factory);
    // A reconstructed source graph must outlive sampling and assembly, but must
    // never transfer its temporary Prefabs to the persisted output graph.
    protected sealed class RenderSources : IDisposable
    {
        internal readonly RenderPrefab[] Roots;
        private readonly IDisposable? _owner;
        internal RenderSources(RenderPrefab[] roots, IDisposable? owner = null)
        { Roots = roots; _owner = owner; }
        public void Dispose() => _owner?.Dispose();
    }
    protected abstract TSample SampleRoad(string owner, Entity selectedRoad, RenderPrefab[] roots,
        ContentPrefab content, float halfDelta, RandomSeed seed);
    protected abstract System.Threading.Tasks.Task<RenderPrefab?> BuildMechanismAsync(TowerFactory factory, string owner, TSample road,
        RenderPrefab[] nativeLevels, float halfDelta, MovableRoadMaterial material);
    protected virtual System.Threading.Tasks.Task<TSample> SampleRoadAsync(string owner, Entity selectedRoad, RenderPrefab[] roots,
        ContentPrefab content, float halfDelta, RandomSeed seed)
        => System.Threading.Tasks.Task.FromResult(SampleRoad(owner,selectedRoad,roots,content,halfDelta,seed));
    protected abstract System.Threading.Tasks.Task AssembleBridgeAsync(NetGeometryPrefab target, TowerFactory factory,
        RenderPrefab[] roots, ContentPrefab content, float halfDelta, TSample road, RenderPrefab moving);

    internal sealed override async System.Threading.Tasks.Task<BridgeStyleVariant?> ApplyAsync(NetGeometryPrefab target, BridgeStyle style,
        float roadWidth, BridgeOptions options, NetGeometryPrefab? measure = null,
        Func<BridgeStyleVariant, bool>? allow = null)
    {
        if (style.Id != StyleId) throw new InvalidOperationException($"{StyleId} generator received another style.");
        var factory = _towers ?? throw new InvalidOperationException($"{StyleId} requires the owned geometry factory.");
        var variant = style.Variants.FirstOrDefault(v => v.Name == NativeVariantName && v.IsAvailable && (allow == null || allow(v)))
            ?? throw new InvalidOperationException($"{StyleId} native content source is unavailable.");
        var selected = measure ?? target;
        var width = WidthOf(selected, roadWidth);
        if (!float.IsFinite(width))
            throw new InvalidOperationException($"{StyleId} selected road has no initialized surface width.");
        var halfDelta = (width - NativeRoadWidth) * 0.5f;
        factory.BeginBridge(StyleId, bridgeName: target.name);
        using var sources = ResolveRenderRoots(factory);
        var roots = sources.Roots;
        var main = roots[0];
        var content = main.GetComponent<ContentPrerequisite>()?.m_ContentPrerequisite
            ?? throw new InvalidOperationException($"{StyleId} mechanism content prerequisite is missing.");
        var prefabs = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<PrefabSystem>();
        if (!prefabs.TryGetEntity(selected, out var selectedEntity))
            throw new InvalidOperationException($"{StyleId} selected road is not registered with the native prefab system.");

        await GenerationWork.Next("selected road sampling");
        var clock = Stopwatch.StartNew();
        using var road = await SampleRoadAsync(target.name, selectedEntity, roots, content, halfDelta, RandomSeed.Next());
        if(road.MaterialError!=null){_report.Failed(target.name,new InvalidOperationException(road.MaterialError));return null;}
        var samplingMs = clock.ElapsedMilliseconds;
        // Shared native tileable asphalt source, independent of each structural atlas.
        if (!AssetDatabase.global.TryGetAsset(Colossal.Hash128.Parse("1d7dad9f64c9fe55a809f1080c39fb92"), out SurfaceAsset surface))
            throw new InvalidOperationException("The native Road surface is unavailable.");
        if (!factory.TryCreateMovableRoadMaterial(surface, out var material)) return null;
        using var materialLifetime = material;
        var nativeLevels = new[] { main }.Concat(main.GetComponent<LodProperties>().m_LodMeshes).ToArray();
        await GenerationWork.Next("mechanism preparation");
        var moving = await BuildMechanismAsync(factory, target.name, road, nativeLevels, halfDelta, material);
        if (moving == null) return null; // The owned graph factory already records the material failure.
        var mechanismMs = clock.ElapsedMilliseconds - samplingMs;
        await GenerationWork.Next("supports and network assembly");
        await AssembleBridgeAsync(target, factory, roots, content, halfDelta, road, moving);
        // These fields are measured style data, not runtime reads from the original
        // network Prefab. The shared native aggregate remains a required resource.
        target.m_AggregateType = factory.ResolveOriginal<AggregateNetPrefab>(NativeAggregateName);
        var ui = target.GetComponent<UIObject>();
        if (ui != null) ui.m_Icon = NativeIcon;
        else _report.Warning($"'{target.name}' has no UIObject; the recorded bridge icon could not be assigned.");
        Mod.Log.Info($"{target.name}: {StyleId} generation samplingMs={samplingMs}; mechanismMs={mechanismMs}; " +
            $"supports/objects/networkMs={clock.ElapsedMilliseconds - samplingMs - mechanismMs}; totalMs={clock.ElapsedMilliseconds}.");
        // Transfer material ownership only after the complete native graph exists.
        if (!material.TryKeep()) return null;
        return variant;
    }
}
