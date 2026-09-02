using System;

namespace BridgePrefabGenerator.Bridges;

/// <summary>
/// What a net section's name says about it.
///
/// Split out from <see cref="NetWidth"/> so it can be tested without the game. The measurement itself
/// needs live prefabs, but the rule that decides which sections count is a string rule, and a string
/// rule that is nearly right is worse than one that is obviously wrong - it produces a plausible
/// number instead of an error.
/// </summary>
internal static class SectionNames
{
    /// <summary>
    /// Whether a section is the net's outward extension rather than part of its surface.
    ///
    /// Every road carries one of these at each end - "Alley Side 0", "Highway Side 0", "Train Side 0"
    /// and so on, the strip that blends the net into the terrain beside it. Road Builder appends them
    /// too, one before the lanes and one after, unless the user turns them off. They are not road: you
    /// cannot drive on them and a tower does not have to span them, which is what the deck width asks.
    /// Counting them made a road of eleven lanes measure thirteen sections and 40 m of road read as
    /// 42 m, which then took a 42 m tower.
    ///
    /// Matched on the whole word, never the substring. "Sidewalk" and "Sidewalk 3.5" begin with the
    /// same four letters and are road - a footway is part of what a tower spans - so a substring test
    /// would quietly drop the pavements from every road it measured.
    ///
    /// Split on spaces only, and that is a limit worth knowing: Road Builder names its sections with
    /// underscores - "RBBridgeDep_NetSectionPrefab_Sidewalk_4_2fb39c7713" - so a Road Builder side
    /// section would not be recognised here. None has been seen; every road measured so far carries
    /// the game's own "Highway Side 0". Widening the split is a change to what every width means, so
    /// it waits for a road that needs it rather than being done on the strength of the observation.
    /// </summary>

    internal static bool IsSide(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        foreach (var word in name!.Split(' '))
        {
            if (string.Equals(word, "Side", StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>
    /// Whether a section is a footway.
    ///
    /// The golden bridge's inner railing stands at the kerb line, so the space between it and the
    /// outer one is the footway's width - and where there is no footway there is nothing for it to
    /// stand at the inside of, so the archetype has none there.
    ///
    /// Matched on the whole word, like <see cref="IsSide"/> and for the same reason. The game's
    /// footways are "Sidewalk 3.5", "Sidewalk 3.5 - NoBicycle", "Sidewalk With Bikelane 3.5"; a
    /// substring test would also catch anything else with those eight letters in it, and a rule that
    /// is nearly right produces a plausible railing position instead of an obvious error.
    /// </summary>
    internal static bool IsSidewalk(string? name)
    {
        if (string.IsNullOrEmpty(name)) return false;

        foreach (var word in name!.Split(' '))
        {
            if (string.Equals(word, "Sidewalk", StringComparison.Ordinal)) return true;
        }

        return false;
    }

}
