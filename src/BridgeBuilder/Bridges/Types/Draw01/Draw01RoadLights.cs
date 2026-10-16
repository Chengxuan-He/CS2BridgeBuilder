using System;
using System.Collections.Generic;
using Game.Prefabs;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// User-approved longitudinal furniture mapping (2026-10-16). Keep selected-road
// lighting; use the four measured native mounting stations instead of a node-centre
// candidate on the open seam. This does not transform structural geometry.
internal static class Draw01RoadLights
{
    internal static MovableNodeObjects.Result Place(MovableNodeObjects.Result source, Pose roadToObject)
    {
        var placements = new List<ObjectSubObjectInfo>();
        var origins = new List<MovableRoadObjects.Rule?>();
        var seen = new HashSet<(NetPiecePrefab Piece, int Rule, float X, float Y, quaternion Rotation)>();
        var q = roadToObject.rotation.normalized;
        var inverse = Quaternion.Inverse(q);
        for (var i = 0; i < source.Placements.Length; i++)
        {
            var placement = source.Placements[i];
            var origin = source.Sources[i];
            if (!placement.m_Object.Has<StreetLightObject>())
            {
                placements.Add(placement); origins.Add(origin); continue;
            }
            if (!origin.HasValue)
                throw new InvalidOperationException("A selected road lamp has no producer identity.");
            var point = q * (Vector3)placement.m_Position + roadToObject.position;
            // The two incident node halves can select the same lateral lamp row.
            // Retain the first selected variant for that producer/row, in native order.
            if (!seen.Add((origin.Value.SourcePiece, origin.Value.SourceRuleIndex,
                point.x, point.y, placement.m_Rotation))) continue;
            foreach (var station in Draw01MechanismData.RoadAttachments(_ => placement.m_Object))
            {
                var mounted = point;
                mounted.z = station.m_Position.z;
                placements.Add(new ObjectSubObjectInfo
                {
                    m_Object = placement.m_Object,
                    m_Position = (float3)(inverse * (mounted - roadToObject.position)),
                    m_Rotation = placement.m_Rotation, m_ParentMesh = placement.m_ParentMesh,
                    m_GroupIndex = placement.m_GroupIndex, m_Probability = placement.m_Probability
                });
                origins.Add(origin);
            }
        }
        return new MovableNodeObjects.Result(placements.ToArray(), origins.ToArray(),
            source.HasStreetLights, source.AlwaysLit);
    }
}
