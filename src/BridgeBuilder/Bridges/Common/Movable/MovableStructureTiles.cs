using System.Collections.Generic;
using System.IO;
using BridgeBuilder.Runtime;
using Colossal.IO.AssetDatabase;
using UnityEngine;

namespace BridgeBuilder.Bridges;

// Measured structural samplers are immutable and prepared offline. Runtime only
// selects their complete native graphs; bridge dimensions never cause a bake.
internal sealed class MovableStructureTiles
{
    internal sealed class Slot
    {
        internal SurfaceAsset Surface=null!;
        internal MovableStructurePacking.Unit? Original;
        internal MovableRepeatUnits.RegionMapping? Region;
        internal Vector2 Map(Vector2 uv)=>Region!=null?Region.Map(uv):Original!=null?Original.SourceUv(uv):uv;
    }

    private readonly string _style;
    private readonly bool _nativeNetworkGraph;
    private readonly Dictionary<SurfaceAsset,string> _sourceDigests=new();
    private readonly Dictionary<SurfaceAsset,SurfaceAsset> _nativeSurfaces=new();
    private readonly Dictionary<string,IReadOnlyList<MovableRepeatUnits.Region>> _regions=new();

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
            _nativeSurfaces.Add(source,result);
        }
        return result;
    }

    internal Dictionary<int,Slot> Build(SurfaceAsset source,IEnumerable<MovableStructurePacking.Unit> units)
    {
        var slots=new Dictionary<int,Slot>();
        int originals=0,sharedUnits=0;
        foreach(var unit in units)
        {
            // Retain a fully compatible original before opening its file or
            // consulting prepared resources. The catalogue owns all other slots.
            if(unit.CanUseSource)
            {
                slots.Add(unit.Id,new Slot{Surface=NativeSurface(source),Original=unit});
                originals++;
                continue;
            }
            if(!_sourceDigests.TryGetValue(source,out var digest))
            {
                using var stream=source.GetReadStream();
                using var bytes=new MemoryStream();
                stream.CopyTo(bytes);
                _sourceDigests.Add(source,digest=MovableStructureKey.Digest(bytes.ToArray()));
            }
            if(!_regions.TryGetValue(digest,out var regions))
                _regions.Add(digest,regions=MovableRepeatUnits.Regions(_style,digest));
            MovableRepeatUnits.RegionMapping? mapping=null;
            foreach(var region in regions)
                if((mapping=MovableRepeatUnits.RegionMapping.Create(region,unit))!=null)break;
            if(mapping!=null)
            {
                slots.Add(unit.Id,new Slot{Surface=BridgePreparedMaterials.RepeatUnit(_style,mapping.Region.BindingKey),Region=mapping});
                sharedUnits++;
                continue;
            }
            var key=MovableStructureKey.For(digest,unit.Triangle,unit.Bounds,unit.A,unit.B,unit.C);
            slots.Add(unit.Id,new Slot{Surface=BridgePreparedMaterials.Structure(_style,key)});
        }
        Mod.Log.Info($"Structural material reuse: style={_style}; original={originals}; genuine-unit={sharedUnits}; prepared={slots.Count-originals-sharedUnits}; runtime samplers=0.");
        return slots;
    }
}
