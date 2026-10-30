"""Compile only real finite/physical domains; repeated runs have continuous unit UVs."""
import json,sys,hashlib
from pathlib import Path
import numpy as np
from scipy.optimize import linprog
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import write_bbn
OUT=ROOT/'artifacts/material-remap/Lift03'
source=json.loads((OUT/'source-road-mapping.json').read_text())['records']
phases=json.loads((OUT/'continuous-phase-fit.json').read_text())['cases']
products=json.loads((OUT/'continuous-shared-grid-v3/bake.json').read_text())['units']
floats=[];cases=[];evidence=[]
for lod,deck in [(0,'Main'),(0,'Auxiliary'),(1,'Main'),(1,'Auxiliary')]:
 mesh=np.load(ROOT/'artifacts/uv-density/native'/('LiftBridge03'+('_LOD1' if lod else '')+' Mesh.npz'))
 for zs in (-1,1):
  for xs in (-1,1):
   rows=[r for r in source if (r['lod'],r['deck'],r['xSide'],r['zSide'])==(lod,deck,xs,zs)]
   cells=sorted(set(r['cell'] for r in rows));runs=[]
   for cell in cells:
    rr=[r for r in rows if r['cell']==cell]
    if rr[0]['repeatV'] and runs and runs[-1][0]['repeatV']:runs[-1]+=rr
    else:runs.append(rr)
   emitted=[]
   for run in runs:
    repeat=run[0]['repeatV'];first=len(floats)//35
    if repeat:
     physical='Moving' if run[0]['physicalZ'][0]<80 else 'Fixed'
     phase=next(p for p in phases if (p['lod'],p['deck'],p['xSide'],p['zSide'],p['domain'])==(lod,deck,xs,zs,physical))
     product=next(p for p in products if '.'+deck+'.' in p['id'])['channels'][0]
     cuts=sorted(set(x for r in run for x in r['physicalX']))
     ids=np.unique(np.concatenate([r['sourceFaces'] for r in run]));verts=np.unique(mesh['mesh0_faces'][ids]);positions=mesh['mesh0_positions'][verts].astype(float);uv=mesh['mesh0_uv0'][verts].astype(float)
     positions[:,0]=-abs(positions[:,0]);positions[:,2]=abs(positions[:,2])
     for left,right in zip(cuts[:-1],cuts[1:]):
      # The first/last authored edge can be quantized just outside the nominal
      # transverse span. Its real vertices still constrain the source mapping.
      mask=((positions[:,0]>=left) if left!=cuts[0] else np.ones(len(positions),dtype=bool))&((positions[:,0]<=right) if right!=cuts[-1] else np.ones(len(positions),dtype=bool));p=positions[mask];u=uv[mask,0]
      h=u.astype(np.float16);lo=(np.nextafter(h,np.float16(-np.inf)).astype(float)+u)/2;hi=(np.nextafter(h,np.float16(np.inf)).astype(float)+u)/2
      X=np.column_stack((p[:,0],p[:,2],np.ones(len(p))));half=(hi-lo)/2
      fit=linprog([0,0,0,1],A_ub=np.vstack((np.column_stack((X,-half)),np.column_stack((-X,-half)))),b_ub=np.r_[hi,-lo],bounds=[(None,None)]*3+[(-1,None)],method='highs');assert fit.success
      evidence.append(dict(lod=lod,deck=deck,xSide=xs,zSide=zs,domain=physical,physicalX=[left,right],extraHalfInterval=float(fit.x[3])))
      assert fit.x[3]<=1e-6, evidence[-1]
      ucoeff=fit.x[:3]*4096/product['width'];ucoeff[2]-=product['sourceOriginPixels'][0]/product['width']
      c=phase['coefficient'];vcoeff=np.array([c[0]*xs,-c[1]*zs,1-c[2]])
      base=next(r for r in run if r['physicalX'][0]<=left and r['physicalX'][1]>=right)
      record=[run[0]['physicalZ'][0],run[-1]['physicalZ'][1],left,right,*[product['contentMin'][0],product['contentMax'][0],0,1],*ucoeff,*vcoeff,*([0]*9),*base['startPlane'],*([0]*9)]
      assert len(record)==35;floats.extend(record)
     count=len(cuts)-1
    else:
     for r in run:
      c=np.array(r['coeff']);a=np.array(r['alternate'] or [[0,0]]*3);b=np.array(r['alternate2'] or [[0,0]]*3)
      # Finite source bounds are immaterial to direct sourceUv evaluation.
      record=[*r['physicalZ'],*r['physicalX'],0,1,0,1,*c[:,0],*c[:,1],*a[:,0],*a[:,1],*r['foldPlane'],*r['startPlane'],*b[:,0],*b[:,1],*r['foldPlane2']];floats.extend(record)
     count=len(run)
    emitted.extend([first,count,int(repeat)])
   cases.append(emitted)
ints=[6,16];offset=34
for case in cases:ints.extend([offset,len(case)//3]);offset+=len(case)
for case in cases:ints.extend(case)
raw=write_bbn(ints,floats)
(OUT/'continuous-road-mapping.bin').write_bytes(raw)
(OUT/'continuous-road-mapping-evidence.json').write_text(json.dumps(dict(uvFits=evidence,cells=[len(c)//3 for c in cases],bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()),indent=2)+'\n')
print('candidate bytes',len(raw),'cells',[len(c)//3 for c in cases])
