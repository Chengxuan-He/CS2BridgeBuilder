"""Stage the complete Draw01 test graph privately; never install or deploy it."""
import hashlib,json,shutil
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
DEST=OUT/'continuous-candidate/Materials/Draw01'
entries=[]
for report_name,products,indices in [
    ('bounded-paired-report.json','bounded-paired-products',[0]),
    ('grating-phase2-bake-report.json','grating-phase2-products',[0]),
    ('grid-bake-report.json','grid-products',[2,3])]:
    report=json.loads((OUT/report_name).read_text())
    for i in indices:
        unit=report['units'][i]
        paths=[Path('RepeatUnits')/unit['key']/'Repeat.Surface']
        paths += [Path('Shared')/(c['cid']+'.Texture') for c in unit['channels']]
        for rel in paths+[Path(str(p)+'.cid') for p in paths]:
            source=OUT/products/'Draw01'/rel
            destination=DEST/rel
            destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copyfile(source,destination)
            entries.append({'path':str(destination.relative_to(ROOT)),'bytes':source.stat().st_size,'sha256':hashlib.sha256(source.read_bytes()).hexdigest()})
assert len({e['path'] for e in entries})==len(entries)
result={'status':'PRIVATE_CANDIDATE_NOT_ACCEPTED','files':entries,'preparedClosureBytes':sum(e['bytes'] for e in entries),'nativeVT':False,'midMips':[],
        'generatedOwnerBytes':None,'ownerMissing':'Selected-input dependencies, native finite/source domains, original nonroad and generated owner graph remain to be measured.',
        'quality':'Mask isolation passed; original-sampler filtering, terminal paint, middle asphalt and cold-load/render acceptance remain incomplete.'}
(OUT/'continuous-candidate-closure.json').write_text(json.dumps(result,indent=2))
print(result['preparedClosureBytes'])
