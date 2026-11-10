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
    internal bool TryPreserveNativeNetwork(string render,int mesh,SurfaceAsset source,out bool preserve,out string? error)
    {
        preserve=false;error=null;
        try
        {
            var cid=source.id.guid.ToString();
            if(!NativeNetworkIdentity(render,mesh,cid))return true;
            source.LoadHeader();
            if(source.materialTemplateHash is not (17 or 33))
            {error="Measured native network surface changed: "+cid;return false;}
            preserve=true;return true;
        }
        catch(System.Exception exception){error="Native network preservation: "+exception.Message;return false;}
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

    internal bool TryNativeSurface(SurfaceAsset source,out SurfaceAsset result,out string? error)
    {
        result=null!;error=null;string? lookupError=null;
        try
        {
        MovableExistingRoadSurface.SurfaceResult Load(string cid)=>BridgePreparedMaterials.NativeSource(_style,cid);
        if(!_nativeSurfaces.TryGetValue(source,out result))
        {
            MovableExistingRoadSurface.SurfaceResult resolved=_style switch
            {
                "Draw01"=>Draw01NativeMaterials.Resolve(source,Load),
                "Draw02"=>Draw02NativeMaterials.Resolve(source,Load),
                "Draw03"=>Draw03NativeMaterials.Resolve(source,Load),
                "Lift01"=>Lift01NativeMaterials.Resolve(source,Load),
                _=>source
            };
            if(!resolved.Success||resolved.Value==null){error=resolved.Error??"Required native structural Surface is absent.";result=null!;return false;}
            result=resolved.Value;
            if(!BridgePreparedMaterials.TryStructuralBinding(result,out error)){result=null!;return false;}
            _nativeSurfaces.Add(source,result);
        }
        return true;
        }
        catch(System.Exception exception){result=null!;error=lookupError??("Native structural material: "+exception);return false;}
    }

}
