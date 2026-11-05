"""Albedo evidence overlay; neither a game render nor a proposed corrected texture."""
from pathlib import Path
import json
import numpy as np
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw03/lane-placement'
SOURCE = ROOT/'artifacts/material-remap/Draw03/full-cross-section-v2/review'
font = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 18)
small = ImageFont.truetype('C:/Windows/Fonts/arial.ttf', 15)
data = json.loads((OUT/'native-lanes.json').read_text())
v_gradient = data['uvAffineXZ'][0][1]*4096
v_origin = data['uvAffineXZ'][2][1]*4096

def panel(kind, width, centers):
    image = Image.new('RGB',(1050,1060),'#14191f')
    draw = ImageDraw.Draw(image)
    original = kind == 'Native source'
    texture = np.asarray(Image.open(SOURCE/('FullPeriodRoadCrossSection-before.png' if original else 'FullPeriodRoadCrossSection-after.png')).convert('RGB'))
    transverse = np.linspace(width/2, -width/2, 940)
    if original:
        rows = v_origin-abs(transverse)*v_gradient-443
    else:
        rows = (1-abs(transverse)/(width/2))*(len(texture)-1)
    pixels = texture[np.clip(rows.astype(int),0,len(texture)-1)[:,None], np.arange(780)[None,:]%968]
    image.paste(Image.fromarray(pixels),(230,80))
    draw.text((18,10),kind,fill='white',font=font)
    draw.text((18,36),'CYAN: lane centers; ORANGE: lane boundaries',fill='white',font=small)
    def y(x): return 80+(width/2-x)/width*939
    for x in centers:
        yy=y(x)
        for start in range(230,1010,28): draw.line((start,yy,start+12,yy),fill='#00e5ff',width=2)
        draw.text((12,yy-9),f'Lane center {x:+.2f} m',fill='#00e5ff',font=small)
    boundaries = [-9,-6,-3,0,3,6,9] if original else [-13.25,-9.25,-5.25,-1.25,1.25,5.25,9.25,13.25]
    for x in boundaries:
        yy=y(x)
        draw.line((210,yy,230,yy),fill='#ffa633',width=3)
        draw.text((120,yy+1),f'{x:+.2f}',fill='#ffa633',font=small)
        for start in range(230,1010,40): draw.line((start,yy,start+4,yy),fill='#ffa633',width=1)
    draw.text((18,1032),'Albedo sampling only; no lighting, normal map or game mesh.',fill='white',font=small)
    return image

left=panel('Native source',18.799808502197266,data['centersMetres'])
right=panel('Candidate 26 mapping / 27 m diagnostic layout',27,[-11.25,-7.25,-3.25,3.25,7.25,11.25])
result=Image.new('RGB',(2100,1060));result.paste(left,(0,0));result.paste(right,(1050,0))
result.save(OUT/'native-and-selected-lanes.png')
(OUT/'overlay-scope.json').write_text(json.dumps(dict(
    nativeLaneProvenance='native-lanes.json; source root/section/piece/lane records',
    selectedLayout='27m diagnostic layout from previous albedo preview; centers are illustrative, not a new saved-lane graph readback',
    conclusion='Original white dashes are already centered on the dark bands. Dark bands cannot be labeled independent tire imprints from brightness alone.',
    sourceImages=[str(SOURCE/'FullPeriodRoadCrossSection-before.png'),str(SOURCE/'FullPeriodRoadCrossSection-after.png')]),indent=2)+'\n')
print(OUT/'native-and-selected-lanes.png')
