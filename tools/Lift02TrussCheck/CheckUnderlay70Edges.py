"""Check complete outer side boundaries against the actual selected-road edge."""
from pathlib import Path
from collections import Counter, defaultdict
import struct,json
import numpy as np
root=Path(__file__).resolve().parents[2];out=root/'artifacts/material-remap/Lift02/underlay70'
boards=json.loads((out/'boards.json').read_text());results=[]
for label in ['road','track','narrow','leftwalk','rightwalk']:
 for lod in range(3):
  raw=(out/f'{label}-lod{lod}-output.bin').read_bytes();nv,ni=struct.unpack_from('<ii',raw)
  p=np.frombuffer(raw,'<f4',count=nv*3,offset=8).reshape(-1,3);f=np.frombuffer(raw,'<i4',count=ni,offset=8+nv*12).reshape(-1,3)
  faces=np.fromfile(out/f'{label}-lod{lod}-faces.bin','<i4');q=p[f].astype(float)
  road=np.frombuffer((out/f'{label}-lod{lod}-road.bin').read_bytes(),'<f4',offset=4).reshape(-1,3,3).astype(float)
  n=np.cross(road[:,1]-road[:,0],road[:,2]-road[:,0]);road=road[n[:,1]>0]
  failures=[];max_contact=0.;points=0;loops=0
  for kind,x in [(1,float(road[:,:,0].min())),(2,float(road[:,:,0].max()))]:
   edges=[]
   for t in road:
    for i in range(3):
     a,b=t[i],t[(i+1)%3]
     if a[0]==x and b[0]==x and a[2]!=b[2]:edges.append(sorted([a,b],key=lambda v:v[2]))
   def height(z):
    return max(a[1]+(b[1]-a[1])*((z-a[2])/(b[2]-a[2])) for a,b in edges if a[2]<=z<=b[2])
   for board in [b for b in boards if b['lod']==min(lod,1)]:
    ts=q[np.isin(faces,board['sourceFaces'][kind])];ts=ts[np.all(ts[:,:,0]==x,axis=1)]
    counts=Counter(tuple(sorted((tuple(t[i]),tuple(t[(i+1)%3])))) for t in ts for i in range(3))
    boundary=[e for e,count in counts.items() if count==1]
    graph=defaultdict(list)
    for a,b in boundary:graph[a].append(b);graph[b].append(a)
    if not graph or any(len(v)!=2 for v in graph.values()):failures.append([kind,board['component'],'non-cycle-boundary']);continue
    seen=set();pending=[next(iter(graph))]
    while pending:
     v=pending.pop()
     if v in seen:continue
     seen.add(v);pending.extend(graph[v])
    if len(seen)!=len(graph):failures.append([kind,board['component'],'multiple-boundary-loops'])
    loops+=1;floor=min(v[1] for v in graph)
    top=[(a,b) for a,b in boundary if a[2]!=b[2] and not(a[1]==floor and b[1]==floor)]
    for a,b in top:
     if a[2]>b[2]:a,b=b,a
     zs=sorted(set([a[2],b[2]]+[v[2] for edge in edges for v in edge if a[2]<v[2]<b[2]]))
     for z in zs:
      y=a[1]+(b[1]-a[1])*((z-a[2])/(b[2]-a[2]));max_contact=max(max_contact,abs(y-height(z)));points+=1
  record=dict(input=label,lod=lod,outerBoundaryLoops=loops,contactSamples=points,
              maximumRoadContactResidualMeters=max_contact,failures=failures)
  results.append(record);print(label,lod,'loops',loops,'failures',len(failures),'contact mm',max_contact*1000)
(out/'edge-result.json').write_text(json.dumps(results,indent=2));assert all(not r['failures'] for r in results)
