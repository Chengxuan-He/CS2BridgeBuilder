from pathlib import Path
import numpy as np,json
from PIL import Image,ImageDraw
R=Path('D:/CS2BridgeBuilder');O=R/'artifacts/material-remap/Draw02/closure53'
for lod,cid in enumerate(['47331a7032e9c0eda9bdb8b522f40ca1','c401f6a114b090084d35c02325febc23','d8ef11a742ab332ffb7e583befeb21dd']):
 d=np.load(R/f'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism/lod{lod}-native.npz');v=d['mesh0_positions'];f=d['mesh0_indices'].reshape(-1,3);im=Image.new('RGB',(1300,600),'#eee');dr=ImageDraw.Draw(im)
 def plot(v,f,side):
  p=v[f];sel=(p[:,:,1].max(1)>9.69)&(p[:,:,2].min(1)<4)&(p[:,:,2].max(1)>-4)
  for t in p[sel]:dr.polygon([(float(x*18+300+650*side),float(z*60+300)) for x,y,z in t],fill='#dbb757',outline='#553311')
 plot(v,f,0)
 a=json.loads((R/f'artifacts/material-remap/shared/draw02-structure52/attributes/{cid}.attributes.json').read_text())
 for m in a['meshes']:plot(np.array(m['positions']),np.array(m['faces']),1)
 dr.text((10,10),'Original LOD '+str(lod+1),fill='black');dr.text((660,10),'Actual 52 structure LOD '+str(lod+1),fill='black');im.save(O/f'center-lod{lod}.png')
