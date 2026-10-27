using System;
using BridgeBuilder.Bridges;
using UnityEngine;

internal static class NativeSamplingTests
{
    private static int Main()
    {
        int passed = 0;
        void Check(bool value, string description)
        { if (!value) throw new Exception(description); passed++; }
        MovableStructurePacking.Unit Unit(bool triangle, params Vector2[] points)
        {
            var unit = new MovableStructurePacking.Unit { Triangle = triangle,
                Bounds = new Vector4(.25f,.25f,.5f,.5f),
                A = new Vector2(.25f,.25f), B = new Vector2(.5f,.25f), C = new Vector2(.25f,.5f) };
            unit.Points.AddRange(points); return unit;
        }
        foreach (bool triangle in new[] { false, true })
        {
            var unit = Unit(triangle, new Vector2(2.125f,-2.875f),
                new Vector2(2.5f,-2.875f), new Vector2(2.125f,-2.5f));
            Check(PedestrianDraw02NativeSampling.TrySourcePeriod(unit, out var source, out var phase), "Single source period rejected");
            Check(phase.x == 2 && phase.y == -3, "Incorrect constant phase");
            foreach (var point in unit.Points)
            {
                var result = source.SourceUv(point - phase);
                var expected = unit.SourceUv(new Vector2(point.x - MathF.Floor(point.x), point.y - MathF.Floor(point.y)));
                Check(result.x == expected.x && result.y == expected.y, "Native source coordinate differs");
            }
            // Sample a triangle interior: one affine map must agree throughout,
            // not merely at the corners as vertex-only modulo would do.
            var middle = (unit.Points[0] + unit.Points[1] + unit.Points[2]) / 3;
            var mapped = source.SourceUv(middle - phase);
            var reference = unit.SourceUv(new Vector2(middle.x - MathF.Floor(middle.x), middle.y - MathF.Floor(middle.y)));
            Check(mapped.x == reference.x && mapped.y == reference.y, "Interior mapping differs");
        }
        Check(!PedestrianDraw02NativeSampling.TrySourcePeriod(Unit(false,new(1.875f,2.125f),new(2.125f,2.125f),new(1.875f,2.5f)),out _,out _), "Cross-period face accepted");
        Check(!PedestrianDraw02NativeSampling.TrySourcePeriod(Unit(true,new(2.75f,2.75f),new(2.8f,2.75f),new(2.75f,2.8f)),out _,out _), "Reflected triangle accepted");
        Check(!PedestrianDraw02NativeSampling.TrySourcePeriod(Unit(false,new(2.125f,2.125f),new(3,2.125f),new(2.125f,2.5f)),out _,out _), "Exact wrap boundary accepted");
        Check(!PedestrianDraw02NativeSampling.TrySourcePeriod(Unit(false,new(float.NaN,2),new(2,2),new(2,3)),out _,out _), "Non-finite input accepted");
        Check(!PedestrianDraw02NativeSampling.TrySourcePeriod(Unit(false,new(.125f,.125f),new(.5f,.125f),new(.125f,.5f)),out _,out _), "Existing identity route intercepted");
        var conditioned = Unit(false,new(.125f,.125f),new(.5f,.125f),new(.125f,.5f));
        conditioned.RepeatOnly = true;
        Check(PedestrianDraw02NativeSampling.TrySourcePeriod(conditioned,out _,out var zero) && zero.x == 0 && zero.y == 0,
            "Already conditioned coordinates were shifted twice or rejected");
        Console.WriteLine($"PASS {passed} native source-period arithmetic checks; actual Unity vector types. Native rendering/mips remain separate acceptance.");
        return 0;
    }
}
