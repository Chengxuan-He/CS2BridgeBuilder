"""Measure the complete light wear shape, then translate without resizing it."""
import json,hashlib,struct
import numpy as np
from PIL import Image,ImageDraw
from CheckLaneAppearance import ROOT,unit,read,D
out=ROOT/'artifacts/material-remap/Draw03/wear-centering';out.mkdir(exist_ok=True)
pixels=read(unit['bindings']['_BaseColorMap'])[0]
guard=round(unit['finiteUvOffset']*4096)
y=np.arange(818,1194);profile=pixels[guard+y-443,:,:3].mean((1,2))
low,high=np.quantile(profile,[.05,.95]);measurements=[]
for fraction in (.2,.4,.5,.6,.8):
    selected=y[profile>=low+(high-low)*fraction]
    measurements.append(dict(fraction=fraction,firstPixel=int(selected.min()),lastPixel=int(selected.max()),
        centerPixelCoordinate=float((selected.min()+selected.max()+1)/2)))
center=float(np.median([m['centerPixelCoordinate'] for m in measurements]))
reference=(D[1]-center)/D[0]
report=dict(source=unit['surface'],sourceBounds=[818,1194],method='Full 968-pixel longitudinal RGB mean; median of five transition-level paired midpoints, using pixel centers.',
    measurements=measurements,visualCenterPixel=center,oldNativeCenterPixel=D[1]-4.5*D[0],
    newReferenceMetres=reference,oldReferenceMetres=4.5,translationMagnitudeMetres=reference-4.5,
    physicalWidthMetres=376/D[0],scope='Translation only; production material hashes, source width, U projection and lane positions unchanged.')
(out/'measurement.json').write_text(json.dumps(report,indent=2)+'\n')
im=Image.new('RGB',(800,380),'white');draw=ImageDraw.Draw(im)
points=[((v-818)*2,340-(p-low)*5) for v,p in zip(y+.5,profile)]
draw.line(points,fill='black',width=2)
for v,color,label in [(center,'green','Measured wear center'),(D[1]-4.5*D[0],'red','Old native lane reference'),(1006,'blue','Crop midpoint')]:
    x=(v-818)*2;draw.line((x,35,x,350),fill=color,width=2);draw.text((x+3,10 if color=='green' else 25 if color=='red' else 365),label,fill=color)
im.save(out/'profile.png')
path=ROOT/'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03LaneAppearance_cs.bin'
raw=bytearray(path.read_bytes());struct.pack_into('<f',raw,32+2*4,reference);raw[-32:]=hashlib.sha256(raw[:-32]).digest();path.write_bytes(raw)
print(json.dumps(report))
