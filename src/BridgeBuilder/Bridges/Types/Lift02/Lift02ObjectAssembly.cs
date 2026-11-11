using System;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;
internal static class Lift02ObjectAssembly
{
    internal static StaticObjectPrefab Create(TowerFactory factory, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road) =>
        GenerationWork.Synchronous(() => CreateAsync(factory, name, mechanism, column, footing, content, halfWidthDelta, road));

    internal static async System.Threading.Tasks.Task<StaticObjectPrefab> CreateAsync(TowerFactory factory, string name, RenderPrefab mechanism, RenderPrefab column, RenderPrefab footing, ContentPrefab content, float halfWidthDelta, MovableStraightSample road)
    {
        await GenerationWork.Next("support Lift02ObjectAssembly.Create");
        // The recorded Lift02 mechanism has no authored ObjectSubObjects.
        // Keep the native three render slots and original procedural skeleton.
        var result = factory.CreateMovableObject(name, false, Lift02MechanismData.Meshes(new[] { mechanism, column, footing }), Lift02MechanismData.AuthoredAttachments(factory.ResolveOriginal<ObjectPrefab>), target =>
        {
            Lift02MechanismData.ApplyComponents(target, content);
            target.GetComponent<StandingObject>().m_LegSize.x += halfWidthDelta * Numeric_Types_Lift02_Lift02ObjectAssembly_cs.Values.F32[0];
        });
        var physical = Lift02RoadPartitions.Carriageway;
        var objects = road.Objects();
        var partitions = new MovableRoadGeometry.Partition[objects.Placements.Length];
        Array.Fill(partitions, physical);
        factory.AttachMovableRoadObjects(result, objects.Placements, road.RoadToObject, partitions);
        return result;
    }
}

// Embedded numerical arrays: Types_Lift02_Lift02ObjectAssembly_cs
internal static class Numeric_Types_Lift02_Lift02ObjectAssembly_cs
{
    static Numeric_Types_Lift02_Lift02ObjectAssembly_cs() {}
    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift02_Lift02ObjectAssembly_cs.Values", () => HardcodedNumbers.Load("Types_Lift02_Lift02ObjectAssembly_cs"));
}
