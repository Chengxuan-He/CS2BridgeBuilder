"""Offline all-LOD source coverage, orientation and width/length checks for Lupu recipes."""
import argparse,json,hashlib,struct
from pathlib import Path
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components

def hanger_inventory(v,f):
 # Independently enumerate every welded source island crossing the deck datum.
 # A hanger starts at the authored shoe level and ends above the deck; arches,
 # columns and terminal ornaments have different vertical extents.
 _,inverse=np.unique(v,axis=0,return_inverse=True);g=inverse[f]
 edges=np.concatenate((g[:,[0,1]],g[:,[1,2]],g[:,[2,0]]))
 _,labels=connected_components(coo_matrix((np.ones(len(edges)),(edges[:,0],edges[:,1])),shape=(inverse.max()+1,)*2),directed=False)
 faceLabels=labels[inverse[f[:,0]]];result=[]
 for label in np.unique(faceLabels):
  faces=np.flatnonzero(faceLabels==label);points=v[np.unique(f[faces])]
  if 34<points[:,2].min()<35 and points[:,2].max()>37.2265625:result.append(faces)
 return result

def read(path):
 b=path.read_bytes();assert hashlib.sha256(b[:-32]).digest()==b[-32:];c=struct.unpack_from('<6i',b);assert c[1:4]==(0,0,0) and c[5]==0;assert len(b)==56+4*(c[0]+c[4]);return np.frombuffer(b,dtype='<i4',count=c[0],offset=24),np.frombuffer(b,dtype='<f4',count=c[4],offset=24+c[0]*4)
def section(triangles, axis, height):
 points=[]
 for triangle in triangles:
  for i,j in ((0,1),(1,2),(2,0)):
   a,b=triangle[i],triangle[j]
   if (a[axis]-height)*(b[axis]-height)<=0 and a[axis]!=b[axis]:points.append(a+(b-a)*((height-a[axis])/(b[axis]-a[axis])))
 return np.asarray(points)

