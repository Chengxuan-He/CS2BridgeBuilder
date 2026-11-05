"""Stage the measured Draw02 lane units, retaining private authoring evidence."""
from pathlib import Path
import hashlib,json,shutil,struct
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3];src=ROOT/'artifacts/material-remap/Draw02/lane-units-v2'
r=json.loads((src/'composition.json').read_text())
a=np.asarray(Image.open(src/'LanePattern-alpha.png'));weight=1-a.mean(0)/255
q=float(np.sum((np.arange(a.shape[1])+.5)/a.shape[1]*weight)/weight.sum())
values=[3.,4.,q,r['units'][1]['physicalSize'][0]]
blob=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(values),0)+struct.pack('<4f',*values);blob+=hashlib.sha256(blob).digest()
(ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02LaneAppearance_cs.bin').write_bytes(blob)
for p in (src/'Materials/Draw02').rglob('*'):
 if not p.is_file():continue
 dst=ROOT/'assets/Materials/Draw02'/p.relative_to(src/'Materials/Draw02');dst.parent.mkdir(parents=True,exist_ok=True)
 if dst.exists():assert dst.read_bytes()==p.read_bytes()
 else:shutil.copyfile(p,dst)
(src/'placement.json').write_text(json.dumps(dict(alphaVisualCenterNormalizedU=q,alphaVisualCenterOriginalX=-3*q,referenceCenter=-1.5,physicalWidth=3.,period=4.,bbnSha256=hashlib.sha256(blob).hexdigest()),indent=2)+'\n')
print('Staged two source-derived units and measured visual-centre BBN',q)
