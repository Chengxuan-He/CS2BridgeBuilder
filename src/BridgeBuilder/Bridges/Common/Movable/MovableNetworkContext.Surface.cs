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
        internal readonly string? Error;
        internal Surface(string error){Error=error;Triangles=Array.Empty<MovableRoadGeometry.Triangle>();Materials=Array.Empty<SurfaceAsset>();Pieces=Array.Empty<BridgePreviewComposition.Piece>();Composition=default;SourceFaces=Array.Empty<BridgePreviewNetMesh.SourceFace[]>();}
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
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0) =>
        GenerationWork.Synchronous(() => CompileSurfaceAsync(edgeIndex, part, origin, replacedStructureLayers, trackSourceFaces, lod));

    internal async System.Threading.Tasks.Task<Surface> CompileSurfaceAsync(int edgeIndex, SurfacePart part, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0)
    {
        try{return await _stages.Get("source-surface",SurfaceStageKey(edgeIndex,part,origin,replacedStructureLayers,trackSourceFaces,lod),
            ()=>CompileSurfaceCoreAsync(edgeIndex,part,origin,replacedStructureLayers,trackSourceFaces,lod));}
        catch(OperationCanceledException){throw;}
        catch(Exception exception){return new Surface($"Native source surface: owner={_generationOwner}; edge={edgeIndex}; part={part}; LOD={lod}; {exception.Message}");}
    }

    private async System.Threading.Tasks.Task<Surface> CompileSurfaceCoreAsync(int edgeIndex, SurfacePart part, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0)
    {
        Surface Fail(string cause)=>new($"Native source surface: owner={_generationOwner}; edge={edgeIndex}; part={part}; LOD={lod}; {cause}");
        try
        {
        if((uint)edgeIndex>=(uint)Edges.Length || part is not (SurfacePart.Edge or SurfacePart.StartNode or SurfacePart.EndNode))return Fail("Invalid native composition input.");
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
            await GenerationWork.Checkpoint("native road surface extraction");
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
                    manager.GetComponentData<Curve>(edge).m_Length, out var mesh, out _, out _, slots, faces, lod))
                return Fail($"Native mesh generation failed for '{piece.Prefab.name}'.");
            foreach (var surface in slots)
                if (surface == null || (surface.materialTemplateHash != 17 && surface.materialTemplateHash != 33))
                    return Fail($"'{piece.Prefab.name}' requires a different composition shader path.");
            var evaluated = await MovableStraightRoadEvaluation.EvaluateAsync(mesh, parameters, origin, i);
            if (evaluated.Error != null) return Fail($"'{piece.Prefab.name}': {evaluated.Error}");
            var triangles = evaluated.Triangles;
            sourceFaces[i] = faces?.ToArray() ?? Array.Empty<BridgePreviewNetMesh.SourceFace>();
            var first = surfaces.Count;
            foreach (var triangle in triangles)
            {
                if ((uint)triangle.Material >= (uint)slots.Count)
                    return Fail("A native composition triangle has no matching surface slot.");
                output.Add(new MovableRoadGeometry.Triangle(triangle.A, triangle.B, triangle.C,
                    first + triangle.Material, triangle.Occurrence, triangle.SourceTriangle));
            }
            surfaces.AddRange(slots);
        }
        // Node compositions have no NetCompositionLane buffer. Lane visuals are
        // extracted independently from native SubLane entities in CompileRoad.
        return new Surface(output.ToArray(), surfaces.ToArray(), pieces, data, sourceFaces);
        }
        catch(OperationCanceledException){throw;}
        catch(Exception exception){return Fail(exception.Message);}
    }

    // All contributions share one closed-pose coordinate frame. Compile lane visuals once over
    // the combined receivers, so a lane crossing an edge/node seam is not truncated to one part.
    internal MovableClosedRoad.Result CompileRoad(IReadOnlyList<(int Edge, SurfacePart Part)> contributions,
        IReadOnlyList<OwnerIndex> laneOwners, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0) =>
        GenerationWork.Synchronous(() => CompileRoadAsync(contributions, laneOwners, origin, replacedStructureLayers, trackSourceFaces, lod));

    internal async System.Threading.Tasks.Task<MovableClosedRoad.Result> CompileRoadAsync(IReadOnlyList<(int Edge, SurfacePart Part)> contributions,
        IReadOnlyList<OwnerIndex> laneOwners, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0)
    {
        try{return await _stages.Get("source-road",RoadStageKey(contributions,laneOwners,origin,replacedStructureLayers,trackSourceFaces,lod),
            ()=>CompileRoadCoreAsync(contributions,laneOwners,origin,replacedStructureLayers,trackSourceFaces,lod));}
        catch(OperationCanceledException){throw;}
        catch(Exception exception){return new MovableClosedRoad.Result($"Native closed road: owner={_generationOwner}; LOD={lod}; {exception.Message}");}
    }

    private async System.Threading.Tasks.Task<MovableClosedRoad.Result> CompileRoadCoreAsync(IReadOnlyList<(int Edge, SurfacePart Part)> contributions,
        IReadOnlyList<OwnerIndex> laneOwners, float3 origin,
        NetPieceLayerMask replacedStructureLayers = 0, bool trackSourceFaces = true, int lod = 0)
    {
        MovableClosedRoad.Result Fail(string cause)=>new($"Native closed road: owner={_generationOwner}; LOD={lod}; {cause}");
        try
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
            var surface = await CompileSurfaceAsync(input.Edge, input.Part, origin, replacedStructureLayers, trackSourceFaces, lod);
            if(surface.Error!=null)return Fail(surface.Error);
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
            return Fail("The selected closed-road contributions contain no visible surface.");
        var packedTriangles=triangles.ToArray();var packedMaterials=materials.ToArray();var packedOccurrences=occurrences.ToArray();
        var receiverRoles=await MovableSidewalkFaces.CreateAsync(packedTriangles,packedMaterials,packedOccurrences);
        if(receiverRoles.Error!=null)return Fail(receiverRoles.Error);
        var resolvedTriangles=await GenerationWork.Background("road receiver coverage",()=>
            MovableRoadCoverage.ResolveFaces(packedTriangles,receiverRoles.Walk,receiverRoles.Curb));
        var result = new MovableClosedRoad.Result(resolvedTriangles, packedMaterials, packedOccurrences,
            compositions, occurrenceStarts, LaneVisuals(laneOwners, origin), rules, sourceFaces.ToArray(), lod, deferLanes: true,
            receiverRoles:receiverRoles,resolvedFaces:true);
        result.LaneGeometry = await MovableLaneGeometry.CompileAsync(result.LaneVisuals, result.Triangles, lod, _laneCompilation);
        if(result.Sidewalks.Error!=null)return Fail(result.Sidewalks.Error);
        var centers = new List<UnityEngine.Vector3>();
        var roadLanes = new List<RoadLane>();
        var laneSources = new HashSet<string>();
        var visited = new HashSet<Entity>();
        foreach (var owner in laneOwners)
        foreach (var entry in World.EntityManager.GetBuffer<Game.Net.SubLane>(Owner(owner), true))
        {
            laneSources.Add(Prefabs.Original(World.EntityManager.GetComponentData<PrefabRef>(Owner(owner)).m_Prefab).name);
            var lane = entry.m_SubLane;
            if (!visited.Add(lane) ||
                World.EntityManager.HasComponent<Game.Net.MasterLane>(lane) ||
                World.EntityManager.HasComponent<Game.Common.Deleted>(lane) ||
                World.EntityManager.HasComponent<Game.Tools.Hidden>(lane)) continue;
            var curve = World.EntityManager.GetComponentData<Curve>(lane).m_Bezier;
            // This appearance recipe is for straight longitudinal road lanes only.
            if (math.abs(curve.a.x-curve.d.x) > .01f || math.abs(curve.a.z-curve.d.z) < .01f) continue;
            if (World.EntityManager.HasComponent<Game.Net.CarLane>(lane))
                centers.Add((UnityEngine.Vector3)((curve.a+curve.d)*.5f-origin));
            var prefab = World.EntityManager.GetComponentData<PrefabRef>(lane).m_Prefab;
            var data = World.EntityManager.GetComponentData<NetLaneData>(prefab);
            // Lane borders are native prefab width about the evaluated lane, never midpoint
            // guesses between adjacent car lanes. Curved/transverse node connectors are not
            // a constant cross-section and must not produce longitudinal rail boundaries.
            if (!(data.m_Width > 0f) || !math.isfinite(data.m_Width) ||
                (data.m_Flags & (LaneFlags.Virtual | LaneFlags.Master | LaneFlags.Utility)) != 0) continue;
            var widthScale = World.EntityManager.HasComponent<NodeLane>(lane)
                ? BatchDataHelpers.BuildCurveScale(World.EntityManager.GetComponentData<NodeLane>(lane), data)
                : new float4(1,0,1,1);
            // Native BuildCurveScale(NodeLane, NetLaneData) uses 1 + offset / width.
            // Preserve both endpoint widths; never replace a varying node width by its mean.
            roadLanes.Add(new RoadLane((UnityEngine.Vector3)(curve.a-origin),
                (UnityEngine.Vector3)(curve.b-origin), (UnityEngine.Vector3)(curve.c-origin),
                (UnityEngine.Vector3)(curve.d-origin), data.m_Width,
                data.m_Width * widthScale.x, data.m_Width * widthScale.z, data.m_Flags,
                curve.a.x == curve.b.x && curve.a.x == curve.c.x && curve.a.x == curve.d.x,
                Prefabs.Original(prefab).name));
        }
        result.CarLaneCenters = centers.ToArray();
        result.RoadLanes = roadLanes.ToArray();
        result.LaneContext = "owner="+_generationOwner+"; sources="+string.Join("|",laneSources);
        return result;
        }
        catch(OperationCanceledException){throw;}
        catch(Exception exception){return Fail(exception.Message);}
    }

    private Entity SurfaceComposition(int edgeIndex, SurfacePart part)
    {
        var composition = World.EntityManager.GetComponentData<Composition>(Edges[edgeIndex]);
        return part switch
        {
            SurfacePart.Edge => composition.m_Edge,
            SurfacePart.StartNode => composition.m_StartNode,
            SurfacePart.EndNode => composition.m_EndNode,
            _ => Entity.Null
        };
    }
}
