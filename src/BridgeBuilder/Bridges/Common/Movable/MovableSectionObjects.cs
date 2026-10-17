using System;
using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// HiddenLayers does not participate in native AddCompositionObjects/TestObjectFlags.
// Moving visual attachments need private pieces without duplicate stationary producers.
// Signal objects remain native network children: their instance masks and updates require
// that ownership, including signal-capable placeholders selected by the native node context.
// This pass preserves lanes, prices, geometry references and every other component.
internal sealed class MovableSectionObjects
{
    private readonly string _owner;
    private readonly List<PrefabBase> _created;
    private readonly Dictionary<NetSectionPrefab, NetSectionPrefab> _sections = new();
    private readonly Dictionary<NetPiecePrefab, NetPiecePrefab> _pieces = new();
    private readonly Dictionary<NetLanePrefab, NetLanePrefab> _lanes = new();
    private readonly Dictionary<ObjectPrefab, ObjectPrefab> _lighting = new();
    private readonly HashSet<NetSectionPrefab> _visiting = new();

    internal MovableSectionObjects(string owner, List<PrefabBase> created)
    { _owner = owner; _created = created; }

    internal static bool IsNetworkSignal(ObjectPrefab prefab) => prefab.Has<TrafficLightObject>();

    internal NetSectionPrefab WithoutMovingObjects(NetSectionPrefab source)
    {
        if (source == null) throw new InvalidOperationException("A moving road subsection is missing.");
        CheckSource(source);
        if (_sections.TryGetValue(source, out var known)) return known;
        if (!_visiting.Add(source))
            throw new InvalidOperationException("Cyclic moving road subsection: " + source.name);
        try
        {
            var changed = false;
            var pieces = source.m_Pieces == null ? null : new NetPieceInfo[source.m_Pieces.Length];
            for (var i = 0; pieces != null && i < pieces.Length; i++)
            {
                var entry = source.m_Pieces![i];
                if (entry?.m_Piece == null)
                    throw new InvalidOperationException("Missing piece in moving road section: " + source.name);
                var piece = WithoutMovingObjects(entry.m_Piece);
                changed |= piece != entry.m_Piece;
                pieces[i] = new NetPieceInfo { m_Piece = piece, m_Offset = entry.m_Offset,
                    m_RequireAll = entry.m_RequireAll, m_RequireAny = entry.m_RequireAny,
                    m_RequireNone = entry.m_RequireNone };
            }
            var children = source.m_SubSections == null ? null : new NetSubSectionInfo[source.m_SubSections.Length];
            for (var i = 0; children != null && i < children.Length; i++)
            {
                var entry = source.m_SubSections![i];
                if (entry == null)
                    throw new InvalidOperationException("Missing subsection in moving road section: " + source.name);
                var section = WithoutMovingObjects(entry.m_Section);
                changed |= section != entry.m_Section;
                children[i] = new NetSubSectionInfo { m_Section = section,
                    m_RequireAll = entry.m_RequireAll, m_RequireAny = entry.m_RequireAny,
                    m_RequireNone = entry.m_RequireNone };
            }
            var result = source;
            if (changed)
            {
                result = (NetSectionPrefab)Clone(source, "Section");
                result.m_Pieces = pieces;
                result.m_SubSections = children;
            }
            _sections.Add(source, result);
            return result;
        }
        finally { _visiting.Remove(source); }
    }

