"""Check complete actual Lift02 receiver containment against packed source charts."""
from pathlib import Path
from collections import Counter,defaultdict
import hashlib,json,struct
import numpy as np
from ExportNativeCharts import ROOT,OUT,read_domains,cross
# read_domains uses the shared schema-2/schema-3 decoder; chart BBN is unchanged.
EPS=1e-12

def on(a,b,p):
    return abs(cross(b-a,p-a))<=EPS and np.dot(p-a,p-b)<=EPS

def inside(loops,p):
    state=False
    for loop in loops:
        for a,b in zip(loop,np.roll(loop,-1,axis=0)):
            if on(a,b,p):return True
            if (a[1]>p[1])!=(b[1]>p[1]) and p[0]<(b[0]-a[0])*(p[1]-a[1])/(b[1]-a[1])+a[0]:state=not state
    return state

def contains(loops,tri):
    if not all(inside(loops,p) for p in tri):return False
    for a,b in zip(tri,np.roll(tri,-1,axis=0)):
        d=b-a;cuts=[0.,1.]
        for loop in loops:
            for c,e in zip(loop,np.roll(loop,-1,axis=0)):
                f=e-c;den=cross(d,f)
                if abs(den)>EPS:
                    t=cross(c-a,f)/den;s=cross(c-a,d)/den
                    if -EPS<=t<=1+EPS and -EPS<=s<=1+EPS:cuts.append(max(0,min(1,t)))
                elif abs(cross(c-a,d))<=EPS and np.dot(d,d)>EPS:
                    cuts.extend(max(0,min(1,np.dot(p-a,d)/np.dot(d,d))) for p in [c,e])
        cuts=sorted(set(cuts))
        if any(not inside(loops,a+d*((x+y)*.5)) for x,y in zip(cuts,cuts[1:])):return False
    # A hole can fit inside a triangle while all its edges remain in the chart.
    for loop in loops:
        for p in loop:
            sides=[cross(b-a,p-a) for a,b in zip(tri,np.roll(tri,-1,axis=0))]
            if all(x>EPS for x in sides) or all(x<-EPS for x in sides):return False
    return True

def metric(p,uv):
    p=np.array(p);uv=np.array(uv);e=p[1]-p[0];f=p[2]-p[0];length=np.linalg.norm(e)
    if length==0:return None
    x=np.dot(e,f)/length;y=np.linalg.norm(np.cross(e,f))/length
    if y==0:return None
    first=(uv[1]-uv[0])/length;second=(uv[2]-uv[0]-first*x)/y
    j=np.stack([first,second],axis=1);return j@j.T

def main():
    meta=json.loads((OUT/'native-chart-integration.json').read_bytes())
    raw=(ROOT/'assets/HardcodedNumbers/Lift02/Types_Lift02_Lift02UvCharts_cs.bin').read_bytes()
    assert hashlib.sha256(raw).hexdigest()==meta['resourceSha256']
    _,_,ni,nu,nl,nul,nf,nd=struct.unpack('<8i',raw[:32]);ints=np.frombuffer(raw,dtype='<i4',count=ni,offset=32);ds=np.frombuffer(raw,dtype='<f8',count=nd,offset=32+4*ni)
    chart_cache={};routes={};index={}
    for route in meta['routes']:
        key=(route['render'],route['mesh']);routes[key]=route
        filename='UvDomain_'+key[0].replace(' ','_')+'_'+str(key[1])+'.bin'
        domain_faces=read_domains(ROOT/'assets/HardcodedNumbers/Lift02'/filename)
        index[key]={f:i for i,faces in enumerate(domain_faces) for f in faces}
    def chart(offset):
        if offset not in chart_cache:
            chart_cache[offset]=[ds[ints[offset+1+i*2]:ints[offset+1+i*2]+ints[offset+2+i*2]*2].reshape(-1,2) for i in range(ints[offset])]
        return chart_cache[offset]
    totals=Counter();cases=defaultdict(Counter);accepted=[];worst=0
    for line in (OUT/'actual-receivers.json.receivers.ndjson').open():
        r=json.loads(line);key=(r['name'],r['mesh']);route=routes[key];domain=index[key][r['face']];off=ints[route['offset']+domain]
        q=np.array(r['q']);mapped=np.array(r['a'])+q[:,0,None]*(np.array(r['b'])-r['a'])+q[:,1,None]*(np.array(r['c'])-r['a']) if r['triangle'] else np.array(r['bounds'][:2])+q*(np.array(r['bounds'][2:])-r['bounds'][:2])
        mapped=mapped.astype(np.float32).astype(float)
        matched=next((int(c) for c in ints[off+1:off+1+ints[off]] if contains(chart(int(c)),mapped)),None)
        error=0.
        if matched is not None:
            before=metric(r['source'],r['uv']);after=metric(r['target'],mapped)
            if before is not None and after is not None and np.linalg.norm(before)>0:
                error=float(np.linalg.norm(before-after)/np.linalg.norm(before));worst=max(worst,error)
                if error>1e-4:totals['containedButFloat32DensityRejected']+=1;matched=None
        kind='nativeChartContained' if matched is not None else 'requiresOtherNativeRepresentation';totals[kind]+=1;cases[(r['name'],r['mesh'],r['delta'])][kind]+=1
        if matched is not None:accepted.append(dict(render=r['name'],mesh=r['mesh'],delta=r['delta'],face=r['face'],domain=domain,chartOffset=matched,sourceUv=mapped.tolist()))
    report={'style':'Lift02','totals':totals,'cases':[dict(render=k[0],mesh=k[1],delta=k[2],**v) for k,v in cases.items()],
      'resourceSha256':meta['resourceSha256'],'receiverSha256':hashlib.sha256((OUT/'actual-receivers.json.receivers.ndjson').read_bytes()).hexdigest(),
      'maximumCandidateMetricRelativeError':worst,'densityTolerance':1e-4,'epsilonUv':EPS,'method':'Full receiver edges partitioned at every source boundary crossing, midpoint even/odd classification, enclosed-hole rejection. No vertex-only containment.','nativeRenderingValidated':False}
    (OUT/'native-chart-coverage.json').write_text(json.dumps(report,indent=2));(OUT/'native-chart-accepted.json').write_text(json.dumps(accepted))
    print(json.dumps(totals))
if __name__=='__main__':main()
