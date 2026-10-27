"""Prepare the three remaining measured units using corrected complete VT inputs."""
import copy
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ENV = ROOT / 'artifacts/agent-material-env/Draw01'
OUT = ENV / 'repeat-units-single-period'


def main():
    seed = json.loads((ENV / 'repeat-units-control-corrected/extracted-source/manifest.json').read_text())
    evidence = ROOT / 'artifacts/prepared-materials/Draw01/genuine-unit-requirements/manifest.json'
    candidates = json.loads(evidence.read_text())['roadCandidates']
    original = Path(seed['units'][0]['descriptor']['path']).read_bytes()
    units = []
    (OUT / 'extracted-source').mkdir(parents=True, exist_ok=True)
    for candidate in candidates:
        if candidate['candidateId'] == 'metal-grating':
            continue
        unit = copy.deepcopy(seed['units'][0])
        unit.update(id=candidate['candidateId'], min=candidate['region']['min'],
                    max=candidate['region']['max'], physicalSize=candidate['proposedPhysicalPeriodMetres'],
                    aliases=candidate['legacyRequestKeys'])
        # Native road repeat semantics differ from the original structural template.
        # Only the descriptor changes; all source pixel channels remain identical.
        template = 14 if candidate['cutout'] else 0
        raw = original[:2] + struct.pack('<i', template) + original[6:]
        descriptor = OUT / 'extracted-source' / f'template-{template}.Surface'
        descriptor.write_bytes(raw)
        unit['descriptor'] = dict(path=str(descriptor), sha256=hashlib.sha256(raw).hexdigest())
        unit['evidence'] = dict(path=str(evidence), sha256=hashlib.sha256(evidence.read_bytes()).hexdigest())
        units.append(unit)
    seed['units'] = units
    path = OUT / 'extracted-source/manifest.json'
    path.write_text(json.dumps(seed, indent=2)+'\n')
    print(path)


if __name__ == '__main__':
    main()
