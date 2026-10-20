"""Emit Draw03 far shell widening and road removal from measured native parts.
Usage: python Draw03FarStructure.py RESEARCH_DIR OUTPUT_CS
The near central-wall boundary supplies the fixed platform split. Original far shells
and materials are retained; no detailed geometry is copied into this LOD.
"""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np

r, output = map(Path, sys.argv[1:])
digest = hashlib.sha256((r/'movable-skin/DrawBridge03_LOD2 Mesh.Geometry').read_bytes()).hexdigest()
assert digest == '3eb2b0a0489ef05c8bbac6c9ca9f536303802705d6f45ad8b4c4eb6d424621dc'
d = np.load(r/'draw03-main-2.npz')
v, faces = d['positions'].astype(float), d['indices']
assert v.shape == (684, 3) and faces.shape == (448, 3)
parts = json.loads((r/'draw03-parts-2.json').read_text())
assert [len(p['vertices']) for p in parts] == [148,148,39,39,152,158]
wall = 10.37841796875
road = 12.5
weights = np.zeros(len(v))
owners = np.full(len(faces), -1)
for j, p in enumerate(parts):
    ids = p['vertices']
    membership = np.isin(faces, ids)
    assert not np.any(membership.any(1) & ~membership.all(1))
    owners[membership.all(1)] = j
    assert set(d['bones'][membership.all(1)]) == {(0,0,0,0,2,1)[j]}
    if j < 2:
        weights[ids] = np.clip(v[ids,0]/wall, -1, 1)
    elif j < 4:
        weights[ids] = np.sign(v[ids,0])
    else:
        extent = (12.992919921875,12.923583984375)[j-4]
        assert np.max(abs(v[ids,0])) == extent
        weights[ids] = v[ids,0]/extent
assert np.all(owners >= 0)

def clip(poly, points, boundary, left):
    result = []
    for a,b in zip(poly, poly[1:]+poly[:1]):
        xa,xb = a@points[:,0],b@points[:,0]
        ina = xa<=boundary if left else xa>=boundary
        inb = xb<=boundary if left else xb>=boundary
        if ina: result.append(a)
        if ina != inb: result.append(a+(b-a)*((boundary-xa)/(xb-xa)))
    clean = []
    for p in result:
        if not clean or np.max(abs(p-clean[-1]))>1e-10: clean.append(p)
    if len(clean)>1 and np.max(abs(clean[0]-clean[-1]))<=1e-10: clean.pop()
    return clean

edges, extra, signs, fragments, removed = [], [], [], [], []
road_faces = []
for ordinal, face in enumerate(faces):
    part = owners[ordinal]
    if part in (2,3): continue
    points = v[face]
    normal = np.cross(points[1]-points[0],points[2]-points[0])
    top = np.all(points[:,1] == 10.356689453125) and normal[1]>0
    polys = [list(np.eye(3))]
    if top:
        # Original near road ends at +/-12.5; preserve the exterior shoulder in far.
        polys = [clip(polys[0],points,-road,True),clip(polys[0],points,road,False)]
        polys = [p for p in polys if len(p)>=3]
        if np.min(points[:,0])>=-road and np.max(points[:,0])<=road:
            removed.append(ordinal);road_faces.append(ordinal);continue
        if np.max(points[:,0])<=-road or np.min(points[:,0])>=road: continue
        road_faces.append(ordinal)
    elif part < 2:
        for cut in (-wall,wall):
            split = []
            for poly in polys:
                xs = [p@points[:,0] for p in poly]
                if min(xs)<cut<max(xs):
                    split.extend((clip(poly,points,cut,True),clip(poly,points,cut,False)))
                else: split.append(poly)
            polys = split
        if len(polys)==1: continue
    else: continue
    indices, local = [], {}
    for poly in polys:
        polygon = []
        for bary in poly:
            nonzero = np.flatnonzero(abs(bary)>1e-10)
            if len(nonzero)==1:
                polygon.append(int(face[nonzero[0]]));continue
            assert len(nonzero)==2
            a,b = map(int,nonzero)
            t = float(np.float32(bary[b]))
            key = (int(face[a]),int(face[b]),t)
            if key not in local:
                local[key] = len(v)+len(edges)
                point = points[a]+(points[b]-points[a])*t
                boundary = road if top else wall
                assert abs(abs(point[0])-boundary)<2e-6
                edges.append(key);extra.append(point);signs.append(-1 if point[0]<0 else 1)
            polygon.append(local[key])
        for i in range(1,len(polygon)-1): indices.extend((polygon[0],polygon[i],polygon[i+1]))
    assert indices
    fragments.append((ordinal,indices))

allv = np.concatenate((v,np.array(extra).reshape(-1,3)))
allw = np.concatenate((weights,np.array(signs)))
replacements = dict(fragments)
result = np.array([tri for j,f in enumerate(faces) if j not in removed
                   for tri in np.array(replacements.get(j,f)).reshape(-1,3)])
