using System;
using System.Collections.Generic;
using System.Linq;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Runtime;

/// <summary>A straight, isolated illustrative span assembled from the generated recipe.</summary>
internal static class BridgePreviewScene
{
    // Presentation-only test course: deck at y=0, flat ground at y=-30.
    // Do not clamp this to a selected road's construction-tool elevation range.
    private const float PreviewElevation = 30f;
    private readonly struct Span
    {
        internal Span(float start, float length, int fixedIndex, CompositionFlags flags)
        { Start = start; Length = length; FixedIndex = fixedIndex; Flags = flags; }
        internal float Start { get; }
        internal float Length { get; }
        internal int FixedIndex { get; }
        internal CompositionFlags Flags { get; }
    }

    internal static bool Build(BridgePreviewSession session, BridgePreviewDrawList draws)
    {
        if (session.Root == null || !session.Root.TryGet<Bridge>(out var bridge))
            return false;
        // This scene consists of ordinary elevated EDGES. Elevated alone does
        // not match pieces requiring Edge (including the road surface and many
        // cable spans); native composition would then leave only the towers.
        NetCompositionHelpers.GetRequirementFlags(
            new[] { NetPieceRequirements.Edge, NetPieceRequirements.Elevated }, out var baseFlags, out _);
        var spans = new List<Span>();
        var cursor = 0f;
        var fixedSegments = bridge.m_FixedSegments ?? Array.Empty<FixedNetSegmentInfo>();
        for (var index = 0; index < fixedSegments.Length; index++)
        {
            var segment = fixedSegments[index];
            if (segment == null || segment.m_Length <= 0f) continue;
            NetCompositionHelpers.GetRequirementFlags(segment.m_SetState, out var set, out _);
            NetCompositionHelpers.GetRequirementFlags(segment.m_UnsetState, out var unset, out _);
            // CompositionSelectSystem.GetEdgeFlags starts fresh for EACH edge.
            // Fixed-segment states are not a running course state. In particular,
            // carrying Front/Opening into a Back segment selects both mirrored
            // cable sections and leaves middle cables on the final approach.
            var flags = new CompositionFlags
            {
                m_General = (baseFlags.m_General & ~unset.m_General) | set.m_General,
                m_Left = (baseFlags.m_Left & ~unset.m_Left) | set.m_Left,
                m_Right = (baseFlags.m_Right & ~unset.m_Right) | set.m_Right
            };
            // A preview has no user-drawn course length: show one occurrence of
            // optional span kinds, and every mandatory occurrence of each kind.
            var count = Math.Max(1, segment.m_CountRange.x);
            for (var i = 0; i < count; i++)
            {
                spans.Add(new Span(cursor, segment.m_Length, index, flags));
                cursor += segment.m_Length;
            }
        }
        if (spans.Count == 0)
        {
            if (bridge.m_SegmentLength <= 0f)
                return false;
            spans.Add(new Span(0f, bridge.m_SegmentLength, -1, baseFlags));
        }
        using var composition = new BridgePreviewComposition();
        var path = new HashSet<NetGeometryPrefab>();
        // Main deck height is identical for every preview. Auxiliary decks
        // retain their authored offset relative to that main deck.
        return AddDeck(session.Root, Matrix4x4.identity, spans, session, draws, composition, path, PreviewElevation)
            && draws.Draws.Count != 0;
    }

