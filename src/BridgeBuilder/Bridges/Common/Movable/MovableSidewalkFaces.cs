using System;
using System.Collections.Generic;
using System.Linq;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using Game.Prefabs;
using static BridgeBuilder.Bridges.MovableRoadGeometry;

namespace BridgeBuilder.Bridges;

// Stable keys survive clipping, coordinate changes and material remapping. Keep the native
// occurrence domain intact for lane, object-rule and source-triangle provenance.
internal sealed class MovableSidewalkFaces
{
    internal string? Error {get;private set;}
    private enum Role { Other, Walk, Curb }
    private readonly Dictionary<(int,int),Role> _faces = new();

    private MovableSidewalkFaces() {}
    internal MovableSidewalkFaces(IReadOnlyList<Triangle> faces,SurfaceAsset[] materials,BridgePreviewComposition.Piece[] occurrences)
    {
        var result=GenerationWork.Synchronous(()=>CreateAsync(faces,materials,occurrences));
        _faces=result._faces;Error=result.Error;
    }
    internal static async System.Threading.Tasks.Task<MovableSidewalkFaces> CreateAsync(IReadOnlyList<Triangle> faces,
        SurfaceAsset[] materials,BridgePreviewComposition.Piece[] occurrences)
    {
        var sources=new MovableRoadRoute.Source?[materials.Length];
        var families=occurrences.Select(p=>Family(p.RootSection?.name)).ToArray();
        var mixed=new bool[occurrences.Length];
        var walkingOnly=occurrences.Select(p=>p.Prefab.TryGet<NetPieceLanes>(out var lanes)&&lanes.active&&
            lanes.m_Lanes is {Length:>0}&&lanes.m_Lanes.All(e=>e?.m_Lane!=null&&e.m_Lane.Has<PedestrianLane>())).ToArray();
        var special=materials.Select(m=>m.id.guid.ToString()=="1d7dad9f64c9fe55a809f1080c39fb92").ToArray();
        foreach(var t in faces)
        {
            if(families[t.Occurrence]!=Role.Walk&&!special[t.Material]||sources[t.Material].HasValue)continue;
            if(!MovableRoadRoute.TryRead(materials[t.Material],out var read,out var error))
            {Mod.Log.Error(error);return new MovableSidewalkFaces{Error=error};}
            sources[t.Material]=read;
        }
        return await GenerationWork.Background("road sidewalk receiver roles",()=>
        {
            var result=new MovableSidewalkFaces();
        var kinds = new MovableRoadAppearance.Type[faces.Count];
        for (int i=0;i<faces.Count;i++)
        {
            var t=faces[i];
            // The same native atlas curb strip can occur in medians and other sections.
            if (families[t.Occurrence]!=Role.Walk && !special[t.Material]) continue;
            var source=sources[t.Material]!.Value;
            kinds[i]=source.Receiver(t);
            if (kinds[i] is MovableRoadAppearance.Type.Sidewalk or MovableRoadAppearance.Type.Curb)
                mixed[t.Occurrence]=true;
        }
        for (int i=0;i<faces.Count;i++)
        {
            var t=faces[i];
            result._faces[(t.Occurrence,t.SourceTriangle)] = Classify(families[t.Occurrence],kinds[i],
                walkingOnly[t.Occurrence],mixed[t.Occurrence],t.A.Normal.y,t.B.Normal.y,t.C.Normal.y);
        }
            return result;
        });
    }

    private static Role Classify(Role family, MovableRoadAppearance.Type source, bool pedestrianOnly,
        bool mixed, float normalA, float normalB, float normalC)
    {
        if (family==Role.Curb || source==MovableRoadAppearance.Type.Curb) return Role.Curb;
        if (family!=Role.Walk) return Role.Other;
        if (source==MovableRoadAppearance.Type.Sidewalk) return Role.Walk;
        return source==MovableRoadAppearance.Type.Asphalt && pedestrianOnly && !mixed &&
            normalA>.7f && normalB>.7f && normalC>.7f ? Role.Walk : Role.Other;
    }

    private static Role Family(string? name)
    {
        const string prefix="RBBridgeDep_NetSectionPrefab_";
        if (name?.StartsWith(prefix,StringComparison.Ordinal)==true)
            name=name.Substring(prefix.Length).Replace('_',' ');
        if (name=="Sidewalk" || name?.StartsWith("Sidewalk ",StringComparison.Ordinal)==true) return Role.Walk;
        if (name=="Curb" || name?.StartsWith("Curb ",StringComparison.Ordinal)==true) return Role.Curb;
        return Role.Other;
    }

    internal bool Walk(Triangle t) => _faces.TryGetValue((t.Occurrence,t.SourceTriangle),out var r) && r==Role.Walk;
    internal bool Curb(Triangle t) => _faces.TryGetValue((t.Occurrence,t.SourceTriangle),out var r) && r==Role.Curb;
    internal Dictionary<(int,int),Partition> Partitions(Partition road,Partition walk) =>
        _faces.ToDictionary(entry=>entry.Key,entry=>entry.Value==Role.Walk ? walk : road);
}
