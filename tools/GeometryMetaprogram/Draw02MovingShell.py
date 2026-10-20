"""Record Draw02 moving-shell road replacement fragments from native geometry.
Usage: python Draw02MovingShell.py RESEARCH_DIRECTORY OUTPUT_CS
Planes are offline measured part boundaries, never runtime classifiers.
"""
from pathlib import Path
import hashlib
import sys
import numpy as np

research, output = map(Path, sys.argv[1:])
records = []
boundary_weights = []
body_sides = []
for lod in range(3):
    d = np.load(research / f'draw02-main-lod{lod}-uv.npz')
    v, faces = d['positions'].astype(np.float32), d['indices'].reshape(-1, 3)
    parent = list(range(len(v)))
    def root(a):
        while parent[a] != a:
            parent[a] = parent[parent[a]]
            a = parent[a]
        return a
    def join(a,b): parent[root(int(a))] = root(int(b))
    for a,b,c in faces:
        join(a,b); join(a,c)
    seen = {}
    for i,p in enumerate(v):
        key = tuple(p)
        if key in seen: join(i,seen[key])
        else: seen[key] = i
    groups = {}
    for i in range(len(v)): groups.setdefault(root(i),[]).append(i)
    # Reviewed primary lateral shell assemblies, distinct from the under-road base.
    expected = ((5844,5844),(5006,5014),(193,188))[lod]
    candidates = []
    for ids in groups.values():
        p=v[ids]; lo,hi=p.min(0),p.max(0)
        if lo[0] < -9 and hi[0] > 9 and (lo[2] > 0 or hi[2] < 0) and max(abs(lo[2]),abs(hi[2])) < 80 and hi[1] >= 11:
            candidates.append(ids)
    assert sorted(map(len,candidates)) == sorted(expected), (lod,list(map(len,candidates)))
    selected=set(i for ids in candidates for i in ids)
    # Six authored lane separators (three per leaf) belong to the prototype road,
    # not to its structural frame. The selected road supplies its own layout.
    # The far proxy has no separate separator components.
    separators=[]
    for ids in groups.values():
        p=v[ids];lo,hi=p.min(0),p.max(0)
        if 9.78 < lo[1] < 9.79 and 10.29 < hi[1] < 10.31 and hi[2]-lo[2] > 70:
            assert lod < 2 and len(ids) == (500 if lod == 0 else 320)
            assert hi[0]-lo[0] < .343
            center=round(float((lo[0]+hi[0])/2))
            assert center in (-3,0,3)
            assert abs(float((lo[0]+hi[0])/2)-center) < .0001
            assert (lo[2] > 0) != (hi[2] < 0)
            separators.append((center,1 if lo[2]>0 else -1,ids))
    assert {(x,z) for x,z,_ in separators} == ({(x,z) for x in (-3,0,3) for z in (-1,1)} if lod<2 else set())
    separator_vertices=set(i for _,_,ids in separators for i in ids)
    assert not separator_vertices.intersection(selected)
    # The primary shell retains lateral frames/inner girders and the base end
    # closures after road replacement. The near/middle under-road base is a
    # separate native assembly; the far proxy combines it into the primary shell.
    # Transverse braces and hinge axles are separate and are NOT assigned here.
    base_parts = []
    if lod < 2:
        for ids in groups.values():
            p=v[ids]; lo,hi=p.min(0),p.max(0)
            if (-5.81 < lo[0] < -5.79 and 5.79 < hi[0] < 5.81 and
                    hi[2]-lo[2] > 70 and (lo[2] > 0 or hi[2] < 0)):
                assert lo[1] == np.float32(4.20556640625)
                assert hi[1] == np.float32(9.699951171875)
                base_parts.append(ids)
        assert sorted(map(len,base_parts)) == ([2556,2556] if lod == 0 else [2542,2544])
    body = sorted(selected.union(i for ids in base_parts for i in ids))
    sides = {side:[i for i in body if np.sign(v[i,0]) == side] for side in (-1,1)}
    body_sides.append(sides)
    for delta in (-.5,0,2,8):
        moved=v[body].copy()
        moved[:,0]+=np.sign(moved[:,0])*np.float32(delta)
        assert np.array_equal(moved[:,1:],v[body,1:])
        assert np.isfinite(moved).all()
        for side in (-1,1):
            mask=np.sign(v[body,0])==side
            assert np.allclose(moved[mask,0]-v[body,0][mask],side*delta,atol=2e-6)
        if delta == 0: assert np.array_equal(moved,v[body])
    changed=[]; fragments=[]; edges=[]; interior=[]; edge_lookup={}; interior_lookup={}
    # Work in source triangle barycentrics so every native attribute can be retained.
    def split(poly, points, axis, boundary, sign):
        positive=[];negative=[]
        if not poly:return positive,negative
        distances=[sign*(float(a @ points[:,axis])-boundary) for a in poly]
        if max(distances)<=0:return [],poly
        if min(distances)>=0:return poly,[]
        for i,a in enumerate(poly):
            b=poly[(i+1)%len(poly)]
            da=sign*(float(a @ points[:,axis])-boundary)
            db=sign*(float(b @ points[:,axis])-boundary)
            if da >= 0:positive.append(a)
            if da <= 0:negative.append(a)
            if (da < 0 < db) or (db < 0 < da):
                t=da/(da-db); q=a+(b-a)*t
                positive.append(q);negative.append(q)
        return positive,negative
    def vertex(bary, tri):
        bary=np.where(abs(bary)<1e-9,0,bary)
        nonzero=np.flatnonzero(bary)
        if len(nonzero)==1:return ('v',int(tri[nonzero[0]]))
        if len(nonzero)==2:
            a,b=map(int,nonzero); ia,ib=int(tri[a]),int(tri[b]);t=float(bary[b])
            if ia>ib:ia,ib,t=ib,ia,1-t
            t=float(np.float32(t));key=(ia,ib,t)
            if key not in edge_lookup:edge_lookup[key]=len(edges);edges.append(key)
            return ('e',edge_lookup[key])
        key=(*map(int,tri),float(np.float32(bary[1])),float(np.float32(bary[2])))
        if key not in interior_lookup:interior_lookup[key]=len(interior);interior.append(key)
        return ('i',interior_lookup[key])
    for ordinal,tri in enumerate(faces):
        if int(tri[0]) in separator_vertices:
            assert all(int(i) in separator_vertices for i in tri)
            changed.append(ordinal)
            continue
        if int(tri[0]) not in selected:continue
        points=v[tri].astype(float); remaining=[np.eye(3)[i] for i in range(3)]; kept=[]
        # Near/middle outer railings are separate recorded components. The far shell
        # incorporates them; retain its two native outer strips before clipping road height.
        planes=[(1,9.699951171875,1)] if lod<2 else [
            (0,-9.64990234375,1),(0,9.64990234375,-1),(1,9.699951171875,1)]
        for axis,boundary,sign in planes:
            remaining,rejected=split(remaining,points,axis,boundary,sign)
            if len(rejected)>=3:kept.append(rejected)
        def area(poly):
            p=np.array(poly)@points
            return sum(np.linalg.norm(np.cross(p[i]-p[0],p[i+1]-p[0]))/2 for i in range(1,len(p)-1))
        removed_area=area(remaining) if len(remaining)>=3 else 0
        if removed_area < 1e-9:continue
        original_area=np.linalg.norm(np.cross(points[1]-points[0],points[2]-points[0]))/2
        assert abs(sum(area(p) for p in kept)+removed_area-original_area)<max(1e-7,original_area*1e-7)
        changed.append(ordinal)
        indices=[]
        for poly in kept:
            if area(poly)<1e-9:continue
            ids=[vertex(b,tri) for b in poly]
            for j in range(1,len(ids)-1):indices.extend((ids[0],ids[j],ids[j+1]))
        if indices:fragments.append((ordinal,indices))
    name='DrawBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh.Geometry'
    sha=hashlib.sha256((research/'movable-skin'/name).read_bytes()).hexdigest()
    records.append((len(v),len(faces),changed,edges,interior,fragments,sha))
    separator_faces=sum(int(tri[0]) in separator_vertices for tri in faces)
    fragment_faces={f for f,_ in fragments}
    assert not any(face in fragment_faces for face,tri in enumerate(faces) if int(tri[0]) in separator_vertices)
    print(f'LOD{lod}: removed {len(separators)} prototype lane separators ({separator_faces} triangles), preserving separate structural groups')
    # All retained construction boundaries belong to the under-road closure or
    # lateral outer shell. Both use rigid half-width displacement on each side.
    # Do not interpolate displacement from a centreline endpoint: that would
    # stretch the end closure instead of applying the recorded base mapping.
    constructed = np.array([
        v[a] + (v[b] - v[a]) * np.float32(t) for a,b,t in edges
    ] + [
        v[a] + (v[b] - v[a]) * np.float32(wb) + (v[c] - v[a]) * np.float32(wc)
        for a,b,c,wb,wc in interior
    ], dtype=np.float32)
    assert len(constructed) == (880,876,24)[lod]
    assert np.all(np.abs(constructed[:,1] - 9.699951171875) < 2e-6)
    weights = np.sign(constructed[:,0]).astype(np.int8)
    boundary_weights.append(weights)
    for delta in (-.5,0,2,8):
        moved = constructed.copy()
        moved[:,0] += weights * np.float32(delta)
        assert np.array_equal(moved[:,1:],constructed[:,1:])
        assert np.allclose(moved[:,0]-constructed[:,0],weights*delta,atol=2e-6)
        assert np.isfinite(moved).all()
        if delta == 0: assert np.array_equal(moved,constructed)
    print(f'LOD{lod}: {len(changed)} replaced faces, {len(fragments)} retained fragments, {len(edges)} edge/{len(interior)} interior vertices; source-area conservation passed')

