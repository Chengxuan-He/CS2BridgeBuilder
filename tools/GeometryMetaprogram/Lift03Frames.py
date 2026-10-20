"""Measure Lift03 U-frame openings and emit per-height side/crossing separation.
Usage: python Lift03Frames.py RESEARCH_DIRECTORY OUTPUT_CS [frames|end-beams|fixed-supports|joined-shells|tall-supports|sloped-supports]
The native opening profile, not the connected component bounds, defines the arms.
"""
from pathlib import Path
from StaticArrayChunks import split_static_arrays
import ctypes as c, hashlib, json, struct, sys
import numpy as np
r,output=map(Path,sys.argv[1:3])
family=sys.argv[3] if len(sys.argv)>3 else 'frames'
assert family in ('frames','end-beams','fixed-supports','joined-shells','tall-supports','sloped-supports')
class_name={'frames':'Lift03Frames','end-beams':'Lift03EndBeams','fixed-supports':'Lift03FixedSupportShells','joined-shells':'Lift03JoinedShells','tall-supports':'Lift03TallSupports','sloped-supports':'Lift03SlopedSupports'}[family]
decoder=Path(__file__).with_name('Lift05Counterweights.py').read_text()
exec(decoder[decoder.index('dll = c.CDLL'):decoder.index('tables=[]')])
hashes=('f8bc2882fcba043d1d57a17fd27a43631b492ab4292f8fc6b9c3beeb00af53cd','455d25f8ec83f91ea097551fb09a9f41abc79e9f15e836b19c0a527ad50ff9fa')
profiles={};levels=[];near={}
def opening_profile(points):
 result=[]
 for y in np.unique(points[:,:,1]):
  candidates=[]
  for sample in (y-1e-5,y+1e-5):
   spans=[]
   for tri in points[(points[:,:,1].min(1)<=sample)&(points[:,:,1].max(1)>=sample)]:
    hits=[]
    for a,b in zip(tri,np.roll(tri,-1,axis=0)):
     if (a[1]<=sample<=b[1] or b[1]<=sample<=a[1]) and a[1]!=b[1]:
      hits.append(a[0]+(b[0]-a[0])*(sample-a[1])/(b[1]-a[1]))
    if hits:spans.append((min(hits),max(hits)))
   merged=[]
   for lo,hi in sorted(spans):
    if merged and lo<=merged[-1][1]+1e-7:merged[-1][1]=max(hi,merged[-1][1])
    else:merged.append([lo,hi])
   if any(lo<=0<=hi for lo,hi in merged):continue
   pos=[lo for lo,hi in merged if lo>0];neg=[hi for lo,hi in merged if hi<0]
   if pos and neg:
    candidates.append((max(neg),min(pos)))
  if candidates:
   left_candidate,right_candidate=min(candidates,key=lambda pair:pair[1]-pair[0])
   # Recover the exact authored corner; the offset probes only identify its side.
   intersections=[]
   for tri in points[(points[:,:,1].min(1)<=y)&(points[:,:,1].max(1)>=y)]:
    for a,b in zip(tri,np.roll(tri,-1,axis=0)):
     if a[1]==y:intersections.append(a[0])
     if min(a[1],b[1])<y<max(a[1],b[1]):
      intersections.append(a[0]+(b[0]-a[0])*(y-a[1])/(b[1]-a[1]))
   xs=np.array(intersections);left=xs[np.argmin(abs(xs-left_candidate))];right=xs[np.argmin(abs(xs-right_candidate))]
   assert abs(left-left_candidate)<.001 and abs(right-right_candidate)<.001
   result.append((float(y),float(left),float(right)))
 assert len(result)>20
 return np.array(result)
def clip(poly, distances, keep_negative):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da,db=distances(a),distances(b)
  ina=da<=1e-10 if keep_negative else da>=-1e-10
  inb=db<=1e-10 if keep_negative else db>=-1e-10
  if ina:result.append(a)
  if ina!=inb:result.append(a+(b-a)*(da/(da-db)))
 clean=[]
 for p in result:
  if not clean or max(abs(p-clean[-1]))>1e-9:clean.append(p)
 if len(clean)>1 and max(abs(clean[0]-clean[-1]))<1e-9:clean.pop()
 return clean
