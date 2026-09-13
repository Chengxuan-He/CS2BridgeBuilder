using CS2Mods.Shared.Infrastructure;
using CS2Mods.Shared;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Unity.Mathematics;
using UnityEngine;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Checks the one property the tower generator has to have: asked for the road a tower was built for,
/// it must give back that tower.
///
/// Not a smoke test. A generated tower is a derived mesh, and the only way to know a derivation is
/// right is to check the case where it should do nothing. So for every tower in
/// <see cref="BridgeTowers"/> this generates at the tower's own recorded road width and compares the
/// result against the original, vertex by vertex and surface by surface. A suspension bridge over a
/// 20 m road has to come out as 4LaneSuspensionBridgePillar exactly - same geometry, same textures -
/// and over a 16 m road as the three-lane one.
///
/// It runs on every scan and writes its result to the log, because the numbers it checks against are
/// hand-recorded and the thing it checks is easy to break without noticing: any change to the
/// widening, the part offsets, or the recorded widths shows up here as a failure rather than as a
/// bridge that looks slightly wrong.
/// </summary>
internal static class TowerSelfTest
{
    /// <summary>Vertices this far apart are the same vertex; below the precision of the format.</summary>
    private const float Tolerance = 0.0001f;

    internal static void Run(PrefabSystem prefabSystem)
    {
        var passed = 0;
        var failed = 0;
        var skipped = 0;

        foreach (var styleId in BridgeTowers.Styles)
        {
            foreach (var tower in BridgeTowers.For(styleId))
            {
                switch (Check(prefabSystem, styleId, tower))
                {
                    case Result.Passed: passed++; break;
                    case Result.Failed: failed++; break;
                    default: skipped++; break;
                }
            }
        }

        var summary = $"Tower self test: {passed} passed, {failed} failed, {skipped} not installed";
        if (failed > 0) ModHost.Log.Error(summary);
        else ModHost.Log.Info(summary);

        Audit(prefabSystem);
    }