lines=['// DrawBridge02 moving-shell fragments, game 1.6.2f1.',
       '// Generated by tools/GeometryMetaprogram/Draw02MovingShell.py.',
       '// Source triangle attributes are interpolated; no runtime geometric classification.',
       'using System;', 'using UnityEngine;', '', 'namespace BridgeBuilder.Bridges;', '',
       'internal static class Draw02MovingShell', '{',
       '    internal static MovableStructureModel.Plan Create(int lod, Vector3[] source, Vector3[] positions, int[] indices, float delta)',
       '    {',
       '        if (!float.IsFinite(delta) || delta <= Draw02TransverseMembers.MinimumHalfWidthDelta)',
       '            throw new InvalidOperationException("Draw02 selected width collapses or reverses its recorded central linkage span.");',
       '        var data = lod switch', '        {']
for lod,(nv,nf,*_) in enumerate(records):
    lines += [f'            {lod} => (Vertices: {nv}, Faces: {nf}, Removed: Removed{lod}, Edges: Edges{lod}, Interior: Interior{lod}, Fragments: Fragments{lod}, Weights: BoundaryWeights{lod}, Left: Left{lod}, Right: Right{lod}),']
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))', '        };',
        '        if (source.Length != data.Vertices || positions.Length != data.Vertices || indices.Length != data.Faces * 3)',
        '            throw new InvalidOperationException("Draw02 shell replacement requires its recorded main mesh.");',
        '        positions = (Vector3[])positions.Clone();',
        '        Draw02RigidParts.Apply(lod, source, positions, delta);',
        '        Draw02FixedBodies.Apply(lod, source, positions, delta);',
        '        if (lod < 2) Draw02TransverseMembers.Apply(lod, source, positions, delta);',
        '        foreach (var index in data.Left) positions[index].x = source[index].x - delta;',
        '        foreach (var index in data.Right) positions[index].x = source[index].x + delta;',
        '        var mask = new bool[data.Faces];',
        '        foreach (var face in data.Removed) mask[face] = true;',
        '        var constructedX = new float[data.Weights.Length];',
        '        for (var i = 0; i < data.Edges.Length; i++)',
        '        {',
        '            var edge = data.Edges[i];',
        '            constructedX[i] = Vector3.LerpUnclamped(source[edge.A], source[edge.B], edge.Fraction).x + data.Weights[i] * delta;',
        '        }',
        '        for (var i = 0; i < data.Interior.Length; i++)',
        '        {',
        '            var vertex = data.Interior[i];',
        '            var a = source[vertex.A];',
        '            constructedX[data.Edges.Length + i] = (a + (source[vertex.B] - a) * vertex.WeightB +',
        '                (source[vertex.C] - a) * vertex.WeightC).x + data.Weights[data.Edges.Length + i] * delta;',
        '        }',
        '        var plan = new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { mask },',
        '            data.Edges, data.Fragments, data.Interior, constructedX);',
        '        return Draw02RoadTops.Remove(lod, plan, delta);', '    }','']
