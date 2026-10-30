"""Conservative full-texel overlap audit of the proposed far interior repair mask."""
from pathlib import Path
import json,numpy as np
ROOT=Path(__file__).resolve().parents[3];R=ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research';OUT=ROOT/'artifacts/material-remap/Lift01/far-finite'
far=np.load(ROOT/'artifacts/uv-density/native/LiftBridge01_LOD2 Mesh.npz')
chosen=set(range(1814,2138))|set(range(6270,7072))|set(range(8966,9766))
levels=[]
for lod in [0,1]:
 m=np.load(R/f'lift01-main-{lod}.npz');p=m['positions'];f=m['indices'];t=p[f];groups={};member={}
 for kind in ['side','crossing']:
  for i,g in enumerate(json.loads((R/f'lift01-{kind}-{lod}.json').read_text())):
   role='rail' if kind=='side' and i in chosen else 'sleeper' if kind=='crossing' and g['minimum'][0]==-1.350006103515625 and g['maximum'][0]==1.350006103515625 and g['minimum'][1]==9.800048828125 and g['maximum'][1]==9.86962890625 else 'other'
   label=kind+':'+str(i);groups[label]=dict(role=role,minimum=g['minimum'],maximum=g['maximum'])
   for v in g['vertices']:member[v]=label
 area=np.cross(t[:,1][:,[0,2]]-t[:,0][:,[0,2]],t[:,2][:,[0,2]]-t[:,0][:,[0,2]])
 use=np.flatnonzero((t[:,:,1].max(1)>9.800048828125)&(t[:,:,1].min(1)<10.4)&(abs(area)>1e-8))
 tri=t[use][:,:,[0,2]];labels=[sorted(set(member.get(int(v),'unknown') for v in f[i])) for i in use]
 roles=np.array([all(groups.get(l,{}).get('role') in ['rail','sleeper'] for l in ls) for ls in labels]);levels.append((lod,tri,tri.min(1),tri.max(1),roles,labels,groups,use))
def clip(poly,a,b,sign):
 out=[]
 for p,q in zip(poly,poly[1:]+poly[:1]):
  dp=np.cross(b-a,p-a)*sign;dq=np.cross(b-a,q-a)*sign
  if dp>=0:out.append(p)
  if (dp>=0)!=(dq>=0):out.append(p+(q-p)*dp/(dp-dq))
 return out
cache={}
def audit(x,y,ids):
 key=(x,y,tuple(ids))
 if key in cache:return cache[key]
 polygons=[]
 for fi in ids:
  v=far['mesh0_faces'][fi];uv=far['mesh0_uv0'][v,:2].astype(float);pos=far['mesh0_positions'][v][:,[0,2]];poly=[np.array(z)/512 for z in [[x,y],[x+1,y],[x+1,y+1],[x,y+1]]];sign=np.sign(np.cross(uv[1]-uv[0],uv[2]-uv[0]))
  for a,b in zip(uv,np.roll(uv,-1,axis=0)):
   poly=clip(poly,a,b,sign)
   if not poly:break
  if len(poly)>=3:
   affine=np.linalg.solve(np.c_[uv,np.ones(3)],pos);polygons.append(np.c_[poly,np.ones(len(poly))]@affine)
 hitrows=[]
 for lod,t,lo,hi,roles,labels,groups,faces in levels:
  hit=set()
  for poly in polygons:
   candidates=np.flatnonzero(np.all(hi>=poly.min(0),1)&np.all(lo<=poly.max(0),1));tr=t[candidates]
   keep=np.ones(len(candidates),bool)
   sign=np.sign(np.cross(poly[1]-poly[0],poly[2]-poly[0]))
   for a,b in zip(poly,np.roll(poly,-1,axis=0)):
    cross=np.cross(b-a,tr-a)*sign;keep&=np.any(cross>=0,1)
   for k in range(3):
    a=tr[:,k];b=tr[:,(k+1)%3];sg=np.sign(np.cross(tr[:,1]-tr[:,0],tr[:,2]-tr[:,0]));cross=np.cross((b-a)[:,None,:],poly[None,:,:]-a[:,None,:])*sg[:,None];keep&=np.any(cross>=0,1)
   hit.update(candidates[keep].tolist())
  hitrows.append(dict(lod=lod,rail=any(roles[i] for i in hit),otherGroups=sorted({l for i in hit if not roles[i] for l in labels[i]}),otherFaces=[int(faces[i]) for i in hit if not roles[i]]))
 cache[key]=hitrows;return hitrows
rows=[];safe=np.zeros((512,512),bool)
for name,box,ds,h,ids in [('moving-negative',[59,236,66,439],[57,67],False,[730,731]),('moving-positive',[364,10,371,214],[361,371],False,[342,343]),('fixed-negative',[190,386,197,418],[187,197],False,[708,709]),('fixed-positive',[354,306,386,313],[303,313],True,[320,321])]:
 x0,y0,x1,y1=box
 for y in range(y0,y1):
  for x in range(x0,x1):
   dx=x if h else ds[int(x>=(x0+x1)/2)];dy=ds[int(y>=(y0+y1)/2)] if h else y
   a=audit(x,y,ids);d=audit(dx,dy,ids)
   ok=all(r['rail'] and not r['otherGroups'] for r in a) and all(not r['otherGroups'] and not r['rail'] for r in d)
   safe[y,x]=ok;rows.append(dict(chart=name,x=x,y=y,donor=[dx,dy],safe=ok,receiver=a,source=d))
report=dict(scope='Conservative entire original far pixel footprint intersection with near/middle authored geometry above road datum. Any non-track overlap rejects a replacement; no center-only classification.',total=len(rows),safe=int(safe.sum()),rejected=len(rows)-int(safe.sum()),rows=rows,groups={str(lod):g for lod,t,lo,hi,roles,l,g,f in levels})
(OUT/'interior-role-safety.json').write_text(json.dumps(report,indent=2));np.save(OUT/'interior-safe-mask.npy',safe);print({k:report[k] for k in ['total','safe','rejected']})
