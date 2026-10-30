"""Emit finite far-LOD source projections with per-vertex Float16 budgets."""
import hashlib, itertools, json, struct
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / 'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'

def interior_line(x, uv):
    x, uv = x.astype(float), uv.astype(float)
    half = uv.astype(np.float16)
    lower = (uv + np.nextafter(half, np.float16(-np.inf)).astype(float)) / 2
    upper = (uv + np.nextafter(half, np.float16(np.inf)).astype(float)) / 2
    origin, extent = x.mean(), np.ptp(x)
    normalized = (x-origin)/extent
    width = (upper-lower)/2
    # Maximize the fraction of each local storage interval left on both sides.
    a = np.concatenate((np.column_stack((normalized,np.ones(len(x)),width)),
                        np.column_stack((-normalized,-np.ones(len(x)),width))))
    b = np.concatenate((upper,-lower))
    best = None
    for active in itertools.combinations(range(len(b)),3):
        matrix = a[list(active)]
        if abs(np.linalg.det(matrix)) < 1e-15: continue
        candidate = np.linalg.solve(matrix,b[list(active)])
        if np.all(a@candidate <= b+1e-12) and (best is None or candidate[2]>best[2]):
            best = candidate
    assert best is not None and best[2]>0
    coefficients = np.array((best[0]/extent,best[1]-best[0]*origin/extent),np.float32)
    actual = np.float32(np.float32(x)*coefficients[0]+coefficients[1]).astype(float)
    assert np.all((actual>lower)&(actual<upper)), (actual,lower,upper)
    assert np.array_equal(actual.astype(np.float16).view(np.uint16),half.view(np.uint16))
    return coefficients, dict(maximumPixelError=float(np.max(np.abs(actual-uv))*512),
        minimumIntervalMargin=float(np.min(np.minimum(actual-lower,upper-actual))),
        sameSourceHalfBits=True, normalizedInteriorMargin=float(best[2]))

def main():
    mesh=np.load(EVIDENCE/'lod2-native.npz')
    p,u,f=(mesh['mesh0_'+k] for k in ('positions','uv','indices'))
    groups=json.loads((EVIDENCE/'far-spans.json').read_text())['groups']
    profiles=[]; checks=[]; candidates=[]
    for end in (-1,1):
      for side in (-1,1):
        fixed=next(g for g in groups if g['bone']==0 and g['end']==end and g['side']==side)
        moving=next(g for g in groups if g['bone']!=0 and g['end']==end and g['side']==side)
        for part in range(4):
          g=fixed if part==3 else moving
          ids=np.unique(f[g['faces']])
          if part<3:
            z=np.abs(p[ids,2])
            ids=ids[(z<=8.00001) if part==0 else ((z>=7.99999)&(z<=60.00001)) if part==1 else z>=59.99999]
          points,uv=p[ids].astype(float),u[ids].astype(float)
          axes=(0,2) if part==3 else (2,0)
          cu,au=interior_line(points[:,axes[0]],uv[:,0])
          cv,av=interior_line(points[:,axes[1]],uv[:,1])
          # Packed rows: U(X,Z,1), V(X,Z,1), original half-width.
          row=[0.,0.,float(cu[1]),0.,0.,float(cv[1]),float(np.max(np.abs(points[:,0])))]
          row[0 if axes[0]==0 else 1]=float(cu[0]); row[3 if axes[1]==0 else 4]=float(cv[0])
          profiles.extend(row)
          checks.append(dict(end=end,side=side,part=part,sourceVertices=ids.tolist(),u=au,v=av))
          for vertex in ids:
            original=u[vertex].astype(float); half=u[vertex].astype(np.float16)
            low=(original+np.nextafter(half,np.float16(-np.inf)).astype(float))/2
            high=(original+np.nextafter(half,np.float16(np.inf)).astype(float))/2
            candidates.append(dict(domain=len(checks)-1,x=float(p[vertex,0]),z=float(p[vertex,2]),
                u=float(original[0]),v=float(original[1]),loU=float(low[0]),hiU=float(high[0]),loV=float(low[1]),hiV=float(high[1])))
    # Physical stations and original far cap endpoints. They remain finite;
    # no repeat period or full-span raster is introduced.
    profiles += [8.,60.,78.69921875,78.7421875,90.,77.970703125,78.072265625,89.7265625,
                 .030002355575561523,.030005931854248047,.030022144317626953]
    payload=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(profiles),0)+struct.pack('<%df'%len(profiles),*profiles)
    payload+=hashlib.sha256(payload).digest()
    output=ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02FarAppearance_cs.bin'
    output.write_bytes(payload)
    report=ROOT/'artifacts/material-remap/Draw02/far-source-interval-validation.json'
    report.write_text(json.dumps(dict(schema='Draw02FiniteFarSourceIntervals/1',profiles=checks,
        binarySha256=hashlib.sha256(payload).hexdigest(),sourceSurface='436d1828c36d1336cf115c85def2c62c'),indent=2)+'\n')
    (report.parent/'far-native-candidates.json').write_text(json.dumps(dict(points=candidates)))
    print('PASS: 16 finite source domains, independent axes, actual float32 evaluation strictly inside every source Float16 interval; re-encoded source UV bits identical.')

if __name__=='__main__': main()
