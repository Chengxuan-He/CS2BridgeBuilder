"""Physical-domain middle fixed-walk scan; retains old UV crop as negative control."""
import ast,json,numpy as np
from collections import Counter
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
tree=ast.parse(Path(__file__).with_name('AuditFarPrecision.py').read_text());fn=next(n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='feasible')
import itertools
exec(compile(ast.Module(body=[fn],type_ignores=[]),'<rounding solver>','exec'))
d=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz');faces=d['mesh0_faces'];positions=d['mesh0_positions'].astype(float);uv=d['mesh0_uv0'].astype(float);p=positions[faces];t=uv[faces]
tangents=np.load(OUT/'middle-tangent-bits.npy');rows=[]
for right in [False,True]:
 for positive in [False,True]:
  x=p[:,:,0]*(1 if right else -1);z=p[:,:,2]*(1 if positive else -1)
  mask=(abs(p[:,:,1]-10).max(1)<1e-5)&(x.min(1)>3.39)&(x.max(1)<6.6)&(z.min(1)>=78)&(z.max(1)<=90.1)
  ids=np.flatnonzero(mask);records=[];groups={}
  for i in ids:
   old=bool(t[i,:,0].min()>=.485 and t[i,:,0].max()<=.676 and t[i,:,1].min()>=.012 and t[i,:,1].max()<=.274)
   role='main' if x[i].max()>6 else 'edge-strip'
   key=(role,float(z[i].min()),float(z[i].max()));groups.setdefault(key,[]).append(int(i))
   records.append(dict(face=int(i),positions=p[i].tolist(),uv=t[i].tolist(),nativeTangentBits=np.unique(tangents[faces[i]]).tolist(),includedByOldCrop=old,role=role))
  charts=[]
  for (role,zmin,zmax),fs in groups.items():
   vi=np.unique(faces[fs]);charts.append(dict(role=role,zRange=[zmin,zmax],faces=fs,affineAxes=feasible(positions[vi],uv[vi])))
  mainIds=[r['face'] for r in records if r['role']=='main'];vi=np.unique(faces[mainIds]);uOnly=uv[vi].copy();uOnly[:,1]=.5;commonU=feasible(positions[vi],uOnly)[0]
  unit=json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/continuous-unit-consumer-metadata.json').read_text())['units'][2]
  vmin,vmax=unit['sourceMin'][1],unit['sourceMax'][1];span=vmax-vmin;phase=[];limits=[]
  for chart in charts:
   if chart['role']!='main':continue
   for face in chart['faces']:
    for vertex in faces[face]:
     value=uv[vertex,1];h=np.float16(value);lo=(float(h)+float(np.nextafter(h,np.float16(-np.inf))))/2;hi=(float(h)+float(np.nextafter(h,np.float16(np.inf))))/2
     predicted=vmin+(chart['zRange'][1]-abs(positions[vertex,2]))*span/4
     limits.append((lo-predicted,hi-predicted,int(face),int(vertex)))
     phase.append(dict(face=int(face),vertex=int(vertex),sourceV=value,predictedV=predicted,insideOriginalRoundingCell=bool(lo<=predicted<=hi)))
  lower=max(limits,key=lambda x:x[0]);upper=min(limits,key=lambda x:x[1]);vi=np.unique(faces[mainIds]);sourceU=uv[vi,0];unitU=unit['contentMin'][0]+(sourceU-unit['sourceMin'][0])*(unit['contentMax'][0]-unit['contentMin'][0])/(unit['sourceMax'][0]-unit['sourceMin'][0])
  edges=Counter()
  for face in mainIds:
   verts=[tuple(float(k) for k in positions[v][[0,2]]) for v in faces[face]]
   for a,b in zip(verts,verts[1:]+verts[:1]):edges[tuple(sorted((a,b)))]+=1
  boundary=[list(e) for e,count in edges.items() if count==1]
  # Shared internal authored reset edges cancel geometrically. Only material-domain outline remains.
  def main_contains(position):
   xx=abs(position[0]);zz=abs(position[2]);a,b=3.4000244140625,3.5999755859375
   start=b+(a-b)*(zz-78.150390625)/(82-78.150390625)
   if not right and positive:inner=max(start,min(b,a+(b-a)*(zz-82)/4))
   else:inner=max(start,a,a+(b-a)*(zz-86)/4)
   return bool(78.150390625<=zz<=90 and xx>=inner-1e-12)
  boundaryChecks=[]
  for face in records:
   pts=np.array(face['positions']);centroid=pts.mean(0);actual=main_contains(centroid);expected=face['role']=='main'
   boundaryChecks.append(dict(face=face['face'],centroidInsideMain=actual,expectedMain=expected,passClassification=actual==expected,allMainVerticesInside=all(main_contains(q) for q in pts) if expected else None))
  assert all(c['passClassification'] and c['allMainVerticesInside'] is not False for c in boundaryChecks)
  phaseAudit=dict(physicalPeriod=4,sourcePeriod=span,commonPhaseOffsetInterval=[lower[0],upper[1]],commonPhaseFeasible=bool(lower[0]<=upper[1]),lowerWitness=list(lower),upperWitness=list(upper),zeroPhaseFailures=[r for r in phase if not r['insideOriginalRoundingCell']],unitURange=[float(unitU.min()),float(unitU.max())],insideDeclaredFiniteGuard=bool(unitU.min()>=0 and unitU.max()<=1),note='UV-transfer/rounding-cell audit only, not filtered-pixel equivalence.')
  rows.append(dict(phaseAndGuard=phaseAudit,right=right,positive=positive,faces=records,charts=charts,mainCommonU=commonU,mainBoundaryEdges=boundary,boundaryClassification=boundaryChecks))
(OUT/'middle-fixedwalk-physical-scan.json').write_text(json.dumps(dict(status='MEASUREMENT_ONLY_NOT_RUNTIME_MAPPING',rows=rows),indent=2))
for r in rows:print(r['right'],r['positive'],len(r['faces']),[(c['role'],c['zRange'],[a['feasible'] for a in c['affineAxes']]) for c in r['charts']])
