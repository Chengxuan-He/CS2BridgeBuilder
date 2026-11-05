"""Audit complete native underlay output by authored board and actual coverage cells."""
from pathlib import Path
import json, struct
import numpy as np

root = Path(__file__).resolve().parents[2]
out = root / 'artifacts/material-remap/Lift02/underlay70'
boards = json.loads((out / 'boards.json').read_text())
results = []
for label, delta in [('road',8.5),('track',0),('narrow',-3.75),('leftwalk',6.75),('rightwalk',6.75)]:
    for lod in range(3):
        b = (out/f'{label}-lod{lod}-output.bin').read_bytes()
        nv, ni = struct.unpack_from('<ii', b)
        p = np.frombuffer(b, '<f4', count=nv*3, offset=8).reshape(-1,3)
        f = np.frombuffer(b, '<i4', count=ni, offset=8+nv*12).reshape(-1,3)
        source_faces = np.fromfile(out/f'{label}-lod{lod}-faces.bin','<i4')
        selected = [b for b in boards if b['lod']==min(lod,1)]
        q = p[f].astype(float)
        road = np.frombuffer((out/f'{label}-lod{lod}-road.bin').read_bytes(), '<f4', offset=4).reshape(-1,3,3)
        left, right = map(float, (road[:,:,0].min(), road[:,:,0].max()))
        errors, samples, total_area, expected_area = [], 0, 0., 0.
        max_area_error = 0.
        for board in selected:
            mask = np.isin(source_faces, board['sourceFaces'][0])
            triangles = q[mask][:,:,[0,2]]
            z0, z1 = map(lambda z: float(np.float32(z)), (board['start'], board['end']))
            region = 0 if z1<=-82 else 2 if z0>=82 else 1
            half = 4.217529296875 if region==1 else 4.9730224609375
            il, ir = float(np.float32(-half-delta)), float(np.float32(half+delta))
            zs = sorted(set([z0,z1]+[z for z in [-103.326171875,-83.587890625,83.587890625,103.326171875] if z0<z<z1]))
            xs = sorted(set([left,right]+[x for x in [il,ir] if left<x<right]))
            a=triangles[:,1]-triangles[:,0];c=triangles[:,2]-triangles[:,0]
            determinants = a[:,0]*c[:,1]-a[:,1]*c[:,0]
            # Downward native winding becomes positive when projected into X/Z.
            if np.any(determinants < 0): errors.append(['inverted-bottom',board['component']])
            area = determinants.sum()/2; expected=0.
            for xa,xb in zip(xs,xs[1:]):
                for za,zb in zip(zs,zs[1:]):
                    x,z=(xa+xb)/2,(za+zb)/2
                    open_fixed=(-103.326171875<z<-83.587890625) or (83.587890625<z<103.326171875)
                    exists = lod<2 or open_fixed or x<il or x>ir
                    expected += (xb-xa)*(zb-za)*exists
                    for ux,uz in [(0.271,0.619),(0.827,0.133),(0.419,0.873)]:
                        point=np.array([xa+(xb-xa)*ux,za+(zb-za)*uz])
                        v=point-triangles[:,0]
                        s1=a[:,0]*v[:,1]-a[:,1]*v[:,0]
                        s2=v[:,0]*c[:,1]-v[:,1]*c[:,0]
                        hits=int(np.sum((determinants>0)&(s1>=0)&(s2>=0)&(s1+s2<=determinants)))
                        samples+=1
                        if hits!=int(exists):errors.append(['coverage',board['component'],point.tolist(),int(exists),hits])
            max_area_error=max(max_area_error,abs(area-expected))
            total_area+=area;expected_area+=expected
        result=dict(input=label,lod=lod,triangles=len(f),coverageSamples=samples,
                    coverageFailures=errors,bottomArea=total_area,expectedBottomArea=expected_area,
                    maximumBoardAreaError=max_area_error,
                    sourceIdentityFaces=len(set(map(int,source_faces))))
        results.append(result)
        print(label,lod,'samples',samples,'failures',len(errors),'area error',total_area-expected_area,'max board',max_area_error)
(out/'coverage-result.json').write_text(json.dumps(results,indent=2))
assert all(not r['coverageFailures'] for r in results)
