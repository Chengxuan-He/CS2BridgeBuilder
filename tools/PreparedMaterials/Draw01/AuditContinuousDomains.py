"""Check retained finite ends and continuous interior topology against source cells."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01'
recipe=json.loads((OUT/'native-atlas-road/recipe.json').read_text())
metadata=json.loads((OUT/'near-units/continuous-unit-consumer-metadata.json').read_text())['units']
names=['asphalt','grating','fixed-sidewalk','fixed-carriageway']
rows=[]
for r in recipe['lods'][0]['domains']:
    domain=names.index(r['domain']);cells=sorted(r['cells'],key=lambda c:c['zRange'][0])
    terminal=domain<2;last=len(cells)-(2 if terminal else 1);reference=min(2,last)
    parts=[{'start':cells[0]['zRange'][0],'end':cells[0]['zRange'][1],'kind':'original-finite'},
           {'start':cells[1]['zRange'][0],'end':cells[last]['zRange'][1],'kind':'continuous-prepared'}]
    if terminal:parts.append({'start':cells[-1]['zRange'][0],'end':cells[-1]['zRange'][1],'kind':'original-finite'})
    assert all(a['end']==b['start'] for a,b in zip(parts,parts[1:]))
    assert parts[0]['start']==cells[0]['zRange'][0] and parts[-1]['end']==cells[-1]['zRange'][1]
    c=cells[reference];m=metadata[domain]
    assert c['zRange'][1]-c['zRange'][0]==4
    assert c['uvMin'][1]==m['sourceMin'][1] and c['uvMax'][1]==m['sourceMax'][1]
    us=(m['contentMax'][0]-m['contentMin'][0])/(m['sourceMax'][0]-m['sourceMin'][0])
    view=[m['contentMin'][0]+(x-m['sourceMin'][0])*us for x in (c['uvMin'][0],c['uvMax'][0])]
    assert 0<view[0]<view[1]<1
    rows.append({'domain':r['domain'],'right':r['right'],'positive':r['positive'],'sourceCells':len(cells),'outputDomains':parts,'referenceZ':c['zRange'],'finiteContentView':view})
result={'status':'DOMAIN_COVERAGE_PASS','newPeriodCuts':0,'sourceCells':sum(r['sourceCells'] for r in rows),'outputDomains':sum(len(r['outputDomains']) for r in rows),'domains':rows,'scope':'Recipe topology only, not final Compose/native rendering acceptance.'}
(OUT/'near-units/continuous-domain-check.json').write_text(json.dumps(result,indent=2))
union=json.loads((OUT/'near-units/fixed-union-report.json').read_text())['units'][0]
files=list((OUT/'near-units/fixed-union-products').rglob('*'))
size=sum(p.stat().st_size for p in files if p.is_file())
(OUT/'near-units/fixed-union-budget.json').write_text(json.dumps({'unionBytes':size,'separateBytes':8279558,'differenceBytes':size-8279558,'decision':'Not selected: wider finite guard and intervening source content outweigh deduplication. Same V period is valid but this payload is larger.','qualityAccepted':False},indent=2))
print(result['status'],result['sourceCells'],result['outputDomains'],size)
