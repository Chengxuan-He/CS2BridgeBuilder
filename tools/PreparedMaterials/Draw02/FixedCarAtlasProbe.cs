using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using BridgeBuilder.Bridges;

public static class FixedCarAtlasProbe
{
    public static void Run()
    {
        try
        {
            var root = Environment.GetEnvironmentVariable("DRAW02_REPO");
            var native = new Colossal.IO.AssetDatabase.SurfaceAsset();
            var data = HardcodedNumbers.Load("Types_Draw02_Draw02FixedCarAtlasAppearance_cs").F32;
            int checks = 0, negativeChecks = 0, maximumTreeVisits = 0; float maxError = 0;
            for (var lod = 0; lod < 2; lod++)
            foreach (var delta in new[] { -2f, 0f, 2f, 5f })
            foreach (var yaw in new[] { 0f, 31f, 90f })
            {
                var pose = new Pose(new Vector3(3f, 2f, -7f), Quaternion.Euler(0f, yaw, 0f));
                var fallback = new MovableRoadGeometry.Partition(0);
                var original = new MovableRoadAppearance.Level(native,
                    new[] { new MovableExistingRoadSurface.Patch(Vector2.zero, Vector2.one) },
                    new[] { MovableRoadAppearance.Type.None }, fallback, fallback);
                var level = Draw02FixedCarAtlasAppearance.Apply(lod, original, delta, pose);
                int Count(MovableRoadGeometry.Partition node, int depth)
                {
                    if (depth > 64) throw new Exception("Appearance tree depth unexpectedly grew.");
                    return node.Negative == null ? 1 : 1 + Count(node.Negative, depth + 1) + Count(node.Positive, depth + 1);
                }
                var visits = Count(level.Carriageway, 0);
                if (visits > 1000) throw new Exception("Repeated fallback paths make partition validation excessive.");
                maximumTreeVisits = Math.Max(maximumTreeVisits, visits);
                var width = data[data.Length - 1];
                Vector3 Road(float x, float z) => Quaternion.Inverse(pose.rotation) *
                    (new Vector3(x * (width + delta) / width, 0f, z) - pose.position);
                int Domain(Vector3 point)
                {
                    var node = level.Carriageway;
                    while (node.Negative != null)
                    {
                        if (Math.Abs(node.Frame.determinant) < .000001f) throw new Exception("Singular appearance frame.");
                        node = node.Frame.MultiplyPoint3x4(point).z < 0 ? node.Negative : node.Positive;
                    }
                    return node.Domain;
                }
                for (var domain = 0; domain < 16; domain++)
                {
                    var r = (lod * 16 + domain) * 17;
                    var end = domain < 8 ? 1f : -1f;
                    var x = (data[r] + data[r + 1]) * .5f;
                    // A point inside the original cap or ramp, before any short
                    // road can occlude the ramp's final millimetres.
                    var low = data[r + 2] * x + data[r + 3];
                    var z = (low + data[r + 4]) * .5f * end;
                    var road = Road(x, z);
                    if (Domain(road) != domain + 1) throw new Exception("Incorrect finite cap/ramp domain.");
                    var patch = level.Patches[domain + 1];
                    if (!patch.SourceUv || patch.PreparedUnit || patch.RepeatU || patch.RepeatV || patch.Surface != native ||
                        level.SupportedTypes[domain + 1] != MovableRoadAppearance.Type.Asphalt)
                        throw new Exception("Finite identity, axis or support gate changed.");
                    var uv = new Vector2(Vector4.Dot(patch.U, new Vector4(road.x, road.y, road.z, 1f)),
                        Vector4.Dot(patch.V, new Vector4(road.x, road.y, road.z, 1f)));
                    var expected = new Vector2(data[r + 5] * x + data[r + 6], data[r + 7] * z + data[r + 8]);
                    maxError = Math.Max(maxError, (uv - expected).magnitude);
                    var direction = Quaternion.Inverse(pose.rotation) * Vector3.left;
                    if (patch.Tangent != new Vector4(direction.x, direction.y, direction.z, data[r + 16]))
                        throw new Exception("Native constant tangent changed.");
                    checks++;
                }
                foreach (var z in new[] { -89f, -81f, -78f, 0f, 78f, 81f, 89f })
                {
                    if (Domain(Road(1f, z)) != 0) throw new Exception("Finite cap/ramp route captured another domain.");
                    negativeChecks++;
                }
            }
            var result = $"PASS actual source mapper with Unity matrices: {checks} finite cap/ramp interiors; {negativeChecks} short/repeat/moving negative controls; two LODs, four widths, three poses. Native Surface/constant tangent and Asphalt-only gates retained. Maximum float projection discrepancy: {maxError:R} UV; expanded tree visits with one fallback leaf: {maximumTreeVisits}. This is not full production Compose or cold-load validation.";
            File.WriteAllText(root + "/artifacts/material-remap/Draw02/fixed-car/finite-mapper-check.txt", result);
            Debug.Log(result); EditorApplication.Exit(0);
        }
        catch (Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
}
