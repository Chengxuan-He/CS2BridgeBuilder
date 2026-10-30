"""Group actual bed tiles by source Float16 UV rounding intervals, not pixel tolerance."""
import json
import sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'artifacts/material-remap/environments/Lift04/python-deps'))
from scipy.optimize import linprog
source=json.loads((ROOT/'artifacts/material-remap/Lift04/near-domain-audit.json').read_text())
original=json.loads((ROOT/'artifacts/layer-decoupling/Lift04/outputs/native-repeat-mechanism/audit.json').read_text())
uvmin=np.array([.4697265625,.8251953125]);extent=np.array([.15576171875,.15625])

def fit(tiles,faces):
    points=[];values=[];radii=[]
    for index,tile in enumerate(tiles):
        sign=np.sign(tile['affineXZ'][1][0])
        for face in tile['faces']:
            f=faces[face];p=np.array(f['positions']);uv=np.array(f['uv0'])
            stored=uv.astype(np.float16)
            lo=(stored.astype(float)+np.nextafter(stored,np.float16(-np.inf)).astype(float))/2
            hi=(stored.astype(float)+np.nextafter(stored,np.float16(np.inf)).astype(float))/2
            center=(lo+hi)/2
            v=(center-uvmin)/extent;v[:,0]+=sign*index
            points.extend(np.c_[p[:,[0,2]],np.ones(3)])
            values.extend(v);radii.extend((hi-lo)/2/extent)
    x=np.array(points);y=np.array(values);radius=np.array(radii)
    coefficients=[];budget=0
    for axis in range(2):
        bounds=np.r_[np.c_[x,-radius[:,axis]],np.c_[-x,-radius[:,axis]]]
        result=linprog([0,0,0,1],A_ub=bounds,b_ub=np.r_[y[:,axis],-y[:,axis]],bounds=[(None,None)]*3+[(0,None)],method='highs')
        if not result.success:return None
        coeff=result.x[:3].astype(np.float32)
        used=float(max(np.max(np.abs(x@coeff-y[:,axis])/radius[:,axis]),
                       np.max(np.abs((x.astype(np.float32)@coeff).astype(float)-y[:,axis])/radius[:,axis])))
        coefficients.append(coeff.tolist());budget=max(budget,used)
    return dict(u=coefficients[0],v=coefficients[1],sourceRoundingBudget=budget,accepted=budget<1)

levels=[]
for level in source['levels']:
    fs={f['face']:f for f in original['lods'][level['lod']]['faces']}
    tiles=[t for t in level['domains'] if t['role']=='generated-road']
    buckets={}
    for t in tiles:
        assert len(t['tangentDecoded'])==1
        key=(tuple(t['bones']),t['positionMin'][0]<-1,tuple(t['tangentDecoded'][0]),int(np.sign(t['affineXZ'][1][0])))
        buckets.setdefault(key,[]).append(t)
    groups=[];rejections=[]
    for key,values in buckets.items():
        values.sort(key=lambda t:t['positionMin'][2]);current=[];mapping=None
        for tile in values:
            adjacent=not current or current[-1]['positionMax'][2]==tile['positionMin'][2]
            candidate=fit(current+[tile],fs) if adjacent else None
            if current and (candidate is None or not candidate['accepted']):
                groups.append(dict(tiles=current,mapping=mapping))
                rejections.append(dict(after=current[-1]['faces'],before=tile['faces'],reason='physical gap' if not adjacent else 'outside original Float16 UV rounding intervals',fit=candidate))
                current=[];candidate=fit([tile],fs)
            assert candidate and candidate['accepted'],(level['lod'],tile['faces'],candidate)
            current.append(tile);mapping=candidate
        if current:groups.append(dict(tiles=current,mapping=mapping))
    result=[]
    for g in groups:
        ts=g['tiles'];result.append(dict(faces=[f for t in ts for f in t['faces']],tileCount=len(ts),
            min=np.min([t['positionMin'] for t in ts],axis=0).tolist(),max=np.max([t['positionMax'] for t in ts],axis=0).tolist(),
            tangent=ts[0]['tangentDecoded'][0],bone=ts[0]['bones'][0],repeatLongitudinal=len(ts)>1,**g['mapping']))
    assert sorted(f for g in result for f in g['faces'])==sorted(f for t in tiles for f in t['faces'])
    levels.append(dict(lod=level['lod'],domainCount=len(result),domains=result,rejections=rejections))
out=ROOT/'artifacts/material-remap/Lift04/near-domain-groups.json'
out.write_text(json.dumps(dict(levels=levels,rule='Float32 packed output projection must fit original Float16 UV rounding intervals at every source vertex. Integer phase lifts follow authored reset ordering; tangent comes from native PipelinePlugin unpack, never a UV derivative guess.',scope='Candidate grouping only; original domains are not rasterized or intersected against prototype triangles.'),indent=2))
print([(l['lod'],l['domainCount']) for l in levels])
