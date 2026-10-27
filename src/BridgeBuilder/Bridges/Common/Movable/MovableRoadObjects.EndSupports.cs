using System;
using System.Collections.Generic;
using Game.Net;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

internal static partial class MovableRoadObjects
{
    private static void AddEndSupports(ref IReadOnlyList<ObjectSubObjectInfo> placements,
        Pose frame, ref IReadOnlyList<MovableRoadGeometry.Partition> partitions, Func<int, bool> moving)
    {
        var output = new List<ObjectSubObjectInfo>(placements);
        var domains = new List<MovableRoadGeometry.Partition>(partitions);
        var rotation = frame.rotation.normalized;
        var inverse = Quaternion.Inverse(rotation);
        var world = World.DefaultGameObjectInjectionWorld;
        var prefabs = world.GetExistingSystemManaged<PrefabSystem>();
        var manager = world.EntityManager;
        for (var i = 0; i < placements.Count; i++)
        {
            var source = placements[i];
            var utility = source.m_Object.GetComponent<UtilityObject>();
            if (utility == null || (utility.m_UtilityType &
                (UtilityTypes.Catenary | UtilityTypes.LowVoltageLine)) == 0) continue;
            var point = rotation * (Vector3)source.m_Position + frame.position;
            var facing = rotation * (Quaternion)source.m_Rotation;
            if (!prefabs.TryGetEntity(source.m_Object, out var entity) ||
                !manager.HasComponent<ObjectGeometryData>(entity))
                throw new InvalidOperationException("End support has no native object bounds: " + source.m_Object.name);
            var bounds = manager.GetComponentData<ObjectGeometryData>(entity).m_Bounds;
            // Equal inward clearance at both ends, including the entire native crossarm.
            var clearance = 0f;
            for (var corner = 0; corner < 8; corner++)
            {
                var local = new Vector3((corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z);
                clearance = Math.Max(clearance, Math.Abs((facing * local).z));
            }
            clearance += 0.001f;
            foreach (var station in MovableAttachmentStations.Ends(point, partitions[i], moving, clearance))
            {
                var target = new Vector3(point.x, point.y, station);
                var exists = false;
                foreach (var candidate in output)
                {
                    if (candidate.m_Object != source.m_Object) continue;
                    var other = rotation * (Vector3)candidate.m_Position + frame.position;
                    if (Math.Abs(other.x - target.x) < 0.001f && Math.Abs(other.y - target.y) < 0.001f &&
                        Math.Abs(other.z - target.z) < clearance)
                    { exists = true; break; }
                }
                if (exists) continue;
                output.Add(new ObjectSubObjectInfo
                {
                    m_Object = source.m_Object, m_Position = inverse * (target - frame.position),
                    m_Rotation = source.m_Rotation, m_GroupIndex = source.m_GroupIndex,
                    m_Probability = source.m_Probability
                });
                domains.Add(partitions[i]);
            }
        }
        placements = output;
        partitions = domains;
    }
}
