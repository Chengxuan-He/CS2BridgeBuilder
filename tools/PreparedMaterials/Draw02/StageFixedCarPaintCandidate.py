"""Stage only the reviewed paint variant for development; not quality acceptance."""
from pathlib import Path
import hashlib
import json
import shutil
import struct

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
report = json.loads((OUT/'paint-producer-report.json').read_text())
isolation = json.loads((OUT/'paint-unit-isolation-check.json').read_text())
unit = report['units'][1]
assert isolation['passed'] and isolation['paintedKey'] == unit['key']
recipe = json.loads((OUT/'paint-composition-recipe.json').read_text())
content = unit['channels'][0]
assert all(c['contentMin'] == content['contentMin'] and c['contentMax'] == content['contentMax'] for c in unit['channels'])
values = recipe['sourceRegion']['min']+recipe['sourceRegion']['max']+content['contentMin']+content['contentMax']+[.25]
blob = struct.pack('<8I', 0x314e4242, 1, 0, 0, 0, 0, len(values), 0)+struct.pack('<%df' % len(values), *values)
blob += hashlib.sha256(blob).digest()
(ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02FixedCarPreparedAtlas_cs.bin').write_bytes(blob)
source = OUT/'paint-output/Draw02'; destination = ROOT/'assets/Materials/Draw02'
paths = [Path('RepeatUnits')/unit['key']/'Repeat.Surface']+[Path('Shared')/(c['cid']+'.Texture') for c in unit['channels']]
paths += [Path(str(p)+'.cid') for p in paths]
files = []
for relative in paths:
    original, target = source/relative, destination/relative
    data = original.read_bytes()
    assert not target.exists() or target.read_bytes() == data, 'CID conflict: '+str(target)
    target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(original, target)
    files.append(dict(path=str(relative), bytes=len(data), sha256=hashlib.sha256(data).hexdigest()))
(OUT/'staged-painted-candidate.json').write_text(json.dumps(dict(key=unit['key'], files=files,
    bytes=sum(f['bytes'] for f in files), bbnSha256=hashlib.sha256(blob).hexdigest(),
    deployable=False, quality='NOT ACCEPTED', ownerGraphBudget='Not measured',
    purpose='Development production routing; successful paint isolation is not source-filtering equivalence.'), indent=2)+'\n')
print('Staged', len(files), 'native files;', sum(f['bytes'] for f in files), 'bytes; quality not accepted.')
