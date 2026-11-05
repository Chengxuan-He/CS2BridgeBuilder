"""Measure authored repeat boundaries at every native decoded mip."""
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from CheckLaneAppearance import ROOT,read,unit,D,bands
from PrepareDerivedBackground import normals

OUT=ROOT/'artifacts/material-remap/Draw03/derived-background'
recipe=json.loads((OUT/'composition.json').read_text());checks={}
for prop,cid in recipe['bindings'].items():
 if prop=='_ControlMask':continue
 rows=[]
 for mip,a in enumerate(read(cid)):
  edges={}
  for axis in (0,1):
   first=np.take(a,0,axis);last=np.take(a,-1,axis)
   delta=np.abs(first.astype(float)-last.astype(float))
   value=dict(meanEncodedByteDifference=delta.mean(0).tolist(),maxEncodedByteDifference=delta.max(0).tolist())
   if prop=='_NormalMap':
    n=normals(a.astype(float)/255)
    dot=(np.take(n,0,axis)*np.take(n,-1,axis)).sum(1)
    degrees=np.degrees(np.arccos(np.clip(dot,-1,1)))
    value['normalAngleDegreesMean']=float(degrees.mean());value['normalAngleDegreesMax']=float(degrees.max())
   edges['V' if axis==0 else 'U']=value
  rows.append(dict(mip=mip,size=[a.shape[1],a.shape[0]],edges=edges))
 checks[prop]=rows
fixture=json.loads((OUT/'owner28-layout.json').read_text(encoding='utf-8-sig'))
centers=fixture['CarLaneCenters'];span=fixture['SurfaceSpan'];xs=np.linspace(span[1],span[0],1400)
source=np.zeros(len(xs));wear=np.zeros(len(xs),bool)
for right in (False,True):
 for lo,hi,scale,shift,iswear in bands(centers,span,right,uniform=True):
  use=(xs>=lo)&(xs<=hi)&((xs>=0) if right else (xs<0))
  source[use]=xs[use]*scale+shift;wear[use]=iswear
sv=D[1]-abs(source)*D[0];v=unit['finiteUvOffset']+(sv-unit['sourceBounds'][1])/unit['textureSize'][1]
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16)
for prop,cid in recipe['bindings'].items():
 if prop=='_ControlMask':continue
 a=read(unit['bindings'][prop])[0];background=read(cid)[0]
 yy=np.clip((v*a.shape[0]-.5).astype(int),0,a.shape[0]-1)
 rendered=a[yy[:,None],np.arange(968)[None,:]].copy()
 by=np.floor(xs*D[0]).astype(int)%128
 rendered[~wear]=background[by[~wear,None],np.arange(968)[None,:]]
 canvas=Image.new('RGB',(968,1440),'#14191f');ImageDraw.Draw(canvas).text((10,10),'Production candidate: common middle wear / derived background',font=font,fill='white')
 canvas.paste(Image.fromarray(rendered).convert('RGB'),(0,40));canvas.save(OUT/(prop[1:]+'-owner28.png'))
report=dict(status='DIAGNOSTIC_NOT_VISUAL_ACCEPTANCE',mips=checks,
 limitations='Opposite edge differences measure finite texture samples, not a pass threshold. Native Repeat interpolates periodically, but seam contrast and wear interfaces need lighting review. Preview depicts one U period at original source density, not the full game projection.')
(OUT/'boundary-validation.json').write_text(json.dumps(report,indent=2)+'\n');print(OUT/'BaseColorMap-owner28.png')
