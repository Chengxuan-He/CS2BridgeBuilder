using System;
using System.Threading.Tasks;
namespace BridgeBuilder.Bridges;

// Common data-driven SIMD numerical entry point. Groups schedules complete
// independent mesh jobs; no per-vertex numerical callback API is available.
internal static partial class AffineGeometry
{
    [ThreadStatic] private static bool _worker;
    internal static void Groups(int count, Action<int> body)
    {
        if (_worker || count <= 1 || Environment.ProcessorCount <= 1)
        {
            for (int i = 0; i < count; i++) body(i);
            return;
        }
        var parameters = GenerationParameters.Capture();
        Parallel.For(0, count, new ParallelOptions
        { MaxDegreeOfParallelism = Math.Min(2, Math.Max(1, Environment.ProcessorCount - 1)) }, i =>
        {
            using var parameterLease = GenerationParameters.Attach(parameters);
            var previous = _worker;
            _worker = true;
            try { body(i); }
            finally { _worker = previous; }
        });
    }
    internal static void Execute<TVector>(HardcodedNumbers program, int entry, TVector[][] coordinates,
        float[][] scalars, float[] parameters, int first = 0, int count = -1, int[][]? indices = null) where TVector : struct
        => StructureCoordinateProgram.Execute(program, entry, coordinates, scalars, parameters, first, count, indices);

}