assert (len(road_faces), len(removed), len(fragments), len(edges), len(result)) == (48, 0, 80, 160, 536)
for ordinal in road_faces:
    retained = allv[np.array(replacements[ordinal]).reshape(-1,3)]
    assert np.all((retained[:,:,0].min(1) >= road-2e-6) |
                  (retained[:,:,0].max(1) <= -road+2e-6))
for ordinal, indices in fragments:
    points=v[faces[ordinal]];n=np.cross(points[1]-points[0],points[2]-points[0])
    q=allv[np.array(indices).reshape(-1,3)]
    area=np.linalg.norm(np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]),axis=1).sum()
    if ordinal in road_faces:
        # Retained exterior area plus analytically clipped interior equals original.
        poly=clip(clip(list(np.eye(3)),points,-road,False),points,road,True)
        cut=np.array([p@points for p in poly])
        area+=sum(np.linalg.norm(np.cross(cut[i]-cut[0],cut[i+1]-cut[0])) for i in range(1,len(cut)-1))
    assert abs(area-np.linalg.norm(n))<1e-4
for delta in (-16,-8,-.1,0,2,8):
    q=allv.copy();q[:,0]+=allw*delta
    assert np.array_equal(q[:,1:],allv[:,1:])
    p0,p1=allv[result],q[result]
    n0=np.cross(p0[:,1]-p0[:,0],p0[:,2]-p0[:,0]);n1=np.cross(p1[:,1]-p1[:,0],p1[:,2]-p1[:,0])
    valid=np.linalg.norm(n0,axis=1)>1e-8
    if delta >= -.1: assert np.all(np.sum(n0[valid]*n1[valid],axis=1)>0)
    # Recheck the composed output, not only each source clipping operation. Native
    # far geometry has no separate raised sidewalk joint: all retained deck-level
    # top faces must stay outside the replacement road after widening as well.
    deck_top = np.all(p0[:,:,1] == 10.356689453125, axis=1) & (n0[:,1] > 0)
    assert int(deck_top.sum()) == 140
    top = p1[deck_top]
    assert np.all((top[:,:,0].min(1) >= road+delta-2e-6) |
                  (top[:,:,0].max(1) <= -road-delta+2e-6))
    for side in (-1,1):
        mask=allw==side
        assert np.allclose(q[mask]-allv[mask],[side*delta,0,0])

def lit(x): return format(float(np.float32(x)),'.9g')+'f'
lines=['// Native DrawBridge03 far structure; generated by Draw03FarStructure.py.',
       '// Geometry SHA-256: '+digest,'using System;','using UnityEngine;','',
       'namespace BridgeBuilder.Bridges;','','internal static class Draw03FarStructure','{',
       '    internal static MovableStructureModel.Plan Create(Vector3[] source, int[] indices, float delta)',
       '    {',
       '        if (source.Length != 684 || indices.Length != 1344 || !float.IsFinite(delta))',
       '            throw new InvalidOperationException("Draw03 far structure requires its measured native layout.");',
       '        var positions = (Vector3[])source.Clone();',
       '        for (var i = 0; i < positions.Length; i++) positions[i].x += Weights[i] * delta;',
       '        var removed = new bool[448];',
       '        foreach (var face in Removed) removed[face] = true;',
       '        var x = new float[Edges.Length];',
       '        for (var i = 0; i < x.Length; i++)',
       '        {', '            var edge = Edges[i];',
       '            x[i] = Mathf.LerpUnclamped(source[edge.A].x, source[edge.B].x, edge.Fraction) + Signs[i] * delta;',
       '        }',
       '        return new MovableStructureModel.Plan(source, positions, new[] { indices },',
       '            new[] { removed }, Edges, Fragments, constructedX: x);','    }','',
       '    private static readonly float[] Weights =','    {']
for i in range(0,len(weights),12): lines+=['        '+', '.join(map(lit,weights[i:i+12]))+',']
lines+=['    };','    private static readonly int[] Removed = { '+', '.join(map(str,removed))+' };',
        '    private static readonly MovableStructureModel.EdgeVertex[] Edges =','    {']
lines += [f'        new({a}, {b}, {lit(t)}),' for a,b,t in edges]
lines += ['    };','    private static readonly float[] Signs = { '+', '.join(map(lit,signs))+' };',
          '    private static readonly MovableStructureModel.Fragment[] Fragments =','    {']
lines += ['        new(0, '+str(j)+', '+', '.join(map(str,ids))+'),' for j,ids in fragments]
lines += ['    };','}','']
output.write_text('\n'.join(lines),encoding='utf-8')
np.savez(r/'draw03-far-structure-plan.npz',positions=allv,indices=result,weights=allw)
print(f'Far: {len(road_faces)} road-bearing faces; {len(removed)} whole removals; {len(fragments)} fragments; {len(edges)} edge vertices; {len(result)} final structural triangles. Area and six signed widths and positive-span winding pass.')