    private static bool AddDeck(NetGeometryPrefab deck, Matrix4x4 transform, List<Span> spans,
        BridgePreviewSession session, BridgePreviewDrawList draws,
        BridgePreviewComposition composition, HashSet<NetGeometryPrefab> path, float elevation)
    {
        if (!path.Add(deck)) return false;
        try
        {
            var deckWidth = 0f;
            foreach (var span in spans)
            {
                var pieces = composition.Compose(deck, span.Flags, out var data);
                deckWidth = data.m_Width;
                if (pieces.Length == 0)
                    return false;
                var deckDrawn = false;
                foreach (var entry in pieces)
                {
                    var piece = entry.Composition;
                    if ((piece.m_PieceFlags & NetPieceFlags.HasMesh) == 0 ||
                        (piece.m_SectionFlags & NetSectionFlags.Hidden) != 0) continue;
                    if (HiddenLayer(piece)) continue;
                    if (piece.m_Size.z <= 0f)
                        return false;
                    deckDrawn |= (piece.m_SectionFlags & NetSectionFlags.Overhead) == 0;
                }
                if (!deckDrawn)
                {
                    return false;
                }
                if (!draws.AddNetSpan(pieces, data, transform, span.Start, span.Length))
                    return false;
                if (!draws.AddLanes(pieces, data, transform, span.Start, span.Length))
                    return false;
            }
            if (deck.TryGet<NetSubObjects>(out var objects))
            {
                foreach (var item in objects.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
                {
                    if (item?.m_Object is not ObjectGeometryPrefab geometry) continue;
                    if (item.m_RequireOutsideConnection || item.m_RequireOrphan) continue;
                    var replacements = Resolve(geometry, session, draws, deckWidth, elevation + item.m_Position.y);
                    if (replacements.Count == 0)
                        return false;
                    foreach (var replacement in replacements)
                    {
                        var concrete = replacement.Prefab;
                        var anchorTop = item.m_AnchorTop;
                        var onGround = false;
                        if (concrete.TryGet<PillarObject>(out var pillar))
                        {
                            anchorTop |= pillar.m_Type == PillarType.Standalone || pillar.m_Type == PillarType.Vertical;
                            onGround = pillar.m_Type == PillarType.Standalone || pillar.m_Type == PillarType.Vertical ||
                                pillar.m_Type == PillarType.Base;
                        }
                        if (!draws.TryObjectLayout(concrete, out var layout))
                            return false;
                        foreach (var z in Positions(item, spans))
                        {
                            var position = (Vector3)item.m_Position;
                            position.x += replacement.Offset;
                            var stack = layout.Align(ref position, anchorTop, item.m_AnchorCenter, onGround, -elevation);
                            var objectElevation = onGround ? 0f :
                                BridgePreviewObjectLayout.ChildElevation(elevation, position.y);
                            position.z += z;
                            if (!draws.AddObject(concrete, transform * Matrix4x4.TRS(position, item.m_Rotation, Vector3.one),
                                    stack, objectElevation))
                                return false;
                        }
                    }
                }
            }
            if (deck.TryGet<AuxiliaryNets>(out var auxiliary))
                foreach (var item in auxiliary.m_AuxiliaryNets ?? Array.Empty<AuxiliaryNetInfo>())
                {
                    if (item?.m_Prefab is not NetGeometryPrefab other) continue;
                    var local = Matrix4x4.Translate(item.m_Position);
                    var childSpans = spans;
                    var leftHandTraffic = Unity.Entities.World.DefaultGameObjectInjectionWorld
                        .GetExistingSystemManaged<Game.City.CityConfigurationSystem>().leftHandTraffic;
                    if (Game.Net.NetUtils.ShouldInvert(item.m_InvertWhen, leftHandTraffic))
                    {
                        // CourseSplitSystem.GetAuxCourse reverses endpoints and
                        // rotates their frames by PI; its upgrade flags swap sides.
                        // For this straight course, reverse about its midpoint.
                        var end = spans.Max(span => span.Start + span.Length);
                        local *= Matrix4x4.Translate(new Vector3(0f, 0f, end)) *
                            Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));
                        childSpans = spans.AsEnumerable().Reverse().Select(span => new Span(
                            end - span.Start - span.Length, span.Length, span.FixedIndex,
                            NetCompositionHelpers.InvertCompositionFlags(span.Flags))).ToList();
                    }
                    if (!AddDeck(other, transform * local, childSpans,
                            session, draws, composition, path, elevation + item.m_Position.y)) return false;
                }
            return true;
        }
        finally { path.Remove(deck); }
    }

    private static List<(ObjectGeometryPrefab Prefab, float Offset)> Resolve(ObjectGeometryPrefab prefab,
        BridgePreviewSession session, BridgePreviewDrawList draws, float width, float elevation)
    {
        var result = new List<(ObjectGeometryPrefab, float)>();
        if (!prefab.Has<PlaceholderObject>()) { result.Add((prefab, 0f)); return result; }
        // SubObjectSystem.CreateSubObject selects THREE independent groups, not
        // a single alphabetical winner. Reproduce its pillar scoring for the
        // flat preview course; this does not alter any source mesh or width.
        var selected = new ObjectGeometryPrefab?[3];
        var scores = new[] { -1f, -1f, -1f };
        var offsets = new float[3];
        var weights = new int[3];
        var random = new System.Random(0);
        foreach (var candidate in session.ObjectCandidates)
        {
            if (!candidate.TryGet<SpawnableObject>(out var spawn) || spawn.m_Probability <= 0 ||
                spawn.m_Placeholders == null || !spawn.m_Placeholders.Any(p => ReferenceEquals(p, prefab))) continue;
            if (!draws.TryObjectLayout(candidate, out var layout)) continue;
            var group = 0;
            var score = 0f;
            var offset = 0f;
            if (candidate.TryGet<PillarObject>(out var pillar))
            {
                group = pillar.m_Type == PillarType.Horizontal ? 1 : pillar.m_Type == PillarType.Base ? 2 : 0;
                if (pillar.m_Type == PillarType.Horizontal)
                    score = 1f / (1f + Mathf.Abs(layout.Size.x - (width - 1f))) +
                        .01f / (1f + Mathf.Max(0f, pillar.m_VerticalPillarOffsetRange.max));
                else
                {
                    var difference = layout.Size.y - layout.PlacementOffset - elevation;
                    score = 1f / (1f + (difference < 0f ? -difference : 2f * difference));
                    if (pillar.m_Type == PillarType.Vertical)
                        offset = Mathf.Max(0f, (width - layout.Size.x) * .5f);
                }
            }
            if (score < scores[group]) continue;
            if (score > scores[group]) weights[group] = 0;
            weights[group] += spawn.m_Probability;
            if (score == scores[group] && random.Next(weights[group]) >= spawn.m_Probability) continue;
            scores[group] = score;
            offsets[group] = offset;
            selected[group] = candidate;
        }
        // Final flat-course alignment of a paired horizontal/vertical pillar
        // uses the horizontal prefab's authored support range, not the initial
        // width-based placement guess (AlignDoubleVerticalPillars).
        if (selected[0] != null && selected[1] != null &&
            selected[0]!.TryGet<PillarObject>(out var vertical) && vertical.m_Type == PillarType.Vertical &&
            selected[1]!.TryGet<PillarObject>(out var horizontal) &&
            draws.TryObjectLayout(selected[0]!, out var verticalLayout))
        {
            var range = horizontal.m_VerticalPillarOffsetRange;
            offsets[0] = range.min <= 0f || offsets[0] == 0f ? 0f :
                Mathf.Max((range.min + range.max) * .5f, verticalLayout.Size.x * .5f);
        }
        for (var group = 0; group < selected.Length; group++)
        {
            var candidate = selected[group];
            if (candidate == null) continue;
            result.Add((candidate, offsets[group]));
            if (offsets[group] != 0f) result.Add((candidate, -offsets[group]));
        }
        return result;
    }

    private static bool HiddenLayer(NetCompositionPiece piece)
        => ((piece.m_PieceFlags & NetPieceFlags.Surface) != 0 && (piece.m_SectionFlags & NetSectionFlags.HiddenSurface) != 0)
        || ((piece.m_PieceFlags & NetPieceFlags.Bottom) != 0 && (piece.m_SectionFlags & NetSectionFlags.HiddenBottom) != 0)
        || ((piece.m_PieceFlags & NetPieceFlags.Top) != 0 && (piece.m_SectionFlags & NetSectionFlags.HiddenTop) != 0)
        || ((piece.m_PieceFlags & NetPieceFlags.Side) != 0 && (piece.m_SectionFlags & NetSectionFlags.HiddenSide) != 0);

    private static IEnumerable<float> Positions(NetSubObjectInfo item, List<Span> spans)
    {
        var positions = new HashSet<float>();
        var end = spans[spans.Count - 1].Start + spans[spans.Count - 1].Length;
        foreach (var span in spans)
        {
            var fixedMatch = item.m_FixedIndex == span.FixedIndex;
            switch (item.m_Placement)
            {
                case NetObjectPlacement.CourseStart: positions.Add(0f); break;
                case NetObjectPlacement.CourseEnd: positions.Add(end); break;
                case NetObjectPlacement.EdgeMiddle:
                case NetObjectPlacement.NotWaterwayCrossingEdgeMiddle:
                    positions.Add(span.Start + span.Length * 0.5f); break;
                case NetObjectPlacement.EdgeMiddleFixedSegment:
                    if (fixedMatch) positions.Add(span.Start + span.Length * 0.5f); break;
                case NetObjectPlacement.EdgeStartFixedSegment:
                case NetObjectPlacement.EdgeStartOrNodeFixedSegment:
                case NetObjectPlacement.NodeBeforeFixedSegment:
                    if (fixedMatch) positions.Add(span.Start); break;
                case NetObjectPlacement.EdgeEndFixedSegment:
                case NetObjectPlacement.EdgeEndOrNodeFixedSegment:
                case NetObjectPlacement.NodeAfterFixedSegment:
                    if (fixedMatch) positions.Add(span.Start + span.Length); break;
                case NetObjectPlacement.NodeBetweenFixedSegment:
                    if (fixedMatch && span.Start != 0f) positions.Add(span.Start); break;
                case NetObjectPlacement.EdgeEndsFixedSegment:
                case NetObjectPlacement.EdgeEndsOrNodeFixedSegment:
                    if (fixedMatch) { positions.Add(span.Start); positions.Add(span.Start + span.Length); } break;
                case NetObjectPlacement.Node:
                case NetObjectPlacement.EdgeEnds:
                case NetObjectPlacement.EdgeEndsOrNode:
                case NetObjectPlacement.NotWaterwayCrossingNode:
                case NetObjectPlacement.NotWaterwayCrossingEdgeEndsOrNode:
                    positions.Add(span.Start); positions.Add(span.Start + span.Length); break;
            }
        }
        return positions.OrderBy(value => value);
    }
}
