using Colossal.AssetPipeline;
using BridgeBuilder.Runtime;
using Colossal.AssetPipeline.Importers;
using Colossal.IO.AssetDatabase;
using Colossal.Mathematics;


using Game.Prefabs;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Unity.Mathematics;
using UnityEngine;


namespace BridgeBuilder.Bridges;

internal sealed partial class TowerFactory
{
    /// <summary>
    /// One widened copy of one of the source's meshes. Every level of detail is widened the same way,
    /// or the tower would change shape as the camera pulls back.
    /// </summary>
    private RenderPrefab? Widen(
        RenderPrefab original,
        string towerName,
        int index,
        float extra)
    {
        var name = index == 0 ? towerName + " Mesh" : towerName + " Mesh " + index;
        return Widen(
            original,
            ScriptableObject.CreateInstance<RenderPrefab>(),
            name,
            extra);
    }

    /// <summary>
    /// Widens <paramref name="original"/> into <paramref name="widened"/>, which the caller has already
    /// created as whatever kind of render prefab it needs.
    ///
    /// The caller chooses the type because a net piece is a <see cref="RenderPrefab"/> too - that is the
    /// whole reason the cables can be fixed at all. It no longer chooses the rule: which parts stretch
    /// and which move is decided by the geometry, by whether a part crosses the centre line.
    /// </summary>
    private T? Widen<T>(
        RenderPrefab original, T widened, string name, float extra,
        TowerWidening.Profile? profile = null, bool railings = false, bool preserveGeometry = false)
        where T : RenderPrefab
    {
        name = TowerPrefabNaming.Safe(name);
        Mesh[]? loaded = null;
        var models = new List<ModelImporter.Model>();
        // A rejected preview still owns the ScriptableObject allocated by the caller.
        // Track it before loading/transforming can fail.
        if (_previewGeometry != null) _created.Add(widened);
        try
        {
            var box = original.bounds;
            var shift = extra * 0.5f;
            var resultBounds = preserveGeometry ? box : new Bounds3(
                new float3(box.min.x - shift, box.min.y, box.min.z),
                new float3(box.max.x + shift, box.max.y, box.max.z));
            var span = resultBounds.max.x - resultBounds.min.x;
            // A source plane may legitimately have zero thickness. Reject inverted
            // bounds, or a positive source span collapsed by narrowing, not authored planes.
            if (!float.IsFinite(span) || span < 0f || (span == 0f && extra < 0f))
            {
                _report.Defect(string.Format(
                    CultureInfo.InvariantCulture,
                    "'{0}' would have invalid width {1:R} m after a {2:R} m change. "
                    + "The bridge was stopped before geometry was written.", name, span, extra));
                if (_previewGeometry == null) UnityEngine.Object.Destroy(widened);
                return null;
            }
            loaded = PrivateGeometryReader.Read(original);
            if (loaded == null || loaded.Length == 0) return null;

            // Surface slots are positional, spanning ALL submeshes of ALL meshes.
            // Never remove a missing entry: doing so assigns later materials to
            // the wrong parts. Reject before allocating persistent geometry.
            var surfaces = original.surfaceAssets?.ToArray() ?? Array.Empty<SurfaceAsset>();
            var expectedSurfaces = loaded.Sum(mesh => mesh == null ? 0 : mesh.subMeshCount);
            if (surfaces.Any(surface => surface == null) || surfaces.Length != expectedSurfaces)
            {
                _report.Defect($"'{original.name}' has missing/misaligned surface slots: "
                    + $"{surfaces.Length} surfaces for {expectedSurfaces} submeshes. '{name}' was not generated.");
                if (_previewGeometry == null) UnityEngine.Object.Destroy(widened);
                return null;
            }

            // Every mesh the source holds, not the first of them. A render prefab can hold several -
            // the levels of detail - and it carries one surface for each; declaring one mesh while
            // handing over the whole set leaves the renderer pairing them off wrongly.
            var totalVertices = 0;
            var totalIndices = 0;
            var recordedTruss02 = TrussArch02Geometry.IsRecorded(_styleId, original.name);
            var recordedTruss03 = railings
                && TrussArch03Geometry.IsRecorded(_styleId, original.name);
            var recordedGoldenSupport = railings
                && GoldenSupportGeometry.IsRecorded(_styleId, original.name);
            var recordedGeometry = recordedTruss02 || recordedTruss03 || recordedGoldenSupport;
            var suspensionSheetSpan = 0f;
            var continuousSuspensionSheet = railings
                && SuspensionGeometry.TryGetContinuousSpan(
                    _styleId, original.name, out suspensionSheetSpan);
            var rigidSuspensionSidePart = !railings
                && SuspensionGeometry.IsRigidSidePart(_styleId, original.name);
            var rigidGoldenGateCable = railings
                && GoldenGateGeometry.IsCable(_styleId, original.name);

            // One profile for everything widened here. A section hands one in, because its pieces
            // are one structure; a tower part measures its own, from its full detail mesh and the
            // prefabs its levels of detail live in, together. Those are one structure too - the same part drawn coarsely -
            // and letting each answer for itself is how a leg came to be carried at full detail and
            // scaled at distance, which read as the bridge changing width as the camera pulled back.
            //
            var shapes = new List<float3[]>();
            var outlines = new List<IReadOnlyList<int>?>();
            if (!recordedGeometry && !preserveGeometry)
            {
                foreach (var part in loaded)
                {
                    if (part == null) continue;

                    shapes.Add(ToPoints(part.vertices));
                    outlines.Add(part.triangles);
                }
            }

            // CONTRACT rule 8: an LOD cannot vote on what the part is. Keep the full-detail
            // archetype measurement before adding any coarse substitute. TrussArch01's portal uses
            // this exact side-body boundary at every viewing distance.
            var fullDetailScope = preserveGeometry ? null
                : profile ?? TowerWidening.Profile.Of(shapes, outlines);

            // The levels of detail are named by a component and live in prefabs of their own, so they
            // have to be fetched to be included. They were not, and the comment above said they were:
            // the scope was the full detail mesh alone, a coarse mesh's outermost material fell outside
            // the places that scope called carried, and it was scaled where the fine one was carried -
            // 7.899 m against 8, which is the bridge changing width as the camera pulls back.
            if (profile == null && !recordedGeometry && !preserveGeometry)
            {
                foreach (var lod in original.GetComponent<LodProperties>()?.m_LodMeshes
                    ?? Array.Empty<RenderPrefab>())
                {
                    if (lod == null) continue;

                    Mesh[]? lodRead = null;
                    try
                    {
                        lodRead = PrivateGeometryReader.Read(lod);
                        foreach (var mesh in lodRead)
                        {
                            if (mesh == null) continue;

                            shapes.Add(ToPoints(mesh.vertices));
                            outlines.Add(mesh.triangles);
                        }

                    }
                    catch (Exception)
                    {
                        // Generation diagnostics are silent; retain the external API exception boundary.
                    }
                    finally { PrivateGeometryReader.Release(lodRead); }
                }
            }

            var scope = recordedGeometry || preserveGeometry
                ? null
                : profile
                    ?? (IsBluePrototypeMainPier(original)
                        ? fullDetailScope
                        : TowerWidening.Profile.Of(shapes, outlines));

            for (var index = 0; index < loaded.Length; index++)
            {
                var part = loaded[index];
                if (part == null) continue;

                // All three arch-above colours are open trusses. Their top beams cross x=0 and must
                // be stretched. Blue and white author one logical transverse assembly as several
                // render islands, which all share the complete full-detail reach. Green welds side
                // arches to transverse work, so it uses one continuous x-map measured from the
                // full-detail side boundary. The same decision is carried into every LOD.
                var source = ToPoints(part.vertices);
                var openTruss = IsThroughArchSection(railings);
                var preserveOpenTrussSides =
                    BridgeStyleDefinitions.PreservesOpenTrussSideAssembly(_styleId);
                var rigidBlueBase = IsBluePrototypeBase(original);
                var bluePortal = IsBluePrototypeMainPier(original);
                var blueSection = IsBluePrototypeSection(original);
                TowerWidening.TrussWideningFacts trussFacts = default;
                var usedRecordedTruss02 = false;
                var usedRecordedTruss03 = false;
                float3[] moved;
                bool[]? dropped = null;
                bool[]? protectedGoldenSupport = null;
                if (preserveGeometry)
                {
                    // Independent copy of the recorded central sheet, including every LOD.
                    // No spatial inference or road-width deformation belongs to this assembly.
                    moved = source;
                }
                else if (recordedGoldenSupport)
                {
                    if (!GoldenSupportGeometry.TryWiden(original.name, source, extra, out moved,
                            out protectedGoldenSupport))
                    {
                        _report.Defect($"'{name}' does not match its recorded golden support vertex map; geometry was not written.");
                        return null;
                    }
                }
                else if (recordedTruss02)
                {
                    var applied = false;
                    if (railings && _structureWidths.HasValue)
                    {
                        applied = TrussArch02Geometry.TryWidenSection(
                            original.name,
                            source,
                            _structureWidths.Value.Outer,
                            _structureWidths.Value.InnerLeft,
                            _structureWidths.Value.InnerRight,
                            _roadEdges.HasValue && !_roadEdges.Value.Left.IsSidewalk,
                            _roadEdges.HasValue && !_roadEdges.Value.Right.IsSidewalk,
                            out moved,
                            out _,
                            out dropped);
                    }
                    else if (!railings)
                    {
                        var leftDelta = _structureWidths.HasValue
                            ? _structureWidths.Value.InnerLeft
                                - TrussArch02Geometry.PrototypeSectionInnerLeft
                            : extra * 0.5f;
                        var rightDelta = _structureWidths.HasValue
                            ? _structureWidths.Value.InnerRight
                                - TrussArch02Geometry.PrototypeSectionInnerRight
                            : extra * 0.5f;
                        applied = TrussArch02Geometry.TryWidenTowerPart(
                            original.name,
                            source,
                            leftDelta,
                            rightDelta,
                            out moved,
                            out _);
                    }
                    else
                    {
                        moved = source;
                    }

                    if (!applied)
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not match its immutable TrussArchBridge02 inner/outer "
                            + "vertex map. The derived prefab was stopped before geometry was written.",
                            name));
                        return null;
                    }

                    usedRecordedTruss02 = true;
                }
                else if (openTruss && recordedTruss03)
                {
                    if (!TrussArch03Geometry.TryWidenSection(
                            original.name,
                            source,
                            extra,
                            out moved,
                            out _,
                            out _))
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not match its immutable TrussArchBridge03 vertex map. "
                            + "The derived prefab was stopped before geometry was written.",
                            name));
                        return null;
                    }

                    usedRecordedTruss03 = true;
                }
                else
                {
                    moved = rigidGoldenGateCable
                        ? TowerWidening.Widen(source, extra)
                        : continuousSuspensionSheet
                        // The source-prefab metaprogram marks this entire net as one continuous
                        // transverse sheet. Full detail and every named LOD use the same recorded
                        // full-detail span, so distance cannot change its width decision.
                        ? TowerWidening.Stretch(source, suspensionSheetSpan, extra)
                        : rigidSuspensionSidePart
                            // These exact source identities contain side material only. Carrying all
                            // non-centre x coordinates preserves the columns and bases at every LOD.
                            ? TowerWidening.Widen(source, extra)
                    : rigidBlueBase
                        // AGENTS rule 8: the TrussArch01 deck base is authored as side material.
                        // Start from its prototype vertices and carry every non-zero x by the whole d.
                        // This is a translation, never a proportional widening of the base.
                        ? TowerWidening.WidenRigidBase(source, extra)
                        : bluePortal
                            // Generated metadata names every prototype vertex: the columns and side
                            // fittings translate rigidly, while only transverse beams crossing x=0
                            // stretch. LOD2 inherits the high-detail decision even though it is welded.
                            ? WidenBluePrototypePier(original, source, extra, name)
                        : blueSection
                            // Exact offline metadata keeps every side island rigid and stretches each
                            // centre-crossing logical top-truss assembly against its own archetype span.
                            // Using one global reach is what left shorter diagonal assemblies several
                            // metres short of the translated side arches.
                            ? WidenBluePrototypeSection(original, source, extra, name)
                        : openTruss
                        ? TowerWidening.WidenOpenTruss(
                            source, part.triangles, extra,
                            preserveOpenTrussSides,
                            scope!,
                            out trussFacts)
                        : TowerWidening.WidenParts(source, extra, scope!);
                }

                if (railings && APylonCableGeometry.IsRecorded(_styleId, original.name)
                    && !APylonCableGeometry.TryApply(original.name, source, extra, moved))
                {
                    _report.Defect($"'{name}' does not match the recorded A-pylon cable vertices; generation stopped.");
                    return null;
                }

                if (openTruss && !usedRecordedTruss02 && !usedRecordedTruss03 && !blueSection
                    && !trussFacts.ContractSatisfied)
                {
                    _report.Defect(string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' could not satisfy the recorded x=0 transform for every vertex. "
                        + "The current derived prefab was stopped before any geometry was written.",
                        name));
                    return null;
                }

                if (railings && _styleId == "CoveredWood"
                    && !CoveredWoodGeometry.TryApply(original.name, source, moved))
                {
                    _report.Defect($"'{name}' does not match the recorded CoveredWood upright map; geometry was not written.");
                    return null;
                }

                if (railings && TrussArch03BaseGeometry.IsRecorded(_styleId, original.name))
                {
                    if (!TrussArch03BaseGeometry.TryApply(
                            original.name,
                            source,
                            moved,
                            extra * 0.5f,
                            out var baseMoved,
                            out _,
                            out var baseError))
                    {
                        _report.Defect(string.Format(
                            CultureInfo.InvariantCulture,
                            "'{0}' did not apply its recorded TrussArchBridge03 deck-base transform: {1}. "
                            + "The derived prefab was stopped before geometry was written.",
                            name,
                            baseError));
                        return null;
                    }

                    moved = baseMoved;
                }
                // Planned from the first mesh, which shows the most, and carried out on every one of
                // them. A level of detail asked for itself finds one railing where there are two and
                // keeps what the full detail mesh took away: a railing that is there from a distance
                // and gone up close.
                // Only where a railing is being fitted. The plan is held in a field so that a piece
                // and its levels of detail share one, and a field outlives the piece that set it: the
                // towers are derived after the sections, so a plan left standing was applied to them
                // too and drew whatever stood in its band - part of a leg - to a single point.
                if (!railings && _styleId == "SuspensionGolden"
                    && GoldenOrnamentGeometry.IsRecorded(original.name)
                    && !GoldenOrnamentGeometry.TryApply(original.name, source, moved, extra))
                {
                    _report.Defect($"'{name}' does not match the recorded golden ornament; geometry was not written.");
                    return null;
                }

                if (!railings && _styleId == "SuspensionGolden"
                    && GoldenOrnamentGeometry.IsRecorded(original.name))
                {
                    // Retain authored stacking bounds; only expand them if the explicitly
                    // requested XY ornament transform places geometry outside that volume.
                    foreach (var point in moved)
                    {
                        resultBounds.min = math.min(resultBounds.min, point);
                        resultBounds.max = math.max(resultBounds.max, point);
                    }
                }

                if (railings && !preserveGeometry && !rigidGoldenGateCable)
                {
                    _kerbPlans ??= PlanKerbRailings(name, source, moved, part.triangles, extra);
                    if (_kerbPlans != null) dropped = ApplyKerbPlans(_kerbPlans, source, moved, protectedGoldenSupport);
                }

                var partVertices = ToVectors(moved);

                var model = BuildModel(
                    models.Count == 0 ? name : name + " LOD" + models.Count,
                    part,
                    partVertices,
                    out var modelError,
                    dropped);
                if (model == null)
                {
                    _report.Defect(modelError ?? string.Format(
                        CultureInfo.InvariantCulture,
                        "'{0}' could not be converted to generated geometry; the current derived "
                        + "prefab was stopped instead of publishing a partial mesh.",
                        name));
                    return null;
                }
                models.Add(model);
                totalVertices += partVertices.Length;
                totalIndices += (int)CountIndices(part);
            }

            if (models.Count == 0) return null;

            GeometryAsset? asset = null;
            if (_previewGeometry != null)
            {
                // Exactly the same generated vertex channels and submeshes as a
                // permanent bridge, but no asset database entry and no disk writes.
                if (!_previewGeometry.Capture(widened, models)) return null;
                widened.hideFlags = HideFlags.HideAndDontSave;
            }
            else
            {
            var geometry = new Geometry(models.ToArray());
            // Defence at the write boundary as well as at each generated-prefab naming boundary:
            // AssetDataPath reads the last period as an extension, and rejects any extension other
            // than the one belonging to GeometryAsset.
            var geometryAssetName = TowerPrefabNaming.Safe(name);
            if (!BridgeBuilder.Runtime.BridgeAssetInfo.MatchesOwner(geometryAssetName, _bridgeName))
            { _report.Defect($"Geometry persistence requires a bridge UUID in its name: '{geometryAssetName}' (owner '{_bridgeName}')."); return null; }
            asset = AssetDatabase.user.AddAsset(
                AssetDataPath.Create("BridgeBuilder", geometryAssetName, EscapeStrategy.None),
                geometry);

            // Registering an asset is not writing it. Without this the geometry existed in the database
            // and nowhere else, so the renderer asked for its mesh a frame later and got nothing.
            asset.Save();

            // And then let go of it. An asset built in memory ends up in a state the loader treats as
            // impossible: its Data already holds the meshes it was constructed from while its Loading
            // still records that nothing has been read, and the header read asserts on the difference.
            // Save put the meshes on disk a line ago, so the load that follows reads them back the
            // ordinary way.
            asset.Unload();
            }

            if (_previewGeometry == null) _created.Add(widened);
            widened.name = name;

            // The archetype's own components, carried across.
            //
            // Not applied from a recorded template. The template held what was measured on the
            // suspension bridge's cable piece and on its tower's parts, and applying it to every
            // family's geometry is the fault rule 9 names: an arch section is not a cable sheet and a
            // truss is not a pylon. Here the archetype is in hand - it is the thing being widened - so
            // there is nothing to recall and nothing to get wrong.
            //
            // AddComponentFrom copies field by field through a JSON round trip, which is also what
            // gives the component its back reference to the prefab that owns it. Adding to
            // `components` directly leaves that null and the component's own Initialize throws.
            foreach (var component in original.components)
            {
                if (component != null) widened.AddComponentFrom(component);
            }

            // The levels of detail the component just named are the archetype's. Derive them.
            DeriveLods(
                widened, name, extra, scope, railings, preserveGeometry);

            widened.geometryAsset = asset;

            // Derived from the source's bounds, not recomputed from the vertices. Bounds are authored,
            // and for a pillar they reach below what the geometry draws; measuring the vertices returns
            // a box that stops where the drawing stops.
            //
            // One expression for every mesh now, because both branches of the widening rule move the
            // outermost vertex by the same half of the same number: a part that crosses the centre is
            // scaled by exactly what puts its outer edge there, and a part that does not is carried
            // there. There is no longer a stretched case and a translated case to tell apart.
            widened.bounds = resultBounds;


            widened.vertexCount = totalVertices;
            widened.indexCount = totalIndices;
            widened.meshCount = models.Count;
            widened.surfaceArea = original.surfaceArea;

            // The surfaces themselves, not the tower they came off. A SurfaceAsset is a shader and its
            // textures; pointing at one is not pointing at another bridge.
            widened.surfaceAssets = surfaces;

            return widened;
        }
        finally
        {
            // Permanent Geometry owns these buffers; in-memory previews own only
            // the converted Unity meshes and must release the importer buffers now.
            if (_previewGeometry != null)
                foreach (var model in models) model.Dispose();
            if (loaded != null)
            {
                try
                {
                    PrivateGeometryReader.Release(loaded);
                }
                catch (Exception)
                {
                    // Releasing is a courtesy to the asset cache, not a correctness requirement.
                }
            }
        }
    }

}
