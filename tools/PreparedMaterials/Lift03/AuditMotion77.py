"""Read preserved task77 native geometry; never open or mutate the owner."""
from pathlib import Path
import json, sys, hashlib
import numpy as np
R=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(R/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import read_bbn
S=R/'artifacts/material-remap/shared/lift03-appearance77'
O=R/'artifacts/material-remap/Lift03/appearance77';O.mkdir(parents=True,exist_ok=True)
_,f=read_bbn((R/'assets/HardcodedNumbers/Lift03/Types_Lift03_Lift03RoadPartitions_g_cs.bin').read_bytes())
moving,start,end=map(float,f)
rows=[];structural=[]
for item in json.loads((S/'skin-attributes/index.json').read_text()):
    name=item['bindings'][0]['prefab']
    if not (name.startswith('Mechanism') or 'Skinned Paint' in name):continue
    data=json.loads(Path(item['file']).read_text())
    for slot,m in enumerate(data['meshes']):
        # Slots 0/1 in the mechanism are retained original structural groups.
        if name.startswith('Mechanism') and slot<2:
            p=np.asarray(m['positions']);q=p[np.asarray(m['faces'])]
            n=np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0])
            selected=np.all(np.abs(q[:,:,1]-9.8)<.002,axis=1)&(n[:,1]>0)
            structural.append(dict(prefab=name,slot=slot,lowerUpwardTriangles=int(selected.sum()),
                upwardArea=float(n[selected,1].sum()/2)))
            assert not selected.any(),structural[-1]
            continue
        p=np.asarray(m['positions']);b=np.asarray(m['blendIndices'])[:,0]
        faces=np.asarray(m['faces']);z=p[:,2]
        for deck,height in [('lower',9.8),('upper',19.8)]:
            selected=np.all(np.abs(p[faces,1]-height)<.002,axis=1)
            q=faces[selected]
            if not len(q):continue
            vertices=np.unique(q)
            inside=np.all(np.abs(z[q])<=moving,axis=1)
            fixed=np.all(np.abs(z[q])>=start,axis=1)
            wrong_moving=int(np.count_nonzero(np.any(b[q[inside]]!=3,axis=1)))
            wrong_fixed=int(np.count_nonzero(np.any(b[q[fixed]]!=0,axis=1)))
            row=dict(prefab=name,geometry=data['cid'],sha256=data['sha256'],slot=slot,deck=deck,
                triangles=len(q),movingTriangles=int(inside.sum()),fixedTriangles=int(fixed.sum()),
                outsideKnownPartition=int((~(inside|fixed)).sum()),wrongMovingBones=wrong_moving,
                wrongFixedBones=wrong_fixed,boneBounds={str(int(bone)):dict(vertices=int(np.count_nonzero(b[vertices]==bone)),
                    min=p[vertices[b[vertices]==bone]].min(0).tolist(),max=p[vertices[b[vertices]==bone]].max(0).tolist()) for bone in np.unique(b[vertices])})
            rows.append(row)
            assert wrong_moving==wrong_fixed==0,row
result=dict(owner='b53f67367-0339-4e7c-811b-613464bd7c6e',movingEnd=moving,fixedStart=start,fixedEnd=end,
    rows=rows,retainedStructuralLowerTop=structural,
    scope='Persisted native geometry and bone indices only. Static city network state and live animation remain separate evidence.')
(O/'motion-bones.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(rows=len(rows),movingEnd=moving,movingTriangles=sum(r['movingTriangles'] for r in rows),wrongBones=0)))
