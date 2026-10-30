"""Derive finite original-atlas edge charts and exact decoded native tangent fields."""
import json,numpy as np
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
audit=json.loads((OUT/'middle-fixedwalk-physical-scan.json').read_text());d=np.load(ROOT/'artifacts/uv-density/native/DrawBridge01_LOD1 Mesh.npz');positions=d['mesh0_positions'];faces=d['mesh0_faces'];bits=np.load(OUT/'middle-tangent-bits.npy')
def decode(b):
 x=int(b)&32767;y=(int(b)>>16)&32767
 if x>=16384:x-=32768
 if y>=16384:y-=32768
 a,bz=x/16383,y/16383;c=1-abs(a)-abs(bz)
 if c<0:a,bz=(1-abs(bz))*(1 if a>=0 else -1),(1-abs(a))*(1 if bz>=0 else -1)
 v=np.array([a,bz,c]);return v/np.linalg.norm(v)
rows=[]
for r in audit['rows']:
 for c in r['charts']:
  if c['role']!='edge-strip':continue
  vi=np.unique(faces[c['faces']]);p=positions[vi].astype(float);ts=np.array([decode(bits[v]) for v in vi]);hands=np.array([-1 if int(bits[v])&(1<<31) else 1 for v in vi]);assert len(set(hands))==1
  if np.all(ts==ts[0]):slope=np.zeros(3);intercept=ts[0]
  else:
   zs=np.unique(p[:,2]);assert len(zs)==2
   ends=[ts[p[:,2]==z] for z in zs];assert all(np.all(t==t[0]) for t in ends)
   slope=(ends[1][0]-ends[0][0])/(zs[1]-zs[0]);intercept=ends[0][0]-slope*zs[0]
  error=float(np.max(abs(p[:,2,None]*slope+intercept-ts)));assert error<1e-14
  rows.append(dict(right=r['right'],positive=r['positive'],sourceFaces=c['faces'],positions=p.tolist(),packedTangents=bits[vi].tolist(),uvAffineAxes=c['affineAxes'],tangentZCoefficient=slope.tolist(),tangentConstant=intercept.tolist(),handedness=int(hands[0]),maxVertexFieldResidual=error,wholeTriangleFieldProof='Every source vertex agrees with one affine Z field. Barycentric interpolation reproduces that field throughout each source triangle, without fitting or pre-normalization.'))
coverage=[]
for r in audit['rows']:
 for face in r['faces']:
  if face['role']!='edge-strip':continue
  pts=np.array(face['positions'])
  for w in ([1/3,1/3,1/3],[.6,.2,.2],[.2,.6,.2],[.2,.2,.6]):
   point=np.array(w)@pts;x=abs(point[0]);z=abs(point[2]);a,b=3.4000244140625,3.5999755859375
   if not r['right'] and r['positive']:
    chart=2 if z<86 else 1;limit=a+(b-a)*(z-82)/4 if z<86 else b
   else:chart=(4 if r['positive'] else 3) if r['right'] else 0;limit=a+(b-a)*(z-86)/4
   inside=bool(x>=a and x<=limit and z<=90)
   match=bool(face['face'] in rows[chart]['sourceFaces']);assert inside and match
   coverage.append(dict(sourceFace=face['face'],sampleBarycentric=w,chart=chart,insideActualEdge=inside))
(OUT/'middle-edge-fields.json').write_text(json.dumps(dict(status='MEASURED_FIELDS_NOT_RENDER_ACCEPTANCE',rows=rows,edgeInteriorCoverage=coverage,composeFrame='Transform each field vector by inverse RoadToObject rotation; affine source object Z is row2(RoadToObject) dot road-local homogeneous position.'),indent=2))
print('Exact native affine edge charts',len(rows))
