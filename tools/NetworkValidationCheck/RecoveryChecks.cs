using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;

internal static class RecoveryChecks
{
    internal static void Run(Action<string, bool> check)
    {
        var root = Path.Combine(Path.GetTempPath(), "BridgeReferenceCheck-" + Guid.NewGuid().ToString("N"));
        UnityEngine.Application.persistentDataPath = root;
        const string name = "RBBridgeDep_Lane-b11111111-1111-1111-1111-111111111111";
        const string identity = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        var directory = Path.Combine(root, "ImportedData", name);
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, name + ".Prefab");
        string Source(string entries) => $$$$"""
        {"$id":0,"$type":"0|Game.Prefabs.NetLanePrefab, Game","name":"{{{{name}}}}",
        "components":{"$id":1,"$rcontent":[{"$id":2,"$type":"1|Game.Prefabs.SecondaryLane, Game",
        "m_LeftLanes":{"$id":3,"$rcontent":[{{{{entries}}}}]}}]}}
        """;
        var encoded = Source("{\"m_Lane\":$fstrref:\"CID:" + identity + "\"}");
        File.WriteAllText(path, encoded);
        var lane = new NetLanePrefab { name = name, asset = new PrefabAsset { path = path } };
        var component = new SecondaryLane { m_LeftLanes = [new()] };
        lane.Add(component);
        var prefabs = new PrefabSystem();
        BridgeReferenceRecovery.Result Repair() => BridgeReferenceRecovery.Repair(lane, prefabs, out _);
        check("serialized CID parsed", BridgeSerializedReferences.TryRead(encoded, out var source)
            && source.Reference("Game.Prefabs.SecondaryLane", "m_LeftLanes", 0, "m_Lane", out var id) && id == "CID:" + identity);
        check("not installed is deferred, never pruned", Repair() == BridgeReferenceRecovery.Result.Deferred && component.m_LeftLanes.Length == 1);
        AssetDatabase.global.Assets[identity] = new PrefabAsset();
        check("asset known but not loaded stays deferred", Repair() == BridgeReferenceRecovery.Result.Deferred);
        var restored = new NetLanePrefab { name = "external healthy lane", isBuiltin = true };
        AssetDatabase.global.Assets[identity].Instance = restored;
        var originalDescriptor = component.m_LeftLanes[0];
        check("late loaded exact CID restored", Repair() == BridgeReferenceRecovery.Result.Healthy
            && ReferenceEquals(component.m_LeftLanes[0].m_Lane, restored));
        check("descriptor copy does not modify shared source", originalDescriptor.m_Lane == null);
        check("repair does not rewrite original Odin", File.ReadAllText(path) == encoded);
        component.m_LeftLanes = [new()];
        prefabs.Registered.Add(lane);
        check("registered prefab is not mutated", Repair() == BridgeReferenceRecovery.Result.Deferred && component.m_LeftLanes[0].m_Lane == null);
        prefabs.Registered.Clear();
        lane.isReadOnly = true;
        check("shared asset is not mutated", Repair() == BridgeReferenceRecovery.Result.Deferred);
        lane.isReadOnly = false;
        File.WriteAllText(path, Source("{\"m_Lane\":null}"));
        check("proven null secondary descriptor pruned", Repair() == BridgeReferenceRecovery.Result.Healthy && component.m_LeftLanes.Length == 0);
        component.m_LeftLanes = [null];
        File.WriteAllText(path, Source("null"));
        check("proven null descriptor pruned", Repair() == BridgeReferenceRecovery.Result.Healthy && component.m_LeftLanes.Length == 0);
        component.m_LeftLanes = [new(), new()];
        File.WriteAllText(path, Source("{\"m_Lane\":null},{\"m_Lane\":$fstrref:\"CID:bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\"}"));
        check("deferred sibling prevents index-shifting partial edits", Repair() == BridgeReferenceRecovery.Result.Deferred && component.m_LeftLanes.Length == 2);
        File.WriteAllText(path, "not Odin");
        check("unreadable serialization retained", Repair() == BridgeReferenceRecovery.Result.Deferred && component.m_LeftLanes.Length == 2);
        check("truncated document rejected", !BridgeSerializedReferences.TryRead("{\"name\":", out _));
        var reused = Source("{\"$id\":5,\"m_Lane\":$fstrref:\"UnityGUID:1234\"},$iref:5");
        check("Odin internal reference preserved", BridgeSerializedReferences.TryRead(reused, out source)
            && source.Reference("Game.Prefabs.SecondaryLane", "m_LeftLanes", 1, "m_Lane", out id) && id == "UnityGUID:1234");
        check("duplicate object IDs rejected", !BridgeSerializedReferences.TryRead(Source("{\"$id\":5},{\"$id\":5}"), out _));
        component.m_LeftLanes = [new()];
        File.WriteAllText(path, Source("{\"m_Lane\":$fstrref:\"UnityGUID:1234\"}"));
        AssetDatabase.global.resources.prefabsMap.Objects["1234"] = restored;
        check("Unity GUID exact lookup", Repair() == BridgeReferenceRecovery.Result.Healthy && component.m_LeftLanes[0].m_Lane == restored);
        component.m_LeftLanes = [new()];
        File.WriteAllText(path, Source("{\"m_Lane\":$fstrref:\"CID:" + identity + "\"}"));
        AssetDatabase.global.Assets[identity] = new PrefabAsset { database = AssetDatabase.user, path = Path.Combine(root, "confirmed-absent.Prefab") };
        check("confirmed missing user file can remove auxiliary entry", Repair() == BridgeReferenceRecovery.Result.Healthy && component.m_LeftLanes.Length == 0);
        var networkName = "b22222222-2222-2222-2222-222222222222";
        var networkDirectory = Path.Combine(root, "ImportedData", networkName);
        Directory.CreateDirectory(networkDirectory);
        var networkPath = Path.Combine(networkDirectory, networkName + ".Prefab");
        File.WriteAllText(networkPath, "{\"name\":\"" + networkName + "\",\"m_Sections\":{\"$rcontent\":[{\"m_Section\":null}]}}");
        var network = new NetGeometryPrefab { name = networkName, asset = new() { path = networkPath }, m_Sections = [new()] };
        check("essential section is not silently pruned into partial bridge", BridgeReferenceRecovery.Repair(network, prefabs, out _) == BridgeReferenceRecovery.Result.Broken && network.m_Sections.Length == 1);
        Console.WriteLine("Read-only recovery fixtures retained: " + root);
    }
}
