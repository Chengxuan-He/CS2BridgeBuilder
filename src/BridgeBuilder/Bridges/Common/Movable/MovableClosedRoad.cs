using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;

namespace BridgeBuilder.Bridges;

/// <summary>
/// Compiles the selected road's visible native composition before any moving-span hiding rules.
/// Returns evaluated geometry plus original surface references, never preview materials. This is
/// the base-surface stage and native lane visual batches. Projecting/curving those batches and
/// converting their surfaces remain separate from extracting the native lane placement.
/// </summary>
internal static class MovableClosedRoad
{
    internal sealed class Result
    {
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly BridgePreviewComposition.Piece[] Occurrences;
        internal readonly NetCompositionData[] Compositions;
        internal readonly int[] OccurrenceStarts;
        // Global occurrence -> compiled pre-atlas triangle ordinal -> original mesh face.
        internal readonly BridgePreviewNetMesh.SourceFace[][]? SourceFaces;
        internal readonly MovableLaneVisuals.Batch[] LaneVisuals;
        internal UnityEngine.Vector3[] CarLaneCenters = Array.Empty<UnityEngine.Vector3>();
        internal readonly MovableLaneGeometry.Result LaneGeometry;
        internal readonly MovableRoadObjects.Rule[][] ObjectRules;
        internal Result(MovableRoadGeometry.Triangle[] triangles, SurfaceAsset[] surfaces,
            BridgePreviewComposition.Piece[] occurrences, NetCompositionData[] compositions,
            int[] occurrenceStarts, MovableLaneVisuals.Batch[] laneVisuals, MovableRoadObjects.Rule[][] objectRules,
            BridgePreviewNetMesh.SourceFace[][]? sourceFaces = null, int lod = 0)
        { Triangles = triangles; Surfaces = surfaces; Occurrences = occurrences; Compositions = compositions;
            OccurrenceStarts = occurrenceStarts;
            SourceFaces = sourceFaces;
            LaneVisuals = laneVisuals; LaneGeometry = MovableLaneGeometry.Compile(laneVisuals, triangles, lod);
            ObjectRules = objectRules; }


    }

    internal static bool TryCompile(NetGeometryPrefab selectedRoad, CompositionFlags flags, float span,
        out Result? result, out string? error)
    {
        result = null; error = null;
        if (selectedRoad == null || float.IsNaN(span) || float.IsInfinity(span) || span <= 0f ||
            (flags.m_General & (CompositionFlags.General.Node | CompositionFlags.General.FixedNodeSize)) != 0 ||
            (flags.m_General & CompositionFlags.General.Elevated) == 0)
        { error = "Closed road compilation requires a selected road, finite positive span and ordinary elevated edge flags."; return false; }
        try
        {
            using var session = new BridgePreviewSession();
            using var resources = new BridgePreviewRenderResources(session);
            using var composition = new BridgePreviewComposition(trackOccurrences: true);
            using var generator = new BridgePreviewNetMesh();
            var pieces = composition.Compose(selectedRoad, flags, out var data);
            if (pieces.Length == 0 || data.m_Width <= 0f || float.IsNaN(data.m_Width) || float.IsInfinity(data.m_Width))
            { error = "The selected closed road has no valid native composition."; return false; }
            var output = new List<MovableRoadGeometry.Triangle>();
            var surfaces = new List<SurfaceAsset>();
            var sourceFaces = new BridgePreviewNetMesh.SourceFace[pieces.Length][];
            for (var i = 0; i < pieces.Length; i++)
            {
                sourceFaces[i] = Array.Empty<BridgePreviewNetMesh.SourceFace>();
                var piece = pieces[i];
                if (!BridgePreviewNetMesh.Visible(piece.Composition)) continue;
                var slots = new List<SurfaceAsset>();
                var faces = new List<BridgePreviewNetMesh.SourceFace>();
                // Generating one occurrence at a time retains native packing and total road width,
                // but prevents material merging from destroying source-triangle provenance.
                if (!generator.Build(new[] { piece }, data, resources, span, out var mesh,
                        out _, out _, slots, faces))
                { error = $"Native mesh composition failed at '{piece.SourcePath}' ({piece.Prefab.name})."; return false; }
                foreach (var surface in slots)
                    if (surface == null || (surface.materialTemplateHash != 17 && surface.materialTemplateHash != 33))
                    { error = $"'{piece.SourcePath}' uses a material outside the measured network composition shader path."; return false; }
                if (!MovableStraightRoadEvaluation.TryEvaluate(mesh, data.m_Width, span, i,
                        out var triangles, out error))
                { error = piece.SourcePath + ": " + error; return false; }
                sourceFaces[i] = faces.ToArray();
                var firstSlot = surfaces.Count;
                foreach (var triangle in triangles)
                {
                    if (triangle.Material >= slots.Count)
                    { error = $"'{piece.SourcePath}' has no surface for material slot {triangle.Material}."; return false; }
                    output.Add(new MovableRoadGeometry.Triangle(triangle.A, triangle.B, triangle.C,
                        firstSlot + triangle.Material, triangle.Occurrence, triangle.SourceTriangle));
                }
                surfaces.AddRange(slots);
            }
            if (output.Count == 0)
            { error = "The selected road has no visible base surface in this state."; return false; }
            // Temporary-world entity identifiers must never escape as usable persistent references.
            var occurrences = new BridgePreviewComposition.Piece[pieces.Length];
            for (var i = 0; i < pieces.Length; i++)
            {
                var entry = pieces[i].Composition;
                entry.m_Piece = Unity.Entities.Entity.Null;
                occurrences[i] = new BridgePreviewComposition.Piece(pieces[i].Prefab, entry,
                    pieces[i].SourcePath, pieces[i].RootSection);
            }
            if (!MovableLaneVisuals.TryExtract(pieces, data, span, out var laneVisuals, out error)) return false;
            var objectRules = MovableRoadObjects.Extract(pieces, data);
            result = new Result(output.ToArray(), surfaces.ToArray(), occurrences, new[] { data },
                new[] { 0 }, laneVisuals, new[] { objectRules }, sourceFaces: sourceFaces);
            return true;
        }
        catch (Exception exception)
        { error = "Closed road compilation failed: " + exception.Message; return false; }
    }
}
