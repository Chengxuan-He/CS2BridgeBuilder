using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

// Multiple road occurrences/decks may carry separate slots for the exact same
// borrowed Surface. Use its first slot for batching, without comparing names,
// texture contents or CIDs and without changing triangle occurrence/bone data.
internal static class MovableMaterialSlots
{
    internal static int[] Canonical<T>(IReadOnlyList<T> surfaces) where T : class
    {
        var result = new int[surfaces.Count];
        for (var slot = 0; slot < surfaces.Count; slot++)
        {
            if (surfaces[slot] == null)
                throw new ArgumentException("Moving geometry has an unresolved surface slot.", nameof(surfaces));
            result[slot] = slot;
            for (var previous = 0; previous < slot; previous++)
                if (ReferenceEquals(surfaces[slot], surfaces[previous]))
                {
                    result[slot] = result[previous];
                    break;
                }
        }
        return result;
    }
}
