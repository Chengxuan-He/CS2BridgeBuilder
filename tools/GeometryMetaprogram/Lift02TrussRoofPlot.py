"""Orthographic geometry diagnostic, not a game rendering or visual acceptance."""
from pathlib import Path
import json,numpy as np,sys
from PIL import Image,ImageDraw
repo=Path(sys.argv[1]);r=repo/'artifacts/material-remap/Lift02/truss69'
d=np.load(repo/'artifacts/uv-density/native/LiftBridge02 Mesh.npz');v,f=d['mesh0_positions'],d['mesh0_faces']
old=np.frombuffer((r/'old68/lod0-delta8.5.bin').read_bytes(),'<f4',offset=4).reshape(-1,3)
new=np.frombuffer((r/'new69r3/lod0-delta8.5.bin').read_bytes(),'<f4',offset=4).reshape(-1,3)
extra=np.zeros(len(v),bool)
for p in json.loads((r/'additional-members-lod0.json').read_text()):extra[p['vertices']]=True
faces=np.flatnonzero((v[f][:,:,1].min(1)>18)&(v[f][:,:,2].min(1)>-5.3)&(v[f][:,:,2].max(1)<26.9))
im=Image.new('RGB',(1500,1000),'white');dr=ImageDraw.Draw(im)
for panel,positions,title in [(0,old,'Installed 68: actual Plan, delta 8.5m'),(1,new,'Candidate 69r3: actual Plan, delta 8.5m')]:
 dr.text((panel*750+20,15),title,fill='black')
 for i in faces[np.argsort(v[f[faces],1].mean(1))]:
  q=positions[f[i]];pts=[(panel*750+375+float(p[0])*23,70+(float(p[2])+5.3)*27) for p in q]
  color='#d65045' if extra[f[i]].all() else ('#257bb5' if v[f[i],0].min()<0<v[f[i],0].max() else '#999999')
  dr.polygon(pts,fill=color)
 dr.text((panel*750+20,960),'Red: added complete members; blue: crossing faces; gray: remaining source',fill='black')
im.save(r/'roof-comparison.png')
