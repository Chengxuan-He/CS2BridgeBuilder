"""Prepare a private native-bake trial; no shipping assets or runtime changes."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/offline-atlas-units'
plan_path = OUT/'plan.json'
plan = json.loads(plan_path.read_text())
old = json.loads((ROOT/'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/manifest.json').read_text())['units'][0]
near = ROOT/'artifacts/material-remap/Draw02/near-cells'
moving = json.loads((near/'native-atlas-wrap-plan.json').read_text())[0]['domains'][0]
walk = json.loads((near/'walk-atlas-plan.json').read_text())[0]
fixed = next(x for x in json.loads((near/'fixed-walk-atlas-plan.json').read_text()) if x['repeatV'])
# This is reference geometry metadata, not a universal finite-axis density rule.
gradients = [abs(moving['u'][0]), (walk['sourceMaximum'][0]-walk['sourceMinimum'][0])/(walk['sourceX'][1]-walk['sourceX'][0]), abs(fixed['profile'][0])]
units = []
for index, (unit, gradient) in enumerate(zip(plan['consolidatedUnits'], gradients)):
    lo, hi = unit['minimum'], unit['maximum']
    units.append(dict(id=f'Draw02/offline-atlas-{index}',sourceCid=old['sourceCid'],sourceDigest=old['sourceDigest'],
        min=lo,max=hi,physicalSize=[(hi[0]-lo[0])/gradient,4],repeatAxes='V',finiteSourceGrid=True,
        sourceGradientSampling=True,finiteGridMip=0,
        evidence=dict(path=str(plan_path),sha256=hashlib.sha256(plan_path.read_bytes()).hexdigest()),
        descriptor=old['descriptor'],channels=[dict(name=c['name'],cid=c['cid'],sha256=c['sha256'],path=c['path'],encoding='BC7') for c in old['channels']]))
(OUT/'trial-manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3',style='Draw02',units=units),indent=2)+'\n')
print('Prepared three private candidates; source-boundary and filtering acceptance remain unresolved.')
pairs=[]
for suffix in ('','-raw'):
    product_path=OUT/f'producer-report{suffix}.json'
    if not product_path.exists():
        continue
    product=json.loads(product_path.read_text())
    for index, unit in enumerate(product['units']):
        bounds=plan['consolidatedUnits'][index]
        base_source=next(c for c in old['channels'] if c['name']=='_BaseColorMap')['path']
        base_product=next(c for c in unit['channels'] if c['name']=='_BaseColorMap')['cid']
        for channel in unit['channels']:
            source=next(c for c in old['channels'] if c['name']==channel['name'])
            pairs.append(dict(name=f'unit{index}{suffix}/{channel["name"]}',source=source['path'],
                sourceColor=base_source,derivedColor=str(OUT/f'output{suffix}/Draw02/Shared/{base_product}.Texture'),
                derived=str(OUT/f'output{suffix}/Draw02/Shared/{channel["cid"]}.Texture'),
                bounds=bounds['minimum']+bounds['maximum'],content=channel['contentMin']+channel['contentMax'],normal=channel['name']=='_NormalMap'))
(OUT/'filter-manifest.json').write_text(json.dumps(dict(pairs=pairs),indent=2)+'\n')
