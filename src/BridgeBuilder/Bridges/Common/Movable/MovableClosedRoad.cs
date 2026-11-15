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
        internal readonly string? Error;
        internal Result(string error)
        {
            Error=error;Triangles=Array.Empty<MovableRoadGeometry.Triangle>();Sidewalks=null!;Surfaces=Array.Empty<SurfaceAsset>();
            Occurrences=Array.Empty<BridgePreviewComposition.Piece>();Compositions=Array.Empty<NetCompositionData>();OccurrenceStarts=Array.Empty<int>();
            SourceFaces=null;LaneVisuals=Array.Empty<MovableLaneVisuals.Batch>();LaneGeometry=null!;ObjectRules=Array.Empty<MovableRoadObjects.Rule[]>();
        }
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly MovableSidewalkFaces Sidewalks;
        internal readonly SurfaceAsset[] Surfaces;
        internal readonly BridgePreviewComposition.Piece[] Occurrences;
        internal readonly NetCompositionData[] Compositions;
        internal readonly int[] OccurrenceStarts;
        // Global occurrence -> compiled pre-atlas triangle ordinal -> original mesh face.
        internal readonly BridgePreviewNetMesh.SourceFace[][]? SourceFaces;
        internal readonly MovableLaneVisuals.Batch[] LaneVisuals;
        internal UnityEngine.Vector3[] CarLaneCenters = Array.Empty<UnityEngine.Vector3>();
        internal MovableNetworkContext.RoadLane[] RoadLanes = Array.Empty<MovableNetworkContext.RoadLane>();
        internal string LaneContext = string.Empty;
        internal MovableLaneGeometry.Result LaneGeometry;
        internal readonly MovableRoadObjects.Rule[][] ObjectRules;
        internal Result(MovableRoadGeometry.Triangle[] triangles, SurfaceAsset[] surfaces,
            BridgePreviewComposition.Piece[] occurrences, NetCompositionData[] compositions,
            int[] occurrenceStarts, MovableLaneVisuals.Batch[] laneVisuals, MovableRoadObjects.Rule[][] objectRules,
            BridgePreviewNetMesh.SourceFace[][]? sourceFaces = null, int lod = 0, bool deferLanes = false, MovableSidewalkFaces? receiverRoles = null, bool resolvedFaces = false)
        { Sidewalks = receiverRoles ?? new MovableSidewalkFaces(triangles, surfaces, occurrences);
            Triangles = resolvedFaces ? triangles : MovableRoadCoverage.ResolveFaces(triangles, Sidewalks.Walk, Sidewalks.Curb); Surfaces = surfaces; Occurrences = occurrences; Compositions = compositions;
            OccurrenceStarts = occurrenceStarts;
            SourceFaces = sourceFaces;
            LaneVisuals = laneVisuals; LaneGeometry = deferLanes ? null! : MovableLaneGeometry.Compile(laneVisuals, Triangles, lod);
            ObjectRules = objectRules; }


    }

}
