"""Identify native beam side continuations versus finite end fascias, all LODs."""
import collections
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np
from Lift04UvChartBindings import read_domains

ROOT=Path(__file__).resolve().parents[2]
OUT=ROOT/'artifacts/material-resume/Lift04/outputs'
RECIPE=ROOT/'tools/PreparedMaterials/Lift04/profile-continuations.json'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def main():
    raw=(ROOT/'assets/HardcodedNumbers/Lift04/Types_Lift04_StructuralProfileRegions.bin').read_bytes()
    # Original three accepted profile coverages remain the reference family.
    profiles=np.frombuffer(raw,dtype='<f8',count=24,offset=32).reshape(-1,8)
    rectangles={(1956,2076,2027,3342),(1956,2076,2170,3342),(1910,2076,2176,3342)}
    records=[]
    for name in ('LiftBridge04 Mesh','LiftBridge04_LOD1 Mesh'):
        path=ROOT/'artifacts/uv-density/native'/(name+'.npz');n=np.load(path)
        xyz=n['mesh0_positions'].astype(float);uv=n['mesh0_uv0'][:,:2].astype(float);faces=n['mesh0_faces']
        domains=read_domains(ROOT/'assets/HardcodedNumbers/Lift04'/('UvDomain_'+name.replace(' ','_')+'_0.bin'))
        edges=collections.defaultdict(list)
        for fi,face in enumerate(faces):
            for a,b in zip(face,np.roll(face,-1)):
                edges[tuple(sorted((tuple(xyz[a]),tuple(xyz[b]))))].append((fi,int(a),int(b)))
        for di,fs in enumerate(domains):
            pts=uv[faces[list(fs)]];bounds=tuple(np.concatenate((pts.min((0,1)),pts.max((0,1))))*4096)
            if bounds not in rectangles:continue
            assert (len(fs),len(np.unique(faces[list(fs)]))) in ((1,3),(2,4))
            joins=[];caps=[];long_edges=[]
            for fi in fs:
                for a,b in zip(faces[fi],np.roll(faces[fi],-1)):
                    if uv[a,0]==uv[b,0] and uv[a,1]!=uv[b,1]:long_edges.append([xyz[a].tolist(),xyz[b].tolist()])
                    for fj,c,d in edges[tuple(sorted((tuple(xyz[a]),tuple(xyz[b]))))]:
                        if fj in fs:continue
                        q=uv[faces[fj]]
                        eligible=[k for k,v in enumerate(profiles) if np.all(q>=v[4:6]) and np.all(q<=v[6:8])]
                        if uv[a,0]==uv[b,0] and uv[a,1]!=uv[b,1] and eligible:
                            phase={tuple(xyz[c]):uv[c,1],tuple(xyz[d]):uv[d,1]}
                            assert uv[a,1]==phase[tuple(xyz[a])] and uv[b,1]==phase[tuple(xyz[b])]
                            joins.append(dict(sourceFace=int(fi),neighborFace=fj,knownProfiles=eligible,edgeXYZ=[xyz[a].tolist(),xyz[b].tolist()],sourceUv=[uv[a].tolist(),uv[b].tolist()],neighborUv=[uv[c].tolist(),uv[d].tolist()],exactVPhase=True))
                        elif uv[a,1]==uv[b,1] and uv[a,0]!=uv[b,0]:
                            assert np.all(q>=0) and np.all(q<=1)
                            caps.append(dict(face=fj,uv=q.tolist(),positions=xyz[faces[fj]].tolist()))
            vertex=np.unique(faces[list(fs)]);p=xyz[vertex];q=uv[vertex]
            if joins:
                role='mixed-beam-profile-continuation'
                # U is exactly the existing wide unit cross-profile interval,
                # with unchanged source texture/coordinates, not remapped pixels.
                assert np.all(q[:,0]>=profiles[0,0]) and np.all(q[:,0]<=profiles[0,2])
                assert bounds[1:4:2]==(2076,3342)
            else:
                role='finite-end-fascia'
                assert bounds==(1910,2076,2176,3342) and len(fs)==2 and len(caps)==2 and len(long_edges)==2
                assert np.ptp(p[:,2])==0 and np.all(q>=0) and np.all(q<=1)
                assert all(not any(np.all(np.asarray(cap['uv'])>=v[4:6]) and np.all(np.asarray(cap['uv'])<=v[6:8]) for v in profiles) for cap in caps)
            records.append(dict(render=name,mesh=0,domain=di,faces=list(fs),role=role,nativeSha256=sha(path),boundsPixels=bounds,vertices=vertex.tolist(),positions=p.tolist(),uv=q.tolist(),longitudinalJoins=joins,finiteEndCaps=caps))
    counts=collections.Counter(r['role'] for r in records)
    assert counts=={'mixed-beam-profile-continuation':28,'finite-end-fascia':4}
    # The native detailed and middle LODs retain the same three finite source
    # rectangles; merged flange triangles explain the extra LOD1 domain count.
    assert {tuple(r['boundsPixels']) for r in records if '_LOD1' not in r['render']}=={tuple(r['boundsPixels']) for r in records if '_LOD1' in r['render']}==rectangles
    proof=dict(schema=1,style='Lift04',sourceCid='6b973ac2133fa9bd8d897bc9eff3363f',sourceDigest='ab2b09795bfa036e68938504786a9b2f3e62ae9b000fc60cbcb6e0f3b1a45a39',unitKey='f443914a8812fa50dda65930e5046391736fc0c5a1bca024561c108f99fcd96f',unitPixels=[1910,2076,2176,2480],repeatCoveragePixels=[[1956,2076,2027,3342],[1956,2076,2170,3342]],counts=dict(counts),records=records,criterion='Mixed continuations share exact physical longitudinal edges and exact V phase with measured native beam/flange family; source U remains an identical subset of the existing cross-profile unit. Finite fascias instead join two native finite end-cap charts across short edges and retain their single complete authored UV traversal under native Clamp. This distinction uses source geometry and native mapping, not image similarity or generated width.')
    OUT.mkdir(exist_ok=True,parents=True);(OUT/'physical-continuations.json').write_text(json.dumps(proof,indent=2)+'\n')
    if '--write' in sys.argv:RECIPE.write_text(json.dumps(proof,indent=2)+'\n')
    print(json.dumps(counts))

if __name__=='__main__':main()
