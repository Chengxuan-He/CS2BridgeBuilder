using System.Collections.Generic;
using BridgeBuilder.Bridges;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Runtime;

internal static partial class BridgePreviewScene
{
    private static async System.Threading.Tasks.Task<bool> AddPieceObjectsAsync(
        BridgePreviewObjectCourse course, Matrix4x4 transform, float elevation,
        BridgePreviewDrawList draws)
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var prefabs = world.GetExistingSystemManaged<PrefabSystem>();
        using var environment = new MovableSamplingEnvironment(world);
        MovableNodeObjects.PreviewSelection Selection()
        {
            var selector = new MovableRoadObjectSelection(world.EntityManager, prefabs, environment.Theme);
            var groups = new Dictionary<ObjectPrefab, Unity.Mathematics.Random>();
            ObjectSubObjectInfo? Select(MovableRoadObjectPlacement.Candidate candidate,
                MovableRoadObjectSelection.TrafficSignNeeds needs, ref Unity.Mathematics.Random random)
            {
                if (prefabs.TryGetEntity(candidate.Prefab, out _))
                    return selector.TryResolve(candidate, needs, elevation, ref random, out var selected, preview: true)
                        ? selected : null;
                // Newly constructed structure objects have no published ECS identity.
                // Keep their authoring geometry and the native ordinary-object random consumption.
                var objectRandom = random;
                random.NextInt(); random.NextInt();
                if (groups.TryGetValue(candidate.Prefab, out var grouped)) objectRandom = grouped;
                else groups.Add(candidate.Prefab, objectRandom);
                if (objectRandom.NextInt(100) >= candidate.Probability) return null;
                var position = (Vector3)candidate.Object.m_Position;
                if (candidate.Prefab is ObjectGeometryPrefab geometry)
                {
                    draws.TryObjectLayout(geometry, out var layout);
                    layout.Align(ref position, (candidate.Flags & SubObjectFlags.AnchorTop) != 0,
                        (candidate.Flags & SubObjectFlags.AnchorCenter) != 0,
                        (candidate.Flags & SubObjectFlags.OnGround) != 0, 0);
                }
                return new ObjectSubObjectInfo { m_Object = candidate.Prefab, m_Position = position,
                    m_Rotation = candidate.Object.m_Rotation, m_Probability = 100 };
            }
            return Select;
        }
        // Native selection reads theme, traffic lanes and object metadata and must stay
        // on the native thread. Geometry decoding and draw preparation remain asynchronous.
        var placements = course.Objects(Selection);
        foreach (var placement in placements)
        {
            if (placement.m_Object is not ObjectGeometryPrefab geometry) continue;
            var position = (Vector3)placement.m_Position;
            position.y -= elevation;
            draws.TryObjectLayout(geometry, out var layout);
            var stack = layout.InitialStack();
            if (!await draws.AddObjectAsync(geometry,
                transform * Matrix4x4.TRS(position, placement.m_Rotation, Vector3.one), stack,
                BridgePreviewObjectLayout.ChildElevation(elevation, position.y))) return false;
        }
        return true;
    }
}
