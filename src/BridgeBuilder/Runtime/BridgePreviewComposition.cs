using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;

namespace BridgeBuilder.Runtime;

/// <summary>
/// Runs the game's section packing in an unregistered, disposable world. No
/// entity or prefab from this world is published to the loaded city.
/// </summary>
internal sealed class BridgePreviewComposition : IDisposable
{
    internal readonly struct Piece
    {
        internal Piece(NetPiecePrefab prefab, NetCompositionPiece composition)
        { Prefab = prefab; Composition = composition; }
        internal NetPiecePrefab Prefab { get; }
        internal NetCompositionPiece Composition { get; }
    }

    private readonly World _world = new("BridgeBuilder temporary preview composition");
    private readonly Dictionary<NetSectionPrefab, Entity> _sections = new();
    private readonly Dictionary<NetPiecePrefab, Entity> _pieces = new();
    private readonly Dictionary<Entity, NetPiecePrefab> _prefabs = new();
    private readonly HashSet<NetSectionPrefab> _sectionPath = new();
    private bool _invalidGraph;
    private bool _disposed;

    internal Piece[] Compose(NetGeometryPrefab prefab, CompositionFlags flags, out NetCompositionData data,
        bool priceComposition = false, bool includeOverhead = true)
    {
        data = default;
        if (_disposed || prefab == null || _invalidGraph) return Array.Empty<Piece>();
        // Auxiliary decks use the same edge composition path as the main deck.
        // A fixed segment's custom states must not turn it into a node preview.
        if (!priceComposition) flags.m_General |= CompositionFlags.General.Edge;
        var setStates = default(CompositionFlags);
        foreach (var source in priceComposition ? Array.Empty<NetEdgeStateInfo>() :
            prefab.m_EdgeStates ?? Array.Empty<NetEdgeStateInfo>())
        {
            if (source == null) continue;
            var state = new NetGeometryEdgeState();
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAll, out state.m_CompositionAll, out _);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAny, out state.m_CompositionAny, out _);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireNone, out state.m_CompositionNone, out _);
            NetCompositionHelpers.GetRequirementFlags(source.m_SetState, out state.m_State, out _);
            if (NetCompositionHelpers.TestEdgeFlags(state, flags)) setStates |= state.m_State;
        }
        flags |= setStates;
        var geometry = new List<NetGeometrySection>();
        AddSections(geometry, prefab.m_Sections, 0);
        if (includeOverhead && prefab.TryGet<OverheadNetSections>(out var overhead))
            AddSections(geometry, overhead.m_Sections, NetSectionFlags.Overhead);
        // The default price includes buried utilities, unlike the visible preview.
        if (priceComposition && prefab.TryGet<UndergroundNetSections>(out var underground))
            AddSections(geometry, underground.m_Sections, NetSectionFlags.Underground);
        if (_invalidGraph) return Array.Empty<Piece>();
        using var input = new NativeArray<NetGeometrySection>(geometry.ToArray(), Allocator.Temp);
        using var output = new NativeList<NetCompositionPiece>(Allocator.Temp);
        var helper = _world.GetOrCreateSystemManaged<CompositionSystem>();
        data = helper.Compose(input, output, flags);
        var result = new List<Piece>();
        foreach (var piece in output.AsArray())
            if (_prefabs.TryGetValue(piece.m_Piece, out var piecePrefab)) result.Add(new Piece(piecePrefab, piece));
        return result.ToArray();
    }

    private void AddSections(List<NetGeometrySection> geometry, NetSectionInfo[] sections,
        NetSectionFlags sectionFlags)
    {
        if (sections == null) return;
        var firstMedian = int.MaxValue;
        var lastMedian = int.MinValue;
        for (var i = 0; i < sections.Length; i++)
            if (sections[i]?.m_Median == true)
            { firstMedian = Math.Min(firstMedian, i * 2); lastMedian = Math.Max(lastMedian, i * 2); }
        if (firstMedian == int.MaxValue)
        {
            firstMedian = lastMedian = sections.Length - 1;
            sectionFlags |= NetSectionFlags.AlignCenter;
        }
        for (var i = 0; i < sections.Length; i++)
        {
            var source = sections[i];
            if (source?.m_Section == null) continue;
            var section = new NetGeometrySection
            {
                m_Section = Section(source.m_Section), m_Offset = source.m_Offset,
                m_Flags = sectionFlags
            };
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAll, out section.m_CompositionAll, out _);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAny, out section.m_CompositionAny, out _);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireNone, out section.m_CompositionNone, out _);
            if (source.m_Invert) section.m_Flags |= NetSectionFlags.Invert;
            if (source.m_Flip) section.m_Flags |= NetSectionFlags.FlipLanes | NetSectionFlags.FlipMesh;
            if (source.m_HalfLength) section.m_Flags |= NetSectionFlags.HalfLength;
            var hidden = source.m_HiddenLayers;
            if ((hidden & NetPieceLayerMask.Surface) != 0) section.m_Flags |= NetSectionFlags.HiddenSurface;
            if ((hidden & NetPieceLayerMask.Bottom) != 0) section.m_Flags |= NetSectionFlags.HiddenBottom;
            if ((hidden & NetPieceLayerMask.Top) != 0) section.m_Flags |= NetSectionFlags.HiddenTop;
            if ((hidden & NetPieceLayerMask.Side) != 0) section.m_Flags |= NetSectionFlags.HiddenSide;
            const NetPieceLayerMask all = NetPieceLayerMask.Surface | NetPieceLayerMask.Bottom |
                NetPieceLayerMask.Top | NetPieceLayerMask.Side;
            if ((hidden & all) == all) section.m_Flags |= NetSectionFlags.Hidden;
            section.m_Flags |= i * 2 < firstMedian ? NetSectionFlags.Left :
                i * 2 > lastMedian ? NetSectionFlags.Right : NetSectionFlags.Median;
            geometry.Add(section);
        }
    }

    private Entity Section(NetSectionPrefab prefab)
    {
        if (_sectionPath.Contains(prefab))
        {
            // A cached entity alone does not break a cycle in the native walk.
            _invalidGraph = true;
            return Entity.Null;
        }
        if (_sections.TryGetValue(prefab, out var existing)) return existing;
        _sectionPath.Add(prefab);
        var manager = _world.EntityManager;
        var entity = manager.CreateEntity();
        _sections.Add(prefab, entity);
        manager.AddBuffer<NetSubSection>(entity);
        manager.AddBuffer<NetSectionPiece>(entity);
        var children = new List<NetSubSection>();
        foreach (var source in prefab.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
        {
            if (source?.m_Section == null) continue;
            var child = new NetSubSection { m_SubSection = Section(source.m_Section) };
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAll, out child.m_CompositionAll, out child.m_SectionAll);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAny, out child.m_CompositionAny, out child.m_SectionAny);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireNone, out child.m_CompositionNone, out child.m_SectionNone);
            children.Add(child);
        }
        var pieces = new List<NetSectionPiece>();
        foreach (var source in prefab.m_Pieces ?? Array.Empty<NetPieceInfo>())
        {
            if (source?.m_Piece == null) continue;
            var piece = new NetSectionPiece { m_Piece = RegisterPiece(source.m_Piece), m_Offset = source.m_Offset };
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAll, out piece.m_CompositionAll, out piece.m_SectionAll);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireAny, out piece.m_CompositionAny, out piece.m_SectionAny);
            NetCompositionHelpers.GetRequirementFlags(source.m_RequireNone, out piece.m_CompositionNone, out piece.m_SectionNone);
            piece.m_Flags = PieceFlags(source.m_Piece);
            pieces.Add(piece);
        }
        // Recursive entity creation invalidates DynamicBuffer handles. Obtain
        // them only after all structural changes for the children have finished.
        var subBuffer = manager.GetBuffer<NetSubSection>(entity);
        foreach (var child in children) subBuffer.Add(child);
        var pieceBuffer = manager.GetBuffer<NetSectionPiece>(entity);
        foreach (var piece in pieces) pieceBuffer.Add(piece);
        _sectionPath.Remove(prefab);
        return entity;
    }

    private Entity RegisterPiece(NetPiecePrefab prefab)
    {
        if (_pieces.TryGetValue(prefab, out var existing)) return existing;
        var manager = _world.EntityManager;
        var entity = manager.CreateEntity();
        manager.AddComponentData(entity, new NetPieceData
        {
            m_Width = prefab.m_Width, m_Length = prefab.m_Length,
            m_HeightRange = prefab.m_HeightRange, m_SurfaceHeights = prefab.m_SurfaceHeights,
            m_WidthOffset = prefab.m_WidthOffset, m_NodeOffset = prefab.m_NodeOffset,
            m_SideConnectionOffset = prefab.m_SideConnectionOffset
        });
        _pieces.Add(prefab, entity);
        _prefabs.Add(entity, prefab);
        return entity;
    }

    private static NetPieceFlags PieceFlags(NetPiecePrefab prefab)
    {
        var flags = prefab.m_Layer switch
        {
            NetPieceLayer.Surface => NetPieceFlags.Surface,
            NetPieceLayer.Bottom => NetPieceFlags.Bottom,
            NetPieceLayer.Top => NetPieceFlags.Top,
            NetPieceLayer.Side => NetPieceFlags.Side,
            _ => (NetPieceFlags)0
        };
        if (prefab.meshCount != 0) flags |= NetPieceFlags.HasMesh;
        if (prefab.TryGet<NetDividerPiece>(out var divider))
        {
            if (divider.m_PreserveShape) flags |= NetPieceFlags.PreserveShape | NetPieceFlags.DisableTiling;
            if (divider.m_BlockTraffic) flags |= NetPieceFlags.BlockTraffic;
            if (divider.m_BlockCrosswalk) flags |= NetPieceFlags.BlockCrosswalk;
        }
        if (prefab.TryGet<NetPieceTiling>(out var tiling) && tiling.m_DisableTextureTiling)
            flags |= NetPieceFlags.DisableTiling;
        if (prefab.TryGet<MovePieceVertices>(out var move))
        {
            if (move.m_LowerBottomToTerrain) flags |= NetPieceFlags.LowerBottomToTerrain;
            if (move.m_RaiseTopToTerrain) flags |= NetPieceFlags.RaiseTopToTerrain;
            if (move.m_SmoothTopNormal) flags |= NetPieceFlags.SmoothTopNormal;
        }
        if (prefab.TryGet<AsymmetricPieceMesh>(out var asymmetric))
        {
            if (asymmetric.m_Sideways) flags |= NetPieceFlags.AsymmetricMeshX;
            if (asymmetric.m_Lengthwise) flags |= NetPieceFlags.AsymmetricMeshZ;
        }
        return flags;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _world.Dispose();
        _sections.Clear(); _pieces.Clear(); _prefabs.Clear(); _sectionPath.Clear();
    }

    [DisableAutoCreation]
    internal sealed partial class CompositionSystem : SystemBase
    {
        protected override void OnUpdate() { }
        internal NetCompositionData Compose(NativeArray<NetGeometrySection> sections,
            NativeList<NetCompositionPiece> pieces, CompositionFlags flags)
        {
            NetCompositionHelpers.GetCompositionPieces(pieces, sections, flags,
                GetBufferLookup<NetSubSection>(true), GetBufferLookup<NetSectionPiece>(true));
            var data = new NetCompositionData { m_Flags = flags };
            NetCompositionHelpers.CalculateCompositionData(ref data, pieces.AsArray(),
                GetComponentLookup<NetPieceData>(true), GetComponentLookup<NetVertexMatchData>(true));
            return data;
        }
    }
}
