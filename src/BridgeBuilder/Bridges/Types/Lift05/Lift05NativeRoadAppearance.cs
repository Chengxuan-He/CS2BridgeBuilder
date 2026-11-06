using System;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using UnityEngine;
using Patch = BridgeBuilder.Bridges.MovableExistingRoadSurface.Patch;
using Partition = BridgeBuilder.Bridges.MovableRoadGeometry.Partition;

namespace BridgeBuilder.Bridges;
// Measured continuous unit phase; only genuine gradient, finite and motion boundaries remain.
internal static class Lift05NativeRoadAppearance
{
    private sealed class LevelData
    {
        internal readonly int[] Roots, Bands, Units, Children;
        internal readonly double[][] Patches, Planes;
        internal LevelData(int[] roots, int[] bands, int[] units, int[] children, double[][] patches, double[][] planes)
        {
            Roots = roots;
            Bands = bands;
            Units = units;
            Children = children;
            Patches = patches;
            Planes = planes;
        }
    }

    private static class Data
    {
        static Data()
        {
        }

        internal static LevelData[] Levels => GenerationParameters.Get<LevelData[]>("BridgeBuilder.Bridges.Lift05NativeRoadAppearance.Data.Levels", () => Read());
        private static LevelData[] Read()
        {
            var d = HardcodedNumbers.Load("Types_Lift05_RoadProjection");
            int i = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[0], j = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[1];
            if (d.I32[i++] != Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[2] || d.I32[i++] != Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[3])
                throw new InvalidOperationException("Invalid Lift05 continuous road schema.");
            var levels = new LevelData[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[4]];
            for (int lod = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[5]; lod < levels.Length; lod++)
            {
                int count = d.I32[i++], nodes = d.I32[i++];
                var roots = new[]
                {
                    d.I32[i++],
                    d.I32[i++],
                    d.I32[i++]
                };
                var bands = new int[count];
                var units = new int[count];
                var patches = new double[count][];
                var planes = new double[nodes][];
                var children = new int[nodes * Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[6]];
                for (int p = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[7]; p < count; p++)
                {
                    bands[p] = d.I32[i++];
                    units[p] = d.I32[i++];
                    patches[p] = new double[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[8]];
                    for (int k = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[9]; k < Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[10]; k++)
                        patches[p][k] = d.F64[j++];
                }

                for (int n = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[11]; n < nodes; n++)
                {
                    children[n * Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[12]] = d.I32[i++];
                    children[n * Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[13] + Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[14]] = d.I32[i++];
                    planes[n] = new[]
                    {
                        d.F64[j++],
                        d.F64[j++],
                        d.F64[j++]
                    };
                }

                levels[lod] = new LevelData(roots, bands, units, children, patches, planes);
            }

            if (i != d.I32.Length || j != d.F64.Length)
                throw new InvalidOperationException("Unconsumed Lift05 continuous road metadata.");
            return levels;
        }
    }

