"""Offline review only: count the smallest U-period cuts on 129 measured profiles.

This is not an authorized production recipe. It writes no runtime data or assets.
Source attributes on new points are represented by exact triangle barycentrics.
"""
import json
import math
from collections import defaultdict
from pathlib import Path

import numpy as np
from CheckOriginalMapping import ROOT, motion, digest


def phases(parts, p, uv, indices, changed):
    """Offline measured adjacency, never runtime source-part discovery."""
    edges=defaultdict(list); graph=defaultdict(list); roots={}
    for part in parts:
        seed=part['seedFace']; ids=indices[part['faces']].ravel()
        x=p[ids,0]; us=uv[ids,0]; lo,hi=us.min(),us.max()
        slope=(us[np.argmax(x)]-us[np.argmin(x)])/np.ptp(x)
        anchor=int(ids[np.argmin(x)]); shift=changed[anchor,0]-p[anchor,0]
        near=int(ids[np.argmin(np.abs(x))])
        roots[seed]=(abs(p[near,0]),float(-slope*(changed[near,0]-p[near,0]-shift)/(hi-lo)),near)
        for face in part['faces']:
            ix=indices[face]
            q=(uv[ix,0]+slope*(changed[ix,0]-p[ix,0]-shift)-lo)/(hi-lo)
            old=(uv[ix,0]-lo)/(hi-lo)
            for a,b in ((0,1),(1,2),(2,0)):
                order=sorted((a,b),key=lambda k:tuple(p[ix[k]]))
                key=tuple(tuple(p[ix[k]]) for k in order)
                edges[key].append((seed,q[order],old[order],int(face)))
    constraints=[]
    for edge,owners in edges.items():
        if len(owners)!=2 or owners[0][0]==owners[1][0]:continue
        a,b=owners
        delta=(a[1]-b[1])-(a[2]-b[2])
        assert abs(delta[0]-delta[1])<1e-10
        correction=float(delta[0])
        first_vertex=next(int(i) for i in indices[a[3]] if tuple(p[i])==edge[0])
        second_vertex=next(int(i) for i in indices[b[3]] if tuple(p[i])==edge[0])
        graph[a[0]].append((b[0],correction,first_vertex,second_vertex))
        graph[b[0]].append((a[0],-correction,second_vertex,first_vertex))
        constraints.append(dict(first=a[0],second=b[0],faces=[a[3],b[3]],offsetDifference=correction,
            firstVertex=first_vertex,secondVertex=second_vertex,
            originalPhaseDifference=(a[2]-b[2]).tolist(),positions=[list(v) for v in edge]))
    offsets={};components=[];maximum=0.;tree=[]
    for root in sorted(roots,key=lambda k:(roots[k][0],roots[k][2],k)):
        if root in offsets:continue
        offsets[root]=roots[root][1];pending=[root];group=[]
        tree.append(dict(domain=root,parent=None,anchorVertex=roots[root][2]))
        while pending:
            a=pending.pop();group.append(a)
            for b,c,va,vb in graph[a]:
                wanted=offsets[a]+c
                if b not in offsets:
                    offsets[b]=wanted;pending.append(b)
                    tree.append(dict(domain=b,parent=a,parentVertex=va,vertex=vb))
                else:maximum=max(maximum,abs(wanted-offsets[b]))
        components.append(dict(root=root,anchorVertex=roots[root][2],domains=sorted(group)))
    assert maximum<1e-10
    return offsets,dict(components=components,constraints=constraints,tree=tree,maxCycleResidual=maximum,
        rule='Anchor each measured connected component at its authored vertex nearest X=0. Transport signed U gradients; propagate offsets over original shared edges preserving their original phase difference, including mirrors. Disconnected components retain independent native phase.')


def clipped(poly, q, boundary, greater):
    result = []
    for a, b in zip(poly, poly[1:]+poly[:1]):
        qa, qb = float(a@q), float(b@q)
        inside_a = qa >= boundary if greater else qa <= boundary
        inside_b = qb >= boundary if greater else qb <= boundary
        if inside_a:
            result.append(a)
        if inside_a != inside_b:
            result.append(a+(b-a)*((boundary-qa)/(qb-qa)))
    clean = []
    for p in result:
        if not clean or np.max(np.abs(clean[-1]-p)) > 1e-12:
            clean.append(p)
    if len(clean)>1 and np.max(np.abs(clean[0]-clean[-1]))<1e-12:
        clean.pop()
    return clean