def main():
 ap=argparse.ArgumentParser();ap.add_argument('resources');ap.add_argument('source');a=ap.parse_args();root=Path(a.resources);source=Path(a.source);report=json.loads((source/'measurements.json').read_text());P=read(root/'Types_Lupu_Parameters.bin')[1];results=[];contacts=[];hangerChecks=[];maxDerivative=0.
 for lod,rec in enumerate(report['lods'][:2]):
  count=0;maxerror=0.
  for sr in rec['slots']:
   slot=sr['slot'];d=np.load(source/(Path(rec['entry']).stem+'_'+str(slot)+'.npz'));v=d['positions'];f=d['faces'];ownership=np.zeros(len(f),dtype='i4')
   if slot==0:
    inventory=hanger_inventory(v,f);recorded=sr.get('hangers',[])
    assert {tuple(x) for x in inventory}=={tuple(x['faces']) for x in recorded}, 'Source hanger inventory differs from adapted roles'
    assert len(inventory)==54
    assert sorted(len(x) for x in inventory)==[36]*52+[44]*2
    assert sum(x['side']==1 for x in recorded)==sum(x['side']==-1 for x in recorded)==27
   for part in range(5):
    prefix=f'Types_Lupu_{lod}_{slot}_{part}_';get=lambda k:read(root/(prefix+k+'.bin'));orig=get('Origins')[1].reshape(-1,3);ids=get('SourceVertices')[0];corners=get('Corners')[0].reshape(-1,3);dc=get('Displacement')[1];keep=np.ones(len(f),bool);keep[get('Omitted')[0]]=False;ownership[keep]+=1;count+=int(keep.sum())
    assert np.array_equal(ids[corners[keep]],f[keep])
    if not len(ids):continue
    expected=np.column_stack((v[ids,1]-P[4],v[ids,2]-P[3],v[ids,0]-P[5]));restored=orig.copy()
    if part:restored[:,2]+=P[8+(part-1)%2]-P[5]
    hangerGrad=np.zeros((len(orig),3),dtype="f4");supportGrad=np.zeros((len(orig),3),dtype='f4');capSources=set()
    if part==0 and slot==0:
     localGrad=read(root/f'Types_Lupu_{lod}_0_SupportFrames.bin')[1].reshape(-1,6);hangerGrad=localGrad[:,:3];supportGrad=localGrad[:,3:]
     for hanger in sr.get('hangers',[]):
      selected=np.isin(ids,hanger['sourceVertices']);weight=np.clip((hanger['upperJoint']-v[ids,2])/(hanger['upperJoint']-hanger['lowerJoint']),0,1);wall=hanger['shoeWall'];origin=hanger['shoeOrigin'];tilt=-(wall[0]*(v[ids,2]-origin[0])+wall[1]*(v[ids,0]-origin[1]));expected[selected,0]+=hanger['side']*(np.float32(hanger['outwardShift'])+tilt[selected])*weight[selected];expected[selected,1]+=np.float32(hanger['rise'])*weight[selected]
     for item in sr.get('columnMoves',[]):
      sourceVertices=np.unique(f[item['faces']]);capVertices=sourceVertices[v[sourceVertices,2]>=P[15]]
      if item.get('role')=='integral terminal bearing':capVertices=sourceVertices[v[sourceVertices,2]==P[15]]
      capSources.update(capVertices.tolist())
     cap=np.isin(ids,list(capSources));expected[cap,1]=-.5
     assert np.max(abs(orig[cap,1]+.5))==0.
    if part and slot==0:expected[np.isin(ids,sr['balconySourceVertices']),1]-=.5
    for brace in sr.get('braces',[]):
     selected=np.isin(ids,brace['centralVertices'])
     expected[selected,0]=v[ids[selected],1]-np.float32(brace['center'])
    maxerror=max(maxerror,float(abs(expected-restored).max()));assert maxerror<4e-5
    expectedDisplacement=np.sign(v[ids,1]-P[4])
    for brace in sr.get('braces',[]):
     selected=np.isin(ids,np.unique(f[brace['faces']]))
     x=v[ids[selected],1]-np.float32(brace['center'])
     expectedDisplacement[selected]=np.where(np.isin(ids[selected],brace['centralVertices']),0,np.sign(x))
    assert np.array_equal(dc,expectedDisplacement)
    if part:assert np.all(v[ids,1]<P[4]) if part<3 else np.all(v[ids,1]>P[4])
    grad=np.column_stack([get(k)[1] for k in ('Frames','ShearY','ShearZ')])
    for width in (float(P[0]),12.,22.5,30.5,40.):
     half=(width-P[0])/2;output=orig.copy();output[:,0]+=dc*half
     if part==0 and slot==0:
      assert len(sr['braces'])==8
      for brace in sr['braces']:
       selected=np.isin(ids,np.unique(f[brace['faces']]))
       ends=np.isin(ids,brace['endVertices']);central=np.isin(ids,brace['centralVertices'])
       sourceX=v[ids,1]-P[4]
       assert np.max(abs(output[ends,0]-(sourceX[ends]+np.sign(sourceX[ends])*half)))<4e-6
       assert np.array_equal(output[central,0],orig[central,0])
       assert abs(float(orig[central,0].min()+orig[central,0].max()))<.0005
       assert np.array_equal(output[selected,1:],orig[selected,1:])
      for group,item in enumerate(sr.get('columnMoves',[])):
       sourceVertices=np.unique(f[item['faces']]);selected=np.flatnonzero(np.isin(ids,sourceVertices)&np.isin(ids,list(capSources)))
       error=float(np.max(abs(output[selected,1]+.5)));assert error==0.
       contacts.append(dict(lod=lod,width=width,group=group,plane=-.5,maximumCapPlaneError=error))
      for hanger in sr.get('hangers',[]):
       tri=output[corners[hanger['faces']]];points=tri.reshape(-1,3);points=points[points[:,1]<=1.];points=np.concatenate((points,section(tri,1,1.)))
       clearance=float(np.min(hanger['side']*points[:,0])-width/2);assert clearance>=-3e-6
       wallContacts=[float(np.min(hanger['side']*section(tri,1,y)[:,0])-width/2) for y in (-.2,0.,.8)];assert max(abs(x) for x in wallContacts)<.0001
       upper=np.flatnonzero(np.isin(ids,hanger['upperVertices']));originalUpper=np.column_stack((v[ids[upper],1]-P[4],v[ids[upper],2]-P[3],v[ids[upper],0]-P[5]));assert np.array_equal(orig[upper],originalUpper)
       lower=np.flatnonzero(np.isin(ids,hanger['lowerVertices']));assert abs(float(output[lower,1].min())+.5)<2e-6
       hangerChecks.append(dict(lod=lod,width=width,component=hanger['component'],lowBandClearance=clearance,maximumWallContactGap=max(abs(x) for x in wallContacts),upperAnchorDisplacement=0))
      sourceOrig=np.column_stack((v[ids,1]-P[4],v[ids,2]-P[3],v[ids,0]-P[5]))
      for fi in np.flatnonzero(keep):
       ci=corners[fi];u=sourceOrig[ci];o=output[ci];static=np.eye(3);static[0]+=hangerGrad[ci[0]];static[1]+=supportGrad[ci[0]];widthJ=np.eye(3);widthJ[0]+=half*grad[ci[0]];J=widthJ@static
       err=np.max(abs((u[1:]-u[0])@J.T-(o[1:]-o[0])));maxDerivative=max(maxDerivative,float(err));assert err<6e-5
      hangerSources=[vi for h in sr.get('hangers',[]) for vi in h['sourceVertices']];untouched=~np.isin(ids,list(capSources)+hangerSources);assert np.array_equal(output[untouched,1],sourceOrig[untouched,1])
     elif part!=0 and slot==0:
      balcony=np.flatnonzero(np.isin(ids,sr['balconySourceVertices']));sourceBottom=balcony[v[ids[balcony],2]==P[3]]
      assert len(sourceBottom)>0 and np.max(abs(output[sourceBottom,1]+.5))==0.
      assert np.array_equal(output[:,1],orig[:,1]) # No whole-tower or runtime height shift.
    # Duplicate source positions share the same signed transverse displacement.
    positions={}
    for i,point in enumerate(v[ids]):
     key=tuple(point)
     if key in positions:assert dc[i]==positions[key]
     else:positions[key]=dc[i]
   assert np.all(ownership==1)
  results.append(dict(lod=lod,faces=count,maxSourceFrameError=maxerror))
 print(json.dumps(dict(checks='All source faces and source corner attributes retained; centred chevron joints and complete end-section translation at both ends and both LODs; side-part displacement; four separate rigid towers; fixed y=-0.5 construction plane; flat column caps and lower balcony bottom coplanar; unchanged feet and tower body; complete balcony assemblies translated down 0.5; no runtime road-height sampling; analytical face Jacobians; original upper hanger anchors; lower hanger shoes at full road edges and fixed plane; all hanger solids outside road through construction height 1 m',lods=results,contactCases=len(contacts),maxJacobianError=maxDerivative,hangerChecks=hangerChecks,maxCapPlaneError=max(x['maximumCapPlaneError'] for x in contacts),contacts=contacts,limitations='User-authorized fixed construction plane y=-0.5. Previous actual-owner diagnostic establishes the superseded behavior; no candidate owner has yet been generated for this revision. Native in-game appearance and cold loading remain unverified.'),indent=2))
if __name__=='__main__':main()
