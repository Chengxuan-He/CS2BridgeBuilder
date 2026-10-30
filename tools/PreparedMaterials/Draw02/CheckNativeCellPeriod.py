"""Compare native road-top UV projection over successive authored four-metre cells."""
import json
from pathlib import Path
import numpy as np
root=Path(__file__).resolve().parents[3];out=root/'artifacts/material-remap/Draw02/near-cells';reports=[]
for lod in (0,1):
    m=np.load(root/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz')
    p=m['mesh0_positions'].astype(float);uv=m['mesh0_uv'].astype(float);faces=m['mesh0_indices'];q=p[faces]
    ids=np.where((np.abs(q[:,:,1]-9.800048828125).max(1)<1e-6)&(q[:,:,2].max(1)>0))[0]
    xx,zz=np.meshgrid(np.linspace(-6.09,6.09,81),np.linspace(.015,3.985,33));base=np.column_stack((xx.ravel(),zz.ravel()))
    samples={}
    for station in range(4,76,4):
        points=base+np.array((0.,station));mapped=np.full((len(points),2),np.nan)
        for face in ids:
            tri=q[face][:,(0,2)]
            if tri[:,1].min()>=station+4 or tri[:,1].max()<=station:continue
            a,b,c=tri;mat=np.column_stack((b-a,c-a))
            if abs(np.linalg.det(mat))<1e-9:continue
            weights=(points-a)@np.linalg.inv(mat).T;mask=(weights>=-1e-8).all(1)&(weights.sum(1)<=1+1e-8)
            values=uv[faces[face]];mapped[mask]=values[0]+weights[mask]@(values[1:]-values[0])
        samples[station]=mapped
    ref=samples[40];validref=np.isfinite(ref).all(1);rows=[]
    for station,mapped in samples.items():
        valid=np.isfinite(mapped).all(1);both=valid&validref
        error=np.max(np.abs(mapped[both]-ref[both]),axis=1)*4096
        rows.append(dict(station=station,missingSamples=int((~valid).sum()),
            differentCoverage=int(np.sum(valid!=validref)),maximumSourcePixelDifference=float(error.max()),
            samplesExceedingOneSourcePixel=int((error>1).sum()),compared=int(both.sum())))
    reports.append(dict(lod=lod,referenceCell=[40,44],sampleGrid=[81,33],cells=rows))
(out/'native-period-check.json').write_text(json.dumps(reports,indent=2)+'\n')
for r in reports:print(r['lod'],[(c['station'],c['differentCoverage'],round(c['maximumSourcePixelDifference'],4),c['samplesExceedingOneSourcePixel'])for c in r['cells']])
