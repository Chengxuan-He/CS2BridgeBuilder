"""Measure genuine native road projection changes, independent of triangulation."""
import json
import hashlib
from collections import defaultdict
from fractions import Fraction
from itertools import combinations
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
SOURCE=ROOT/'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism'

def affine_lower_bound(first,second):
    points=[]
    for face in (first,second):
        for p,uv in zip(face['positions'],face['uv']):
            record=(p[0],p[2],*uv)
            if record not in points:points.append(record)
    def det(a,b,c):return a[0]*(b[1]-c[1])-a[1]*(b[0]-c[0])+b[0]*c[1]-b[1]*c[0]
    lower=Fraction(0)
    for four in combinations(points,4):
        xyz=[[Fraction(v) for v in p[:2]] for p in four]
        weights=[(-1)**i*det(*(xyz[j] for j in range(4) if j!=i)) for i in range(4)]
        total=sum(abs(v) for v in weights)
        if total:
            for axis in (2,3):lower=max(lower,abs(sum(w*Fraction(p[axis]) for w,p in zip(weights,four)))/total)
    ulp=max(float(np.nextafter(np.float32(p[k]),np.float32(np.inf))-np.float32(p[k])) for p in points for k in (2,3))
    return dict(minimumRequiredChangeToStoredUv=float(lower),maximumFloat32UvUlp=ulp,
        interpretation='Exact rational affine-null constraint on decoded stored UV values. This is a lower bound on any common affine approximation, not a fit residual. Authored Half16 values are exact inputs; pre-quantization UVs are not reconstructed.')

def main():
    levels=[]
    for lod in range(3):
        path=SOURCE/f'road-lod{lod}.json'
        data=json.loads(path.read_text());faces=data['faces'];by_id={f['id']:f for f in faces};coefs={};edges=defaultdict(list)
        for face in faces:
            xyz=np.array(face['positions']);uv=np.array(face['uv'])
            coefs[face['id']]=np.linalg.solve(np.column_stack((xyz[:,0],xyz[:,2],np.ones(3))),uv)
            for a,b in ((0,1),(1,2),(2,0)):
                key=tuple(sorted((tuple(xyz[a,[0,2]]),tuple(xyz[b,[0,2]]))))
                edges[key].append(face['id'])
        parent={f['id']:f['id'] for f in faces}
        def find(i):
            while parent[i]!=i:i=parent[i]
            return i
        boundaries=[];removed=[]
        for edge,ids in edges.items():
            if len(ids)!=2:continue
            a,b=ids;xy=np.array([*edge,tuple(np.mean(edge,axis=0))]);frame=np.column_stack((xy,np.ones(3)))
            # Solving the same authored Float32 coordinates introduces double
            # roundoff. This bound is linear-algebra noise, not texture tolerance.
            coefficient_error=float(np.max(np.abs(coefs[a]-coefs[b])))
            roundoff=64*np.finfo(float).eps*max(1,float(np.max(np.abs(coefs[a]))),float(np.max(np.abs(coefs[b]))))
            if coefficient_error<=roundoff:
                parent[find(b)]=find(a);removed.append(dict(faces=ids,edge=edge));continue
            error=float(np.max(np.abs(frame@(coefs[a]-coefs[b]))))
            (x0,z0),(x1,z1)=edge
            line=[z1-z0,x0-x1,x1*z0-x0*z1]
            boundaries.append(dict(faces=ids,edge=edge,lineXZ1=line,
                sourceEdgeUVJump=error,kind='reset' if error>roundoff else 'gradient',
                coefficientDifference=coefficient_error,nonmergeProof=affine_lower_bound(by_id[a],by_id[b])))
        groups=defaultdict(list)
        for f in faces:groups[find(f['id'])].append(f['id'])
        levels.append(dict(lod=lod,sourceSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
            sourceFaces=len(faces),affineRegions=list(groups.values()),
            removedTriangulationEdges=removed,necessaryProjectionBoundaries=boundaries,
            coefficients={str(k):v.tolist() for k,v in coefs.items()}))
    out=ROOT/'artifacts/material-remap/Lift05/road-projection-boundaries.json'
    out.write_text(json.dumps(dict(levels=levels,
        scope='Offline source evidence only. Runtime must use measured projection boundaries, never source-face intersection or a chart fit.'),indent=2)+'\n')
    print([(l['lod'],l['sourceFaces'],len(l['affineRegions']),len(l['removedTriangulationEdges']),len(l['necessaryProjectionBoundaries'])) for l in levels])

if __name__=='__main__':main()
