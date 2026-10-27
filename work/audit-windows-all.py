import sys,json,hashlib
from pathlib import Path
import numpy as np
R=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(R/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry
from MovableUvDomainFormat import decode_arrays,read_bbn
out=R/'artifacts/prepared-materials/shared-nontiling/windows';out.mkdir(exist_ok=True)
reader=NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
records=[]
for entry in json.loads((R/'artifacts/uv-density/native/inventory.json').read_text())['sources']:
 p=Path(entry['source']);render=p.stem
 style=next((s for s,prefix in [('Draw01','DrawBridge01'),('Draw02','DrawBridge02'),('Draw03','DrawBridge03'),('Lift01','LiftBridge01'),('Lift02','LiftBridge02'),('Lift03','LiftBridge03'),('Lift04','LiftBridge04'),('Lift05','LiftBridge05'),('PedestrianDraw01','PedestrianDrawBridge01'),('PedestrianDraw02','PedestrianDrawBridge02')] if render.startswith(prefix)),None)
 if not style:continue
 for slot,m in enumerate(reader.read(p,include_colors=True)):
  u=m.get('uv0');c=m.get('color_bytes',m.get('color'));f=m['faces']
  if u is None or u.shape[1]!=4 or c is None:continue
  good=np.isfinite(u[f]).all(axis=(1,2))&np.isfinite(c[f]).all(axis=(1,2))&((u[f,1:3]>=0)&(u[f,1:3]<=1)).all(axis=(1,2))&(u[f,3]==u[f,3][:,:1]).all(axis=1)&(c[f]==c[f][:,:1,:]).all(axis=(1,2))
  b=R/f'assets/HardcodedNumbers/{style}/UvDomain_{render.replace(" ","_")}_{slot}.bin'
  domains=[]
  if b.exists():
   d,_=decode_arrays(*read_bbn(b.read_bytes()));at=2
   for i in range(d[1]):
    n=abs(int(d[at+3]));at+=4;fs=d[at:at+n];at+=n
    if good[fs].all():domains.append(i)
  rec=dict(style=style,render=render,mesh=slot,geometry=str(p),geometrySha256=hashlib.sha256(p.read_bytes()).hexdigest(),faces=len(f),sourceLocalFaces=np.flatnonzero(good).tolist(),sourceLocalDomains=domains,uv0=u.tolist(),colors=c.tolist(),indices=f.tolist(),scope='Candidate local YZ evidence only. Requires independent template2/shader550 confirmation; no flags changed.')
  name=f'{style}-{render.replace(" ","_")}-{slot}.json';(out/name).write_text(json.dumps(rec))
  records.append({k:v for k,v in rec.items() if k not in ('uv0','colors','indices','sourceLocalFaces','sourceLocalDomains')}|dict(sourceLocalFaces=len(rec['sourceLocalFaces']),sourceLocalDomains=len(domains),report=name))
(out/'summary.json').write_text(json.dumps(records,indent=2))
print(json.dumps(records,indent=2))