for lod in range(2):
 name='LiftBridge03'+('_LOD1' if lod else '')+' Mesh'
 raw=(r/'movable-skin'/(name+'.Geometry')).read_bytes();assert hashlib.sha256(raw).hexdigest()==hashes[lod]
 version,meshes,flags=struct.unpack_from('<HII',raw);assert version==1 and meshes==2
 headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(meshes)];h=headers[0]
 section=decoder[decoder.index('    def decode('):decoder.index('    cached=np.load')]
 exec('\n'.join(line[4:] if line.startswith('    ') else line for line in section.splitlines()))
 v=v.astype(float);cached=np.load(r/f'lift03-main-{lod}-0.npz');components=cached['component']
 assert np.array_equal(f,cached['indices']) and np.allclose(v,cached['positions'],rtol=1e-7,atol=1e-6)
 bones=[];bone=''
 for line in (r/'movable-skin'/(name+'.mesh-0.obj')).read_text().splitlines():
  if line.startswith('g '):bone=line[2:]
  elif line.startswith('f '):bones.append(bone)
 assert len(bones)==len(f)
 records=json.loads((r/'lift03-main-parts.json').read_text());row=next(x for x in records if x['lod']==lod and x['mesh']==0)
 halves={'frames':(10.218994,),'end-beams':(17.024414,),'fixed-supports':(12.55542,10.900879,12.67749),'joined-shells':(10.599609,),'tall-supports':(12.55542,),'sloped-supports':(10.900879,12.67749)}[family]
 parts=[p for p in row['parts'] if p['maximum'][0] in halves and p['minimum'][0]==-p['maximum'][0]]
 assert len(parts)=={'frames':17,'end-beams':2,'fixed-supports':6,'joined-shells':3,'tall-supports':2,'sloped-supports':4}[family]
 weights={};added=[];added_weights=[];fragments=[];selected=[];extra=[];edge_recipes={}
 for part in parts:
  ids=np.flatnonzero(components==part['id']);fi=np.flatnonzero((components[f]==part['id']).all(1));selected.extend(fi)
  kind=halves.index(part['maximum'][0]) if family in ('fixed-supports','sloped-supports') else (0 if family=='end-beams' or part['maximum'][1]==19.787598 else 1)
  if lod==0 and kind not in profiles:profiles[kind]=opening_profile(v[f[fi]])
  profile=profiles[kind] if lod==0 else opening_profile(v[f[fi]])
  if family=='sloped-supports':
   # The two sloped families have different measured inner longitudinal walls.
   # Preserve the complete outer wall bands; widen their transverse connection.
   span=(9.994384765625,11.77099609375)[kind]
   wall=v[f[fi]]
   for sign in (-1,1):
    inner=wall[np.all(abs(wall[:,:,0]-sign*span)<1e-8,axis=1)]
    assert len(inner)>0 and inner[:,:,1].min()==3.371826171875 and inner[:,:,1].max()==10.58642578125
   profile=profile.copy()
   profile[:,1]=np.minimum(profile[:,1],-span)
   profile[:,2]=np.maximum(profile[:,2],span)
  if family=='tall-supports':
   # The vertical inner walls delimit the transverse bowl joining the columns.
   # Verify both mirrored walls in every detailed level, not just a projected gap.
   wall=v[f[fi]]
   for sign in (-1,1):
    inner=wall[np.all(abs(wall[:,:,0]-sign*5.3037109375)<1e-8,axis=1)]
    assert len(inner)>0 and inner[:,:,1].min()==7.781982421875 and inner[:,:,1].max()==13.1591796875
   profile=profile.copy()
   profile[:,1]=np.minimum(profile[:,1],-5.3037109375)
   profile[:,2]=np.maximum(profile[:,2],5.3037109375)
  if family=='joined-shells':
   # The near prototype's central longitudinal beam supplies the bottom connection span.
   # Coarse welded corners inherit this measured part boundary, not a new inferred span.
   if lod==0:
    central=v[f[fi]];central=central[(np.ptp(central[:,:,2],axis=1)>4)&(abs(central[:,:,0]).max(1)<3)]
    assert central[:,:,0].min()==-2.08642578125 and central[:,:,0].max()==2.08642578125
   profile=profile.copy();profile[:,1]=np.minimum(profile[:,1],-2.08642578125);profile[:,2]=np.maximum(profile[:,2],2.08642578125)
  if family in ('joined-shells','tall-supports','sloped-supports'):
   # Collinear samples do not change the measured opening. Retaining them would
   # cut every crossing face at redundant heights, especially below the beam.
   original_profile=profile.copy()
   retained=[]
   for row in profile:
    retained.append(row)
    while len(retained)>2:
     first,middle,last=retained[-3:]
     expected=first[1:]+(last[1:]-first[1:])*((middle[0]-first[0])/(last[0]-first[0]))
     if np.max(np.abs(expected-middle[1:]))>1e-10:break
     retained.pop(-2)
   profile=np.array(retained)
   for axis in (1,2):
    assert np.max(abs(np.interp(original_profile[:,0],profile[:,0],profile[:,axis])-original_profile[:,axis]))<1e-9
  ys,lefts,rights=profile.T
  assert np.all(lefts<0) and np.all(rights>0), (family,lod,part['id'],'Centre-closing opening requires its own base connection recipe')
  boundary=lambda y,sign:np.interp(y,ys,lefts if sign<0 else rights)
  weight=lambda p:float(np.clip(p[0]/abs(boundary(p[1],-1 if p[0]<0 else 1)),-1,1))
  key=(kind,round((part['minimum'][2]+part['maximum'][2])/2,2))
  assert set(np.array(bones)[fi])==({'bone_0'} if family in ('fixed-supports','sloped-supports') or abs(key[1])>80 else {'bone_3'})
  if lod==0:near[key]=(part['minimum'],part['maximum'])
  else:
   original=near[key]
   assert original[0][:2]==part['minimum'][:2] and original[1][:2]==part['maximum'][:2]
   assert abs(original[0][2]-part['minimum'][2])<=.002 and abs(original[1][2]-part['maximum'][2])<=.002
  for i in ids:weights[int(i)]=weight(v[i])
  if family=='sloped-supports':
   for sign in (-1,1):
    wall_ids=ids[v[ids,0]*sign>=span]
    assert all(weights[int(i)]==sign for i in wall_ids)
  for ordinal in fi:
   face=f[ordinal];points=v[face];polys=[list(np.eye(3))]
   face_weights=[weights[int(i)] for i in face]
   # A coarse side-arm chord must remain the same authored side part.
   # Do not insert the detailed curved opening into its interior.
   if all(x==1 for x in face_weights) or all(x==-1 for x in face_weights):continue
   # Split only at measured profile corners and the two arm boundaries.
   for y in ys[(ys>points[:,1].min()+1e-9)&(ys<points[:,1].max()-1e-9)]:
    next_polys=[]
    for poly in polys:
     dist=lambda b:float(b@points[:,1]-y)
     ds=[dist(b) for b in poly]
     if min(ds)<-1e-9 and max(ds)>1e-9:next_polys += [clip(poly,dist,True),clip(poly,dist,False)]
     else:next_polys.append(poly)
    polys=next_polys
   for sign in (-1,1):
    next_polys=[]
    for poly in polys:
     dist=lambda b:float((b@points)[0]-boundary((b@points)[1],sign))
     ds=[dist(b) for b in poly]
     if min(ds)<-1e-9 and max(ds)>1e-9:next_polys += [clip(poly,dist,True),clip(poly,dist,False)]
     else:next_polys.append(poly)
    polys=next_polys
   if len(polys)==1:continue
   local={};indices=[]
   for poly in polys:
    polygon=[]
    for bary in poly:
     nz=np.flatnonzero(abs(bary)>1e-8)
     if len(nz)==1:idx=int(face[nz[0]])
     else:
      key=tuple(np.round(bary,10))
      if key not in local:
       wb,wc=map(float,np.float32(bary[1:]));pos=points[0]*(1-wb-wc)+points[1]*wb+points[2]*wc
       if len(nz)==2:
        ia,ib=map(int,nz);fraction=float(np.float32(bary[ib]/(bary[ia]+bary[ib])))
        assert 0<fraction<1
        edge_recipes[len(added)]=(int(face[ia]),int(face[ib]),fraction)
        pos=points[ia]+(points[ib]-points[ia])*fraction
       local[key]=len(v)+len(added);added.append((*map(int,face),wb,wc));extra.append(pos);added_weights.append(weight(pos))
      idx=local[key]
     polygon.append(idx)
    for k in range(1,len(polygon)-1):indices.extend((polygon[0],polygon[k],polygon[k+1]))
   fragments.append((int(ordinal),indices))
 # Native Plan stores edge vertices before strictly interior barycentric vertices.
 order=list(edge_recipes)+[i for i in range(len(added)) if i not in edge_recipes]
 remap={len(v)+old:len(v)+new for new,old in enumerate(order)}
 fragments=[(i,[remap.get(index,index) for index in inds]) for i,inds in fragments]
 edges=[edge_recipes[i] for i in edge_recipes]
 interiors=[added[i] for i in order[len(edges):]]
 for _,_,_,wb,wc in interiors:assert wb>0 and wc>0 and np.float32(wb)+np.float32(wc)<1
 extra=[extra[i] for i in order];added_weights=[added_weights[i] for i in order]
 all_v=np.concatenate((v,np.array(extra).reshape(-1,3)));w=np.zeros(len(all_v))
 for i,value in weights.items():w[i]=value
 w[len(v):]=added_weights
 assert np.all(np.isfinite(all_v)) and np.all(np.isfinite(w)), 'Non-finite structural construction is not valid output'
 replaced={i for i,_ in fragments};sf=np.array([list(f[i]) for i in selected if i not in replaced]+[inds[k:k+3] for _,inds in fragments for k in range(0,len(inds),3)])
 for ordinal,inds in fragments:
  p=v[f[ordinal]];q=all_v[np.array(inds).reshape(-1,3)]
  area=np.linalg.norm(np.cross(q[:,1]-q[:,0],q[:,2]-q[:,0]),axis=1).sum();original=np.linalg.norm(np.cross(p[1]-p[0],p[2]-p[0]))
  assert abs(area-original)<.0002,(lod,ordinal,area,original)
 for delta in (-.25,0,2,8):
  q=all_v.copy();q[:,0]+=w*delta
  a,b=all_v[sf],q[sf];na=np.cross(a[:,1]-a[:,0],a[:,2]-a[:,0]);nb=np.cross(b[:,1]-b[:,0],b[:,2]-b[:,0]);valid=np.linalg.norm(na,axis=1)>1e-7
  failed=valid & ((na*nb).sum(1)<=0)
  if failed.any():
   first=int(np.flatnonzero(failed)[0]);print('WINDING FAILURE',lod,delta,int(failed.sum()),a[first],w[sf[first]],na[first],nb[first])
   for ordinal,inds in fragments:
    if set(sf[first])<=set(inds):
     print('SOURCE',ordinal,v[f[ordinal]],w[f[ordinal]])
  assert not failed.any()
  assert np.array_equal(q[:,1:],all_v[:,1:])
  for sign in (-1,1):
   rigid=np.all(w[sf]==sign,axis=1)
   assert np.allclose(b[rigid]-a[rigid],(sign*delta,0,0),rtol=0,atol=1e-10)
 if family=='joined-shells':
  faces=f[np.array(selected)];points=v[faces]
  side=(np.ptp(points[:,:,2],axis=1)>4)&(abs(points[:,:,0]).min(1)>7)
  assert np.all(np.abs(w[faces[side]])==1) and np.all(np.ptp(w[faces[side]],axis=1)==0)
 if family=='tall-supports':
  faces=f[np.array(selected)];points=v[faces]
  side=(np.ptp(points[:,:,2],axis=1)>4)&(abs(points[:,:,0]).min(1)>=5.3037109375)
  assert np.all(np.abs(w[faces[side]])==1) and np.all(np.ptp(w[faces[side]],axis=1)==0)
 levels.append((len(v),len(f)*3,weights,edges,interiors,added_weights,fragments))
 selected_mask=np.zeros(len(v),bool);selected_mask[list(weights)]=True
 for other in ('deck-bases','crossing-members'):
  assert not np.any(selected_mask & np.load(r/f'lift03-{other}-{lod}.npz')['selected'])
 assert not np.any(selected_mask & np.load(r/f'lift03-rigid-{lod}-0.npz')['selected'])
 if family!='frames':assert not np.any(selected_mask & np.load(r/f'lift03-frames-{lod}.npz')['selected'])
 if family in ('fixed-supports','joined-shells','tall-supports','sloped-supports'):assert not np.any(selected_mask & np.load(r/f'lift03-end-beams-{lod}.npz')['selected'])
 if family in ('tall-supports','sloped-supports'):assert not np.any(selected_mask & np.load(r/f'lift03-joined-shells-{lod}.npz')['selected'])
 if family=='sloped-supports':assert not np.any(selected_mask & np.load(r/f'lift03-tall-supports-{lod}.npz')['selected'])
 np.savez(r/f'lift03-{family}-{lod}.npz',positions=all_v,indices=sf,weights=w,selected=selected_mask)
 print(f'PASS LOD{lod}: {len(weights)} original vertices, {len(added)} constructed vertices, {len(fragments)} split faces; source area, rigid arms, four widths')
