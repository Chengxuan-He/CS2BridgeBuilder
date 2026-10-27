"""Lift02 source-atlas chart sharing and bounded rectangle seam construction.
No source texture pixels are decoded, altered, copied or baked.
"""
from pathlib import Path
import collections, hashlib, json, math, re
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/prepared-materials/Lift02/source-sharing'

def cross(a,b): return a[0]*b[1]-a[1]*b[0]
def area(p): return abs(sum(cross(p[i],p[(i+1)%len(p)]) for i in range(len(p)))/2) if len(p)>2 else 0.
def hull(points):
    pts=sorted(set(points))
    if len(pts)<3:return pts
    def side(seq):
        out=[]
        for p in seq:
            while len(out)>1 and cross(np.subtract(out[-1],out[-2]),np.subtract(p,out[-1]))<=0:out.pop()
            out.append(p)
        return out
    return side(pts)[:-1]+side(pts[::-1])[:-1]
def inside(poly,p):
    return all(cross(np.subtract(poly[(i+1)%len(poly)],a),np.subtract(p,a))>=-1e-12 for i,a in enumerate(poly))
def clip(poly,axis,value,positive):
    out=[]
    for a,b in zip(poly,poly[1:]+poly[:1]):
        da=(a[axis]-value)*(1 if positive else -1);db=(b[axis]-value)*(1 if positive else -1)
        if da>=0:out.append(a)
        if (da>=0)!=(db>=0):
            t=da/(da-db);out.append([x+(y-x)*t for x,y in zip(a,b)])
    return out

def split_rectangle(uv,bounds,budget=32):
    lo=np.array(bounds[:2]);span=np.array(bounds[2:])-lo
    q=(np.array(uv)-lo)/span
    low=np.floor(q.min(0)+1e-10).astype(int);high=np.ceil(q.max(0)-1e-10).astype(int)
    # Budget is a bounded implementation limit, not permission to drop faces.
    if np.prod(np.maximum(1,high-low))>budget:return None
    start=[list(p)+[float(i==j) for j in range(3)] for i,p in enumerate(q)]
    result=[]
    for x in range(low[0],max(low[0]+1,high[0])):
      for y in range(low[1],max(low[1]+1,high[1])):
        poly=start
        for axis,a,b in [(0,x,x+1),(1,y,y+1)]:
            poly=clip(poly,axis,a,True);poly=clip(poly,axis,b,False)
            if not poly:break
        if len(poly)<3 or area(poly)<1e-14:continue
        for i in range(1,len(poly)-1):
            tri=[poly[0],poly[i],poly[i+1]]
            if area(tri)<1e-14:continue
            result.append({'weights':[p[2:] for p in tri],
                           'uv':[(lo+(np.array(p[:2])-[x,y])*span).tolist() for p in tri],
                           'period':[x,y]})
            if len(result)>budget:return None
    return result

