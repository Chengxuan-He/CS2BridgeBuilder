using System.Collections.Generic;
using Unity.Mathematics;

namespace BridgeBuilder.Runtime;

internal static class BridgePreviewCourseDomain
{
    // Select whole native occurrences, not clipped meshes. Adjacent intervals form a union.
    internal static bool Contains(IReadOnlyList<float2> intervals, float anchor)
    {
        foreach (var interval in intervals)
            if (anchor >= math.min(interval.x, interval.y) && anchor <= math.max(interval.x, interval.y))
                return true;
        return false;
    }
}
