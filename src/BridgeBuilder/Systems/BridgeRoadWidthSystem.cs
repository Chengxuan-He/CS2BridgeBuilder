using System.Collections.Generic;
using System.Globalization;
using Game.Prefabs;
using Unity.Collections;
using Unity.Entities;
using BridgeBuilder.Bridges;

namespace BridgeBuilder.Systems;

/// <summary>Read-only source-road measurement using initialized native piece data.</summary>
[DisableAutoCreation]
internal sealed partial class BridgeRoadWidthSystem : SystemBase
{
    private readonly Dictionary<(Entity, bool), string> _lastDiagnostics = new();
    protected override void OnUpdate() { }

    internal float Read(NetGeometryPrefab road, ICollection<string>? breakdown,
        List<(string Name, float Width)>? allocations = null, bool extensions = false)
    {
        var prefabs = World.GetExistingSystemManaged<PrefabSystem>();
        if (prefabs == null || !prefabs.TryGetEntity(road, out var entity)
            || !EntityManager.HasBuffer<NetGeometrySection>(entity))
        {
            Mod.Log.Warn($"RoadWidth unavailable: road='{road.name}', native section buffer not ready.");
            return 0f;
        }
        EntityManager.CompleteAllTrackedJobs();
        var diagnostic = new List<string>
        {
            $"RoadWidth road='{road.name}', entity={entity}, extensions={extensions}",
        };
        // Size the structure for the road it will actually carry: an elevated
        // edge, not the ground-level asset thumbnail/default width. In native
        // roads the same sidewalk section can select a narrower elevated piece.
        // Resolve authored conditions; never rewrite or widen any road piece.
        NetCompositionHelpers.GetRequirementFlags(
            new[] { NetPieceRequirements.Edge, NetPieceRequirements.Elevated }, out var flags, out _);
        var setStates = default(CompositionFlags);
        foreach (var source in road.m_EdgeStates ?? System.Array.Empty<NetEdgeStateInfo>())
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

        using var sections = new NativeList<NetGeometrySection>(Allocator.Temp);
        var widths = GetComponentLookup<NetPieceData>(true);
        var componentTotal = 0f;
        var sectionIndex = 0;
        foreach (var section in EntityManager.GetBuffer<NetGeometrySection>(entity, true))
        {
            var sourceSection = prefabs.GetPrefab<NetSectionPrefab>(section.m_Section);
            var excludedLayer = (section.m_Flags & (NetSectionFlags.Underground | NetSectionFlags.Overhead)) != 0;
            var excludedSide = SectionNames.IsSide(sourceSection.name) != extensions;
            using var probeSections = new NativeArray<NetGeometrySection>(new[] { section }, Allocator.Temp);
            using var probePieces = new NativeList<NetCompositionPiece>(Allocator.Temp);
            NetCompositionHelpers.GetCompositionPieces(probePieces, probeSections, flags,
                GetBufferLookup<NetSubSection>(true), GetBufferLookup<NetSectionPiece>(true));
            diagnostic.Add($"Input section[{sectionIndex++}]='{sourceSection.name}', flags={section.m_Flags}, "
                + $"all={FlagText(section.m_CompositionAll)}, any={FlagText(section.m_CompositionAny)}, "
                + $"none={FlagText(section.m_CompositionNone)}, selectedPieces={probePieces.Length}, "
                + $"excludedLayer={excludedLayer}, excludedSide={excludedSide}");
            foreach (var selectedPiece in probePieces.AsArray())
            {
                var selectedPrefab = prefabs.GetPrefab<NetPiecePrefab>(selectedPiece.m_Piece);
                diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
                    "  selected='{0}', entity={1}, serializedWidth={2:R}, nativeWidth={3:R}",
                    selectedPrefab.name, selectedPiece.m_Piece, selectedPrefab.m_Width,
                    widths[selectedPiece.m_Piece].m_Width));
            }
            if (excludedLayer || excludedSide) continue;
            // Keep the existing road-surface boundary: terrain-blending extensions
            // are measured separately, never counted again as road surface.
            sections.Add(section);
            // Sum lateral component slots, using only the variants selected by
            // the native requirement resolver. Pieces in the same slot are
            // overlapping render layers, not additional lanes or footways.
            var slotWidths = new Dictionary<int, float>();
            var slotNames = new Dictionary<int, string>();
            foreach (var selected in probePieces.AsArray())
            {
                var width = widths[selected.m_Piece].m_Width;
                var pieceName = prefabs.GetPrefab<NetPiecePrefab>(selected.m_Piece).name;
                if (float.IsNaN(width) || float.IsInfinity(width) || width < 0f)
                {
                    diagnostic.Add("RESULT unavailable: invalid selected component width.");
                    allocations?.Clear();
                    Publish();
                    return 0f;
                }
                if (!slotWidths.TryGetValue(selected.m_SectionIndex, out var previous) || width > previous)
                {
                    slotWidths[selected.m_SectionIndex] = width;
                    slotNames[selected.m_SectionIndex] = pieceName;
                }
                else if (width == previous && SectionNames.IsSidewalk(pieceName))
                {
                    // A bottom/deck render layer in the same slot is not another
                    // component and must not hide the selected sidewalk identity.
                    slotNames[selected.m_SectionIndex] = pieceName;
                }
            }
            var sectionWidth = 0f;
            var slots = new List<int>(slotWidths.Keys);
            slots.Sort();
            foreach (var slot in slots)
            {
                var width = slotWidths[slot];
                sectionWidth += width;
                // The parent section may contain several selected subsections.
                // Keep their real elevated widths and identities separately for
                // outermost-footway lookup rather than labelling the entire
                // parent's summed width as one sidewalk.
                allocations?.Add((slotNames[slot], width));
            }
            componentTotal += sectionWidth;
            diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
                "Component sum section='{0}', contribution={1:R}, cumulative={2:R}",
                sourceSection.name, sectionWidth, componentTotal));
        }
        using var pieces = new NativeList<NetCompositionPiece>(Allocator.Temp);
        NetCompositionHelpers.GetCompositionPieces(pieces, sections.AsArray(), flags,
            GetBufferLookup<NetSubSection>(true), GetBufferLookup<NetSectionPiece>(true));
        if (pieces.Length == 0)
        {
            diagnostic.Add("RESULT unavailable: no selected surface pieces.");
            Publish();
            return 0f;
        }
        var data = new NetCompositionData { m_Flags = flags };
        NetCompositionHelpers.CalculateCompositionData(ref data, pieces.AsArray(), widths,
            GetComponentLookup<NetVertexMatchData>(true));
        foreach (var piece in pieces.AsArray())
        {
            var source = prefabs.GetPrefab<NetPiecePrefab>(piece.m_Piece);
            diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
                "Native road section={0}, piece='{1}', width={2:R}, size={3}, offset={4}, flags={5}",
                piece.m_SectionIndex, source.name, widths[piece.m_Piece].m_Width,
                piece.m_Size, piece.m_Offset, piece.m_SectionFlags));
        }
        diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
            "Native elevated edge composition flags={0}, width={1:R}", FlagText(flags), data.m_Width));
        diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
            "RESULT selected component width sum={0:R}; native composition is diagnostic only.", componentTotal));
        if (EntityManager.HasComponent<NetGeometryData>(entity))
            diagnostic.Add(string.Format(CultureInfo.InvariantCulture,
                "Initialized ground default width={0:R}; elevated surface-only width={1:R}; extensions={2}",
                EntityManager.GetComponentData<NetGeometryData>(entity).m_DefaultWidth,
                data.m_Width, extensions));
        Publish();
        return componentTotal;

        void Publish()
        {
            foreach (var line in diagnostic) breakdown?.Add(line);
            var snapshot = string.Join("\n", diagnostic);
            var key = (entity, extensions);
            if (_lastDiagnostics.TryGetValue(key, out var previous) && previous == snapshot) return;
            _lastDiagnostics[key] = snapshot;
            Mod.Log.Info(snapshot);
        }
    }

    private static string FlagText(CompositionFlags flags) =>
        $"general={flags.m_General};left={flags.m_Left};right={flags.m_Right}";
}
