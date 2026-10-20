"""Measure A-frame side legs, transverse webs and the independent centre shaft seat.

Generate an explicit subdivision plan at the detailed inner-leg profile. The
offline clipping planes never appear as runtime geometric classifiers.
"""
from pathlib import Path
import hashlib,json,sys
import numpy as np

r,output=map(Path,sys.argv[1:])
audit=json.loads((r/'all-movable-audit.json').read_text())
# Detailed inner leg stations; below the transverse web the legs are separate.
profile=np.array([[23.389648,1.4051819],[23.962402,1.2958984],
                  [25.894043,.9272766],[26.66211,.7806854],
                  [28.37207,.4543686]],dtype=float)

def span(y):return float(np.interp(y,profile[:,0],profile[:,1]))
def weight(p,stem=False):
    if stem:return p[0]/.24104309
    return float(np.clip(p[0]/span(p[1]),-1,1))

def clip(poly,values,positive):
    result=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        va,vb=float(a@values),float(b@values)
        ina=va>=-1e-10 if positive else va<=1e-10
        inb=vb>=-1e-10 if positive else vb<=1e-10
        if ina:result.append(a)
        if ina!=inb:result.append(a+(b-a)*(va/(va-vb)))
    clean=[]
    for p in result:
        if not clean or np.max(abs(p-clean[-1]))>1e-9:clean.append(p)
    if len(clean)>1 and np.max(abs(clean[0]-clean[-1]))<1e-9:clean.pop()
    return clean

levels=[]
for lod in range(3):
    name='PedestrianDrawBridge01'+(f'_LOD{lod}' if lod else '')+' Mesh'
    digest=hashlib.sha256((r/'movable-skin'/(name+'.Geometry')).read_bytes()).hexdigest()
    assert any(a.get('sha256')==digest for a in audit['assets'].values())
    d=np.load(r/f'pedestrian01-main-{lod}-0.npz');v,f=d['positions'],d['indices']
    selected=np.zeros(len(v),bool);selected[np.load(r/f'pedestrian01-unmapped-{lod}.npy')]=True
    faces=np.flatnonzero(selected[f].any(1))
    stem=(np.abs(v[f,0])<=.24104309).all(1)&(v[f,1]>=23.296875).all(1)&(v[f,1]<=23.962402).all(1)&selected[f].all(1)
    stems=np.zeros(len(v),bool);stems[np.unique(f[stem])]=True
    assert np.all(stems[f].any(1)==stem)
    weights={int(i):weight(v[i],stems[i]) for i in np.flatnonzero(selected)}
    # Existing foot vertices belong to the base map and must agree at the join.
    for i in np.unique(f[faces]):
        if not selected[i]:assert weight(v[i])==np.sign(v[i,0])
    edges=[];interior=[];fragments=[];extra=[];extra_weights=[]
    for fid in faces:
        tri=f[fid];p=v[tri];w=np.array([weight(q,bool(stem[fid])) for q in p])
        if stem[fid] or np.all(w==w[0]):continue
        polys=[list(np.eye(3))]
        # Each slab makes the inner-leg boundary a linear plane.
        for y in profile[:,0]:
            values=p[:,1]-y;new=[]
            for poly in polys:
                vals=np.array([b@values for b in poly])
                if vals.min() < -1e-9 and vals.max()>1e-9:
                    new.extend([clip(poly,values,False),clip(poly,values,True)])
                else:new.append(poly)
            polys=new
        split=[]
        for poly in polys:
            ys=np.array([b@p[:,1] for b in poly]);lo,hi=ys.min(),ys.max()
            slope=(span(hi)-span(lo))/(hi-lo) if hi-lo>1e-9 else 0
            intercept=span(lo)-slope*lo
            pieces=[poly]
            for sign in (-1,1):
                values=p[:,0]-sign*(slope*p[:,1]+intercept);new=[]
                for piece in pieces:
                    vals=np.array([b@values for b in piece])
                    if vals.min() < -1e-9 and vals.max()>1e-9:
                        new.extend([clip(piece,values,False),clip(piece,values,True)])
                    else:new.append(piece)
                pieces=new
            split.extend(pieces)
        # Store barycentric recipes; edge and interior indices are resolved after
        # all faces have been processed, matching the native structure model.
        local={};out=[]
        for poly in split:
            polygon=[]
            for bary in poly:
                nz=np.flatnonzero(abs(bary)>1e-8)
                if len(nz)==1:polygon.append(('s',int(tri[nz[0]])));continue
                key=tuple(np.round(bary,10))
                if key not in local:
                    point=bary@p;ww=weight(point)
                    if len(nz)==2:
                        a,b=map(int,nz);idx=len(edges);edges.append((int(tri[a]),int(tri[b]),float(bary[b]),ww));kind='e'
                    else:
                        idx=len(interior);interior.append((*map(int,tri),float(bary[1]),float(bary[2]),ww));kind='i'
                    local[key]=(kind,idx)
                polygon.append(local[key])
            for j in range(1,len(polygon)-1):out.extend((polygon[0],polygon[j],polygon[j+1]))
        fragments.append((int(fid),out))
    def index(item):
        kind,i=item
        return i if kind=='s' else len(v)+i+(len(edges) if kind=='i' else 0)
    fragments=[(fid,[index(x) for x in ii]) for fid,ii in fragments]
    for a,b,t,w in edges:extra.append(v[a]+(v[b]-v[a])*t);extra_weights.append(w)
    for a,b,c,wb,wc,w in interior:extra.append(v[a]+(v[b]-v[a])*wb+(v[c]-v[a])*wc);extra_weights.append(w)
    original=np.vstack((v,np.array(extra).reshape(-1,3)))
    motion=np.zeros(len(original))
    for i,w in weights.items():motion[i]=w
    for i in np.unique(f[faces]):
        if not selected[i]:motion[i]=np.sign(v[i,0])
    motion[len(v):]=extra_weights
    replaced={fid for fid,_ in fragments}
    tested=np.array([f[fid].tolist() for fid in faces if fid not in replaced]+[ii[j:j+3] for _,ii in fragments for j in range(0,len(ii),3)])
    a=original[tested];n=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);valid=np.linalg.norm(n,axis=1)>1e-9
    for delta in (-.03,0,2,8):
        q=original.copy();q[:,0]+=motion*delta;b=q[tested]
        m=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);assert np.all((n[valid]*m[valid]).sum(1)>0)
    np.savez(r/f'pedestrian01-frame-plan-{lod}.npz',positions=original,indices=tested,motion=motion)
    levels.append((len(v),len(f),digest,weights,edges,interior,fragments))
    print('PASS',lod,'original mapped',len(weights),'edge',len(edges),'interior',len(interior),'replaced faces',len(fragments))

