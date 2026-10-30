"""Exact-rational affine representability, separate from Float32 evaluation."""
import itertools,json,sys
from fractions import Fraction as F
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/PedestrianDraw02'

def solve(rows):
    a=[list(r) for r in rows]
    for k in range(3):
        pivot=next((i for i in range(k,3) if a[i][k]),None)
        if pivot is None:return None
        a[k],a[pivot]=a[pivot],a[k];s=a[k][k];a[k]=[v/s for v in a[k]]
        for i in range(3):
            if i!=k:
                s=a[i][k];a[i]=[v-s*w for v,w in zip(a[i],a[k])]
    return [r[3:] for r in a]

def main():
    ds=json.loads((OUT/'continuous-atlas-domains.json').read_text())['levels'][0]['domains']
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())['levels'][0]['panels']
    frames=json.loads((OUT/'native-panel-frames.json').read_text())['levels'][0]['panels']
    pos={t['face']:t['position'] for p in recipe for t in p['triangles']}
    tang={t['face']:t['tangents'] for p in frames for t in p['sourceFrames']}
    rows=[];flags=[];samples=[];coefficients=[]
    for index,d in enumerate(ds):
        if d.get('preparedUnitIndex'):flags.append(0);coefficients.extend([0]*9);continue
        data=sorted(set(tuple(map(F,[p[0],p[2],1,*t[:3]])) for face in d['sourceFaces'] for p,t in zip(pos[face],tang[face])))
        constant=len({r[3:] for r in data})==1
        c=next((s for triple in itertools.combinations(data,3) if (s:=solve(triple)) is not None),None)
        exact=c is not None and all(sum(r[k]*c[k][j] for k in range(3))==r[3+j] for r in data for j in range(3))
        flags.append(int(exact and not constant))
        coefficients.extend(np.array(c,dtype=np.float64).astype(np.float32).reshape(-1).tolist() if exact and not constant else [0]*9)
        rows.append(dict(domain=index,faces=d['sourceFaces'],constant=constant,exactRawAffine=exact))
        if exact and not constant:
            coeff=np.array(c,dtype=np.float64).astype(np.float32)
            for face in d['sourceFaces']:
                p=np.array(pos[face]);t=np.array(tang[face])
                for i in range(9):
                    for j in range(9-i):
                        bary=np.array([i,j,8-i-j])/8
                        position=bary@p;source=bary@t
                        samples.append(dict(domain=index,position=position.tolist(),source=source.tolist(),coefficients=coeff.reshape(-1).tolist()))
    report=dict(domains=rows,samples=samples,flags=flags,
        proof='Exact rational equality of original binary-float decoded T at vertices; affine interpolation then holds over each original triangle. Float32 storage/evaluation and native packing are separate tests.')
    (OUT/'exact-tangent-fields.json').write_text(json.dumps(report,indent=2)+'\n')
    sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
    from MovableUvDomainFormat import write_bbn
    (ROOT/'assets/HardcodedNumbers/PedestrianDraw02/Types_PedestrianDraw02_SourceTangentFields.bin').write_bytes(write_bbn(flags,coefficients))
    print(json.dumps(dict(selected=sum(flags),domains=[i for i,f in enumerate(flags) if f],samples=len(samples))))

if __name__=='__main__':main()
