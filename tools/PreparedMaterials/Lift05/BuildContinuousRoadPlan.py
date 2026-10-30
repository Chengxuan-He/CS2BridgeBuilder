"""Build reviewable continuous partitions without prototype faces in consumer records."""
import json
from pathlib import Path
import numpy as np
from BuildRoadProjectionPlan import area,line_for,make_tree,coverage,locate,EPS
from ReviewRoadProjectionBoundaries import ROOT,SOURCE
OUT=ROOT/'artifacts/material-remap/Lift05/road-continuous-candidate'
def main():
 evidence=json.loads((OUT/'mapping-evidence.json').read_text());old=json.loads((OUT.parent/'road-projection-plan.json').read_text());boundaries=json.loads((OUT.parent/'road-projection-boundaries.json').read_text());levels=[]
 for l,prior,be in zip(evidence['levels'],old['levels'],boundaries['levels']):
  lod=l['lod'];faces={f['id']:f for f in json.loads((SOURCE/f'road-lod{lod}.json').read_text())['faces']};groups=l['continuousComponents']+[[p['patch']] for p in l['patches'] if p['mode']=='original-finite'];byface={f:g for g,ps in enumerate(groups) for p in ps for f in prior['patches'][p]['faces']}
  patches=[]
  for ids in groups:
   p=l['patches'][ids[0]];co=np.array(p.get('unitCoefficients',p['coefficients']));err=0
   for i in ids:err=max(err,float(np.max(abs(co-np.array(l['patches'][i].get('unitCoefficients',l['patches'][i]['coefficients']))))))
   bound=256*EPS*max(1,float(np.max(abs(co))));assert err<=bound,(lod,ids,err,bound)
   patches.append(dict(mode=p['mode'],unit=p.get('unit'),coefficients=co.tolist(),sourceBounds=[prior['patches'][ids[0]]['uvMin'],prior['patches'][ids[0]]['uvMax']],maximumCoefficientRoundoff=err))
  removed={tuple(tuple(v) for v in e['edge']) for e in be['removedTriangulationEdges']};removed|={tuple(tuple(v) for v in e['physicalEdge']) for e in l['removedPeriodBoundaries']}
  nodes=[];bands=[];checks=[]
  for low,high in ((-6.,-3.),(-3.,3.),(3.,6.)):
   selected=[f for f in faces.values() if low<np.mean(np.array(f['positions'])[:,0])<high];fragments=[(byface[f['id']],np.array(f['positions'])[:,[0,2]]) for f in selected];lines=[];keys=set()
   for f in selected:
    xy=np.array(f['positions'])[:,[0,2]]
    for a,b in ((0,1),(1,2),(2,0)):
     edge=tuple(sorted((tuple(xy[a]),tuple(xy[b]))))
     if edge in removed:continue
     line=line_for(edge);key=tuple(line)
     if key not in keys:keys.add(key);lines.append(line)
   cell=np.array([[low,-84.],[high,-84.],[high,84.],[low,84.]]);root=make_tree(cell,fragments,lines,nodes);actual={};coverage(cell,root,nodes,actual)
   expected={g:sum(area(p) for gg,p in fragments if gg==g) for g,p in fragments};expected[-1]=area(cell)-sum(expected.values());error=max(abs(actual.get(g,0)-v) for g,v in expected.items());bound=256*EPS*84**2*(len(fragments)+len(nodes)+len(cell));assert error<=bound
   for f in selected:
    xy=np.array(f['positions'])[:,[0,2]]
    for w in ((1/3,1/3,1/3),(.8,.1,.1),(.1,.8,.1),(.1,.1,.8)):assert locate(np.array(w)@xy,root,nodes)==byface[f['id']]
   bands.append(dict(minX=low,maxX=high,root=root));checks.append(dict(maximumAreaError=error,arithmeticBound=bound,unsupportedArea=actual.get(-1,0)))
  levels.append(dict(lod=lod,patches=patches,nodes=nodes,bands=bands,coverage=checks));print(lod,'continuous patches',len(patches),'nodes',len(nodes),'coverage PASS')
 (OUT/'consumer-plan.json').write_text(json.dumps(dict(levels=levels,status='Reviewable measured consumer records; not yet production-bound. No source face/vertex/index Boolean input.'),indent=2)+'\n')
if __name__=='__main__':main()