# Runtime only consumes immutable recipes, preserving raw source channels.
lines=['// Generated by PedestrianDraw01Frames.py from measured A-frame inner-leg stations.',
       'using System;','using UnityEngine;','','namespace BridgeBuilder.Bridges;','',
       'internal static class PedestrianDraw01Frames','{',
       '    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, int[] indices, float delta)',
       '    {','        var data = lod switch','        {']
for lod,(count,faces,*_) in enumerate(levels):lines.append(f'            {lod} => (Count: {count}, Faces: {faces}, Motion: Motion{lod}, Edges: Edges{lod}, Interior: Interior{lod}, Fragments: Fragments{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))','        };',
        '        if (source.Length != data.Count || indices.Length != data.Faces * 3 || !float.IsFinite(delta) || delta <= -0.24104309f)',
        '            throw new InvalidOperationException("PedestrianDraw01 A-frame layout or span is invalid.");',
        '        var positions = (Vector3[])source.Clone();',
        '        foreach (var item in data.Motion) positions[item.Index].x = source[item.Index].x + item.Weight * delta;',
        '        var edges = new MovableStructureModel.EdgeVertex[data.Edges.Length];',
        '        var inside = new MovableStructureModel.TriangleVertex[data.Interior.Length];',
        '        var x = new float[edges.Length + inside.Length];',
        '        for (var i = 0; i < edges.Length; i++) { var e = data.Edges[i];',
        '            edges[i] = new MovableStructureModel.EdgeVertex(e.A, e.B, e.T);',
        '            x[i] = Mathf.LerpUnclamped(source[e.A].x, source[e.B].x, e.T) + e.W * delta; }',
        '        for (var i = 0; i < inside.Length; i++) { var e = data.Interior[i];',
        '            inside[i] = new MovableStructureModel.TriangleVertex(e.A, e.B, e.C, e.BW, e.CW);',
        '            x[edges.Length + i] = source[e.A].x + (source[e.B].x-source[e.A].x)*e.BW + (source[e.C].x-source[e.A].x)*e.CW + e.W*delta; }',
        '        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[data.Faces] },',
        '            addedVertices: edges, interiorVertices: inside, fragments: data.Fragments, constructedX: x);',
        '    }','']
for lod,(_,_,digest,weights,edges,interior,fragments) in enumerate(levels):
    lines+=['    // Geometry SHA-256: '+digest,f'    private static readonly (int Index, float Weight)[] Motion{lod} = {{']
    lines += [f'        ({i}, {w:.9g}f),' for i,w in weights.items()];lines+=['    };']
    for label,typ,rows,integer_count in [('Edges','int A, int B, float T, float W',edges,2),('Interior','int A, int B, int C, float BW, float CW, float W',interior,3)]:
        lines += [f'    private static readonly ({typ})[] {label}{lod} = {{']
        lines += ['        ('+', '.join(str(x) if j<integer_count else f'{x:.9g}f' for j,x in enumerate(row))+'),' for row in rows];lines+=['    };']
    lines += [f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} = {{']
    lines += [f'        new MovableStructureModel.Fragment(0, {fid}, '+', '.join(map(str,ii))+'),' for fid,ii in fragments];lines+=['    };','']
lines+=['}',''];output.write_text('\n'.join(lines),encoding='utf-8')
