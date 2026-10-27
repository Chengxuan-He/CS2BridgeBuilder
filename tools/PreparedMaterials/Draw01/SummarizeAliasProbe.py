"""Summarize native Draw01 filter comparisons, retaining falsifiable examples."""
import collections
import hashlib
import json
from pathlib import Path

root=Path(__file__).resolve().parents[3]
folder=root/'artifacts/prepared-materials/Draw01'
raw=folder/'alias-filter-results.json'
data=json.loads(raw.read_text())
inputs=json.loads((folder/'alias-filter-input.json').read_text())
rows=data['samples']
summary=dict(gpu=data['gpu'],api=data['api'],pairs=data['pairs'],comparisons=len(rows),
    rawSHA256=hashlib.sha256(raw.read_bytes()).hexdigest(),threshold=1/255,modes={})
for mode in ['same-index','footprint-adjusted','self-control']:
    items=[x for x in rows if x['mode']==mode]
    summary['modes'][mode]=dict(comparisons=len(items),
        failPairs=len({x['oldKey'] for x in items if max(x['interiorMax'],x['seamMax'])>1/255}),
        mip0FailPairs=len({x['oldKey'] for x in items if x['oldMip']==0 and max(x['interiorMax'],x['seamMax'])>1/255}),
        interiorFailPairs=len({x['oldKey'] for x in items if x['interiorMax']>1/255}),
        seamFailPairs=len({x['oldKey'] for x in items if x['seamMax']>1/255}),
        maxInterior=max(x['interiorMax'] for x in items),maxSeam=max(x['seamMax'] for x in items),
        maxRms=max(x['rms'] for x in items),maxNormalAngle=max(x['maxNormalAngle'] for x in items),
        channels={c:dict(maxInterior=max(x['interiorMax'] for x in items if x['channel']==c),
                        maxSeam=max(x['seamMax'] for x in items if x['channel']==c),
                        failPairs=len({x['oldKey'] for x in items if x['channel']==c and max(x['interiorMax'],x['seamMax'])>1/255}))
                  for c in sorted({x['channel'] for x in items})})
summary['counterexamples']={}
for name,predicate,metric in [('mip0Color',lambda x:x['oldMip']==0 and x['channel']=='_BaseColorMap','seamMax'),
                              ('normal',lambda x:x['channel']=='_NormalMap','maxNormalAngle'),
                              ('higherMip',lambda x:x['oldMip']>=1,'interiorMax')]:
    record=max((x for x in rows if x['mode']=='same-index' and predicate(x)),key=lambda x:x[metric])
    pair=next(p for p in inputs['pairs'] if p['oldKey']==record['oldKey'])
    channel=next(c for c in pair['channels'] if c['name']==record['channel'])
    summary['counterexamples'][name]=dict(**record,matrix=pair['matrix'],
        minimumStretch=channel['minimumStretch'],maximumStretch=channel['maximumStretch'])
summary['allCandidatesHaveNonOrthogonalPixelMap']=all(any(abs(c['minimumStretch']-1)>1e-6 or abs(c['maximumStretch']-1)>1e-6 for c in p['channels']) for p in inputs['pairs'])
(folder/'alias-filter-summary.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps(summary,indent=2))