def charts(name,mesh):
    path=ROOT/'artifacts/uv-density/native'/f'{name}.npz'
    data=np.load(path);faces=data[f'mesh{mesh}_faces'];uv=data[f'mesh{mesh}_uv0'][:,:2].astype(float)
    triangles=[tuple(sorted(map(tuple,uv[f]))) for f in faces]
    unique=list(dict.fromkeys(triangles));indices={p:i for i,p in enumerate(unique)}
    parent=list(range(len(unique)));edges={}
    def find(a):
        while parent[a]!=a:parent[a]=parent[parent[a]];a=parent[a]
        return a
    for i,t in enumerate(unique):
      for j,k in [(0,1),(1,2),(2,0)]:
        edge=tuple(sorted((t[j],t[k])))
        if edge[0]==edge[1]:continue
        if edge in edges:parent[find(i)]=find(edges[edge])
        else:edges[edge]=i
    groups=collections.defaultdict(list)
    for i,t in enumerate(unique):groups[find(i)].append(t)
    mapped={}; records={}
    for key,ts in groups.items():
        ps=[p for t in ts for p in t];lo=np.min(ps,axis=0);hi=np.max(ps,axis=0);outline=hull(ps)
        covered=sum(area(t) for t in ts);convex=covered>0 and abs(covered-area(outline))<max(1e-14,covered*1e-9)
        rectangle=convex and abs(covered-np.prod(hi-lo))<max(1e-14,covered*1e-9)
        ident=hashlib.sha256(json.dumps(sorted(ts),separators=(',',':')).encode()).hexdigest()
        records[key]={'id':ident,'nativeTriangles':len(ts),'bounds':list(lo)+list(hi),'convex':bool(convex),'rectangle':bool(rectangle),'outline':outline}
    for face,t in enumerate(triangles):mapped[face]=records[find(indices[t])]
    return mapped,list(records.values()),hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    render_sources={}
    for filename in ['Lift02SourceRenders.cs','Lift02PillarSources.cs']:
        s=(ROOT/'src/BridgeBuilder/Bridges/Types/Lift02'/filename).read_text()
        for name,geo,materials in re.findall(r'Create\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+)',s):
            render_sources[name]=re.findall(r'"([a-f0-9]{32})"',materials)
    cache={};summary=collections.Counter();cases=collections.defaultdict(collections.Counter);sample=[];chart_records=[];maxerr=0
    payload_path=OUT/'bounded-chart-receivers.ndjson'
    with payload_path.open('w') as writer:
      for line in (OUT/'actual-receivers.json.receivers.ndjson').open():
        r=json.loads(line);key=(r['name'],r['mesh']);case=(r['name'],r['mesh'],r['delta'])
        if key not in cache:
            mapping,records,digest=charts(*key);cache[key]=mapping
            chart_records.append({'render':key[0],'mesh':key[1],'sourceCid':render_sources[key[0]][key[1]],'nativeSha256':digest,'charts':records})
        chart=cache[key][r['face']];q=np.array(r['q'])
        if r['triangle']:mapped=np.array(r['a'])+q[:,0,None]*(np.array(r['b'])-r['a'])+q[:,1,None]*(np.array(r['c'])-r['a'])
        else:mapped=np.array(r['bounds'][:2])+q*(np.array(r['bounds'][2:])-r['bounds'][:2])
        summary['receivers']+=1;cases[case]['receivers']+=1
        if chart['convex'] and all(inside(chart['outline'],p) for p in mapped):
            result=[{'weights':np.eye(3).tolist(),'uv':mapped.tolist(),'period':[0,0]}];kind='directNativeChart'
        elif chart['rectangle']:
            result=split_rectangle(mapped,chart['bounds']);kind='boundedRectangleRepeat' if result else 'seamBudgetExceeded'
        else:result=None;kind='nonrectangularExtendedChart'
        summary[kind]+=1;cases[case][kind]+=1
        if result:
            source_area=area(mapped);out_area=sum(area(p['uv']) for p in result)
            relative=abs(source_area-out_area)/max(source_area,1e-20);maxerr=max(maxerr,relative)
            if relative>1e-5:raise ValueError((case,r['face'],'area mismatch',relative))
            for fragment in result:
                w=np.array(fragment['weights']);assert np.max(abs(w.sum(1)-1))<1e-10
                # UV derivatives before/after wrap differ only by constant translation.
                expected=w@mapped
                actual=np.array(fragment['uv'])
                shifts=actual-expected
                assert np.max(abs(shifts-shifts[0]))<1e-10
            summary['outputTriangles']+=len(result);cases[case]['outputTriangles']+=len(result)
            writer.write(json.dumps({'render':r['name'],'mesh':r['mesh'],'delta':r['delta'],'face':r['face'],'sourceCid':render_sources[r['name']][r['mesh']],
                 'chart':chart['id'],'kind':kind,'sourceVertexPositions':r['source'],'targetVertexPositions':r['target'],'fragments':result},separators=(',',':'))+'\n')
        elif len(sample)<15:sample.append({'render':r['name'],'face':r['face'],'delta':r['delta'],'kind':kind,'chart':chart['id']})
    report={'style':'Lift02','summary':summary,'maxRelativeUvAreaError':maxerr,'maxTrianglesPerReceiver':32,
        'cases':[dict(render=k[0],mesh=k[1],delta=k[2],**v) for k,v in cases.items()],
        'unsupportedExamples':sample,'chartSources':chart_records,'texturePayloadsCreated':0,
        'nativeRenderingValidated':False,'limitations':['Rectangle repeat candidates require native edge/filter/mip validation.','Nonrectangular extended charts are not replaced with old trianglefold.','Budget-exceeding receivers retain existing resources pending common implementation.']}
    (OUT/'bounded-chart-report.json').write_text(json.dumps(report,indent=2))
    print(json.dumps({'summary':summary,'maxRelativeUvAreaError':maxerr,'payloadBytes':payload_path.stat().st_size},indent=2))
if __name__=='__main__':main()