    private NetPiecePrefab WithoutMovingObjects(NetPiecePrefab source)
    {
        CheckSource(source);
        if (_pieces.TryGetValue(source, out var known)) return known;
        var result = source;
        if (source.TryGet<NetPieceObjects>(out var sourceObjects))
        {
            var retained = new List<NetPieceObjectInfo>();
            var changed = false;
            foreach (var rule in sourceObjects.m_PieceObjects)
            {
                if (rule?.m_Object == null)
                    throw new InvalidOperationException("Missing object in moving road piece: " + source.name);
                CheckSource(rule.m_Object);
                if (IsNetworkSignal(rule.m_Object)) retained.Add(rule);
                else if (rule.m_Object.Has<StreetLightObject>())
                {
                    if (!_lighting.TryGetValue(rule.m_Object, out var producer))
                    {
                        producer = MovableStreetLights.CreateProducer(_owner, rule.m_Object, _created);
                        _lighting.Add(rule.m_Object, producer);
                    }
                    retained.Add(new NetPieceObjectInfo
                    {
                        m_Object = producer, m_Position = rule.m_Position, m_Offset = rule.m_Offset,
                        m_Rotation = rule.m_Rotation, m_RequireAll = rule.m_RequireAll,
                        m_RequireAny = rule.m_RequireAny, m_RequireNone = rule.m_RequireNone,
                        m_Probability = rule.m_Probability, m_MinLength = rule.m_MinLength,
                        m_CurveOffsetRange = rule.m_CurveOffsetRange, m_Spacing = rule.m_Spacing,
                        m_UseCurveRotation = rule.m_UseCurveRotation,
                        m_FlipWhenInverted = rule.m_FlipWhenInverted,
                        m_EvenSpacing = rule.m_EvenSpacing, m_SpacingOverride = rule.m_SpacingOverride
                    });
                    changed = true;
                }
            }
            if (changed || retained.Count != sourceObjects.m_PieceObjects.Length)
            {
                result = (NetPiecePrefab)Clone(source, "Piece");
                result.TryGet<NetPieceObjects>(out var objects);
                if (retained.Count != 0)
                    objects.m_PieceObjects = retained.ToArray();
                else
                {
                    result.Remove<NetPieceObjects>();
                    UnityEngine.Object.DestroyImmediate(objects);
                }
            }
        }
        if (source.TryGet<NetPieceLanes>(out var sourceLanes) && sourceLanes.m_Lanes != null)
        {
            var changed = false;
            var lanes = new NetLaneInfo[sourceLanes.m_Lanes.Length];
            for (var i = 0; i < lanes.Length; i++)
            {
                var entry = sourceLanes.m_Lanes[i];
                if (entry?.m_Lane == null)
                    throw new InvalidOperationException("Missing lane in moving road piece: " + source.name);
                var lane = WithoutTrackEndObject(entry.m_Lane);
                changed |= lane != entry.m_Lane;
                lanes[i] = new NetLaneInfo
                { m_Lane = lane, m_Position = entry.m_Position, m_FindAnchor = entry.m_FindAnchor };
            }
            if (changed)
            {
                if (result == source) result = (NetPiecePrefab)Clone(source, "Piece");
                result.GetComponent<NetPieceLanes>().m_Lanes = lanes;
            }
        }
        _pieces.Add(source, result);
        return result;
    }

    private NetLanePrefab WithoutTrackEndObject(NetLanePrefab source)
    {
        CheckSource(source);
        if (_lanes.TryGetValue(source, out var known)) return known;
        var result = source;
        if (source.TryGet<TrackLane>(out var track) && track.m_EndObject != null)
        {
            CheckSource(track.m_EndObject);
            // The original closed-network pass bakes this visual into the mechanism. Clear
            // only its stationary producer; keep pathfinding, fallback, track type and width.
            // Signals remain network-owned even when an input uses one as an end object.
            if (!IsNetworkSignal(track.m_EndObject))
            {
                result = (NetLanePrefab)Clone(source, "Lane");
                result.GetComponent<TrackLane>().m_EndObject = null!;
            }
        }
        _lanes.Add(source, result);
        return result;
    }

    private void CheckSource(PrefabBase source)
    {
        if (BridgeAssetInfo.IsOtherBridgePrefab(source.asset?.path, _owner,
                Path.Combine(Application.persistentDataPath, "ImportedData")))
            throw new InvalidOperationException("Moving section source belongs to another generated bridge.");
    }

    private PrefabBase Clone(PrefabBase source, string kind)
    {
        var result = source.Clone(_owner + " Moving " + kind + " " + _created.Count);
        _created.Add(result);
        // Native AddComponentFrom copies serialized fields, including the owner reference.
        foreach (var component in result.components) component.prefab = result;
        result.isDirty = true;
        return result;
    }
}