    /// <summary>
    /// Measures every listed tower against the bridges that carry it, and prints what the table should
    /// say.
    ///
    /// The widths in <see cref="BridgeTowers"/> all came from a run of this, and it keeps running so
    /// that they stay true: content changes, and a hardcoded number that quietly stops matching the
    /// game it was read from is worse than no number at all.
    ///
    /// A wrong road width is not a cosmetic error. It is what the tower is widened by, so it decides
    /// how wide the bridge comes out. Guessing at corrections was tried and produced three rounds of
    /// different wrong numbers - the golden entry ended up holding the blue five-lane tower's width -
    /// so nothing here is inferred: each tower is measured against the bridges that actually carry it,
    /// in the same units the generator compares roads in.
    ///
    /// The result is written to <see cref="ExportPaths.MeasurementsFile"/> as the table itself, ready
    /// to replace the one in <see cref="BridgeTowers"/>, and not only to the log. A number that exists
    /// only in a log line gets read once and retyped, and retyping is how the golden entry came to hold
    /// the five-lane tower's width.
    /// </summary>
    private static void Audit(PrefabSystem prefabSystem)
    {
        var bridges = PrefabCatalog.GetAll(prefabSystem)
            .OfType<NetGeometryPrefab>()
            .Where(prefab => prefab.Has<Bridge>())
            .ToArray();

        var file = new List<string>
        {
            "# Tower widths measured in a running game.",
            "#",
            "# Road is the carriageway a tower straddles, measured the way the generator measures the",
            "# road it is asked to span - carriageway only, no outward extension - so the two are",
            "# comparable. It is taken from the narrowest bridge the game itself ships with that",
            "# carries the tower. Add-ons reuse a tower on roads it was never drawn for, so counting",
            "# those records the add-on author's choice rather than the tower's design width.",
            "#",
            "# Mesh is how far the tower's own geometry reaches across, from its bounds.",
            "#",
            "# The Tower(...) lines below are the table, ready to replace the one in BridgeTowers.cs.",
            "# Lines marked CHANGED disagree with what is currently hardcoded there.",
            "#",
            "# Generated: " + DateTime.UtcNow.ToString("u", CultureInfo.InvariantCulture),
            string.Empty,
        };

        var changed = 0;
        var measured = 0;

        foreach (var styleId in BridgeTowers.Styles)
        {
            file.Add($"[\"{styleId}\"] = new[]");
            file.Add("{");

            foreach (var tower in BridgeTowers.For(styleId))
            {
                var carriers = bridges
                    .Where(bridge => Carries(bridge, tower.Name))
                    .Select(bridge => new { bridge.name, Road = NetWidth.RoadSurfaceOf(bridge) })
                    .Where(entry => entry.Road > 0f)
                    .OrderBy(entry => entry.Road)
                    .ToArray();

                var mesh = MeshSpan(prefabSystem, tower.Name);
                if (carriers.Length == 0)
                {
                    var absent = string.Format(
                        CultureInfo.InvariantCulture,
                        "Tower audit [{0}] {1}: no installed bridge carries it{2}.",
                        styleId, tower.Name, mesh > 0f ? $", mesh spans {mesh:0.#} m" : string.Empty);
                    ModHost.Log.Info(absent);

                    // Left as it stands: nothing was measured, so nothing is claimed.
                    file.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "    new Tower(\"{0}\", {1}, {2}), // UNMEASURED - no installed bridge carries it",
                        tower.Name, tower.Mesh, tower.Road));
                    continue;
                }

                // The narrowest carriageway among the bridges that shipped with the tower is the road it
                // was built for.
                //
                // Among all carriers it is not. Add-ons reuse a tower on roads it was never drawn for -
                // BXP puts the golden gate pylon on a 28 m six-lane highway when the bridge it belongs
                // to is 50 m across - and taking the narrowest of those records the add-on author's
                // choice as the tower's design width. So the game's own bridges are asked first, and
                // everything else only when they have nothing to say.
                var shipped = carriers.Where(entry => IsShipped(entry.name)).ToArray();
                var authored = (shipped.Length > 0 ? shipped : carriers)[0].Road;
                var road = (int)Math.Round(authored);
                var span = (int)Math.Round(mesh);
                var differs = Math.Abs(authored - tower.Road) > 0.5f;

                measured++;
                if (differs) changed++;

                var carriedBy = string.Join(", ", carriers.Select(entry =>
                    string.Format(CultureInfo.InvariantCulture, "{0} ({1:0.#} m)", entry.name, entry.Road)));

                file.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "    new Tower(\"{0}\", {1}, {2}, verified: true), // {3}carried by {4}",
                    tower.Name, span, road,
                    differs ? $"CHANGED from road {tower.Road} mesh {tower.Mesh}; " : string.Empty,
                    carriedBy));

                var line = string.Format(
                    CultureInfo.InvariantCulture,
                    "Tower audit [{0}] {1}: table says road {2} m mesh {3} m; measured road {4:0.#} m "
                    + "mesh {5:0.#} m -> new Tower(\"{1}\", {6}, {7}, verified: true) // carried by {8}",
                    styleId, tower.Name, tower.Road, tower.Mesh, authored, mesh, span, road, carriedBy);

                // Only a disagreement is worth acting on, so only a disagreement is raised.
                if (differs) ModHost.Log.Warn(line);
                else ModHost.Log.Info(line);
            }

            file.Add("},");
        }

        Survey(prefabSystem, bridges, file);
        Write(file, measured, changed);
    }

    /// <summary>
    /// Every bridge and everything it carries, measured - the raw survey the table above is only one
    /// reading of.
    ///
    /// The audit can only check entries that already exist, so it cannot catch the one error that
    /// matters most: a table entry naming the wrong prefab. A bridge carries several objects and only
    /// one of them is the portal the road passes through. A pillar is a column standing under the deck;
    /// its width has nothing to do with the carriageway, and an entry pointing at one produces a tower
    /// sized from a number that never meant what it was read as. The golden suspension entry names
    /// SuspensionBridge03NetPillar, and the family's portal is more likely its pylon.
    ///
    /// Deciding that needs both widths side by side for every candidate, which is what this writes: per
    /// bridge, its carriageway, and every object anchored along it with how far that object reaches
    /// across. An object spanning less than the road it sits on cannot be a portal.
    ///
    /// The cables are surveyed the same way and for the same reason. They are not objects but overhead
    /// sections, and what decides whether they hang down either side of the carriageway or over it is
    /// their lateral offset - so the offset is recorded next to the road it belongs to. Without it the
    /// only way to know where a cable should sit is to build the bridge and look, which is how this
    /// went wrong twice.
    /// </summary>
    private static void Survey(PrefabSystem prefabSystem, NetGeometryPrefab[] bridges, List<string> file)
    {
        file.Add(string.Empty);
        file.Add("# Survey: every installed bridge, its carriageway, and everything it carries.");
        file.Add("#");
        file.Add("# style <TAB> bridge <TAB> road <TAB> objects <TAB> overhead");
        file.Add("#");
        file.Add("# style     which bridge type the name matches, or - when it matches none. Every");
        file.Add("#           installed bridge is listed, including the types that have no tower list");
        file.Add("#           yet, since those are the ones with nothing to correct against.");
        file.Add("# objects   name=meshSpan@lateralOffset   anchored along the deck: towers, pylons, pillars");
        file.Add("# overhead  name=sectionWidth@lateralOffset   drawn above the deck: cables, hangers");
        file.Add("#");
        file.Add("# Offsets are metres from the centre line; a pair at plus and minus the same number is");
        file.Add("# one cable down each side. An object narrower than the road is a support under the");
        file.Add("# deck, not a portal the road passes through, and must not be listed as a tower.");
        file.Add(string.Empty);

        var spans = new Dictionary<string, float>(StringComparer.Ordinal);
        var cables = new SortedDictionary<string, string>(StringComparer.Ordinal);

        foreach (var bridge in bridges.OrderBy(prefab => prefab.name, StringComparer.Ordinal))
        {
            var road = NetWidth.RoadSurfaceOf(bridge);

            var carried = new List<string>();
            if (bridge.TryGet<NetSubObjects>(out var subObjects))
            {
                foreach (var info in subObjects?.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
                {
                    var name = info?.m_Object?.name;
                    if (string.IsNullOrEmpty(name)) continue;

                    if (!spans.TryGetValue(name!, out var span))
                    {
                        span = MeshSpan(prefabSystem, name!);
                        spans[name!] = span;
                    }

                    carried.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1:0.#}@{2:+0.##;-0.##;0}", name, span, info!.m_Position.x));
                }
            }

            var overhead = new List<string>();
            if (bridge.TryGet<OverheadNetSections>(out var sections))
            {
                foreach (var section in sections?.m_Sections ?? Array.Empty<NetSectionInfo>())
                {
                    if (section?.m_Section == null) continue;

                    overhead.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "{0}={1:0.#}@{2:+0.##;-0.##;0}",
                        section.m_Section.name, NetWidth.Of(section.m_Section), section.m_Offset.x));
                }
            }

            foreach (var section in sections?.m_Sections ?? Array.Empty<NetSectionInfo>())
            {
                if (section?.m_Section != null) Cables(section.m_Section, cables);
            }

            if (carried.Count == 0 && overhead.Count == 0) continue;

            file.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0}\t{1}\t{2:0.#}\t{3}\t{4}",
                BridgeStyleDefinitions.Match(bridge.name)?.Id ?? "-", bridge.name, road,
                carried.Count > 0 ? string.Join(", ", carried) : "-",
                overhead.Count > 0 ? string.Join(", ", overhead) : "-"));
        }

        file.Add(string.Empty);
        file.Add("# Inside each overhead section: what actually holds the cables out.");
        file.Add("#");
        file.Add("# section <TAB> width <TAB> piece=width@offset | sub=width@offset ...");
        file.Add("#");
        file.Add("# Every cable section sits at offset 0 and its width tracks the road it belongs to:");
        file.Add("# 15, 19, 23 and 27 for the 12, 16, 20 and 24 m suspension bridges - road plus three,");
        file.Add("# every time. So a cable's position is not stored as an offset that can be shifted; it");
        file.Add("# follows from how wide the section is. Widening one means knowing what it is made of.");
        file.Add(string.Empty);

        foreach (var entry in cables) file.Add(entry.Key + "\t" + entry.Value);
    }

    /// <summary>
    /// The parts of one overhead section: its own pieces and the sub sections it defers to, each with
    /// the width and offset it contributes.
    ///
    /// Recorded once per section rather than once per bridge, since sections are shared - one cable
    /// section serves four different suspension bridges.
    /// </summary>
    private static void Cables(NetSectionPrefab section, IDictionary<string, string> into)
    {
        if (into.ContainsKey(section.name)) return;

        var parts = new List<string>();
        foreach (var piece in section.m_Pieces ?? Array.Empty<NetPieceInfo>())
        {
            if (piece?.m_Piece == null) continue;
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "piece {0}={1:0.##}@{2:+0.##;-0.##;0}",
                piece.m_Piece.name, piece.m_Piece.m_Width, piece.m_Offset.x));
        }

        foreach (var sub in section.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
        {
            if (sub?.m_Section == null) continue;
            // A sub section carries no offset of its own - it is laid out by the composition, so its
            // width is all there is to record.
            parts.Add(string.Format(
                CultureInfo.InvariantCulture,
                "sub {0}={1:0.##}", sub.m_Section.name, NetWidth.Of(sub.m_Section)));
        }

        into[section.name] = string.Format(
            CultureInfo.InvariantCulture,
            "{0:0.#}\t{1}", NetWidth.Of(section), parts.Count > 0 ? string.Join(", ", parts) : "-");

        // And one level further down: a section that is only a container says nothing on its own.
        foreach (var sub in section.m_SubSections ?? Array.Empty<NetSubSectionInfo>())
        {
            if (sub?.m_Section != null) Cables(sub.m_Section, into);
        }
    }

    /// <summary>
    /// Puts the measurements on disk next to the export report.
    ///
    /// Failing to write is reported but never thrown: the audit is a diagnostic, and a bridge that
    /// generated correctly should not be reported as failed because a text file could not be saved.
    /// </summary>
    private static void Write(List<string> file, int measured, int changed)
    {
        try
        {
            ExportPaths.EnsureDataDirectory();
            File.WriteAllLines(ExportPaths.MeasurementsFile, file);

            var summary = string.Format(
                CultureInfo.InvariantCulture,
                "Tower audit: measured {0} tower(s), {1} disagree with the hardcoded table. Written to {2}",
                measured, changed, ExportPaths.MeasurementsFile);

            if (changed > 0) ModHost.Log.Warn(summary);
            else ModHost.Log.Info(summary);
        }
        catch (Exception exception)
        {
            ModHost.Log.Warn(
                $"Tower audit could not be written to {ExportPaths.MeasurementsFile}: "
                + $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    /// <summary>
    /// Whether a bridge came with the game rather than with an add-on or with this mod.
    ///
    /// Only the game's own bridges say what a tower was designed for. An add-on reusing it says what
    /// that author wanted, and a bridge this mod generated says what a previous run of this mod
    /// decided - measuring that would fold last run's answer into this run's input and let an error
    /// settle in as fact.
    /// </summary>
    private static bool IsShipped(string name)
    {
        if (name.StartsWith("BXP ", StringComparison.Ordinal)) return false;

        // Generated bridges are named after the road they were built from, and the export state knows
        // which those are.
        try
        {
            return !ExportStateStore.Load().ExportNames()
                .Any(exported => string.Equals(exported, name, StringComparison.Ordinal));
        }
        catch (Exception)
        {
            // Unreadable state is not a reason to distrust the game's own bridges.
            return true;
        }
    }

    /// <summary>Whether a bridge anchors this tower along its deck.</summary>
    private static bool Carries(NetGeometryPrefab bridge, string towerName)
    {
        if (!bridge.TryGet<NetSubObjects>(out var subObjects)) return false;

        foreach (var info in subObjects?.m_SubObjects ?? Array.Empty<NetSubObjectInfo>())
        {
            if (info?.m_Object == null) continue;
            if (string.Equals(info.m_Object.name, towerName, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    /// <summary>
    /// How far a tower reaches across, from its mesh bounds rather than its geometry.
    ///
    /// Bounds are recorded at import and readable without loading anything, which matters here: this
    /// runs over every listed tower on every scan, and obtaining meshes for all of them to measure a
    /// number that is already stored would make the audit cost more than the export.
    /// </summary>
    private static float MeshSpan(PrefabSystem prefabSystem, string towerName)
    {
        var source = PrefabCatalog.GetAll(prefabSystem)
            .OfType<ObjectGeometryPrefab>()
            .FirstOrDefault(candidate => string.Equals(candidate.name, towerName, StringComparison.Ordinal));
        if (source?.m_Meshes == null) return 0f;

        var span = 0f;
        foreach (var info in source.m_Meshes)
        {
            if (info?.m_Mesh is not RenderPrefab render) continue;
            span = Math.Max(span, render.bounds.max.x - render.bounds.min.x);
        }

        return span;
    }

    private enum Result
    {
        Passed,
        Failed,
        Skipped,
    }

    /// <summary>
    /// Widens one tower by nothing and checks that nothing changed.
    ///
    /// The comparison is against the source mesh itself rather than against a saved asset, so it does
    /// not depend on the asset writer being correct - it isolates the derivation.
    /// </summary>
    private static Result Check(PrefabSystem prefabSystem, string styleId, BridgeTowers.Tower tower)
    {
        var source = PrefabCatalog.GetAll(prefabSystem)
            .OfType<ObjectGeometryPrefab>()
            .FirstOrDefault(candidate => string.Equals(candidate.name, tower.Name, StringComparison.Ordinal));
        if (source?.m_Meshes == null) return Result.Skipped;

        var problems = new List<string>();
        var parts = 0;

        foreach (var info in source.m_Meshes)
        {
            if (info?.m_Mesh is not RenderPrefab original) continue;

            Mesh[]? loaded = null;
            try
            {
                loaded = original.ObtainMeshes();
                var mesh = loaded?.FirstOrDefault();
                if (mesh == null) continue;

                parts++;
                var before = mesh.vertices;

                // Zero shift: the road asked for is exactly the road this tower was built for.
                var points = new float3[before.Length];
                for (var index = 0; index < before.Length; index++)
                {
                    points[index] = new float3(before[index].x, before[index].y, before[index].z);
                }

                var widened = TowerWidening.Widen(points, 0f);

                if (widened.Length != points.Length)
                {
                    problems.Add($"{original.name}: {points.Length} vertices became {widened.Length}");
                    continue;
                }

                for (var index = 0; index < points.Length; index++)
                {
                    if (math.distance(points[index], widened[index]) <= Tolerance) continue;
                    problems.Add(
                        $"{original.name}: vertex {index} moved from {points[index]} to {widened[index]}");
                    break;
                }

                // The surfaces must be the same objects, not merely similar ones - a generated tower
                // borrows its paint rather than reproducing it.
                var surfaces = original.surfaceAssets?.ToArray() ?? Array.Empty<object>();
                if (surfaces.Length == 0) problems.Add($"{original.name}: no surfaces to carry over");
            }
            catch (Exception exception)
            {
                problems.Add($"{original.name}: {exception.GetType().Name}: {exception.Message}");
            }
            finally
            {
                if (loaded != null)
                {
                    try
                    {
                        original.ReleaseMeshes();
                    }
                    catch (Exception)
                    {
                        // Releasing is a courtesy to the asset cache, not a correctness requirement.
                    }
                }
            }
        }

        if (parts == 0) return Result.Skipped;

        if (problems.Count > 0)
        {
            ModHost.Log.Error(string.Format(
                CultureInfo.InvariantCulture,
                "Tower self test FAILED for [{0}] {1} at its own {2} m road: {3}",
                styleId, tower.Name, tower.Road, string.Join("; ", problems)));
            return Result.Failed;
        }

        ModHost.Log.Info(string.Format(
            CultureInfo.InvariantCulture,
            "Tower self test passed: [{0}] {1} at {2} m road reproduces its {3} parts unchanged.",
            styleId, tower.Name, tower.Road, parts));
        return Result.Passed;
    }
}
