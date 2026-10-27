"""Independently test final binary32 UV vertices against original source triangles."""
from fractions import Fraction as F
from pathlib import Path
import hashlib, json, struct, sys
ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tools/PreparedMaterials/Lift03'))
from ExportRecipes import numbers
BASE = ROOT / 'artifacts/prepared-materials/Lift03/exact-union-chart-candidate'
def f32(x): return struct.unpack('<f', struct.pack('<f', x))[0]
def point(p): return tuple(F(f32(x)) for x in p)
def cross(a,b,p): return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])
def inside(p,t):
    signs = [cross(t[i],t[(i+1)%3],p) for i in range(3)]
    return (all(x>=0 for x in signs) or all(x<=0 for x in signs)) and cross(*t)!=0
def rational(p): return [[str(x.numerator),str(x.denominator)] for x in p]
losses=json.loads((BASE/'lost-units.json').read_text())
source_hashes=json.loads((BASE/'source-evidence.json').read_text())['sourceHashes']
for relative,digest in source_hashes.items():
    assert hashlib.sha256((ROOT/relative).read_bytes()).hexdigest()==digest,relative
cache={};results=[]
for unit in losses:
    key=(unit['name'],unit['mesh'])
    if key not in cache:
        prefix=key[0].replace(' ','_')+'_'+str(key[1])
        path=BASE.parent/'native-charts'/prefix
        meta=json.loads(path.with_suffix('.json').read_text())
        data=numbers(path.with_suffix('.bin'));ints=data['I32'];uv=list(zip(data['F32'][::2],data['F32'][1::2]))
        faces=[ints[i:i+3] for i in range(4,4+ints[2]*3,3)]
        cache[key]=(meta,[[point(uv[v]) for v in t] for t in faces])
    meta,triangles=cache[key]
    ids=meta['domains'][unit['originalDomain']]['charts']
    members=sorted({t for c in ids for t in meta['charts'][c]['triangles']})
    coords={point(e['key']):point(e['value']) for e in (unit['exactSourceUvs'] or [])}
    a,b,c=([f32(x) for x in unit[k]] for k in ('a','b','c'))
    def mapped(p):
        key=point(p)
        if key in coords:return coords[key]
        x,y=map(float,key)
        return point([a[i]+(b[i]-a[i])*x+(c[i]-a[i])*y for i in range(2)])
    vertices=[mapped(p) for p in unit['points']]
    witnesses=[]
    for index,p in enumerate(vertices):
        covered=[t for t in members if inside(p,triangles[t])]
        if not covered:
            # A separating signed edge is recorded for every native triangle.
            separators=[]
            for tid in members:
                t=triangles[tid];orientation=cross(*t)
                if not orientation: continue
                edge=next(i for i in range(3) if cross(t[i],t[(i+1)%3],p)*orientation<0)
                separators.append({'triangle':tid,'edge':edge,'signedCross':str(cross(t[edge],t[(edge+1)%3],p))})
            witnesses.append({'vertex':index,'uv':list(map(float,p)),'exactRationalUv':rational(p),'separatingEdges':separators})
    results.append({'render':key[0],'mesh':key[1],'id':unit['id'],'domain':unit['originalDomain'],'delta':unit['delta'],'scenario':unit['scenario'],'sourceChartIds':ids,'sourceTriangleCount':len(members),'outsideVertices':witnesses})
report={'method':'Exact rational point-in-original-native-triangle union. JSON final UVs are restored to binary32 before Fraction conversion; no epsilon, candidate loops or Shapely used. An outside vertex proves the final triangle is not contained.','sourceHashes':source_hashes,'losses':len(results),'lossesWithOutsideSourceVertex':sum(bool(r['outsideVertices']) for r in results),'results':results}
(BASE/'loss-source-proof.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='results'}))
