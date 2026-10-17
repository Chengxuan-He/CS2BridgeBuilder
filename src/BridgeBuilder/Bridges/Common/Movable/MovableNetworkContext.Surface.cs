using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Net;
using Game.Prefabs;
using Game.Rendering;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkContext
{
    internal enum SurfacePart { Edge, StartNode, EndNode }

    internal sealed class Surface
    {
        internal readonly MovableRoadGeometry.Triangle[] Triangles;
        internal readonly SurfaceAsset[] Materials;
        internal readonly BridgePreviewComposition.Piece[] Pieces;
        internal readonly NetCompositionData Composition;
        internal readonly BridgePreviewNetMesh.SourceFace[][] SourceFaces;
        internal Surface(MovableRoadGeometry.Triangle[] triangles, SurfaceAsset[] materials,
            BridgePreviewComposition.Piece[] pieces, NetCompositionData composition,
            BridgePreviewNetMesh.SourceFace[][] sourceFaces)
        { Triangles = triangles; Materials = materials; Pieces = pieces; Composition = composition;
            SourceFaces = sourceFaces; }
    }

    // The requested composition must be the visible closed-road state before hiding the moving
    // network surface. The actual prepared edge/node geometry supplies its deformation frame.
    internal Surface CompileSurface(int edgeIndex, SurfacePart part, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true)
    {
        var manager = World.EntityManager;
        var edge = Edges[edgeIndex];
        var identity = SurfaceComposition(edgeIndex, part);
        var pieces = Pieces(identity, out var data);
        BatchDataHelpers.CompositionParameters parameters;
        if (part == SurfacePart.Edge)
            BatchDataHelpers.CalculateEdgeParameters(manager.GetComponentData<EdgeGeometry>(edge), false, out parameters);
        else
            BatchDataHelpers.CalculateNodeParameters(part == SurfacePart.StartNode
                ? manager.GetComponentData<StartNodeGeometry>(edge).m_Geometry
                : manager.GetComponentData<EndNodeGeometry>(edge).m_Geometry, data, out parameters);
        using var session = new BridgePreviewSession();
        using var resources = new BridgePreviewRenderResources(session);
        using var generator = new BridgePreviewNetMesh();
        var output = new List<MovableRoadGeometry.Triangle>();
        var surfaces = new List<SurfaceAsset>();
        var sourceFaces = new BridgePreviewNetMesh.SourceFace[pieces.Length][];
        for (var i = 0; i < pieces.Length; i++)
        {
            sourceFaces[i] = Array.Empty<BridgePreviewNetMesh.SourceFace>();
            var piece = pieces[i];
            if (!BridgePreviewNetMesh.Visible(piece.Composition)) continue;
            // The caller identifies native structural layers replaced by its mechanism.
            // Keep the complete occurrence array: source-face and object indices retain
            // their native identity. Surface-layer curbs/medians are never filtered here.
            if ((replacedStructureLayers & (NetPieceLayerMask)(1 << (int)piece.Prefab.m_Layer)) != 0)
                continue;
            var slots = new List<SurfaceAsset>();
            var faces = trackSourceFaces ? new List<BridgePreviewNetMesh.SourceFace>() : null;
            // Build emits native packed geometry. Its preview-only bounds/matrix block is not
            // consumed; evaluation uses the actual network parameters calculated above.
            if (!generator.Build(new[] { piece }, data, resources,
                    manager.GetComponentData<Curve>(edge).m_Length, out var mesh, out _, out _, slots, faces))
                throw new InvalidOperationException($"Native mesh generation failed for '{piece.Prefab.name}'.");
            foreach (var surface in slots)
                if (surface == null || (surface.materialTemplateHash != 17 && surface.materialTemplateHash != 33))
                    throw new InvalidOperationException($"'{piece.Prefab.name}' requires a different composition shader path.");
            if (!MovableStraightRoadEvaluation.TryEvaluate(mesh, parameters, origin, i, out var triangles, out var error))
                throw new InvalidOperationException($"'{piece.Prefab.name}': {error}");
            sourceFaces[i] = faces?.ToArray() ?? Array.Empty<BridgePreviewNetMesh.SourceFace>();
            var first = surfaces.Count;
            foreach (var triangle in triangles)
            {
                if ((uint)triangle.Material >= (uint)slots.Count)
                    throw new InvalidOperationException("A native composition triangle has no matching surface slot.");
                output.Add(new MovableRoadGeometry.Triangle(triangle.A, triangle.B, triangle.C,
                    first + triangle.Material, triangle.Occurrence, triangle.SourceTriangle));
            }
            surfaces.AddRange(slots);
        }
        // Node compositions have no NetCompositionLane buffer. Lane visuals are
        // extracted independently from native SubLane entities in CompileRoad.
        return new Surface(output.ToArray(), surfaces.ToArray(), pieces, data, sourceFaces);
    }

    // All contributions share one closed-pose coordinate frame. Compile lane visuals once over
    // the combined receivers, so a lane crossing an edge/node seam is not truncated to one part.
    internal MovableClosedRoad.Result CompileRoad(IReadOnlyList<(int Edge, SurfacePart Part)> contributions,
        IReadOnlyList<OwnerIndex> laneOwners, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true)
    {
        var triangles = new List<MovableRoadGeometry.Triangle>();
        var materials = new List<SurfaceAsset>();
        var occurrences = new List<BridgePreviewComposition.Piece>();
        var sourceFaces = new List<BridgePreviewNetMesh.SourceFace[]>();
        var compositions = new NetCompositionData[contributions.Count];
        var occurrenceStarts = new int[contributions.Count];
        var rules = new MovableRoadObjects.Rule[contributions.Count][];
        for (var i = 0; i < contributions.Count; i++)
        {
            var input = contributions[i];
            var surface = CompileSurface(input.Edge, input.Part, origin, replacedStructureLayers, trackSourceFaces);
            var firstMaterial = materials.Count;
            var firstOccurrence = occurrences.Count;
            occurrenceStarts[i] = firstOccurrence;
            foreach (var triangle in surface.Triangles)
                triangles.Add(new MovableRoadGeometry.Triangle(triangle.A, triangle.B, triangle.C,
                    checked(firstMaterial + triangle.Material), checked(firstOccurrence + triangle.Occurrence),
                    triangle.SourceTriangle));
            materials.AddRange(surface.Materials);
            sourceFaces.AddRange(surface.SourceFaces);
            foreach (var piece in surface.Pieces)
                occurrences.Add(new BridgePreviewComposition.Piece(piece.Prefab, piece.Composition,
                    $"contribution[{i}]/edge[{input.Edge}]/{input.Part}/{piece.SourcePath}", piece.RootSection));
            compositions[i] = surface.Composition;
            rules[i] = ObjectRules(SurfaceComposition(input.Edge, input.Part));
        }
        if (triangles.Count == 0)
            throw new InvalidOperationException("The selected closed-road contributions contain no visible surface.");
        return new MovableClosedRoad.Result(triangles.ToArray(), materials.ToArray(), occurrences.ToArray(),
            compositions, occurrenceStarts, LaneVisuals(laneOwners, origin), rules, sourceFaces.ToArray());
    }

    private Entity SurfaceComposition(int edgeIndex, SurfacePart part)
    {
        var composition = World.EntityManager.GetComponentData<Composition>(Edges[edgeIndex]);
        return part switch
        {
            SurfacePart.Edge => composition.m_Edge,
            SurfacePart.StartNode => composition.m_StartNode,
            SurfacePart.EndNode => composition.m_EndNode,
            _ => throw new ArgumentOutOfRangeException(nameof(part))
        };
    }
}