    private static SurfaceAsset Resolve(int unit) => BridgePreparedMaterials.RepeatUnit("Lift05", unit switch
    {
        1 => "18d35ed1e221c7a1a1cad597b22896ce5146eb6050095b2b86fd3da7605cbd82",
        3 => "40d7c50979f1502da28972cacf93d06805317986a0d1b1cc6b1faba7ada71284",
        4 => "3061b3f27e2581a706363c6b7c7b86b5fe6fb0aba3e69c87a1b1f6318967ebec",
        5 => "7a355e468b816e195c561191ac1c5931847c874f2d0cbec74482c662ca3fe6f9",
        6 => "240850aa002f85d71ce250f44328a8ab275e0542717942cee71af30bb82f5042",
        _ => throw new InvalidOperationException("Unknown Lift05 prepared road unit.")});
    // Optional finished assets permit isolated native fixture injection; normal creation
    // resolves the identical packaged graph on this existing main-thread native path.
    internal static MovableRoadAppearance.Level ForLod(int lod, RenderPrefab native, Pose roadToObject, float halfDelta, SurfaceAsset[]? finishedUnits = null)
    {
        if (lod < Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[15] || lod >= Data.Levels.Length || !float.IsFinite(halfDelta))
            throw new InvalidOperationException("Invalid Lift05 road arithmetic or LOD.");
        var data = Data.Levels[lod];
        var surface = native.GetSurfaceAsset(Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[16]);
        var pose = Matrix4x4.TRS(roadToObject.position, roadToObject.rotation, Vector3.one);
        // No inverse exists at zero span; keep geometry with Original appearance.
        if (Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.F64[0] + halfDelta == Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[17])
            return new MovableRoadAppearance.Level(surface, new[] { new Patch(Vector2.zero, Vector2.one) }, new[] { MovableRoadAppearance.Type.None }, new Partition(Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[18]), new Partition(Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[19]));
        var patches = new Patch[data.Patches.Length + Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[20]];
        var supported = new MovableRoadAppearance.Type[patches.Length];
        patches[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[21]] = new Patch(Vector2.zero, Vector2.one);
        var finished = new SurfaceAsset[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[22]];
        foreach (int unit in data.Units)
            if (unit >= Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[23] && finished[unit] == null)
                finished[unit] = finishedUnits == null ? Resolve(unit) : finishedUnits[unit];
        double Scale(int band) => band == Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[24] ? Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.F64[1] / (Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.F64[2] + halfDelta) : Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.F64[3];
        double Offset(int band) => band == Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[25] ? halfDelta : band == Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[26] ? -halfDelta : Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.F64[4];
        for (int p = Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[27]; p < data.Patches.Length; p++)
        {
            var v = data.Patches[p];
            int band = data.Bands[p], unit = data.Units[p];
            // Offline pure road periods keep their measured physical gradient
            // and shared object-origin phase; width changes only their domain.
            double scale = unit >= Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[28] ? Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[29] : Scale(band), offset = unit >= Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[30] ? Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[31] : Offset(band);
            var u = pose.transpose * new Vector4((float)(v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[32]] * scale), Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[33], (float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[34]], (float)(v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[35]] + v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[36]] * offset));
            var w = pose.transpose * new Vector4((float)(v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[37]] * scale), Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[38], (float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[39]], (float)(v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[40]] + v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[41]] * offset));
            var min = new Vector2((float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[42]], (float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[43]]);
            var max = new Vector2((float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[44]], (float)v[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[45]]);
            patches[p + Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[46]] = unit < Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[47] ? new Patch(min, max, u, w, surface, sourceUv: true, projectTangent: true) : new Patch(min, max, u, w, finished[unit], preparedUnit: true, repeatU: unit == Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[48] || unit >= Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[49], repeatV: true, projectTangent: true).WithFiniteSampling(Vector2.zero, Vector2.one);
            supported[p + Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[50]] = MovableRoadAppearance.Type.Asphalt;
        }

        Partition Plane(double x, double z, double constant, Partition negative, Partition positive)
        {
            var matrix = Matrix4x4.identity;
            matrix.m00 = (float)z;
            matrix.m02 = (float)-x;
            matrix.m20 = (float)x;
            matrix.m22 = (float)z;
            matrix.m23 = (float)constant;
            return new Partition(matrix * pose, negative, positive);
        }

        Partition Tree(int node, int band)
        {
            if (node < Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[51])
                return new Partition(-node - Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[52]);
            var line = data.Planes[node];
            return Plane(line[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[53]] * Scale(band), line[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[54]], line[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[55]] + line[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[56]] * Offset(band), Tree(data.Children[node * Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[57]], band), Tree(data.Children[node * Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[58] + Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[59]], band));
        }

        var regions = Plane(Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[60], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[61], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[62] + halfDelta, Tree(data.Roots[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[63]], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[64]), Plane(Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[65], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[66], -Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[67] - halfDelta, Tree(data.Roots[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[68]], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[69]), Tree(data.Roots[Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[70]], Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values.I32[71])));
        return new MovableRoadAppearance.Level(surface, patches, supported, regions, regions);
    }
}

// Embedded numerical arrays: Types_Lift05_Lift05NativeRoadAppearance_cs
internal static class Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs
{
    static Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs()
    {
    }

    internal static HardcodedNumbers Values => GenerationParameters.Get<HardcodedNumbers>("BridgeBuilder.Bridges.Numeric_Types_Lift05_Lift05NativeRoadAppearance_cs.Values", () => HardcodedNumbers.Load("Types_Lift05_Lift05NativeRoadAppearance_cs"));
}
