using Colossal.AssetPipeline;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;
using CS2Mods.Shared;
using CS2Mods.Shared.Infrastructure;
using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    /// <summary>One widened object, derived from one authored object.</summary>
    private StaticObjectPrefab? Build(
        ObjectGeometryPrefab source,
        string name,
        float sourceRoadWidth,
        float deckWidth,
        Action<ObjectGeometryPrefab> role)
    {
        {
            var parts = (source.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
                .Where(info => info?.m_Mesh is RenderPrefab)
                .ToArray();
            if (parts.Length == 0)
            {
                _report.Warning($"'{name}' was not generated: '{source.name}' has no readable mesh.");
                return null;
            }

            // One shift for the whole tower, measured across all of its parts together.
            //
            // Measuring each part on its own was the first version, and it took a tower apart: the
            // crossbeam is narrower than the legs it sits on, so it was told to spread by more than
            // they were, and the pieces no longer met. A tower is one object that happens to be
            // modelled in pieces, so the pieces move as one.
            // The shift is what puts the tower the archetype's distance outside the cables, which is
            // what the distance is measured to. At the tower's own width it is zero and the result is
            // that tower unchanged, which is the property the self test checks. `authored` stays the
            // road, because half the road is where the legs begin and that is a different quantity.
            var authored = sourceRoadWidth > 0f ? sourceRoadWidth : WidthOf(parts);
            // From where the cables ended up, not from the road - see ExtraFor. The two agree
            // whenever the tower and the cables came from the same bridge, and only the cables
            // are right when they did not.
            var extra = ExtraFor(parts, authored, deckWidth, name);
            Mod.Log.Info(string.Format(CultureInfo.InvariantCulture,
                "BridgeWidth transform owner='{0}', object='{1}', authoredRoad={2:R}, targetDeck={3:R}, extra={4:R}.",
                _bridgeName, name, authored, deckWidth, extra));

            // Each part against its own opening, which is where its own legs begin.
            //
            // One boundary for the whole tower was tried and is worse. A tower's parts open by
            // different amounts - the golden pillar's four open 43.31, 24.98, 13 and 8 metres - and
            // the widest is inside the legs of every other part: taking 43.31 for all of them put the
            // boundary at 21.66, while the pier's legs begin at 12.49, so the leg was cut in two and
            // its inner nine metres scaled. A leg is never scaled; that is rule 5, and this scaled
            // most of one.
            //
            // What one boundary was meant to fix was shear: parts stretching their interiors by
            // different ratios. They should. A part's interior is the material spanning between that
            // part's own legs, and it stretches to meet them - by its own ratio, because they are its
            // own legs. Two parts that open differently are two spans of different lengths.
            //
            // The inversion that one boundary also fixed is fixed properly in TowerWidening: the ratio
            // is floored at zero and a translation stops at the centre, so a part brought in by more
            // than it opens closes rather than folding through itself.
            var opening = OpeningOf(parts, out var narrowest);

            // A part brought in by more than it stands out is carried through the centre and comes
            // out the other side. Reported, not prevented: holding the whole tower back to whatever
            // its narrowest part can take left every other part - the base most visibly - wider than
            // the road it was built for, which is a fault in the bridge rather than in one part of it.
            if (narrowest > TowerWidening.CentreEpsilon && narrowest + extra <= 0f)
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' has a part opening only {1:0.##} m and is being brought in by {2:0.##} m, "
                    + "so that part is carried through the centre. The road is narrower than this "
                    + "design was drawn for.",
                    name, narrowest, -extra));
            }

            if (opening > TowerWidening.CentreEpsilon
                && (opening * 0.5f) + (extra * 0.5f) <= 0f)
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' opens {1:0.##} m and is being brought in by {2:0.##} m, which closes the "
                    + "portal the road passes through. It was closed to nothing rather than folded "
                    + "through itself, and neither is a bridge the road fits under.",
                    name, opening, -extra));
            }

            var meshes = new List<ObjectMeshInfo>();
            var partExtras = new Dictionary<ObjectMeshInfo, float>();
            var layerWidths = new List<string>();
            foreach (var info in parts)
            {
                var sourceMesh = (RenderPrefab)info.m_Mesh!;
                var partExtra = ExtraForPart(info, extra, name);
                if (_structureWidths.HasValue)
                {
                    partExtra = BridgeTowers.WhiteTrussArchWidths.TowerPartExtra(
                        _styleId, sourceMesh.name,
                        _structureWidths.Value.Inner, partExtra);
                }
                partExtras[info] = partExtra;
                var widened = Widen(sourceMesh, name, meshes.Count, partExtra);
                if (widened == null) continue;

                // The part keeps where it sat, carried outward by the same shift as its vertices.
                // Zeroing these was what collapsed the base, the shaft and the top onto one another.
                var position = info.m_Position;
                if (Math.Abs(position.x) > 0.001f)
                {
                    if (TrussArch02Geometry.IsRecorded(_styleId, sourceMesh.name)
                        && _structureWidths.HasValue)
                    {
                        position.x += position.x > 0f
                            ? _structureWidths.Value.InnerRight
                                - TrussArch02Geometry.PrototypeSectionInnerRight
                            : -(_structureWidths.Value.InnerLeft
                                - TrussArch02Geometry.PrototypeSectionInnerLeft);
                    }
                    else
                    {
                        position.x += position.x > 0f
                            ? partExtra * 0.5f
                            : -partExtra * 0.5f;
                    }
                }

                meshes.Add(new ObjectMeshInfo
                {
                    m_Mesh = widened,
                    m_Position = position,
                    m_Rotation = info.m_Rotation,
                    m_RequireState = info.m_RequireState,
                });

                if (Math.Abs(partExtra - extra) > TowerWidening.CentreEpsilon)
                {
                    layerWidths.Add(string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' by {1:0.###} m", sourceMesh.name, partExtra));
                }
            }

            if (meshes.Count == 0)
            {
                _report.Warning($"'{name}' was not generated: none of '{source.name}' could be read.");
                return null;
            }

            var tower = ScriptableObject.CreateInstance<StaticObjectPrefab>();
            _created.Add(tower);
            tower.name = name;
            tower.m_Meshes = meshes.ToArray();

            // Everything that is not geometry comes from the template.
            //
            // Not from the prefab this was derived from. The tower a bridge is built around may not be
            // installed - a road can be converted with nothing of the style present but the numbers -
            // and a generated tower still has to behave like a tower. BridgeTowerTemplate holds what
            // one is, read out of the game once and written down, so the result does not depend on the
            // archetype being there to copy from.
            role(tower);

            if (_styleId is "GoldenGate" or "GoldenGateDouble")
            {
                // The Golden Gate foundation is a Base candidate, not another standalone
                // shaft. Keep its native role after binding the generated placeholder.
                if (source.TryGet<PillarObject>(out var authoredPillar))
                {
                    var pillar = tower.AddOrGetComponent<PillarObject>();
                    pillar.m_Type = authoredPillar.m_Type;
                    pillar.m_AnchorOffset = authoredPillar.m_AnchorOffset;
                    pillar.m_VerticalPillarOffsetRange = authoredPillar.m_VerticalPillarOffsetRange;
                    pillar.active = authoredPillar.active;
                }

                // Native anchorages explicitly forbid terrain raising AND lowering. Without
                // this component their below-origin geometry excavates a hole at each end.
                if (source.TryGet<BuildingTerraformOverride>(out var terraform))
                    tower.AddComponentFrom(terraform);
                tower.m_Circular = source.m_Circular;
            }

            // Lights and other props mounted on the authored object are part of the tower, not part of
            // the selected road. Earlier generation rebuilt only the placeholder/pillar role and
            // silently discarded ObjectSubObjects, so lit archetypes produced unlit towers. Carry the
            // component whole, then translate each side-mounted child by the same delta as the mesh it
            // names through m_ParentMesh. The light prefab still owns its EffectSource, colour, range
            // and culling data; retaining that exact reference preserves the authored light effect.
            CarryAuthoredSubObjects(source, tower, partExtras, extra, name);

            // The stacking goes on the parts, and it is what lets the tower reach the ground: without
            // it the game builds no StackData, gives the placed tower no Stack, and draws it at the
            // height it was modelled at - hanging above the ground by however far it was raised.
            // The parts carry the archetype's own components now, stacking included - see Widen. What
            // is left here is the check: a multi-part tower with no stacking is the floating tower,
            // and it went unreported for five rounds because nothing looked.
            CheckStacking(name, tower.m_Meshes);

            // And held to its distance from the cables it will stand beside.
            CheckSpacing(name, tower.m_Meshes);

            if (layerWidths.Count > 0 && _structureWidths.HasValue)
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: white truss bridge-pier derived from '{1}': outer deck target {2:0.###} m, "
                    + "inner arch/pier target {3:0.###} m; {4}. The pier column and its footing use "
                    + "the same inner-layer displacement, preserving their prototype joint; the "
                    + "same assignment is reused by every LOD.",
                    name, source.name, _structureWidths.Value.Outer, _structureWidths.Value.Inner,
                    string.Join(", ", layerWidths)));
            }
            else
            {
                _report.Note(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0}: derived from '{1}' ({2:0.#} m authored, {3} part(s)) by moving everything "
                    + "{4:0.#} m apart{5}.",
                    name, source.name, authored, meshes.Count, extra,
                    Math.Abs(extra) < 0.001f ? " - geometry identical to the original" : string.Empty));
            }

            return tower;
        }
    }

    /// <summary>
    /// Carries tower-mounted props, including every authored light, without classifying them by name.
    /// A child object is a rigid part: a non-zero x position follows its parent mesh's half-width
    /// displacement, while a child on x = 0 remains on the centre line. All non-positional fields and
    /// all referenced effect prefabs remain those of the archetype.
    /// </summary>
    private void CarryAuthoredSubObjects(
        ObjectGeometryPrefab source,
        ObjectGeometryPrefab target,
        IReadOnlyDictionary<ObjectMeshInfo, float> partExtras,
        float objectExtra,
        string name)
    {
        if (!source.TryGet<ObjectSubObjects>(out var authored) || authored == null) return;

        target.AddComponentFrom(authored);
        if (!target.TryGet<ObjectSubObjects>(out var carried) || carried == null)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' did not retain the archetype's ObjectSubObjects component, so its mounted "
                + "lights and props were not published.",
                name));
            return;
        }

        var entries = carried.m_SubObjects ?? Array.Empty<ObjectSubObjectInfo>();
        var sourceParts = source.m_Meshes ?? Array.Empty<ObjectMeshInfo>();
        var shifted = 0;

        for (var entryIndex = 0; entryIndex < entries.Length; entryIndex++)
        {
            var entry = entries[entryIndex];
            if (entry == null) continue;

            var childExtra = objectExtra;
            var parent = entry.m_ParentMesh;
            if (parent >= 0
                && parent < sourceParts.Length
                && sourceParts[parent] != null
                && partExtras.TryGetValue(sourceParts[parent], out var recordedExtra))
            {
                childExtra = recordedExtra;
            }

            var position = entry.m_Position;
            // Native ornament-mounted lamps 16..33: move their mounting points with
            // the complete fan, without scaling/rotating the light prefab itself.
            if (_styleId == "SuspensionGolden" && source.name == "SuspensionBridge03NetPillar"
                && entries.Length == 40 && parent == 0 && entryIndex >= 16 && entryIndex <= 33)
            {
                if (entryIndex == 16 || entryIndex == 25) position.x = 0f;
                entry.m_Position = GoldenOrnamentGeometry.Position(position, childExtra);
                shifted++;
                continue;
            }
            var shift = childExtra * 0.5f;
            var movedX = position.x == 0f
                ? position.x
                : position.x + (position.x > 0f ? shift : -shift);
            if (BitConverter.SingleToInt32Bits(movedX)
                != BitConverter.SingleToInt32Bits(position.x))
            {
                shifted++;
            }

            position.x = movedX;
            entry.m_Position = position;
        }

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: carried {1} authored tower-mounted object(s), including their exact light/effect "
            + "prefab references, rotations, parent meshes, groups and probabilities; {2} side "
            + "object(s) followed their parent mesh's width displacement.",
            name,
            entries.Length,
            shifted));
    }

    /// <summary>
    /// How wide the whole tower is: the outermost reach of any of its parts, counting where each part
    /// sits as well as how wide it is.
    /// </summary>
    private static float WidthOf(ObjectMeshInfo[] parts)
    {
        var width = 0f;
        foreach (var info in parts)
        {
            if (info.m_Mesh is not RenderPrefab mesh) continue;
            var bounds = mesh.bounds;
            var reach = math.max(
                Math.Abs(info.m_Position.x + bounds.max.x),
                Math.Abs(info.m_Position.x + bounds.min.x));
            width = math.max(width, reach * 2f);
        }

        return width;
    }

    /// <summary>
    /// A generated placeholder and the generated object it turns into, mirroring the pair the bridge
    /// already had.
    ///
    /// The placeholder is what the net references and what carries the placement behaviour - including
    /// how far down the tower reaches - so it is built from the authored placeholder and keeps its
    /// components. The replacement is built from the object the game would have substituted, which is
    /// where the materials live. Neither half is invented; each is the widened form of the half it
    /// stands in for.
    /// </summary>
    private ObjectPrefab? CreatePair(
        ObjectGeometryPrefab placeholder, string name, float sourceRoadWidth, float deckWidth)
    {
        var concretes = Concretes(placeholder, name);

        // The placeholder is built from the placeholder, exactly as the game builds its own.
        //
        // It was built from the replacement's geometry for a while, on the reasoning that a placeholder
        // holding only the shaft would hang in the air if the swap ever failed. Read out of the game,
        // the reference does not do that:
        //
        //     5LaneSuspensionBridgePillar Placeholder   1 part,  y 0..86.55
        //     5LaneSuspensionBridgePillar               3 parts, y -10..86.55
        //
        // The shaft alone is what a placeholder is meant to be. Padding it out is a second difference
        // from the reference laid over whatever the first one was, and differences from the reference
        // are what every fault here has turned out to be.
        var stand = Build(
            placeholder, name, sourceRoadWidth, deckWidth, BridgeTowerTemplate.ApplyToPlaceholder);
        if (stand == null) return null;

        if (concretes.Length == 0) return stand;

        // Every replacement, not the likeliest one.
        //
        // A standalone pillar is not stretched to reach the ground - SubObjectSystem compares each
        // candidate's own height against the gap between the deck and the terrain and takes one that
        // covers it. The placeholder stands for a set of them at different heights, and that set is how
        // one bridge serves a crossing of any depth. Deriving only the most probable member leaves a
        // single height: high enough for the bridge it was copied from, and short of the ground for a
        // taller one, which is a tower hanging in the air with nothing under it.
        var built = new List<string>();
        foreach (var concrete in concretes)
        {
            var replacement = Build(
                concrete,
                string.Format(CultureInfo.InvariantCulture, "{0} {1}", name, concrete.name),
                sourceRoadWidth,
                deckWidth,
                tower => BridgeTowerTemplate.ApplyToReplacement(tower, stand));
            if (replacement == null) continue;

            built.Add(string.Format(
                CultureInfo.InvariantCulture,
                "{0} ({1:0.#} m tall, probability {2})",
                replacement.name, HeightOf(replacement), BridgeTowerSpec.SpawnProbability));
        }

        if (built.Count == 0)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': none of the {1} replacement(s) for '{2}' could be derived, so the tower has "
                + "only its placeholder and will not reach the ground.",
                name, concretes.Length, placeholder.name));
            return stand;
        }

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: built as a placeholder with {1} replacement(s) - {2}.",
            name, built.Count, string.Join(", ", built)));

        return stand;
    }

    /// <summary>
    /// Every object a placeholder can turn into, shortest first.
    ///
    /// All of them, not the likeliest. A standalone pillar is never stretched to reach the ground:
    /// <c>SubObjectSystem.CreateSubObject</c> reads each candidate's own <c>ObjectGeometryData.m_Size.y</c>,
    /// takes off its placement offset, and compares what is left against the gap between the deck and
    /// the terrain. The placeholder stands for a set of pillars at different heights, and that set is
    /// how one bridge serves a crossing of any depth.
    /// </summary>
    private ObjectGeometryPrefab[] Concretes(ObjectGeometryPrefab placeholder, string name)
    {
        if (!placeholder.Has<PlaceholderObject>()) return Array.Empty<ObjectGeometryPrefab>();

        var found = new List<ObjectGeometryPrefab>();
        foreach (var candidate in _prefabs.OfType<ObjectGeometryPrefab>())
        {
            if (!candidate.TryGet<SpawnableObject>(out var spawnable)) continue;
            if (spawnable?.m_Placeholders == null) continue;
            if (!spawnable.m_Placeholders.Any(entry => ReferenceEquals(entry, placeholder))) continue;

            found.Add(candidate);
        }

        if (found.Count == 0)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}': nothing declares itself a replacement for the placeholder '{1}', so the tower "
                + "keeps the placeholder's stand-in surface and will render untextured.",
                name, placeholder.name));
            return Array.Empty<ObjectGeometryPrefab>();
        }

        var ordered = found.OrderBy(HeightOf).ToArray();

        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: '{1}' stands for {2} object(s) - {3}. All are derived, because the game chooses "
            + "between them by height rather than stretching one.",
            name, placeholder.name, ordered.Length,
            string.Join(", ", ordered.Select(candidate =>
                string.Format(CultureInfo.InvariantCulture, "{0} ({1:0.#} m tall)",
                    candidate.name, HeightOf(candidate))))));

        return ordered;
    }

    /// <summary>
    /// How tall an object's meshes reach, counting where each part sits as well as how tall it is.
    ///
    /// This is the number the game selects pillars on, so it is worth reporting even though nothing
    /// here computes with it: a set of towers that all stop short of the deck leaves the bridge
    /// standing on nothing, and the heights are what say so.
    /// </summary>
    private static float HeightOf(ObjectGeometryPrefab prefab)
    {
        var top = float.MinValue;
        var bottom = float.MaxValue;

        foreach (var info in prefab.m_Meshes ?? Array.Empty<ObjectMeshInfo>())
        {
            if (info?.m_Mesh is not RenderPrefab mesh) continue;
            top = Math.Max(top, info.m_Position.y + mesh.bounds.max.y);
            bottom = Math.Min(bottom, info.m_Position.y + mesh.bounds.min.y);
        }

        return top > bottom ? top - bottom : 0f;
    }

    /// <summary>
    /// What the pillar components say, on the placeholder and on the object it turns into.
    ///
    /// Kept because the tower failed to reach the ground several times for reasons that each looked
    /// settled, and the values themselves went unread through all of them. They turned out identical on
    /// both prefabs - type Standalone, range plus or minus a metre - which is what finally pointed at
    /// selection rather than stretching.
    /// </summary>
    private void RecordPillars(ObjectGeometryPrefab placeholder, ObjectGeometryPrefab source, string name)
    {
        _report.Note(string.Format(
            CultureInfo.InvariantCulture,
            "{0}: pillar data - placeholder '{1}' {2}; source '{3}' {4}.",
            name, placeholder.name, Describe(placeholder), source.name, Describe(source)));
    }

    private static string Describe(PrefabBase prefab)
    {
        if (!prefab.TryGet<PillarObject>(out var pillar) || pillar == null) return "has no PillarObject";

        return string.Format(
            CultureInfo.InvariantCulture,
            "type {0}, anchor {1}, vertical range {2}",
            pillar.m_Type, pillar.m_AnchorOffset, pillar.m_VerticalPillarOffsetRange);
    }


    /// <summary>Whether a prefab of this name is already registered, generated or shipped.</summary>
    private bool Exists(string name)
    {
        // _created matters when one long-lived factory creates several bridges at runtime: the first
        // bridge's dependencies may not have been published to PrefabSystem yet, but their names are
        // already reserved and the next bridge must not create a second prefab under the same key.
        return _created.Any(candidate => candidate != null
                && string.Equals(candidate.name, name, StringComparison.Ordinal))
            || _prefabs
            .Any(candidate => candidate != null
                && string.Equals(candidate.name, name, StringComparison.Ordinal));
    }

    private ObjectGeometryPrefab? Find(string towerName)
    {
        return _prefabs
            .OfType<ObjectGeometryPrefab>()
            .FirstOrDefault(candidate => string.Equals(candidate.name, towerName, StringComparison.Ordinal));
    }

}
