using System;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

internal static class Lift02ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string name,
        RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content,
        float halfWidthDelta, MovableStraightSample road)
    {
        // The recorded Lift02 mechanism has no authored ObjectSubObjects.
        // Keep the native three render slots and original procedural skeleton.
        var result = factory.CreateMovableObject(name, false,
            Lift02MechanismData.Meshes(new[] { mechanism, column, footing }),
            Lift02MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>),
            target =>
            {
                Lift02MechanismData.ApplyComponents(target, content);
                target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * 2f;
            });

        var physical = Lift02RoadPartitions.Carriageway;
        var objects = road.Objects();
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        factory.AttachMovablePaint(result, road.Road.LaneGeometry.Decals, road.RoadToObject,
            physical, MovableRoadGeometry.InFrame(road.Road.Triangles, road.RoadToObject));
        return result;
    }
}