for lod,(nv,nf,changed,edges,interior,fragments,sha) in enumerate(records):
    for side,name in ((-1,'Left'),(1,'Right')):
        ids=body_sides[lod][side]
        lines += [f'    private static readonly int[] {name}{lod} = new int[]', '    {']
        for i in range(0,len(ids),20):
            lines += ['        ' + ', '.join(map(str,ids[i:i+20])) + ',']
        lines += ['    };']
    lines += [f'    private static readonly sbyte[] BoundaryWeights{lod} = new sbyte[]', '    {']
    for i in range(0,len(boundary_weights[lod]),24):
        lines += ['        ' + ', '.join(map(str,boundary_weights[lod][i:i+24])) + ',']
    lines += ['    };']
    lines+=['    // Geometry SHA-256: '+sha,f'    private static readonly int[] Removed{lod} = new int[]','    {']
    for i in range(0,len(changed),20):lines+=['        '+', '.join(map(str,changed[i:i+20]))+',']
    lines+=['    };',f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{lod} = new MovableStructureModel.EdgeVertex[]','    {']
    lines += [f'        new({a}, {b}, {t:.9g}f),' for a,b,t in edges]
    lines+=['    };',f'    private static readonly MovableStructureModel.TriangleVertex[] Interior{lod} = new MovableStructureModel.TriangleVertex[]','    {']
    lines += [f'        new({a}, {b}, {c}, {wb:.9g}f, {wc:.9g}f),' for a,b,c,wb,wc in interior]
    lines+=['    };',f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} = new MovableStructureModel.Fragment[]','    {']
    for face,indices in fragments:
        ids=[i if kind=='v' else nv+i if kind=='e' else nv+len(edges)+i for kind,i in indices]
        lines += [f'        new(0, {face}, '+', '.join(map(str,ids))+'),']
    lines+=['    };','']
lines+=['}','']
output.write_text('\n'.join(lines),encoding='utf-8')
