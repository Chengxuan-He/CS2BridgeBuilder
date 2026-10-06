






using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Collections;

using UnityEngine;


namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{

    /// <summary>
    /// Points a derived prefab's levels of detail at derived meshes instead of the archetype's.
    ///
    /// <c>LodProperties.m_LodMeshes</c> names other render prefabs, and carrying the component across
    /// carries those names with it - so a widened piece kept the archetype's own coarse meshes. Close
    /// up it drew the widened one and looked right; far enough away the game swapped to a level of
    /// detail that was never widened, and the structure snapped back to the width it was authored at.
    /// That is a fault with a viewing distance attached to it, which is a hard thing to catch and an
    /// easy one to describe once seen.
    ///
    /// So each level is derived the same way the mesh above it was, by the same extra and against the
    /// same boundary, and the component is repointed. A level that cannot be derived is dropped rather
    /// than left: no level of detail is a mesh drawn at full detail from further away, while the wrong
    /// level is a mesh of the wrong size.
    /// </summary>
    private void DeriveLods(
        PrefabBase widened, string name, float extra, TowerWidening.Profile? profile,
        bool railings = false, bool preserveGeometry = false)
    {
        var lods = widened.GetComponent<LodProperties>();
        var meshes = lods?.m_LodMeshes;
        if (lods == null || meshes == null || meshes.Length == 0) return;

        var derived = new List<RenderPrefab>();
        for (var index = 0; index < meshes.Length; index++)
        {
            var source = meshes[index];
            if (source == null) continue;

            // The levels of detail belong to the piece, so they take its railing plan too. Left out,
            // a railing taken off the deck up close is still there at a distance.
            var copy = Widen(
                source,
                ScriptableObject.CreateInstance<RenderPrefab>(),
                string.Format(CultureInfo.InvariantCulture, "{0} LOD{1}", name, index + 1),
                extra,
                profile,
                railings,
                preserveGeometry);

            if (copy == null) continue;

            // A level of detail has no levels of its own. The copy carried the source's components
            // across and the source may name its own, which would derive levels of levels without
            // end - and the guard is here rather than at the recursion because the answer is not
            // "stop after N" but "there is nothing below this".
            copy.components.RemoveAll(component => component is LodProperties);
            derived.Add(copy);
        }

        var sources = new HashSet<RenderPrefab>(meshes.Where(mesh => mesh != null));
        lods.m_LodMeshes = derived.ToArray();

        // Nothing derived may still be one of the archetype's own. Checked rather than assumed,
        // because when it is wrong the piece draws correctly at the distance anyone works at and
        // wrongly at some other distance, and no screenshot of the thing being worked on shows it.
        var kept = derived.Where(sources.Contains).ToArray();
        if (kept.Length > 0)
        {
            _report.Defect(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' still points at the archetype's own level(s) of detail: {1}. It will draw at "
                + "the width it was widened to up close and at the width it was authored at from "
                + "further away.",
                name, string.Join(", ", kept.Select(mesh => $"'{mesh.name}'"))));
        }

        if (derived.Count != meshes.Length)
        {
            _report.Warning(string.Format(
                CultureInfo.InvariantCulture,
                "'{0}' kept {1} of {2} level(s) of detail. The rest could not be derived and were "
                + "dropped rather than left pointing at the archetype's own, which would have snapped "
                + "back to its width at a distance.",
                name, derived.Count, meshes.Length));
        }
    }

}
