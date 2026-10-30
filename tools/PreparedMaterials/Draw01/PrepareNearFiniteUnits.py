"""Author full native near finite-U / four-metre-V candidates, with original mip inputs."""
import copy,hashlib,json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
OUT.mkdir(parents=True,exist_ok=True)
p=ROOT/'artifacts/uv-density/native/DrawBridge01 Mesh.npz'
assert hashlib.sha256(p.read_bytes()).hexdigest()=='e70c548d999dd456ba72bc832e7c241833feb9f8595749e54bbbf2be34274447'
d=np.load(p);f=d['mesh0_faces'];pos=d['mesh0_positions'];uv=d['mesh0_uv0']
seed=json.loads((ROOT/'artifacts/agent-material-env/Draw01/repeat-units-control-corrected/extracted-source/manifest.json').read_text())['units'][0]
units=[];evidence=[]
for name,faces in [('asphalt-full-half',[1512,1513]),('moving-sidewalk',[1819,1820]),('fixed-sidewalk',[4865,4866]),('fixed-carriageway',[2674,2675])]:
 ids=np.unique(f[faces]);p0=pos[ids].astype(float);t=uv[ids].astype(float)
 width=float(np.ptp(p0[:,0]));period=float(np.ptp(p0[:,2]));assert period==4
 record=dict(id=name,faces=faces,positions=p0.tolist(),uv=t.tolist(),repeatAxes='V',finiteAxes='U',sourceMeshSha256=hashlib.sha256(p.read_bytes()).hexdigest(),sourceMipPolicy='Each output mip samples corresponding authored source mip; finite U receives 25%-75% guard mapping.',paintPolicy='Original full source appearance retained, including any painted edge. Not yet approved alongside selected road marking layer.')
 evidence.append(record)
 unit=copy.deepcopy(seed);unit.update(id=name,min=t.min(axis=0).tolist(),max=t.max(axis=0).tolist(),physicalSize=[width,period],repeatAxes='V',finiteSourceGrid=True,aliases=[])
 for c in unit['channels']:
  assert hashlib.sha256(Path(c['path']).read_bytes()).hexdigest()==c['sha256']
  c['encoding']='BC7'
 units.append(unit)
e=OUT/'source-evidence.json';e.write_text(json.dumps(evidence,indent=2)+'\n')
for u in units:u['evidence']=dict(path=str(e),sha256=hashlib.sha256(e.read_bytes()).hexdigest())
(OUT/'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3',style='Draw01',units=units),indent=2)+'\n')
print([(u['id'],u['min'],u['max'],u['physicalSize']) for u in units])
