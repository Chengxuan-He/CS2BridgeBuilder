"""Test normalized low-degree fields against native tangent quantization cells.

Polynomial fits are diagnostic candidates, not permission to change shader frames.
"""
import json
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'


def oct15(v):
    p=v/np.abs(v).sum(axis=1)[:,None]
    xy=p[:,:2].copy();fold=p[:,2]<0
    xy[fold]=(1-np.abs(xy[fold,::-1]))*np.where(xy[fold]>=0,1,-1)
    return xy*16383


def main():
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    frames=json.loads((OUT/'native-panel-frames.json').read_text())
    domains=json.loads((OUT/'native-atlas-domains.json').read_text())
    rows=[];periods=[]
    for level,fs,ds in zip(recipe['levels'],frames['levels'],domains['levels']):
        positions={t['face']:np.asarray(t['position'])[:,[0,2]] for p in level['panels'] for t in p['triangles']}
        tangents={t['face']:np.asarray(t['tangents'])[:,:3] for p in fs['panels'] for t in p['sourceFrames']}
        for d in ds['domains']:
            p=np.concatenate([positions[f] for f in d['sourceFaces']]);t=np.concatenate([tangents[f] for f in d['sourceFaces']])
            if len(np.unique(t,axis=0))==1:continue
            pairs={}
            for q,v in zip(p,t):pairs.setdefault(tuple(q),set()).add(tuple(v))
            conflicts=[dict(position=list(q),tangents=[list(v) for v in values]) for q,values in pairs.items() if len(values)>1]
            x=(p-p.mean(axis=0))/np.maximum(np.ptp(p,axis=0),1e-12)
            fits=[]
            for degree in (1,2,3):
                matrix=np.stack([x[:,0]**a*x[:,1]**b for a in range(degree+1) for b in range(degree+1-a)],axis=1)
                predicted=matrix@np.linalg.lstsq(matrix,t,rcond=None)[0]
                predicted/=np.linalg.norm(predicted,axis=1)[:,None]
                target=t/np.linalg.norm(t,axis=1)[:,None]
                residual=np.abs(oct15(predicted)-np.round(oct15(target)))
                angles=np.degrees(np.arccos(np.clip(np.sum(predicted*target,axis=1),-1,1)))
                fits.append(dict(degree=degree,rank=int(np.linalg.matrix_rank(matrix)),columns=matrix.shape[1],
                    maxNativeCellDisplacement=float(residual.max()),withinOriginalHalfCell=bool(np.all(residual<=.5)),maxAngle=float(angles.max())))
            rows.append(dict(lod=level['lod'],faces=d['sourceFaces'],samePositionConflicts=conflicts,fits=fits))
        for bone in (1,2):
            for right in (False,True):
                selected=[p for p in level['panels'] if p['repeatedV'] and p['bone']==bone and (sum([p['physicalMin'][0],p['physicalMax'][0]])>0)==right]
                bits=[next(f['tangentBits'] for f in fs['panels'] if f['firstFace']==p['firstFace']) for p in selected]
                periods.append(dict(lod=level['lod'],bone=bone,right=right,panels=[p['firstFace'] for p in selected],
                    packedTangentSets=bits,allPeriodsSamePackedSet=all(b==bits[0] for b in bits)))
    result=dict(variableDomains=rows,repeatSets=periods,
        limitations=['Cell checks are at original vertices only; interpolation and shader normalization require separate proof.',
            'Higher degree rank deficiency is not a unique or justified physical field.',
            'No runtime changes, normal-error tolerance changes or additional cuts are authorized by this audit.'])
    (OUT/'native-tangent-field-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(variableDomains=len(rows),conflicts=sum(bool(r['samePositionConflicts']) for r in rows),
        affineCellFailures=sum(not r['fits'][0]['withinOriginalHalfCell'] for r in rows),periodSetsEqual=[p['allPeriodsSamePackedSet'] for p in periods])))


if __name__=='__main__':main()
