"""Measure actual generated receiver containment in same-part native UV charts."""
from pathlib import Path
import collections
import gzip
import hashlib
import json
import struct
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/prepared-materials/Draw03/native-charts'
EPS=1e-12


def cross(a,b):return a[...,0]*b[...,1]-a[...,1]*b[...,0]


class Chart:
    def __init__(self,rings):
        self.rings=rings
        self.a=np.concatenate(rings)
        self.b=np.concatenate([np.roll(r,-1,axis=0) for r in rings])
        self.low=self.a.min(0);self.high=self.a.max(0)
    def points(self,ps):
        result=[]
        for p in ps:
            d=self.b-self.a;q=p-self.a
            on=(np.abs(cross(d,q))<=EPS)&(np.sum(q*d,axis=1)>=-EPS)&(np.sum((p-self.b)*d,axis=1)<=EPS)
            if on.any():result.append(True);continue
            candidate=(self.a[:,1]>p[1])!=(self.b[:,1]>p[1])
            a,b=self.a[candidate],self.b[candidate]
            x=a[:,0]+(p[1]-a[:,1])*(b[:,0]-a[:,0])/(b[:,1]-a[:,1])
            result.append(bool(np.count_nonzero(x>p[0])%2))
        return all(result)
    def contains(self,t):
        if np.any(t<self.low-EPS) or np.any(t>self.high+EPS) or not self.points(t):return False
        # Every edge stays inside: reject strict crossings, then test intervals
        # on shared/tangent boundaries through each edge midpoint.
        for p,q in zip(t,np.roll(t,-1,axis=0)):
            a,b=self.a,self.b
            oa,ob=cross(q-p,a-p),cross(q-p,b-p)
            op,oq=cross(b-a,p-a),cross(b-a,q-a)
            if np.any((oa*ob < -EPS*EPS)&(op*oq < -EPS*EPS)):return False
        if not self.points((t+np.roll(t,-1,axis=0))/2):return False
        # A hole wholly enclosed by the receiver has no edge intersection.
        signs=np.stack([cross(q-p,self.a-p) for p,q in zip(t,np.roll(t,-1,axis=0))])
        if np.any(np.all(signs>EPS,axis=0)|np.all(signs<-EPS,axis=0)):return False
        return True


catalogue=json.loads((OUT/'catalogue.json').read_bytes());sources={}
for item in catalogue['resources']:
    b=(ROOT/'assets/HardcodedNumbers/Draw03'/(item['resource']+'.bin')).read_bytes()
    assert hashlib.sha256(b).hexdigest()==item['sha256']
    ni,_,_,_,_,nd=struct.unpack_from('<6i',b,8)
    ints=np.frombuffer(b,'<i4',ni,32);ds=np.frombuffer(b,'<f8',nd,32+ni*4)
    ndom,ncharts=map(int,ints[:2]);charts=[]
    for i in range(ncharts):
        at=int(ints[2+ndom+i]);rings=[]
        for j in range(int(ints[at])):
            pos,count=map(int,ints[at+1+j*2:at+3+j*2]);rings.append(ds[pos:pos+count*2].reshape(-1,2))
        charts.append(Chart(rings))
    domains=[]
    for d in range(ndom):
        at=int(ints[2+d]);domains.append([charts[int(i)] for i in ints[at+1:at+1+ints[at]]])
    sources[item['render'],item['mesh']]=domains

stats=collections.defaultdict(lambda:{'changedReceivers':0,'contained':0,'outside':0,'examples':[]})
for line in gzip.open(OUT/'receivers.json.receivers.jsonl.gz','rt'):
    r=json.loads(line);row=stats[r['name'],r['mesh'],r['delta']];row['changedReceivers']+=1
    t=np.asarray(r['mapped']);charts=sources[r['name'],r['mesh']][r['domain']]
    if any(c.contains(t) for c in charts):row['contained']+=1
    else:
        row['outside']+=1
        if len(row['examples'])<2:row['examples'].append({'domain':r['domain'],'face':r['face'],'fragment':r['fragment'],'mapped':r['mapped']})
report={'scope':'Actual existing width plans, symmetric default sidewalk fixtures, delta 0/2/8. Changed receivers only. Native shader/mip and full saved-asset acceptance remain separate.',
        'absoluteUvArithmeticTolerance':EPS,
        'counts':{k:sum(v[k] for v in stats.values()) for k in ('changedReceivers','contained','outside')},
        'scenarios':[{'render':k[0],'mesh':k[1],'delta':k[2],**v} for k,v in stats.items()],
        'inputs':{str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in (OUT/'catalogue.json',OUT/'receivers.json.receivers.jsonl.gz',ROOT/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll',ROOT/'artifacts/uv-density/layout-plans.bin')}}
(OUT/'receiver-coverage.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report['counts']))
