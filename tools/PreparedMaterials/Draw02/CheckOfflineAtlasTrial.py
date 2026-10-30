"""Compare actual native BC7 trial pixels with original mips; no quality pass implied."""
import json
import sys
from pathlib import Path
import numpy as np
from PlanOfflineAtlasUnits import sample, normal

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw02/offline-atlas-units'
suffix=sys.argv[1] if len(sys.argv)>1 else ''
report=json.loads((OUT/f'producer-report{suffix}.json').read_text())
plan=json.loads((OUT/'plan.json').read_text())
rows=[]
def periodic(image,u,v):
    # Explicitly model whole-texture Repeat along V and finite U away from edges.
    h,w,_=image.shape
    extended=np.concatenate((image[-1:],image,image[:1]),axis=0)
    return sample(extended,u,(np.mod(v,1)*h+1)/(h+2))
for index,unit in enumerate(report['units']):
    bounds=plan['consolidatedUnits'][index]
    lo,hi=np.asarray(bounds['minimum']),np.asarray(bounds['maximum'])
    for c in unit['channels']:
        originals=[]
        for m in range(13):
            n=max(1,4096>>m)
            originals.append(np.memmap(ROOT/f'artifacts/material-remap/Draw02/near-cells/all-source-mips/{c["name"]}/{m}.rgba32f',dtype=np.float32,mode='r',shape=(n,n,4)))
        for m in range(c['mips']):
            w,h=max(1,c['width']>>m),max(1,c['height']>>m)
            image=np.fromfile(OUT/f'decoded{suffix}/unit{index}/{c["name"]}/{m}.rgba32f',dtype=np.float32).reshape(h,w,4)
            x,y=np.meshgrid(np.linspace(0,1,129),np.unique(np.r_[np.linspace(0,1,129),1e-6,1-1e-6]))
            su,sv=lo[0]+x*(hi[0]-lo[0]),lo[1]+y*(hi[1]-lo[1])
            ou=c['contentMin'][0]+x*(c['contentMax'][0]-c['contentMin'][0])
            actual=periodic(image,ou,y)
            lod=c['sourceLodByMip'][m];a=int(np.floor(lod));b=min(12,a+1)
            expected=sample(originals[a],su,sv)*(1-(lod-a))+sample(originals[b],su,sv)*(lod-a)
            delta=np.abs(actual-expected)
            row=dict(unit=index,channel=c['name'],mip=m,sourceLod=lod,samples=x.size,
                maxComponentError=delta.max(axis=(0,1)).tolist(),meanComponentError=delta.mean(axis=(0,1)).tolist(),
                boundaryMaxComponentError=delta[(y==0)|(y==1)].max(axis=0).tolist())
            # Separate the seam footprint from the interior; never drop coarse
            # levels for which no such interior exists.
            source_span=(hi-lo)*4096/(2**lod)
            interior=(x*source_span[0]>=2)&((1-x)*source_span[0]>=2)&(y*source_span[1]>=2)&((1-y)*source_span[1]>=2)
            used=np.zeros_like(x,dtype=bool)
            for view in bounds['views']:
                used|=(su>=view['minimum'][0])&(su<=view['maximum'][0])
            interior &= used
            row['interiorSamples']=int(interior.sum())
            row['interiorMaxComponentError']=delta[interior].max(axis=0).tolist() if interior.any() else None
            if c['name']=='_NormalMap':
                error=np.linalg.norm(normal(actual)-normal(expected),axis=-1)
                row.update(maxDecodedNormalVectorError=float(error.max()),meanDecodedNormalVectorError=float(error.mean()))
                row['interiorMaxDecodedNormalVectorError']=float(error[interior].max()) if interior.any() else None
                worst=np.unravel_index(np.argmax(np.where(interior,error,-1)),error.shape)
                row['interiorWorstSourceUv']=[float(su[worst]),float(sv[worst])]
            if c['name']=='_BaseColorMap':
                row['alphaCutoutMismatchAtHalf']=int(np.count_nonzero((actual[...,3]>=.5)!=(expected[...,3]>=.5)))
                row['interiorAlphaMismatchAtHalf']=int(np.count_nonzero(((actual[...,3]>=.5)!=(expected[...,3]>=.5))&interior))
            rows.append(row)
result=dict(schema='Draw02OfflineAtlasTrialCheck/1',readyForProduction=False,rows=rows,
    nativeFilesBytes=sum(p.stat().st_size for p in (OUT/f'output{suffix}').rglob('*') if p.is_file()),
    model='Native candidate GPU float32 decoded mips; bilinear output Repeat-V against original atlas bilinear/trilinear at producer-declared max-axis source LOD. This is not full anisotropic SampleGrad or rendered acceptance.',
    unresolved=['Source-boundary adaptation has not been approved','Native anisotropic and fractional output LOD comparison','Original finite terminal joins','Complete owner and SDK package graph','Cold loading'])
(OUT/f'sampling-check{suffix}.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(rows=len(rows),nativeFilesBytes=result['nativeFilesBytes'],maxNormalVectorError=max(r.get('maxDecodedNormalVectorError',0) for r in rows),alphaMismatchSum=sum(r.get('alphaCutoutMismatchAtHalf',0) for r in rows))))
