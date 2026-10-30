"""Fit moving-domain continuous phase against actual half-UV rounding intervals."""
import json
from pathlib import Path
import numpy as np
from scipy.optimize import linprog

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift03'
records=json.loads((OUT/'source-road-mapping.json').read_text())['records']
results=[]
for lod in (0,1):
 a=np.load(ROOT/'artifacts/uv-density/native'/('LiftBridge03'+('_LOD1' if lod else '')+' Mesh.npz'))
 for deck in ('Main','Auxiliary'):
  period=8 if deck=='Main' else 4
  for xs in (-1,1):
   for zs in (-1,1):
    for domain in ('Moving','Fixed'):
     rows=[r for r in records if (r['lod'],r['deck'],r['xSide'],r['zSide'])==(lod,deck,xs,zs) and r['repeatV'] and ((r['physicalZ'][1]<80) if domain=='Moving' else (r['physicalZ'][0]>80))]
     ids=np.unique(np.concatenate([r['sourceFaces'] for r in rows]));vertices=np.unique(a['mesh0_faces'][ids])
     p=a['mesh0_positions'][vertices].astype(float);v=a['mesh0_uv0'][vertices,1].astype(float)
     # Source faces can touch the finite terminal; keep only the full-period source vertices.
     keep=np.abs(p[:,2])<=max(r['physicalZ'][1] for r in rows)+1e-9;p=p[keep];v=v[keep]
     low,high=v.min(),v.max();extent=high-low
     q=(high-v)/extent;q+=np.rint(np.abs(p[:,2])/period-q)
     h=v.astype(np.float16)
     below=np.nextafter(h,np.float16(-np.inf)).astype(float);above=np.nextafter(h,np.float16(np.inf)).astype(float)
     # Preserve asymmetric binary16 midpoint intervals, including power-of-two boundaries.
     qlo=q-(above-v)/2/extent;qhi=q+(v-below)/2/extent
     X=np.column_stack((p[:,0],p[:,2],np.ones(len(p))))
     half=(qhi-qlo)/2
     A=np.vstack((np.column_stack((X,-half)),np.column_stack((-X,-half))))
     b=np.r_[qhi,-qlo]
     fit=linprog([0,0,0,1],A_ub=A,b_ub=b,bounds=[(None,None)]*3+[(-1,None)],method='highs')
     assert fit.success,fit.message
     coeff=fit.x[:3];pred=X@coeff;fp=X.astype(np.float32)@coeff.astype(np.float32)
     excess=np.maximum(qlo-pred,pred-qhi);fpExcess=np.maximum(qlo-fp,fp-qhi)
     results.append(dict(domain=domain,lod=lod,deck=deck,xSide=xs,zSide=zs,samples=len(p),sourceVBounds=[low,high],coefficient=coeff.tolist(),maxExtraHalfInterval=float(fit.x[3]),maxSourcePixelResidual=float(np.max(abs(pred-q))*extent*4096),maxFloat32SourcePixelResidual=float(np.max(abs(fp-q))*extent*4096),maxIntervalExcessSourcePixels=float(np.max(excess)*extent*4096),maxFloat32IntervalExcessSourcePixels=float(np.max(fpExcess)*extent*4096),withinSourceHalfIntervals=bool(np.max(excess)<=1e-10)))
(OUT/'continuous-phase-fit.json').write_text(json.dumps(dict(scope='Moving and fixed full-period source vertices fitted separately. Asymmetric binary16 UV midpoint intervals; Float32 dot recorded separately. Finite terminals, PBR filtering and runtime consumption not certified.',cases=results),indent=2)+'\n')
print([(r['lod'],r['deck'],r['xSide'],r['zSide'],r['maxExtraHalfInterval'],r['maxFloat32SourcePixelResidual']) for r in results])
