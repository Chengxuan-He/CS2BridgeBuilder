"""Count a proposed donor overlay on an explicitly synthetic selected road.

This is a review calculation, not a production generator or authorization.
"""
import json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
recipe=json.loads((OUT/'recipe.json').read_text())
def clip(poly,normal,offset):
 result=[]
 for a,b in zip(poly,poly[1:]+poly[:1]):
  da=np.dot(normal,a)+offset;db=np.dot(normal,b)+offset
  if da>=0:result.append(a)
  if (da>=0)!=(db>=0):result.append(a+(b-a)*(da/(da-db)))
 return result
rows=[]
for width in [1.,1.7]:
 half=3.4000244140625*width
 corners=[np.array(p,dtype=float) for p in [(-half,-92),(half,-92),(half,92),(-half,92)]]
 faces=[[corners[i] for i in f] for f in [(0,2,1),(0,3,2)]]
 count=0;area=0.;domains=[]
 for domain in recipe['lods'][0]['domains']:
  if domain['domain']!='asphalt':continue
  sign=1 if domain['positive'] else -1
  lo,hi=(0,half) if domain['right'] else (-half,0)
  for cell in domain['cells']:
   xs=[p[0] for p in cell['positions']];scale=(max(xs)-min(xs))/(hi-lo);shift=min(xs)-scale*lo
   au,zu,ou=cell['affineAxes'][0]['affineXz1'];normal=np.array([au*scale,zu]);offset=au*shift+ou
   z0,z1=cell['zRange'];local=0
   for face in faces:
    poly=clip(face,normal,offset-787/4096)
    if poly:poly=clip(poly,-normal,810/4096-offset)
    if poly:poly=clip(poly,np.array([0,sign]),-z0)
    if poly:poly=clip(poly,np.array([0,-sign]),z1)
    if len(poly)<3:continue
    for i in range(1,len(poly)-1):
     a,b,c=poly[0],poly[i],poly[i+1];value=abs(np.linalg.det(np.stack([b-a,c-a])))*.5
     if value>1e-10:count+=1;local+=1;area+=value
   domains.append(local)
 rows.append(dict(widthScale=width,sourceSelectedTriangles=2,overlayTriangles=count,coveredAreaSquareMetres=area,cellTriangleCounts=domains))
report=dict(status='REVIEW_ONLY_NOT_IMPLEMENTED',fixture='Synthetic single rectangular selected asphalt face pair, ±92 m, half-width3.4000244m; no real generated-road count.',
            sourcePaintBoundsPixels=[787,810,2778,3510],donorBoundsPixels=[403,426,2778,3510],
            actualNearRoadCells=88,minimumIndependentQuadTriangles=176,scenarios=rows)
(OUT/'overlay-geometry-review.json').write_text(json.dumps(report,indent=2)+'\n')
print([(r['widthScale'],r['overlayTriangles'],r['coveredAreaSquareMetres']) for r in rows])
