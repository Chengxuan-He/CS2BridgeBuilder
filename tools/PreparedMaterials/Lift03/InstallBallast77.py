"""Reuse the previously prepared, native-checked pure Lift03 ballast period."""
from pathlib import Path
import json, hashlib, shutil
R=Path(__file__).resolve().parents[3]
C=R/'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2'
O=R/'artifacts/material-remap/Lift03/appearance77';O.mkdir(parents=True,exist_ok=True)
unit=next(x for x in json.loads((C/'bake.json').read_text())['units'] if x['id']=='Lift03.AuxiliaryBallast')
source=next(x for x in json.loads((C/'input/manifest.json').read_text())['units'] if x['id']==unit['id'])
check=json.loads((C/'Lift03.AuxiliaryBallast-native-check.json').read_text())
assert check['unitKey']==unit['key'] and check['completeGraph'] and check['nativeHeaders']
for c in source['channels']:
    assert hashlib.sha256((C/'input'/(c['cid']+'.Texture')).read_bytes()).hexdigest()==c['sha256']
validated={x['path']:x for x in json.loads((C/'validation-summary.json').read_text())['files']}
paths=[f"Lift03/RepeatUnits/{unit['key']}/Repeat.Surface"]
paths += ['Lift03/Shared/'+c['cid']+'.Texture' for c in unit['channels']]
paths += ['Lift03/Shared/729893d593df5d52ba911bc1e7eab77b.Texture']
files=[]
for path in paths:
    for suffix in ('','.cid'):
        relative=path+suffix;p=C/'output'/relative;raw=p.read_bytes()
        assert hashlib.sha256(raw).hexdigest()==validated[relative]['sha256']
        target=R/'assets/Materials'/relative;target.parent.mkdir(parents=True,exist_ok=True)
        if target.exists():assert target.read_bytes()==raw
        else:shutil.copy2(p,target)
        files.append(dict(path=relative,bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest()))
report=dict(sourceSurface=source['sourceCid'],sourceBoundsPixels=[x*4096 for x in (*source['min'],*source['max'])],
    unit=unit,files=files,totalBytes=sum(f['bytes'] for f in files),rebakedPixels=0,
    definition='Reuse a bounded authored pure-ballast repeat, not an assertion of exact original atlas boundary equivalence. Both axes repeat at measured 249 source pixels/metre. All decks/LODs bind the same immutable PBR payloads.',
    nativeEvidence=check,scope='Source checksum, exact prepared-byte reuse and prior complete native mip checks. Current callback/Compose and game visuals checked separately.')
(O/'ballast-reuse.json').write_text(json.dumps(report,indent=2)+'\n')
print('Reused complete ballast graph:',report['totalBytes'],'bytes; no rebake')
