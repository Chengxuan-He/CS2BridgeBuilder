using System;
using System.IO;
using System.Linq;
using BridgeBuilder.Bridges;
using UnityEditor;
using UnityEngine;

// Runs copies of the production partition and road clipping code in a private Unity project.
// This verifies geometry-to-appearance routing, not shader output or game acceptance.
public static class Draw01AppearancePartitionCheck
{
    public static void Run()
    {
        try
        {
            var pose = new Pose(Vector3.zero, Quaternion.identity);
            var road = Draw01RoadPartitions.MaterialRegions(false, pose);
            var walk = Draw01RoadPartitions.MaterialRegions(true, pose);
            var cases = new[] {
                (-5f, 40f, 1), (5f, 40f, 4), (-5f, -40f, 8), (5f, -40f, 11),
                (-5f, 85f, 2), (5f, 85f, 2), (-5f, -85f, 9), (5f, -85f, 9)
            };
            int verified = 0;
            foreach (var item in cases)
            {
                MovableRoadGeometry.Vertex V(float x, float z) => new MovableRoadGeometry.Vertex(
                    new Vector3(x, 9.8f, z), Vector3.up, new Vector4(1, 0, 0, 1),
                    new Vector4(x, z, 0, 0), Color.white);
                var face = new MovableRoadGeometry.Triangle(V(item.Item1, item.Item2),
                    V(item.Item1 + .1f, item.Item2), V(item.Item1, item.Item2 + .1f), 0, 1, 123);
                if (!MovableRoadGeometry.TryBind(new[] { face }, road, 14, 1, out var actual, out var error,
                        occurrencePartitions: new[] { road, walk }))
                    throw new Exception(error);
                if (actual.Length != 1 || actual[0].Bone != item.Item3 || actual[0].Geometry.Occurrence != 1 ||
                    actual[0].Geometry.SourceTriangle != 123 || actual[0].Geometry.A.UV != face.A.UV)
                    throw new Exception("Sidewalk appearance or original provenance was lost.");
                if (!MovableRoadGeometry.TryBind(new[] { face }, road, 14, 1, out var legacy, out error) ||
                    legacy.Any(t => t.Bone == item.Item3))
                    throw new Exception("The regression fixture did not distinguish the old carriageway-only route.");
                verified++;
            }
            var output = Argument("-report");
            File.WriteAllText(output, "{\"status\":\"PASS\",\"cases\":" + verified +
                ",\"productionGeometryAndPartition\":true,\"nativeRenderingVerified\":false}");
            Debug.Log("PASS Draw01 appearance partition: " + verified + " cases; original occurrence and UV preserved.");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    internal static string Argument(string key)
    {
        var args = Environment.GetCommandLineArgs();
        var index = Array.IndexOf(args, key);
        if (index < 0 || index + 1 == args.Length) throw new ArgumentException(key);
        return args[index + 1];
    }
}

namespace BridgeBuilder.Bridges
{
    // The unchanged production partition consumes the actual BBN payload; this isolated
    // reader only replaces assembly-resource lookup in the Unity script compilation.
    internal sealed class HardcodedNumbers
    {
        internal int[] I32;
        internal float[] F32;
        internal static HardcodedNumbers Load(string name)
        {
            using var stream = File.OpenRead(Path.Combine(Draw01AppearancePartitionCheck.Argument("-numbers"), name + ".bin"));
            using var r = new BinaryReader(stream);
            if (r.ReadUInt32() != 0x314e4242 || r.ReadInt32() != 1) throw new InvalidDataException("BBN schema");
            var counts = Enumerable.Range(0, 6).Select(_ => r.ReadInt32()).ToArray();
            var result = new HardcodedNumbers { I32 = Enumerable.Range(0, counts[0]).Select(_ => r.ReadInt32()).ToArray() };
            stream.Position += counts[1] * 4L + counts[2] * 8L + counts[3] * 8L;
            result.F32 = Enumerable.Range(0, counts[4]).Select(_ => r.ReadSingle()).ToArray();
            return result;
        }
    }
    internal static class MovableNodeObjects { internal sealed class Result { internal object[] Placements = Array.Empty<object>(); } }
}
