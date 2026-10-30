"""Stage the native development candidate; sampling quality remains FAILED.

This does not deploy or publish the mod and is not a quality acceptance operation.
"""
import hashlib
import json
import shutil
import struct
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
TRIAL=ROOT/'artifacts/material-remap/Draw02/offline-atlas-units'
plan=json.loads((TRIAL/'plan.json').read_text())
report=json.loads((TRIAL/'producer-report-raw.json').read_text())
values=[]
for evidence,product in zip(plan['consolidatedUnits'],report['units']):
    channel=product['channels'][0]
    assert all(c['contentMin']==channel['contentMin'] and c['contentMax']==channel['contentMax'] for c in product['channels'])
    values+=evidence['minimum']+evidence['maximum']+channel['contentMin']+channel['contentMax']
blob=struct.pack('<8I',0x314e4242,1,0,0,0,0,len(values),0)+struct.pack('<%df'%len(values),*values)
blob+=hashlib.sha256(blob).digest()
target=ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02PreparedAtlas_cs.bin'
target.write_bytes(blob)
source=TRIAL/'output-raw/Draw02'
destination=ROOT/'assets/Materials/Draw02'
records=[]
for path in source.rglob('*'):
    if not path.is_file(): continue
    relative=path.relative_to(source);target=destination/relative
    data=path.read_bytes()
    if target.exists() and target.read_bytes()!=data:
        raise ValueError('Existing candidate identity has different bytes: '+str(relative))
    target.parent.mkdir(parents=True,exist_ok=True)
    shutil.copyfile(path,target)
    records.append(dict(path=str(relative),bytes=len(data),sha256=hashlib.sha256(data).hexdigest()))
(TRIAL/'staged-development-candidate.json').write_text(json.dumps(dict(quality='FAILED',deployable=False,
    files=records,bytes=sum(r['bytes'] for r in records),bbnSha256=hashlib.sha256(blob).hexdigest(),
    reason='Original atlas boundary and internal native mip/filter counterexamples remain unresolved.'),indent=2)+'\n')
print(json.dumps(dict(files=len(records),bytes=sum(r['bytes'] for r in records),bbnFloats=len(values))))
