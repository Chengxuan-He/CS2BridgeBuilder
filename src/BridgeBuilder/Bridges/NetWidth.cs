using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Measures how wide a net prefab is by adding up its sections. Nets do not store a width, so this
/// is the only way to compare a road against the bridge that is about to be draped over it.
/// </summary>
internal static class NetWidth
{
    /// <summary>
    /// Total width in metres, or 0 when the prefab has nothing measurable. Sections that only exist
    /// in an elevated or raised state are included: on a bridge those are the deck edges, and
    /// leaving them out would make every bridge read as narrower than the road it carries.
    /// </summary>
    internal static float Of(NetGeometryPrefab? prefab)
    {
        if (prefab?.m_Sections == null) return 0f;

        var width = 0f;
        foreach (var section in prefab.m_Sections)
        {
            // A median section is drawn once per lane boundary rather than spanning the net, and a
            // half length section is a longitudinal split, not a lateral one.
            if (section?.m_Section == null || section.m_Median) continue;
            width += Of(section.m_Section);
        }

        return width;
    }

    /// <summary>
    /// The width of the road as a road: the whole net minus the outward extension and the sections
    /// that appear only once it is elevated.
    ///
    /// Medians, shoulders and footways all count - they are part of the road the tower has to span.
    /// What is excluded is the railings and deck edges a bridge grows and a ground level road does not,
    /// because those are the reason a bridge prefab measures wider than the road it carries.
    ///
    /// This is what makes a bridge comparable to a road. A bridge prefab's total width counts railings,
    /// shoulders and deck edges that a ground level road simply does not have, so subtracting it from a
    /// tower's width answers the wrong question - it was measuring the tower against the bridge's whole
    /// footprint rather than against the road the tower spans. The game's own SuspensionBridge03 comes
    /// out at 52 m that way against a 50.4 m tower, a clearance of minus 1.6 m, which is how a road
    /// ended up allowed a tower no wider than itself.
    /// </summary>
    internal static float RoadSurfaceOf(NetGeometryPrefab? prefab)
    {
        return RoadSurfaceOf(prefab, null);
    }

    /// <summary>
    /// The same measurement, optionally writing down what each section contributed.
    ///
    /// The breakdown exists because the total on its own is unarguable and unverifiable at the same
    /// time: a road that should be 40 m reading as 42 m says nothing about which section is wrong.
    /// With the breakdown the answer is a line in the report rather than a session of guessing.
    /// </summary>
    internal static float RoadSurfaceOf(NetGeometryPrefab? prefab, ICollection<string>? breakdown)
    {
        if (prefab?.m_Sections == null) return 0f;

        var width = 0f;
        foreach (var section in prefab.m_Sections)
        {
            if (section?.m_Section == null) continue;

            var name = section.m_Section.name;
            if (IsElevatedOnly(section))
            {
                breakdown?.Add(name + " (elevated only, skipped)");
                continue;
            }

            if (IsSide(section))
            {
                breakdown?.Add(string.Format(
                    CultureInfo.InvariantCulture, "{0} (outward extension, skipped {1:0.#})",
                    name, Of(section.m_Section)));
                continue;
            }

            var contribution = Of(section.m_Section);
            width += contribution;

            // Where the section sits as well as how wide it is. The total alone cannot say where a
            // deck's edge is against its kerb line, and on a bridge whose towers straddle the whole
            // deck rather than standing outside the carriageway that is the difference between the
            // width the tower is sized to and the width the road reports.
            //
            // Cumulative from the left, so the numbers read as a cross section. They are turned into
            // offsets from the centre line once the total is known, which is why they are recorded
            // here and adjusted below.
            breakdown?.Add(string.Format(
                CultureInfo.InvariantCulture, "{0}={1:0.#}@{2:0.#}", name, contribution, width));
        }

        return width;
    }

    /// <summary>
    /// Width drawn outside the measured road surface by its two edge-extension sections.
    ///
    /// This is road-composition data, not generated-mesh inference. TrussArchBridge02's outer deck
    /// frame follows these visible edges with its recorded fit adjustment, while its inner arch
    /// follows the outermost footway boundary. Other bridge families continue to size themselves from
    /// <see cref="RoadSurfaceOf"/>.
    /// </summary>
    internal static float OutwardExtensionOf(NetGeometryPrefab? prefab)
    {
        if (prefab?.m_Sections == null) return 0f;

        var width = 0f;
        foreach (var section in prefab.m_Sections)
        {
            if (section?.m_Section == null || section.m_Median || !IsSide(section)) continue;
            width += Of(section.m_Section);
        }

        return width;
    }

    /// <summary>Whether a section is only drawn once the net is elevated or raised.</summary>
    private static bool IsElevatedOnly(NetSectionInfo section)
    {
        return Mentions(section.m_RequireAll) || Mentions(section.m_RequireAny);
    }

    /// <summary>See <see cref="SectionNames.IsSide"/> - the rule lives there so it can be tested.</summary>
    private static bool IsSide(NetSectionInfo section)
    {
        return SectionNames.IsSide(section.m_Section?.name);
    }

    private static bool Mentions(NetPieceRequirements[]? requirements)
    {
        if (requirements == null) return false;
        foreach (var requirement in requirements)
        {
            if (requirement is NetPieceRequirements.Elevated or NetPieceRequirements.Raised) return true;
        }

        return false;
    }

    /// <summary>
    /// A section's width is the widest of its pieces, not their sum: pieces at the same position are
    /// layers - surface, markings, side - drawn on top of each other.
    /// </summary>
    internal static float Of(NetSectionPrefab? section)
    {
        if (section == null) return 0f;
        return Of(section, new HashSet<NetSectionPrefab>(), 0);
    }

    private static float Of(NetSectionPrefab section, HashSet<NetSectionPrefab> seen, int depth)
    {
        // Sub sections can in principle be cyclic, and nothing in the game guarantees they are not.
        if (depth > 8 || !seen.Add(section)) return 0f;

        var width = 0f;
        if (section.m_Pieces != null)
        {
            foreach (var piece in section.m_Pieces)
            {
                if (piece?.m_Piece == null) continue;
                width = Math.Max(width, piece.m_Piece.m_Width);
            }
        }

        if (width > 0f) return width;

        // Sections that are pure containers carry their width in their sub sections instead.
        if (section.m_SubSections != null)
        {
            foreach (var sub in section.m_SubSections)
            {
                if (sub?.m_Section == null) continue;
                width = Math.Max(width, Of(sub.m_Section, seen, depth + 1));
            }
        }

        return width;
    }
}
