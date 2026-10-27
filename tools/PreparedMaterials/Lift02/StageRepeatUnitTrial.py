"""Stage original Lift02 VT inputs for a private full-mip repeat trial."""
from pathlib import Path
import hashlib
import json
import zipfile

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / 'artifacts/prepared-materials/Lift02/repeat-units-v2'
INPUT = ROOT / 'artifacts/agent-material-env/Lift02/repeat-v2-trial/input'


def main():
    INPUT.mkdir(parents=True, exist_ok=True)
    evidence = json.loads((EVIDENCE / 'evidence.json').read_bytes())
    candidate = json.loads((EVIDENCE / 'manifest.pending.json').read_bytes())
    paths = {}
    for key, extension in (('source', 'Surface'), ('backing', 'VTSurface')):
        record = evidence[key]
        with zipfile.ZipFile(record['archive']) as archive:
            raw = archive.read(record['entry'])
        assert hashlib.sha256(raw).hexdigest() == record['sha256']
        target = INPUT / (record['cid'] + '.' + extension)
        if target.exists():
            assert target.read_bytes() == raw
        else:
            target.write_bytes(raw)
        paths[key] = str(target)
    unit = candidate['units'][0]
    unit.pop('cutout', None)
    unit['descriptor'] = dict(path=paths['source'], sha256=evidence['source']['sha256'])
    unit['channels'] = [dict(name=c['name'], cid=evidence['source']['serializedTextureBindings'][c['name']],
                             kind='VTSurface', stack=0, layer=c['layer'],
                             sourcePayloadCid=evidence['backing']['cid'], path=paths['backing'],
                             sha256=evidence['backing']['sha256']) for c in evidence['channels']]
    manifest = dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift02', units=[unit],
                    purpose='Private validation trial; boundary equivalence is not yet accepted.')
    output = INPUT / 'manifest.json'
    output.write_text(json.dumps(manifest, indent=2) + '\n', encoding='utf-8')
    print(output)


if __name__ == '__main__':
    main()
