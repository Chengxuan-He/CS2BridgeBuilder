"""Map flagged far texels to near/middle original geometry; no texture edits."""
from pathlib import Path
import json,hashlib
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
R=ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research'
OUT=ROOT/'artifacts/material-remap/Lift01/far-finite'
far=np.load(ROOT/'artifacts/uv-density/native/LiftBridge01_LOD2 Mesh.npz')
sets=[('moving-negative',[730,731],[229,230,232],[57,60,62,64,67],False),('moving-positive',[342,343],[4,6],[361,364,366,368,371],False),('fixed-negative',[708,709],[380,382,421,422,423],[187,190,192,194,197],False),('fixed-positive',[320,321],[348,350,388,390,391],[303,306,308,310,313],True)]
chosen=set(range(1814,2138))|set(range(6270,7072))|set(range(8966,9766))
levels=[]
for lod in [0,1]:
 mesh=np.load(R/f'lift01-main-{lod}.npz');p=mesh['positions'];f=mesh['indices'];tri=p[f]
 groups={};membership={}
 for kind in ['side','crossing']:
  path=R/f'lift01-{kind}-{lod}.json'
  if not path.exists():continue
  for i,g in enumerate(json.loads(path.read_text())):
   label=kind+':'+str(i);groups[label]=dict(minimum=g['minimum'],maximum=g['maximum'])
   role='rail-or-fastener' if kind=='side' and i in chosen else 'sleeper' if kind=='crossing' and g['minimum'][0]==-1.350006103515625 and g['maximum'][0]==1.350006103515625 and g['minimum'][1]==9.800048828125 and g['maximum'][1]==9.86962890625 else 'other-authored-geometry'
   groups[label]['role']=role
   for v in g['vertices']:membership[v]=label
 levels.append((lod,p,f,tri,groups,membership))
rows=[]
for name,ids,stations,cross,h in sets:
 for station in stations:
  for c in cross:
   uv=np.array([station+.5,c+.5] if h else [c+.5,station+.5])/512
   point=None
   for fi in ids:
    vertices=far['mesh0_faces'][fi];t=far['mesh0_uv0'][vertices,:2].astype(float)
    w=np.linalg.solve(np.r_[t.T,np.ones((1,3))],np.r_[uv,1])
    if w.min()>=0:point=w@far['mesh0_positions'][vertices,:3];break
   entry=dict(chart=name,station=station,transversePixel=c,uv=uv.tolist(),physicalPoint=None if point is None else point.tolist(),lodHits=[])
   if point is not None:
    x,z=point[[0,2]]
    for lod,p,f,t,groups,membership in levels:
     a=t[:,0];b=t[:,1]-a;d=t[:,2]-a;det=b[:,0]*d[:,2]-b[:,2]*d[:,0];ok=abs(det)>1e-14
     u=np.zeros(len(t));v=u.copy();u[ok]=((x-a[ok,0])*d[ok,2]-(z-a[ok,2])*d[ok,0])/det[ok];v[ok]=(b[ok,0]*(z-a[ok,2])-b[ok,2]*(x-a[ok,0]))/det[ok]
     y=a[:,1]+u*b[:,1]+v*d[:,1];hit=ok&(u>=0)&(v>=0)&(u+v<=1)&(y>=9.7)&(y<=10.4)
     records=[]
     for fi in np.flatnonzero(hit):
      labels=sorted(set(membership.get(int(k),'unrecorded') for k in f[fi]));records.append(dict(face=int(fi),y=float(y[fi]),groups=[dict(id=l,**groups.get(l,{})) for l in labels]))
     entry['lodHits'].append(dict(lod=lod,hits=records))
   rows.append(entry)
report=dict(scope='Exact flagged texel centers projected through original far UV triangles, vertical intersection against original near/middle geometry. Pixel support/filter overlap and ambiguous roles remain distinct from center classification.',rows=rows)
(OUT/'terminal-role-audit.json').write_text(json.dumps(report,indent=2));print(OUT/'terminal-role-audit.json')
