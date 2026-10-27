"""Derive Draw02 source charts from native face adjacency, never atlas bounding boxes."""
import argparse
import collections
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
sys.path.insert(0,str(Path(__file__).resolve().parents[3]/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays, read_bbn


def domains(path):
    data=path.read_bytes()
    assert data[:4]==b'BBN1' and hashlib.sha256(data[:-32]).digest()==data[-32:]
    version,*counts=struct.unpack_from('<7i',data,4)
    assert version==1
    ints,_=decode_arrays(*read_bbn(data))
    ints=ints.tolist()
    assert ints[0]==-2
    result=[];at=2
    for _ in range(ints[1]):
        a,b,c,count=ints[at:at+4];at+=4;count=abs(count)
        result.append(list(ints[at:at+count]));at+=count
    assert at==len(ints)
    return result


def extract(positions,uv,faces):
    from shapely.geometry import Polygon
    from shapely.ops import unary_union
    n=len(faces);parent=list(range(n));edges=collections.defaultdict(list)
    points=[tuple(map(float,(*p,*v[:2]))) for p,v in zip(positions,uv)]
    def root(i):
        while parent[i]!=i:parent[i]=parent[parent[i]];i=parent[i]
        return i
    cross=np.cross(positions[faces[:,1]]-positions[faces[:,0]],positions[faces[:,2]]-positions[faces[:,0]]).astype(float)
    lengths=np.linalg.norm(cross,axis=1);cross/=np.maximum(lengths[:,None],1e-30)
    for face,ids in enumerate(faces):
        for j in range(3):
            a,b=points[ids[j]],points[ids[(j+1)%3]]
            if a!=b:edges[tuple(sorted((a,b)))].append((face,a,b))
    for uses in edges.values():
        # Conservative part boundary: require a unique manifold edge, opposite
        # winding, identical endpoint UVs, and the same physical face plane.
        if len(uses)!=2:continue
        (a,p,q),(b,r,s)=uses
        if p==s and q==r and float(np.dot(cross[a],cross[b]))>=1-1e-6:
            parent[root(b)]=root(a)
    members=collections.defaultdict(list)
    for i in range(n):members[root(i)].append(i)
    shapes={}
    for k,ids in members.items():
        polygons=[]
        for face in ids:
            poly=Polygon(np.asarray(uv[faces[face],:2],dtype=float))
            if poly.is_valid and poly.area>0:polygons.append(poly)
        if polygons:shapes[k]=unary_union(polygons)
    return [root(i) for i in range(n)],shapes


def loops(shape):
    polygons=[shape] if shape.geom_type=='Polygon' else list(shape.geoms)
    result=[]
    for polygon in polygons:
        for ring in [polygon.exterior,*polygon.interiors]:
            points=list(ring.coords)[:-1]
            # Stable alias identity independent of face enumeration/orientation.
            variants=[]
            for direction in (points,list(reversed(points))):
                at=min(range(len(direction)),key=lambda i:direction[i])
                variants.append(tuple(direction[at:]+direction[:at]))
            result.append(min(variants))
    return sorted(result)


def main(repo,python_lib):
    if python_lib:sys.path.insert(0,str(python_lib))
    from shapely.geometry import Polygon
    from shapely.ops import unary_union
    base=repo/'artifacts/prepared-materials/Draw02/shared-source-migration'
    meshes=[];charts=[];known={};sources=[]
    for path in sorted((repo/'artifacts/uv-density/native').glob('DrawBridge02*.npz')):
        with np.load(path) as native:
            for field in native.files:
                if not field.endswith('_faces') or '_metric_' in field:continue
                mesh=int(field[4:-6]);render=path.stem
                recipe=repo/'assets/HardcodedNumbers/Draw02'/('UvDomain_'+render.replace(' ','_')+'_'+str(mesh)+'.bin')
                if not recipe.exists():continue
                face_domains=domains(recipe)
                roots,shapes=extract(native[f'mesh{mesh}_positions'],native[f'mesh{mesh}_uv0'],native[field])
                uses=[]
                for domain in face_domains:
                    ids=[]
                    for root in sorted(set(roots[f] for f in domain)):
                        if root not in shapes:continue
                        boundary=loops(shapes[root]);signature=tuple(boundary)
                        if signature not in known:
                            known[signature]=len(charts);charts.append(boundary)
                        ids.append(known[signature])
                    # A polygon union can round an intersection onto the wrong side of
                    # an authored edge. Preserve exact original face polygons as
                    # additional charts rather than expanding a boundary epsilon.
                    for face in domain:
                        poly=Polygon(np.asarray(native[f'mesh{mesh}_uv0'][native[field][face],:2],dtype=float))
                        if not poly.is_valid or poly.area==0:continue
                        if roots[face] in shapes and shapes[roots[face]].covers(poly):continue
                        boundary=loops(poly);signature=tuple(boundary)
                        if signature not in known:
                            known[signature]=len(charts);charts.append(boundary)
                        ids.append(known[signature])
                    uses.append(sorted(set(ids)))
                meshes.append(dict(render=render,mesh=mesh,domains=uses))
                sources.append(dict(path=str(path.relative_to(repo)),sha256=hashlib.sha256(path.read_bytes()).hexdigest(),recipeSha256=hashlib.sha256(recipe.read_bytes()).hexdigest()))
    integers=[len(meshes)]+[0]*len(meshes);values=[]
    # Per-mesh tables reference domain lists; chart data is shared after these tables.
    list_locations={};pending=[]
    for mi,mesh in enumerate(meshes):
        integers[1+mi]=len(integers);integers.extend([len(mesh['domains'])]+[0]*len(mesh['domains']))
        start=integers[1+mi]
        for di,ids in enumerate(mesh['domains']):
            key=tuple(ids)
            if key not in list_locations:
                list_locations[key]=len(integers);integers.extend([len(ids)]+[0]*len(ids));pending.append((list_locations[key],ids))
            integers[start+1+di]=list_locations[key]
    chart_offsets=[]
    for chart in charts:
        chart_offsets.append(len(integers));integers.append(len(chart))
        for loop in chart:
            integers.extend((len(values),len(loop)));values.extend(v for point in loop for v in point)
    for start,ids in pending:
        for i,chart in enumerate(ids):integers[start+1+i]=chart_offsets[chart]
    raw=b'BBN1'+struct.pack('<7i',1,len(integers),0,0,0,0,len(values))
    raw+=struct.pack('<'+'i'*len(integers),*integers)+struct.pack('<'+'d'*len(values),*values)
    raw+=hashlib.sha256(raw).digest()
    binary=repo/'assets/HardcodedNumbers/Draw02/Draw02_NativeUvCharts.bin';binary.write_bytes(raw)
    names=',\n'.join('        '+json.dumps(m['render']+'_'+str(m['mesh'])) for m in meshes)
    code="""using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;
// Generated by tools/PreparedMaterials/Draw02/ExportNativeCharts.py.
// Embedded numerical arrays: Draw02_NativeUvCharts
// Native face ownership and exact polygon loops, not inferred repeat rectangles.
internal static class Draw02UvCharts
{
    private static readonly string[] Keys = {
KEYS
    };
    private static class Data
    {
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Draw02_NativeUvCharts");
        internal static readonly Dictionary<int, MovableUvChart[]> Loaded = new();
    }
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        int mesh = Array.IndexOf(Keys, renderName + "_" + meshIndex);
        if (mesh < 0) return Array.Empty<MovableUvChart>();
        var data = Data.Values;
        int table = data.I32[mesh + 1];
        if (domain < 0 || domain >= data.I32[table]) throw new ArgumentOutOfRangeException(nameof(domain));
        int list = data.I32[table + 1 + domain];
        if (!Data.Loaded.TryGetValue(list, out var result))
        {
            result = new MovableUvChart[data.I32[list]];
            for (int i = 0; i < result.Length; i++)
                result[i] = MovableUvChart.Read(data.I32, data.F64, data.I32[list + 1 + i]);
            Data.Loaded.Add(list, result);
        }
        return result;
    }
}
"""
    (repo/'src/BridgeBuilder/Bridges/Types/Draw02/Draw02UvCharts.cs').write_text(code.replace('KEYS',names))
    data=dict(schema='BridgeBuilder/Draw02NativeCharts/1',sources=sources,meshes=meshes,charts=charts,
        summary=dict(meshes=len(meshes),domains=sum(len(m['domains']) for m in meshes),uniqueCharts=len(charts),binaryBytes=len(raw),newTexturePayloads=0),
        constraints='Unique manifold physical edge, UV continuity, same plane; no cross-part atlas-only merge. Chart shape does not imply repeat.')
    (base/'native-charts.json').write_text(json.dumps(data,indent=2)+'\n')
    print(json.dumps(data['summary']))


if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('repo',type=Path);parser.add_argument('--python-lib',type=Path)
    args=parser.parse_args();main(args.repo.resolve(),args.python_lib.resolve() if args.python_lib else None)