(r/f'lift03-{family}-opening-profiles.json').write_text(json.dumps({str(k):v.tolist() for k,v in profiles.items()},indent=2)+'\n')
def number(x):return format(float(np.float32(x)),'.9g')+'f'
lines=['// Measured Lift03 U-frame arm boundaries, generated by Lift03Frames.py.','using System;','using UnityEngine;','','namespace BridgeBuilder.Bridges;','','internal static class Lift03Frames','{','    internal static MovableStructureModel.Plan Complete(int lod, Vector3[] source, Vector3[] positions, int[] indices, float delta)','    {','        var data = lod switch','        {']
for lod,(n,ic,*_) in enumerate(levels):lines.append(f'            {lod} => (Count: {n}, IndexCount: {ic}, Weights: Weights{lod}, Edges: Edges{lod}, Interior: Interior{lod}, ExtraWeights: ExtraWeights{lod}, Fragments: Fragments{lod}),')
lines+=['            _ => throw new ArgumentOutOfRangeException(nameof(lod))','        };','        if (source.Length != data.Count || positions.Length != data.Count || indices.Length != data.IndexCount || !float.IsFinite(delta))','            throw new InvalidOperationException("Lift03 frames require their recorded native layout.");','        foreach (var item in data.Weights)','            positions[item.Index].x = source[item.Index].x + item.Weight * delta;','        var x = new float[data.ExtraWeights.Length];','        for (var i = 0; i < data.Edges.Length; i++)','        {','            var edge = data.Edges[i];','            x[i] = source[edge.A].x + (source[edge.B].x - source[edge.A].x) * edge.Fraction + data.ExtraWeights[i] * delta;','        }','        for (var i = 0; i < data.Interior.Length; i++)','        {','            var a = data.Interior[i];','            var index = data.Edges.Length + i;','            x[index] = source[a.A].x + (source[a.B].x - source[a.A].x) * a.WeightB + (source[a.C].x - source[a.A].x) * a.WeightC + data.ExtraWeights[index] * delta;','        }','        return new MovableStructureModel.Plan(source, positions, new[] { indices }, new[] { new bool[indices.Length / 3] },','            addedVertices: data.Edges, fragments: data.Fragments, interiorVertices: data.Interior, constructedX: x);','    }']
for lod,(_,_,weights,edges,interior,extra,frags) in enumerate(levels):
 lines += ['    // Geometry SHA-256: '+hashes[lod],f'    private static readonly (int Index, float Weight)[] Weights{lod} =','    {']+[f'        ({i}, {number(w)}),' for i,w in sorted(weights.items())]+['    };']
 lines += [f'    private static readonly MovableStructureModel.EdgeVertex[] Edges{lod} =','    {']+[f'        new({a}, {b}, {number(t)}),' for a,b,t in edges]+['    };']
 lines += [f'    private static readonly MovableStructureModel.TriangleVertex[] Interior{lod} =','    {']+[f'        new({a}, {b}, {cc}, {number(wb)}, {number(wc)}),' for a,b,cc,wb,wc in interior]+['    };']
 lines += [f'    private static readonly float[] ExtraWeights{lod} =','    {','        '+', '.join(map(number,extra)),'    };']
 lines += [f'    private static readonly MovableStructureModel.Fragment[] Fragments{lod} =','    {']+['        new(0, '+str(i)+', '+', '.join(map(str,inds))+'),' for i,inds in frags]+['    };']
lines+=['}',''];text='\n'.join(lines).replace('internal static class Lift03Frames','internal static class '+class_name)
if family=='end-beams':text=text.replace('U-frame arm boundaries','arched end-beam arm boundaries').replace('Lift03 frames require','Lift03 end beams require')
if family=='fixed-supports':text=text.replace('U-frame arm boundaries','fixed support shell boundaries').replace('Lift03 frames require','Lift03 fixed support shells require')
if family=='joined-shells':text=text.replace('U-frame arm boundaries','joined-shell arm and central-beam boundaries').replace('Lift03 frames require','Lift03 joined shells require')
if family=='tall-supports':text=text.replace('U-frame arm boundaries','tall-support wall and transverse-bowl boundaries').replace('Lift03 frames require','Lift03 tall supports require')
if family=='sloped-supports':text=text.replace('U-frame arm boundaries','sloped-support wall and transverse-connection boundaries').replace('Lift03 frames require','Lift03 sloped supports require')
output.write_text(split_static_arrays(text),encoding='utf-8')