def main():
    policy_path=ROOT/'artifacts/prepared-materials/Lift05/non-tiling/classification.json'
    policy=json.loads(policy_path.read_text())
    levels=[]
    for lod in range(2):
        arrays_path=ROOT/f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz'
        arrays=np.load(arrays_path)
        p=arrays['mesh0_positions'].astype(float)
        uv=arrays['mesh0_uv0'].astype(float)
        f=arrays['mesh0_indices']
        bones=arrays['mesh0_boneIndices'].ravel()
        weights,hashes=motion(lod,len(p))
        changed=p.copy()
        changed[:,0]=(p[:,0].astype(np.float32)+weights*np.float32(3)).astype(float)
        render='LiftBridge05 Mesh' if lod==0 else 'LiftBridge05_LOD1 Mesh'
        parts=next(m for m in policy['meshes'] if m['render']==render and m['mesh']==0)['mixedProfileEvidence']
        offsets,phase_evidence=phases(parts,p,uv,f,changed)
        faces=[]; split_keys=set(); original_keys=set(); original_vertices=set()
        max_residual=0.
        for part in parts:
            ids=f[part['faces']].ravel()
            xyz=p[ids]; us=uv[ids,0]
            coeff=np.linalg.lstsq(np.column_stack((xyz[:,0],np.ones(len(ids)))),us,rcond=None)[0]
            residual=float(np.max(np.abs(np.column_stack((xyz[:,0],np.ones(len(ids))))@coeff-us)))
            max_residual=max(max_residual,residual)
            assert residual < 1e-10
            lo,hi=float(us.min()),float(us.max()); period=hi-lo
            assert period>0 and abs(float(np.ptp(xyz[:,0]))-4)<1e-5
            coeff[0]=(us[np.argmax(xyz[:,0])]-us[np.argmin(xyz[:,0])])/float(np.ptp(xyz[:,0]))
            # Anchor the existing minimum-X endpoint and preserve signed native U
            # gradient, including mirrors. This choice is explicit review data.
            anchor=int(ids[np.argmin(xyz[:,0])])
            shift=changed[anchor,0]-p[anchor,0]
            for face_id in part['faces']:
                indices=f[face_id]; source=uv[indices]; positions=changed[indices]
                q=(source[:,0]+coeff[0]*(positions[:,0]-p[indices,0]-shift)-lo)/period+offsets[part['seedFace']]
                assert len(set(bones[indices]))==1
                original_vertices.update(map(int,indices))
                cuts=list(range(math.floor(float(q.min()))+1,math.ceil(float(q.max()))))
                intervals=[]
                for cell in range(math.floor(float(q.min())),math.ceil(float(q.max()))):
                    poly=clipped(clipped(list(np.eye(3)),q,cell,True),q,cell+1,False)
                    if len(poly)<3:continue
                    area=sum(np.linalg.norm(np.cross((poly[i]@positions)-(poly[0]@positions),
                        (poly[i+1]@positions)-(poly[0]@positions))) for i in range(1,len(poly)-1))
                    if area<1e-12:continue
                    verts=[]
                    for bary in poly:
                        pos=bary@positions
                        mapped=bary@source
                        mapped[0]=lo+(float(bary@q)-cell)*period
                        original=bary@p[indices]
                        assert np.max(np.abs(bary.sum()-1))<1e-12
                        active=np.flatnonzero(np.abs(bary)>1e-12)
                        if len(active)==1:
                            key=('original',int(indices[active[0]]),float(mapped[0]))
                        else:
                            assert len(active)==2
                            a,b=sorted(map(int,active),key=lambda i:int(indices[i]))
                            boundary=round(float(bary@q))
                            fraction=(boundary-q[a])/(q[b]-q[a])
                            # Deduplicate only an identical indexed source edge.
                            # Coincident positions with different raw attributes
                            # remain separate, as in the original mesh.
                            key=('seam',int(indices[a]),int(indices[b]),float(fraction),boundary-cell)
                        split_keys.add(key)
                        if len(active)==1:original_keys.add(key)
                        verts.append(dict(barycentric=bary.tolist(),position=pos.tolist(),originalPosition=original.tolist(),
                            sourceUV=(bary@source).tolist(),outputUV=mapped.tolist(),bone=int(bones[indices[0]])))
                    intervals.append(dict(cell=cell,vertices=verts,triangles=len(poly)-2))
                faces.append(dict(domainSeed=part['seedFace'],face=face_id,sourceIndices=indices.tolist(),
                    uRange=[lo,hi],phaseAnchorVertex=anchor,periodCoordinates=q.tolist(),cuts=cuts,
                    outputTriangles=sum(x['triangles'] for x in intervals),fragments=intervals))
        assert len({x['face'] for x in faces})==len(faces)
        generated=[v['position'] for face in faces for frag in face['fragments'] for v in frag['vertices']]
        expected=changed[list(original_vertices)]
        assert np.allclose(np.min(generated,axis=0),expected.min(axis=0),atol=1e-12,rtol=0)
        assert np.allclose(np.max(generated,axis=0),expected.max(axis=0),atol=1e-12,rtol=0)
        levels.append(dict(lod=lod,profiles=len(parts),sourceTriangles=len(faces),
            cutTriangles=sum(bool(x['cuts']) for x in faces),
            outputTriangles=sum(x['outputTriangles'] for x in faces),
            addedTriangles=sum(x['outputTriangles']-1 for x in faces),
            newSeamVerticesDeduplicated=len(split_keys-original_keys),
            newSeamVerticesFaceLocal=sum(sum(len(frag['vertices']) for frag in x['fragments'])-3 for x in faces),
            sourceVertices=len(original_vertices),retainedOriginalVertexVariants=len(original_keys),
            boundsMin=expected.min(axis=0).tolist(),boundsMax=expected.max(axis=0).tolist(),
            sourceSHA256=digest(arrays_path),recipeHashes=hashes,maxProfileAffineResidual=max_residual,
            phaseEvidence=phase_evidence,faces=faces))
    assert sum(x['profiles'] for x in levels)==129
    result=dict(style='Lift05',productionAuthorized=True,productionImplemented=False,halfWidthDelta=3,levels=levels,
        phaseRule='Connected-component source-edge phase constraints; see each level phaseEvidence. Only the 129 explicitly authorized domains are analyzed.',
        attributes='Positions and every non-U attribute use original-face barycentric interpolation. Original fixed/moving bone index is unchanged. At each seam use identical geometry twice with U=max on the previous cell and U=min on the next. V remains interpolated source V.',
        limitations='Offline geometry feasibility/count only. No production tables or meshes changed. The user authorized necessary seams only on these 129 domains. Native tangent/normal packed-channel interpolation and mip seam appearance still require native output verification; these counts do not certify those checks.',
        localization='No runtime text changed.')
    out=ROOT/'artifacts/material-remap/Lift05/structural-seam-review.json'
    out.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps([{k:v for k,v in x.items() if k not in ('faces','recipeHashes','phaseEvidence')} for x in levels],indent=2))


if __name__=='__main__':main()
