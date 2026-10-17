using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;
using Transform = Game.Objects.Transform;
using Elevation = Game.Net.Elevation;

namespace BridgeBuilder.Bridges;

// Non-Temp construction path of native SecondaryObjectSystem.CreateSecondaryEdgeObjects,
// after composition objects: track end objects, then arbitrated utility junction objects.
internal static class MovableEdgeLaneObjects
{
    private struct UtilityNode
    {
        internal Transform m_Transform;
        internal Entity m_Prefab;
        internal PathNode m_PathNode;
        internal int m_Count;
        internal float m_Elevation, m_LanePriority;
        internal UtilityTypes m_UtilityTypes;
        internal bool m_Vertical;
    }

    internal static void Visit(EntityManager manager, Entity owner, NetCompositionData composition,
        Func<Entity, ObjectPrefab> original, ref Random random,
        MovableRoadObjectPlacement.ResolveCandidate resolve)
    {
        var lanes = manager.GetBuffer<Game.Net.SubLane>(owner, true).ToNativeArray(Unity.Collections.Allocator.Temp);
        try
        {
            var utilities = new List<UtilityNode>();
            foreach (var item in lanes)
            {
                var lane = item.m_SubLane;
                if (manager.HasComponent<Game.Net.SecondaryLane>(lane)) continue;
                if (manager.HasComponent<Game.Net.UtilityLane>(lane) && !manager.HasComponent<EdgeLane>(lane))
                {
                    var data = manager.GetComponentData<UtilityLaneData>(manager.GetComponentData<PrefabRef>(lane).m_Prefab);
                    if (data.m_NodeObjectPrefab == Entity.Null) continue;
                    var curve = manager.GetComponentData<Curve>(lane);
                    var path = manager.GetComponentData<Lane>(lane);
                    var priority = math.length(MathUtils.Size(manager.GetComponentData<ObjectGeometryData>(data.m_NodeObjectPrefab).m_Bounds));
                    var found = false;
                    for (var i = 0; i < utilities.Count; i++)
                    {
                        var entry = utilities[i];
                        if ((entry.m_UtilityTypes & data.m_UtilityTypes) == 0 || !entry.m_PathNode.Equals(path.m_StartNode)) continue;
                        if (priority > entry.m_LanePriority)
                        { entry.m_Prefab = data.m_NodeObjectPrefab; entry.m_LanePriority = priority; entry.m_Count = 1; }
                        else if (priority == entry.m_LanePriority) entry.m_Count++;
                        utilities[i] = entry;
                        found = true;
                        break;
                    }
                    if (found) continue;
                    var elevation = manager.HasComponent<Elevation>(lane)
                        ? manager.GetComponentData<Elevation>(lane).m_Elevation.x : float.MinValue;
                    utilities.Add(new UtilityNode
                    {
                        m_Transform = new Transform(curve.m_Bezier.a, NetUtils.GetNodeRotation(MathUtils.StartTangent(curve.m_Bezier))),
                        m_Prefab = data.m_NodeObjectPrefab, m_PathNode = path.m_StartNode,
                        m_Count = 1, m_Elevation = elevation == float.MinValue ? 0f : elevation,
                        m_LanePriority = priority, m_UtilityTypes = data.m_UtilityTypes, m_Vertical = true
                    });
                }
                else if (manager.HasComponent<Game.Net.TrackLane>(lane))
                {
                    var flags = manager.GetComponentData<Game.Net.TrackLane>(lane).m_Flags;
                    if ((flags & (TrackLaneFlags.StartingLane | TrackLaneFlags.EndingLane)) == 0) continue;
                    var data = manager.GetComponentData<TrackLaneData>(manager.GetComponentData<PrefabRef>(lane).m_Prefab);
                    if (data.m_EndObjectPrefab == Entity.Null) continue;
                    var curve = manager.GetComponentData<Curve>(lane).m_Bezier;
                    if ((flags & TrackLaneFlags.StartingLane) != 0)
                        End(curve.a, MathUtils.StartTangent(curve), data.m_EndObjectPrefab, ref random);
                    if ((flags & TrackLaneFlags.EndingLane) != 0)
                        End(curve.d, -MathUtils.EndTangent(curve), data.m_EndObjectPrefab, ref random);
                }
            }
            if (utilities.Count == 0) return;
            foreach (var item in lanes)
            {
                var lane = item.m_SubLane;
                if (manager.HasComponent<Game.Net.SecondaryLane>(lane) ||
                    !manager.HasComponent<Game.Net.UtilityLane>(lane) || !manager.HasComponent<EdgeLane>(lane)) continue;
                var data = manager.GetComponentData<UtilityLaneData>(manager.GetComponentData<PrefabRef>(lane).m_Prefab);
                if (data.m_NodeObjectPrefab == Entity.Null) continue;
                var path = manager.GetComponentData<Lane>(lane);
                var priority = math.length(MathUtils.Size(manager.GetComponentData<ObjectGeometryData>(data.m_NodeObjectPrefab).m_Bounds));
                for (var i = 0; i < utilities.Count; i++)
                {
                    var entry = utilities[i];
                    if ((entry.m_UtilityTypes & data.m_UtilityTypes) == 0 || !entry.m_PathNode.EqualsIgnoreCurvePos(path.m_MiddleNode)) continue;
                    if (priority > entry.m_LanePriority)
                    { entry.m_LanePriority = priority; entry.m_Count = 2; entry.m_Vertical = false; }
                    else if (priority == entry.m_LanePriority) entry.m_Count += 2;
                    entry.m_Prefab = data.m_NodeObjectPrefab;
                    utilities[i] = entry;
                }
            }
            foreach (var entry in utilities)
            {
                if (entry.m_Count == 2 && !entry.m_Vertical) continue;
                var local = default(Transform);
                local.m_Position.y = entry.m_Elevation;
                resolve(new MovableRoadObjectPlacement.Candidate(-1, original(entry.m_Prefab),
                    entry.m_Transform, local, entry.m_Transform, SubObjectFlags.AnchorCenter, 100), ref random);
            }
        }
        finally { lanes.Dispose(); }

        void End(float3 position, float3 tangent, Entity prefab, ref Random state)
        {
            var rotation = MathUtils.TryNormalize(ref tangent) ? quaternion.LookRotation(tangent, math.up()) : quaternion.identity;
            position.y += composition.m_SurfaceHeight.max;
            var transform = new Transform(position, rotation);
            resolve(new MovableRoadObjectPlacement.Candidate(-1, original(prefab), transform,
                default, transform, 0, 100), ref state);
        }
    }
}
