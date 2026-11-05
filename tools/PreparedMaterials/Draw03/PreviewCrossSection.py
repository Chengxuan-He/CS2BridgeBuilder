"""Source-texture comparison for the reported six-lane Draw03 cross-section."""
from pathlib import Path
import json
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw03/full-cross-section-v2/review'
OLD=ROOT/'artifacts/material-remap/Draw03/composed-periods-v8/review'
width=27.0
x=np.linspace(-width/2,width/2,1080)
z=np.linspace(0,32,1280)
u=((z/8)%1*968).astype(int)
new=np.asarray(Image.open(OUT/'FullPeriodRoadCrossSection-after.png'))
wear=np.asarray(Image.open(OLD/'FullPeriodLaneWear-after.png'))
gap=np.asarray(Image.open(OLD/'FullPeriodLaneGap-after.png'))
before=np.zeros((len(x),len(z),4),dtype=np.uint8)
after=before.copy()
centers=np.array([-11.25,-7.25,-3.25,3.25,7.25,11.25])
for i,xx in enumerate(x):
    # Each prototype half maps its finite outside-to-centre traversal.
    v=1-abs(xx)/(width/2)
    after[i]=new[min(len(new)-1,int(v*(len(new)-1))),u]
    nearest=centers[np.argmin(abs(centers-xx))]
    distance=xx-nearest
    if abs(distance)<=1.55:
        before[i]=wear[min(len(wear)-1,int((distance+1.55)/3.1*(len(wear)-1))),u]
    else:
        before[i]=gap[len(gap)//2,u]
for image in (before,after):
    for line in (-13.25,-9.25,-5.25,-1.25,1.25,5.25,9.25,13.25):
        rows=abs(x-line)<.09
        dashed=(abs(line) in (5.25,9.25))
        cols=(z%8<3) if dashed else np.ones(len(z),dtype=bool)
        image[np.ix_(rows,cols)]=255
Image.fromarray(before).save(OUT/'selected-width-before.png')
Image.fromarray(after).save(OUT/'selected-width-after.png')
(OUT/'selected-width-preview.json').write_text(json.dumps(dict(widthMetres=width,lengthMetres=32,
    markings='Read-only parent measurement of saved paint x intervals; centre positions rounded to interval centres.',
    scope='Albedo-only sampling preview, no lighting or normal map. Before illustrates historical lane/gap mapping; exact saved geometry UV readback remains a separate test.'),indent=2)+'\n')
