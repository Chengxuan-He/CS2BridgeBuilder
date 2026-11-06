using System.Collections.Generic;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;

namespace BridgeBuilder.Bridges;

// Measured structural samplers are immutable and prepared offline. Runtime only
// selects their complete native graphs; bridge dimensions never cause a bake.
internal sealed class MovableStructureTiles
{
    internal sealed class Slot
    {
        internal SurfaceAsset Surface = null!;
        internal MovableStructurePacking.Unit Original = null!;
        internal UnityEngine.Vector2 Map(UnityEngine.Vector2 point) => Original.SourceUv(point);
    }

    private readonly string _style;
    private readonly bool _nativeNetworkGraph;
    private readonly Dictionary<SurfaceAsset,SurfaceAsset> _nativeSurfaces=new();

    internal MovableStructureTiles(string style):this(style,false) { }
    internal MovableStructureTiles(string style, bool nativeNetworkGraph)
    { _style=style; _nativeNetworkGraph=nativeNetworkGraph; }

    // This capability belongs to a complete fixed NetPiece graph, including its
    // plain RenderPrefab LOD children. Identity alone never grants that context.
    internal bool PreserveNativeNetwork(string render,int mesh,SurfaceAsset source)
    {
        var cid=source.id.guid.ToString();
        if(!NativeNetworkIdentity(render,mesh,cid))return false;
        source.LoadHeader();
        if(!source.isBuiltin || source.materialTemplateHash is not (17 or 33))
            throw new System.InvalidOperationException("Measured native network surface changed: "+cid);
        return true;
    }

    internal bool NativeNetworkIdentity(string render,int mesh,string cid)
        =>_nativeNetworkGraph && (_style switch
        {
            "Draw01"=>Draw01NativeMaterials.IsNativeNetwork(render,mesh,cid),
            "Draw02"=>Draw02NativeMaterials.IsNativeNetwork(render,mesh,cid),
            "Draw03"=>Draw03NativeMaterials.IsNativeNetwork(render,mesh,cid),
            "Lift01"=>Lift01NativeMaterials.IsNativeNetwork(render,mesh,cid),
            "Lift03"=>Lift03NativeMaterials.IsNativeNetwork(render,mesh,cid),
            _=>false
        });

    internal MovableUvChart[] Charts(string render,int mesh,int parentDomain)
        => MovableUvCharts.For(_style,render,mesh,parentDomain);

    internal bool PreserveSourceUv(string render,int mesh,int parentDomain)
        => MovableUvSampling.For(_style,render,mesh,parentDomain);

    internal int RetainedAxes(string render,int mesh,int parentDomain)
        => MovableUvSampling.Axes(_style,render,mesh,parentDomain);

    internal SurfaceAsset NativeSurface(SurfaceAsset source)
    {
        if(!_nativeSurfaces.TryGetValue(source,out var result))
        {
            result=_style switch
            {
                "Draw01"=>Draw01NativeMaterials.Resolve(source,cid=>BridgePreparedMaterials.NativeSource(_style,cid)),
                "Draw02"=>Draw02NativeMaterials.Resolve(source,cid=>BridgePreparedMaterials.NativeSource(_style,cid)),
                "Draw03"=>Draw03NativeMaterials.Resolve(source,cid=>BridgePreparedMaterials.NativeSource(_style,cid)),
                "Lift01"=>Lift01NativeMaterials.Resolve(source,cid=>BridgePreparedMaterials.NativeSource(_style,cid)),
                _=>source
            };
            BridgePreparedMaterials.StructuralBinding(result);
            _nativeSurfaces.Add(source,result);
        }
        return result;
    }

}
