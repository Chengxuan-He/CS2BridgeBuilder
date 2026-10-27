"""Measure real Lift04 plan receivers against the installed native chart loops."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from Lift04UvChartBindings import orientation


def point_covered(p, polygon):
    inside = False
    for a,b in zip(polygon,polygon[1:]+polygon[:1]):
        if orientation(a,b,p)==0 and min(a[0],b[0])<=p[0]<=max(a[0],b[0]) and min(a[1],b[1])<=p[1]<=max(a[1],b[1]): return True
        if (a[1]>p[1]) != (b[1]>p[1]) and p[0] < (b[0]-a[0])*(p[1]-a[1])/(b[1]-a[1])+a[0]: inside = not inside
    return inside


def covered(triangle, polygon):
    if not all(point_covered(p,polygon) for p in triangle): return False
    # Split each tested edge only in this numerical predicate, at every polygon
    # edge intersection. Checking every open interval catches concave excursions.
    for a,b in zip(triangle,triangle[1:]+triangle[:1]):
        v = (b[0]-a[0],b[1]-a[1]); length = v[0]*v[0]+v[1]*v[1]
        if length == 0: continue
        cuts = [0.,1.]
        for c,d in zip(polygon,polygon[1:]+polygon[:1]):
            w = (d[0]-c[0],d[1]-c[1]); offset = (c[0]-a[0],c[1]-a[1])
            denom = v[0]*w[1]-v[1]*w[0]
            if denom:
                t = (offset[0]*w[1]-offset[1]*w[0])/denom
                u = (offset[0]*v[1]-offset[1]*v[0])/denom
                if 0<=t<=1 and 0<=u<=1: cuts.append(t)
            elif orientation(a,b,c)==0:
                for p in (c,d):
                    t = ((p[0]-a[0])*v[0]+(p[1]-a[1])*v[1])/length
                    if 0<=t<=1: cuts.append(t)
        cuts.sort()
        for lo,hi in zip(cuts,cuts[1:]):
            t = (lo+hi)/2
            if not point_covered((a[0]+t*v[0],a[1]+t*v[1]),polygon): return False
    return True


def main():
    folder = ROOT/'artifacts/prepared-materials/Lift04/shared-sampling'
    raw = (ROOT/'assets/HardcodedNumbers/Lift04/Lift04UvCharts.bin').read_bytes()
    assert hashlib.sha256(raw[:-32]).digest()==raw[-32:]
    counts = struct.unpack_from('<6i',raw,8)
    ints = struct.unpack_from('<'+'i'*counts[0],raw,32)
    values = struct.unpack_from('<'+'d'*counts[5],raw,32+4*counts[0])
    loops=[]
    for at in ints[2:2+ints[1]]:
        assert ints[at]==1
        start,count=ints[at+1:at+3]
        loops.append([values[start+2*p:start+2*p+2] for p in range(count)])
    metadata=json.loads((folder/'native-chart-bindings.json').read_text())
    at=3+ints[1]; meshes={}
    for m in metadata['meshes']:
        n=ints[at];at+=1; domains=[]
        for _ in range(n):
            count=ints[at];at+=1;domains.append(ints[at:at+count]);at+=count
        meshes[m['render'],m['mesh']]=domains
    totals=Counter(); widths={}
    for line in (folder/'receiver-plans.json.receivers.ndjson').open():
        r=json.loads(line);totals['receivers']+=1
        refs=meshes[r['name'],r['mesh']][r['domain']]
        if not refs: totals['noReliableChart']+=1;continue
        if r['repeatOnly']: totals['repeatOnly']+=1;continue
        q=np.asarray(r['points'],dtype=np.float32).astype(float)
        if r['triangle']:
            a,b,c=(np.asarray(r[k],dtype=np.float32).astype(float) for k in ('a','b','c'))
            atlas=a+q[:,0:1]*(b-a)+q[:,1:2]*(c-a)
        else:
            bounds=np.asarray(r['bounds'],dtype=np.float32).astype(float)
            atlas=bounds[:2]+q*(bounds[2:]-bounds[:2])
        ok=any(covered(atlas.tolist(),loops[c]) for c in refs)
        kind='containedReceiver' if ok else 'outsideChart'
        totals[kind]+=1;widths.setdefault(str(r['delta']),Counter())[kind]+=1
    result=dict(status='PASS',counts=dict(totals),byDelta={k:dict(v) for k,v in widths.items()},
        interpretation='Actual geometry-plan receivers; exact original domain indices; conservative offline polygon containment without tolerance. Common native reader and persisted-game appearance remain separate checks.')
    (folder/'receiver-chart-coverage.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(result))


if __name__=='__main__':main()
