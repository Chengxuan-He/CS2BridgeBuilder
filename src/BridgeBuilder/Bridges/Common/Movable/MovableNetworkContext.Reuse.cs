using System;
using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Game.Prefabs;
using Unity.Entities;
using Unity.Mathematics;

namespace BridgeBuilder.Bridges;

internal sealed partial class MovableNetworkContext
{
    private readonly GenerationStageCache _stages=new();
    private readonly MovableLaneGeometry.Cache _laneCompilation=new();
    private readonly Dictionary<Entity,(BridgePreviewComposition.Piece[] Pieces,NetCompositionData Data)> _pieceSnapshots=new();
    private readonly Dictionary<(int,int),RenderPrefab> _resolvedLods=new();
    private readonly Dictionary<string,MovableLaneVisuals.Batch[]> _laneSnapshots=new();

    private RenderPrefab ResolveRoadLod(RenderPrefab source,int lod)
    {
        var key=(source.GetInstanceID(),lod);
        if(_resolvedLods.TryGetValue(key,out var found))return found;
        var levels=source.GetComponent<LodProperties>()?.m_LodMeshes;
        var render=lod>0 && levels!=null && levels.Length>0?levels[Math.Min(lod-1,levels.Length-1)]:source;
        _resolvedLods.Add(key,render);return render;
    }
    private static string LaneSnapshotKey(IReadOnlyList<OwnerIndex> owners,float3 origin)
    {
        var bits=math.asuint(origin);var key=$"{bits.x}/{bits.y}/{bits.z}";
        foreach(var owner in owners)key+=$"/{owner.IsEdge}/{owner.Index}";
        return key;
    }
    private object[] SurfaceStageKey(int edge,SurfacePart part,float3 origin,NetPieceLayerMask excluded,bool faces,int lod)
    {
        var pieces=Pieces(SurfaceComposition(edge,part),out _);var bits=math.asuint(origin);
        var key=new List<object>{edge,part,bits.x,bits.y,bits.z,excluded,faces};
        foreach(var piece in pieces)
        {
            if(!BridgePreviewNetMesh.Visible(piece.Composition) || (excluded&(NetPieceLayerMask)(1<<(int)piece.Prefab.m_Layer))!=0)continue;
            key.Add(ResolveRoadLod(piece.Prefab,lod));
        }
        return key.ToArray();
    }
    private object[] RoadStageKey(IReadOnlyList<(int Edge,SurfacePart Part)> contributions,IReadOnlyList<OwnerIndex> owners,
        float3 origin,NetPieceLayerMask excluded,bool faces,int lod)
    {
        var key=new List<object>{contributions.Count,owners.Count};
        foreach(var c in contributions){var part=SurfaceStageKey(c.Edge,c.Part,origin,excluded,faces,lod);key.Add(part.Length);key.AddRange(part);}
        var batches=LaneVisuals(owners,origin);key.Add(batches);
        foreach(var batch in batches)key.Add(batch.Decal?batch.Source:ResolveRoadLod(batch.Source,lod));
        return key.ToArray();
    }
}
