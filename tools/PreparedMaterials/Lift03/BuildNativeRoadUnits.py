"""Compile complete original Lift03 moving-road units; finite transverse domain."""
import copy,hashlib,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]; OUT=ROOT/'artifacts/material-remap/Lift03/native-units';OUT.mkdir(parents=True,exist_ok=True)
audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift03/outputs/native-repeat-mechanism/audit.json').read_text())
base=json.loads((ROOT/'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2/input/manifest.json').read_text())['units'][0]
units=[];evidence=[]
for r in audit['reports'][:4]:
 lod=r['lod'];deck=r['deck'];period=8 if deck=='Main' else 4
 path=ROOT/'artifacts/uv-density/native'/('LiftBridge03'+('_LOD1' if lod else '')+' Mesh.npz');assert hashlib.sha256(path.read_bytes()).hexdigest()==r['sourceNPZSHA256']
 a=np.load(path);ids=np.array(r['faceIds']);p=a['mesh0_positions'][a['mesh0_faces'][ids]].astype(float);q=a['mesh0_uv0'][a['mesh0_faces'][ids]].astype(float)
 sel=np.all(p[:,:,2]>=40,axis=1)&np.all(p[:,:,2]<=40+period,axis=1)&np.all(p[:,:,0]<=0,axis=1);p,q,ids=p[sel],q[sel],ids[sel]
 half=-p[:,:,0].min();bounds=np.unique(p[:,:,0]);bands=[]
 ordered=bounds if deck=='Main' else bounds[::-1]
 for left,right in zip(ordered[:-1],ordered[1:]):
  ua=np.unique(q[:,:,0][p[:,:,0]==left]);ub=np.unique(q[:,:,0][p[:,:,0]==right]);assert len(ua)==len(ub)==1
  bands.append(dict(start=float((left+half)/half if deck=='Main' else -left/half),end=float((right+half)/half if deck=='Main' else -right/half),repeatY=1,min=[float(ua[0]),float(q[:,:,1].min())],max=[float(ub[0]),float(q[:,:,1].max())]))
 if deck!='Main':bands=sorted(bands,key=lambda b:b['start'])
 assert bands[0]['start']==0 and bands[-1]['end']==1
 for b in bands:assert b['min'][0]<b['max'][0]
 row=dict(id=f'Lift03.{deck}.LOD{lod}.Moving',lod=lod,deck=deck,sourceNPZSHA256=r['sourceNPZSHA256'],faceIds=ids.tolist(),positions=p.tolist(),uv=q.tolist(),physicalSize=[half,period],bands=bands,repeatAxes='V',finiteAxis='U',geometryCutsAdded=0,scope='One complete originally authored moving half-deck period, not an arbitrary clean crop or full span.')
 # Prove the bands reproduce each original vertex, including the real transverse UV fold.
 errs=[]
 for pp,qq in zip(p.reshape(-1,3),q.reshape(-1,2)):
  u=(pp[0]+half)/half if deck=='Main' else -pp[0]/half
  b=next(b for b in bands if b['start']<=u<=b['end']); mapped=b['min'][0]+(u-b['start'])/(b['end']-b['start'])*(b['max'][0]-b['min'][0]);errs.append(abs(mapped-qq[0]))
 row['maxSourceUError']=max(errs);assert max(errs)<1e-15
 evidence.append(row)
 unit=copy.deepcopy(base);unit.update(id=row['id'],min=q.min((0,1)).tolist(),max=q.max((0,1)).tolist(),physicalSize=[half,period],repeatAxes='V',bands=bands,aliases=[])
 for c in unit['channels']:c['encoding']='BC7';assert hashlib.sha256(Path(c['path']).read_bytes()).hexdigest()==c['sha256']
 units.append(unit)
epath=OUT/'evidence.json';epath.write_text(json.dumps(evidence,indent=2)+'\n');digest=hashlib.sha256(epath.read_bytes()).hexdigest()
for u in units:u['evidence']=dict(path=str(epath),sha256=digest)
(OUT/'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift03',units=units),indent=2)+'\n')
print([(x['id'],x['physicalSize'],x['maxSourceUError']) for x in evidence])
