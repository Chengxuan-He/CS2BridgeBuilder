"""Artifact-only comparison of a common complete middle-lane wear source."""
import json
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from CheckLaneAppearance import ROOT,D,unit,read,bands,native_background

out=ROOT/'artifacts/material-remap/Draw03/uniform-wear-proposal';out.mkdir(exist_ok=True)
centers=[-15.25,-11.25,-7.25,-3.25,3.25,7.25,11.25,15.25];span=(-19.25,19.25)
xs=np.linspace(span[1],span[0],1400);width=968;zs=(np.arange(width)+.5)/width*8
native,_=native_background();names={'_BaseColorMap':'BaseColor','_NormalMap':'Normal','_MaskMap':'MaskMap'}
font=ImageFont.truetype('C:/Windows/Fonts/arial.ttf',16);diagnostics={}
for prop,cid in unit['bindings'].items():
 if prop=='_ControlMask':continue
 atlas=read(cid)[0];background=native[names[prop]][0]
 quilt=np.asarray(Image.open(ROOT/'artifacts/material-remap/Draw03/background-proposal'/(prop[1:]+'-unit.png')))
 results=[];stats=[]
 for uniform,derived in ((False,False),(True,False),(True,True)):
  source=np.zeros(len(xs));wear=np.zeros(len(xs),bool)
  for right in (False,True):
   for lo,hi,scale,shift,iswear in bands(centers,span,right,uniform=uniform):
    use=(xs>=lo)&(xs<=hi)&((xs>=0) if right else (xs<0))
    source[use]=xs[use]*scale+shift;wear[use]=iswear
  sv=D[1]-abs(source)*D[0];v=unit['finiteUvOffset']+(sv-unit['sourceBounds'][1])/unit['textureSize'][1]
  rows=np.clip((v*atlas.shape[0]-.5).astype(int),0,atlas.shape[0]-1)
  result=atlas[rows[:,None],np.arange(width)[None,:]].copy()
  if derived:
   by=np.floor(xs*D[0]).astype(int)%len(quilt)
   result[~wear]=quilt[by[~wear,None],np.arange(width)[None,:]]
  else:
   bx=np.floor(xs*.4*1024).astype(int)%1024;bz=np.floor(zs*.4*1024).astype(int)%1024
   result[~wear]=background[bz[None,:],bx[~wear,None]]
  edges=np.flatnonzero(wear[1:]!=wear[:-1]);diff=np.abs(result[edges].astype(float)-result[edges+1].astype(float))
  stats.append(dict(uniform=uniform,derivedBackground=derived,edgeCount=len(edges),meanChannelError=diff.mean((0,1)).tolist(),
   maxChannelError=diff.max((0,1)).tolist(),units='encoded bytes, not perceptual/normal-angle acceptance'))
  results.append(result)
 canvas=Image.new('RGB',(width*3,1440),'#14191f');draw=ImageDraw.Draw(canvas)
 labels=['Current: inner / middle / outer sources','Proposal: same middle source; current background','Proposal: same middle source; derived background (unapproved)']
 for i,result in enumerate(results):
  draw.text((width*i+10,10),labels[i],font=font,fill='white');canvas.paste(Image.fromarray(result).convert('RGB'),(width*i,40))
 canvas.save(out/(prop[1:]+'-comparison.png'));diagnostics[prop]=stats
report=dict(status='ARTIFACT_ONLY_NO_PRODUCTION_MAPPING_CHANGE',owner='b09951756-3d07-450c-a89e-f2438f26fe34',
 centers=centers,span=span,sourceV=[818,1194],sourceLaneCenter=4.5,
 sourceCenterPixel=D[1]-4.5*D[0],physicalWidth=376/D[0],periodU=8,
 sourceRelativeRange=[(D[1]-1194)/D[0]-4.5,(D[1]-818)/D[0]-4.5],
 channelDiagnostics=diagnostics,
 limits='Same source removes inter-lane source variation, not the discontinuity to background. No background-repeat permission inferred. No native resources changed; no mip/render acceptance claimed.')
(out/'comparison.json').write_text(json.dumps(report,indent=2)+'\n')
print(out/'BaseColorMap-comparison.png')
