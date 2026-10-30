"""Compile actual original UV reset stations; never force a nominal period grid."""
import hashlib,json,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3];sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'));from MovableUvDomainFormat import write_bbn
out=ROOT/'artifacts/material-remap/Lift03';audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift03/outputs/native-repeat-mechanism/audit.json').read_text());floats=[];ints=[5,16];cases=[];reports=[]
def compile_case(lod,deck,xSide,zSide):
 r=next(r for r in audit['reports'] if r['lod']==lod and r['deck']==deck);path=ROOT/'artifacts/uv-density/native'/('LiftBridge03'+('_LOD1' if lod else '')+' Mesh.npz');a=np.load(path);assert hashlib.sha256(path.read_bytes()).hexdigest()==r['sourceNPZSHA256'];ids=np.array(r['faceIds']);p=a['mesh0_positions'][a['mesh0_faces'][ids]].astype(float);q=a['mesh0_uv0'][a['mesh0_faces'][ids]].astype(float)
 main=deck=='Main';width=8 if main else 5
 p[abs(p)<1e-8]=0
 select=np.all(p[:,:,0]*xSide>=0,axis=1)&np.all(p[:,:,2]*zSide>=0,axis=1)
 p,q,ids=p[select],q[select],ids[select];p[:,:,0]=-abs(p[:,:,0]);p[:,:,2]=abs(p[:,:,2])
 # Native axis-origin roundoff only; no positive authored reset station is moved.
 p[:,:,2][abs(p[:,:,2])<1e-8]=0
 # Trace endpoints that lie on original edges are recovered from the actual mesh
 # vertex coordinate, avoiding numerical line-intersection roundoff.
 stations=np.unique(p[:,:,2]);snap=lambda z:float(stations[np.argmin(abs(stations-z))])
 segments=[];fixed=-6 if main else -2
 for face,(points,uv) in enumerate(zip(p,q)):
  hits=[]
  for i,j in [(0,1),(1,2),(2,0)]:
   if (points[i,0]-fixed)*(points[j,0]-fixed)>0 or points[i,0]==points[j,0]:continue
   t=(fixed-points[i,0])/(points[j,0]-points[i,0])
   if 0<=t<=1:hits.append((float(points[i,2]+t*(points[j,2]-points[i,2])),(uv[i]+t*(uv[j]-uv[i])).tolist()))
  hits=sorted({z:uv for z,uv in hits}.items())
  if len(hits)==2 and hits[1][0]-hits[0][0]>1e-8:segments.append(dict(start=hits[0][0],end=hits[1][0],uvStart=hits[0][1],uvEnd=hits[1][1]))
 segments=sorted(segments,key=lambda s:(s['start'],s['end']));cells=[];current=[]
 for s in segments:
  if current and (abs(s['start']-current[-1]['end'])>1e-8 or abs(s['uvStart'][1]-current[-1]['uvEnd'][1])>.001):cells.append(current);current=[]
  current.append(s)
 if current:cells.append(current)
 crows=[]
 for cell,segments in enumerate(cells):
  zmin,zmax=snap(segments[0]['start']),snap(segments[-1]['end']);vfirst,vlast=segments[0]['uvStart'][1],segments[-1]['uvEnd'][1]
  eligible=np.all(p[:,:,0]<=0,axis=1)&(p[:,:,2].mean(axis=1)>=zmin)&(p[:,:,2].mean(axis=1)<zmax)&(p[:,:,0].mean(axis=1)>-width)
  edgeUses={}
  for f in np.flatnonzero(eligible):
   for i,j in [(0,1),(1,2),(2,0)]:
    edge=tuple(sorted([tuple(p[f,i]),tuple(p[f,j])]))
    edgeUses[edge]=edgeUses.get(edge,0)+1
  xs=[edge[0][0] for edge,count in edgeUses.items() if count==2 and edge[0][0]==edge[1][0] and edge[0][2]!=edge[1][2]]
  cuts=sorted(set([-width,0]+[float(x) for x in xs if -width<x<0]))
  # A reset cell is a real authored domain, even when source quantization changes
  # its length. Partial motion terminals remain finite and never wrap.
  expectedV=(.009307861328125,.252197265625) if main else (.255126953125,.498291015625)
  repeat=vlast==expectedV[0] and vfirst==expectedV[1]
  crows.extend([len(floats)//35,len(cuts)-1,int(repeat)])
  for xmin,xmax in zip(cuts[:-1],cuts[1:]):
   select=eligible&(p[:,:,0].min(axis=1)<xmax)&(p[:,:,0].max(axis=1)>xmin);found=np.flatnonzero(select)
   if len(found)==0:found=np.flatnonzero(eligible&(p[:,:,0].min(axis=1)<=xmin)&(p[:,:,0].max(axis=1)>=xmax))
   assert len(found)>0,(lod,deck,cell,zmin,zmax,xmin,xmax)
   candidates=[]
   for f in found:
    matrix=np.column_stack([p[f,:,0],p[f,:,2],np.ones(3)])
    if abs(np.linalg.det(matrix))>1e-10:candidates.append((f,np.linalg.solve(matrix,q[f])))
   f,coeff=candidates[0];samples=np.column_stack([p[found,:,0].reshape(-1),p[found,:,2].reshape(-1),np.ones(len(found)*3)]);error=np.max(np.abs(samples@coeff-q[found].reshape(-1,2)),axis=0)*4096
   minimum=q[found].min((0,1));maximum=q[found].max((0,1))
   allpoints=p[found].reshape(-1,3);alluv=q[found].reshape(-1,2)
   startpoints=np.unique(allpoints[alluv[:,1]==maximum[1]][:,[0,2]],axis=0)
   assert len(startpoints)>=2,(lod,deck,cell,'missing start edge',p[found].tolist(),q[found].tolist())
   left,right=startpoints[np.argmin(startpoints[:,0])],startpoints[np.argmax(startpoints[:,0])]
   assert right[0]>left[0]
   slope=(right[1]-left[1])/(right[0]-left[0]);startplane=[-slope,1,float(slope*left[0]-left[1])]
   alternate=np.zeros((3,2));alternate2=np.zeros((3,2));fold=[0,0,0];fold2=[0,0,0];foldEvidence=None
   if max(error)>1e-7:
    assert 2<=len(candidates)<=3,(lod,deck,cell,'unexpected real fold count')
    adjacency={int(f):[] for f,c in candidates};matrices={int(f):c for f,c in candidates}
    for first,_ in candidates:
     for second,_ in candidates:
      if first==second:continue
      if len(set(map(tuple,p[first])).intersection(map(tuple,p[second])) )==2:adjacency[int(first)].append(int(second))
    first=next(f for f,neighbors in adjacency.items() if len(neighbors)==1);middle=adjacency[first][0];ordered=[first,middle]
    if len(candidates)==3:ordered.append(next(f for f in adjacency[middle] if f!=first))
    coeff=matrices[first];alternate=matrices[middle]
    def split(first,second):
     shared=sorted(set(map(tuple,p[first])).intersection(map(tuple,p[second])));assert len(shared)==2
     e0,e1=np.array(shared);direction=e1-e0;plane=np.array([direction[2],-direction[0],-direction[2]*e0[0]+direction[0]*e0[2]])
     side=lambda point:float(np.array([point[0],point[2],1])@plane)
     if side(p[first].mean(0))>0:plane=-plane
     assert side(p[first].mean(0))<0 and side(p[second].mean(0))>0
     continuity=[float(np.max(abs(np.array([e[0],e[2],1])@matrices[first]-np.array([e[0],e[2],1])@matrices[second]))) for e in [e0,e1]];assert max(continuity)<1e-12
     return plane,dict(sourceFaces=[int(ids[first]),int(ids[second])],sharedPhysicalEdge=[e0.tolist(),e1.tolist()],sourceUvContinuous=True,maxEdgeUvDifference=max(continuity))
    fold,edge1=split(first,middle);edges=[edge1]
    if len(ordered)==3:
     alternate2=matrices[ordered[2]];fold2,edge2=split(middle,ordered[2]);edges.append(edge2)
     assert np.array([*p[ordered[2]].mean(0)[[0,2]],1])@fold>0
    f=first
    foldEvidence=dict(sourceFaces=[int(ids[i]) for i in ordered],edges=edges,minimumAdditionalPlanes=len(ordered)-1,singleAffineMaxSourcePixelError=error.tolist(),coverage='Only nonmergeable connected affine charts are separated by their measured shared edges. Each next chart is on the opposite side; source UV is continuous on every edge. No mergeable triangle edge is emitted.')
   record=[zmin,zmax,xmin,xmax,minimum[0],maximum[0],minimum[1],maximum[1],coeff[0,0],coeff[1,0],coeff[2,0],coeff[0,1],coeff[1,1],coeff[2,1],alternate[0,0],alternate[1,0],alternate[2,0],alternate[0,1],alternate[1,1],alternate[2,1],*fold,*startplane,alternate2[0,0],alternate2[1,0],alternate2[2,0],alternate2[0,1],alternate2[1,1],alternate2[2,1],*fold2];floats.extend(record)
   reports.append(dict(lod=lod,deck=deck,xSide=xSide,zSide=zSide,cell=cell,repeatV=repeat,sourceFaces=ids[found].tolist(),referenceFace=int(ids[f]),physicalX=[xmin,xmax],physicalZ=[zmin,zmax],maxSourcePixelError=error.tolist(),coeff=coeff.tolist(),alternate=alternate.tolist() if foldEvidence else None,foldPlane=list(fold),alternate2=alternate2.tolist() if np.any(fold2) else None,foldPlane2=list(fold2),startPlane=startplane,foldEvidence=foldEvidence))
 return crows
for lod,deck in [(0,'Main'),(0,'Auxiliary'),(1,'Main'),(1,'Auxiliary')]:
 for zSide in [-1,1]:
  for xSide in [-1,1]:cases.append(compile_case(lod,deck,xSide,zSide))
offset=2+32
for c in cases:ints.extend([offset,len(c)//3]);offset+=len(c)
for c in cases:ints.extend(c)
raw=write_bbn(ints,floats);(out/'historical-source-road-mapping-schema5.bin').write_bytes(raw)
summary=[]
for lod,deck in [(0,'Main'),(0,'Auxiliary'),(1,'Main'),(1,'Auxiliary')]:
 rr=[r for r in reports if r['lod']==lod and r['deck']==deck];folds=sum(r['foldEvidence']['minimumAdditionalPlanes'] if r['foldEvidence'] else 0 for r in rr)
 summary.append(dict(lod=lod,deck=deck,quadrants=4,resetCells=len(set((r['xSide'],r['zSide'],r['cell']) for r in rr)),transverseDomains=len(rr),nonAffineDomains=sum(bool(r['foldEvidence']) for r in rr),minimumFoldPlanes=folds,patches=len(rr)+folds))
(out/'source-road-mapping.json').write_text(json.dumps(dict(records=reports,resourceBytes=len(raw),resourceSHA256=hashlib.sha256(raw).hexdigest(),caseCellCounts=[len(c)//3 for c in cases],complexity=summary,limitation='Measured source domains and minimum affine folds. Runtime rendering, geometry, independent loading and full material footprint remain integration checks.'),indent=2))
print(json.dumps(dict(bytes=len(raw),cases=summary),indent=2))
