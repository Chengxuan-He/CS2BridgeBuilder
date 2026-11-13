using System;
using System.Collections.Generic;
using BridgeBuilder.Systems;
using Game.Prefabs;
using Game.Net;
using Unity.Entities;

namespace BridgeBuilder.Bridges;

// One generation-time elevated source context. It is not persisted or a runtime
// controller: native generated assets remain independent of this scope.
internal sealed class BridgeRoadContext
{
    internal readonly NetGeometryPrefab Source;
    internal readonly Entity SourceEntity;
    internal readonly CompositionFlags Flags;
    internal readonly float Width, ExtensionWidth;
    internal readonly (string Name, float Width)[] Sections;

    internal BridgeRoadContext(NetGeometryPrefab source, PrefabSystem prefabs,
        BridgeRoadWidthSystem widths)
    {
        Source = source;
        if (!prefabs.TryGetEntity(source, out SourceEntity))
            throw new InvalidOperationException("The selected elevated road is not registered.");
        Flags = BridgeRoadWidthSystem.ElevatedFlags(source);
        var sections = new List<(string Name, float Width)>();
        Width = widths.Read(source, null, sections, selectedFlags: Flags);
        Sections = sections.ToArray();
        ExtensionWidth = widths.Read(source, null, extensions: true, selectedFlags: Flags);
    }

    // Physical node/edge/motion flags remain local; only road level is fixed.
    internal CompositionFlags ApplyLevel(CompositionFlags flags)
    {
        flags.m_General = (flags.m_General & ~CompositionFlags.General.Tunnel)
            | CompositionFlags.General.Elevated;
        return flags;
    }

    internal static NetGeometryData SelectionGeometry(NetGeometryData data)
    {
        data.m_Flags = (data.m_Flags | GeometryFlags.RequireElevated)
            & ~GeometryFlags.ElevatedIsRaised;
        return data;
    }

    // Partial evaluation only: discard branches impossible at this road level.
    // Node/Edge, side, direction and mechanism conditions remain for native
    // composition; an Any clause with such an unknown condition remains possible.
    internal bool AllowsLevel(NetPieceRequirements[]? all, NetPieceRequirements[]? any,
        NetPieceRequirements[]? none)
    {
        NetCompositionHelpers.GetRequirementFlags(all ?? Array.Empty<NetPieceRequirements>(), out var required, out _);
        NetCompositionHelpers.GetRequirementFlags(any ?? Array.Empty<NetPieceRequirements>(), out var alternatives, out var localAny);
        NetCompositionHelpers.GetRequirementFlags(none ?? Array.Empty<NetPieceRequirements>(), out var excluded, out _);
        const CompositionFlags.General level = CompositionFlags.General.Elevated | CompositionFlags.General.Tunnel;
        var selected = Flags.m_General & level;
        if ((required.m_General & level & ~selected) != 0 ||
            (excluded.m_General & level & selected) != 0) return false;
        if (alternatives == default(CompositionFlags) && localAny == 0) return true;
        if ((alternatives.m_General & level & selected) != 0) return true;
        alternatives.m_General &= ~level;
        return alternatives != default(CompositionFlags) || localAny != 0;
    }
}

// Native APIs stay on the main thread; suspended requests retain an isolated scoped map
// lets unchanged width/layout consumers reuse the resolved context without a
// global prefab cache, source mutation or cross-request state.
internal sealed class BridgeRoadGeneration : IDisposable
{
    private static readonly System.Threading.AsyncLocal<BridgeRoadGeneration?> Current = new();
    private static BridgeRoadGeneration? _current { get=>Current.Value;set=>Current.Value=value; }
    private readonly BridgeRoadGeneration? _previous;
    internal readonly BridgeBuilder.Runtime.BridgeCreationDependencies Dependencies = new(MovableLaneMidMips.TryRequiredCaches);
    internal static BridgeBuilder.Runtime.BridgeCreationDependencies? CurrentDependencies => _current?.Dependencies;
    private readonly Dictionary<NetGeometryPrefab, BridgeRoadContext> _sources = new();
    private readonly Dictionary<Entity, BridgeRoadContext> _entities = new();

    internal BridgeRoadGeneration(PrefabSystem prefabs, params NetGeometryPrefab?[] sources)
    {
        _previous = _current;
        var widths = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<BridgeRoadWidthSystem>();
        foreach (var source in sources)
        {
            if (source == null || _sources.ContainsKey(source)) continue;
            var context = new BridgeRoadContext(source, prefabs, widths);
            _sources.Add(source, context);
            _entities.Add(context.SourceEntity, context);
        }
        _current = this;
    }

    internal static BridgeRoadContext? Find(NetGeometryPrefab source)
        => _current != null && _current._sources.TryGetValue(source, out var context) ? context : null;
    internal static BridgeRoadContext? Find(Entity source)
        => _current != null && _current._entities.TryGetValue(source, out var context) ? context : null;
    internal static BridgeRoadContext? ForSampling(Entity source)
    {
        var context = Find(source);
        return context;
    }
    internal IReadOnlyDictionary<NetGeometryPrefab, BridgeRoadContext> Capture()
        => new Dictionary<NetGeometryPrefab, BridgeRoadContext>(_sources);
    internal static void RegisterClone(NetGeometryPrefab source, NetGeometryPrefab clone)
    {
        var context = Find(source);
        if (context != null) _current!._sources[clone] = context;
    }
    public void Dispose() => _current = _previous;
}
