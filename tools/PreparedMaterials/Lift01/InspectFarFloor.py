"""Inspect exact original finite LOD2 floor content before approving material routing."""
import hashlib
import json
import math
import struct
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
out=ROOT/'artifacts/material-remap/Lift01/far-finite'
texture=out/'_BaseColorMap.Texture'
raw=texture.read_bytes()
assert struct.unpack_from('<4H2B',raw)==(5,512,512,1,10,108)
image=Image.frombytes('RGBA',(512,512),raw[18:18+512*512],'bcn',(7,'BC7'))
image.save(out/'original-far-atlas.png')
mesh=np.load(ROOT/'artifacts/uv-density/native/LiftBridge01_LOD2 Mesh.npz')
charts=[]
for name,ids in [('fixed-negative',[708,709]),('moving-negative',[730,731]),('moving-positive',[342,343]),('fixed-positive',[320,321])]:
    uv=mesh['mesh0_uv0'][mesh['mesh0_faces'][ids],:2].reshape(-1,2).astype(float)
    low,high=uv.min(0),uv.max(0)
    box=[math.floor(low[0]*512),math.floor(low[1]*512),math.ceil(high[0]*512),math.ceil(high[1]*512)]
    crop=image.crop(box);crop.save(out/(name+'-original-floor.png'))
    crop.resize((crop.width*8,crop.height*8),Image.Resampling.NEAREST).save(out/(name+'-inspection.png'))
    charts.append(dict(name=name,sourceFaces=ids,min=low.tolist(),max=high.tolist(),inspectionPixelBounds=box,
                       actualAtlasTexelSpan=((high-low)*512).tolist()))
report=dict(sourceSurface='e0101c8ca7e9dc14016d187d012704a4',sourceTextureSHA256=hashlib.sha256(raw).hexdigest(),charts=charts,
    nearInspectionDoesNotApply=True,
    observedMovingNegative='The exact source UV crop visibly includes longitudinal rails and regularly spaced sleepers baked together with the bed. These are not an independent texture layer or merely an albedo tint.',
    requiresFurtherComposition=True,
    donorStatus='No clean donor has been approved. Side strips are visible but their under-track content and full normal/mask/mip equivalence have not been demonstrated; do not silently crop/tile/inpaint them.',
    runtimeRisk='Replacing an arbitrary selected Ballast bed by this original complete far chart also replaces its rail/sleeper appearance. Selected rails are supposed to remain on their own lane path; source UV correctness alone does not qualify this Surface.',
    sourcePixelsModified=False)
(out/'floor-content-inspection.json').write_text(json.dumps(report,indent=2))
print(out/'floor-content-inspection.json')
