"""Summarize actual native Plan contact witnesses without an acceptance-gap threshold."""
from pathlib import Path
import sys,json,numpy as np
out=Path(sys.argv[1])/'artifacts/material-remap/Lift02/truss69'
a=json.loads((out/'surface-connections-r3.json').read_text())
assert all(v['actualNewPlan'] for v in a)
lines=['# Actual 69r3 contact measurements','', 'Production DLL FDD5D026A30258B9FA34998F2E19A90EB82DF0F465F9765AF0182E39E28ED03C, halfDelta 8.5m. Original vertex/other-component triangle candidates use a 0.02m source proximity envelope; this is not a pass threshold. Final source and new distance includes edge/triangle intersections and interior edge/edge minima. No gameplay visual acceptance is implied.','', '| LOD | Candidates | Increase P50 | P90 | P95 | P99 | Maximum |','| --- | ---: | ---: | ---: | ---: | ---: | ---: |']
summary=[]
for lod in range(3):
 x=[v for v in a if v['lod']==lod];q=np.quantile([v['newSurfaceDistance']-v['sourceDistance'] for v in x],[.5,.9,.95,.99,1]);lines.append('| '+str(lod)+' | '+str(len(x))+' | '+' | '.join(f'{z:.9f}' for z in q)+' |')
 # Arithmetic residual classification of original zero contact, not a geometric
 # acceptance threshold: the source projection calculation is float64.
 zero=[v for v in x if v['sourceDistance']<1e-10]
 summary.append(dict(lod=lod,candidates=len(x),increaseQuantiles=q.tolist(),originalZeroContactCount=len(zero),maximumOriginalZeroContactDistance=max(v['newSurfaceDistance'] for v in zero)))
lines+=['','## Originally touching subset','', 'Source-distance < 1e-10m identifies arithmetic zero in double-precision projection; it is not an allowed new gap.','', '| LOD | Original zero contacts | Maximum current distance |','| --- | ---: | ---: |']
for v in summary:lines.append(f"| {v['lod']} | {v['originalZeroContactCount']} | {v['maximumOriginalZeroContactDistance']:.12g} |")
old=json.loads((out/'residual23-first69-refined.json').read_text());lookup={(v['lod'],*sorted((v['a'],v['b']))):v for v in a};result=[]
lines+=['','## All 23 previously isolated residual pairs','', '| LOD | Components | Prototype | First 69 | 69r3 | Difference from prototype |','| --- | --- | ---: | ---: | ---: | ---: |']
for v in old:
 n=lookup[(v['lod'],*sorted((v['a'],v['b'])))];result.append(dict(n,first69SurfaceDistance=v['newSurfaceDistance']));lines.append(f"| {v['lod']} | {v['a']} / {v['b']} | {n['sourceDistance']:.9f} | {v['newSurfaceDistance']:.9f} | {n['newSurfaceDistance']:.9f} | {n['newSurfaceDistance']-n['sourceDistance']:.9f} |")
lines+=['','Distances are metres. Nonzero source gaps can expand under the explicitly required uniform source-X scaling. Near pairs 44/1256 and 112/1256 retain their original 19.775391mm minimum after complete edge/edge measurement. Two mid outer interfaces remain separated: 152/32 by 0.928425mm and 525/19 by 15.352970mm. Both internal members use the required uniform moving-assembly k; their outer gusset/vertical plate retain the required original edge map. Far asymmetric source ends are preserved; a single origin-centred k cannot enforce two unequal end datums simultaneously against unchanged rigid edge translations. Do not describe the whole bridge or all signed widths as absolutely seamless.']
(out/'ContactMeasurements69r3.md').write_text('\n'.join(lines)+'\n');(out/'contact-summary-r3.json').write_text(json.dumps(summary,indent=2));(out/'residual23-r3.json').write_text(json.dumps(result,indent=2));print(summary)
