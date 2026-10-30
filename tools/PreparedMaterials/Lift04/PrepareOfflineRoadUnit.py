"""Prepare the measured bed-unit trial without generating receiver period cuts."""
import copy
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Lift04/offline-road-unit'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    source = ROOT / 'artifacts/prepared-materials/Lift04/structure-inventory/profile-manifest.json'
    old = json.loads(source.read_text())['units'][0]
    unit = {key: copy.deepcopy(old[key]) for key in
            ('sourceCid', 'sourceDigest', 'descriptor', 'channels')}
    assert unit['sourceCid'] == '6b973ac2133fa9bd8d897bc9eff3363f'
    for item in [unit['descriptor'], *unit['channels']]:
        assert digest(Path(item['path'])) == item['sha256'], item['path']
    evidence = ROOT / 'artifacts/material-remap/Lift04/near-exact-cells.json'
    levels = json.loads(evidence.read_text())['levels']
    exemplar = levels[0]['strips'][0]['cells'][0]
    unit.update(id='Lift04/MeasuredBedFiniteURepeatV',
                min=[.4697265625, .8251953125],
                max=[.62548828125, .9814453125],
                repeatAxes='V', finiteSourceGrid=True, finiteGridMip=0,
                physicalSize=[1 / abs(exemplar['u'][1]), 1 / abs(exemplar['v'][0])],
                sourceGradientSampling=True,
                normalTransform=[1, 0, 0, 0, 1, 0, 0, 0, 1],
                evidence={'path': str(evidence), 'sha256': digest(evidence)})
    # The required dimensions describe the first measured cell only; they are not
    # a global mapping rule. Longitudinal pitches are not uniform.
    # Do not invent a global four-metre period from one terminal cell.
    changes = []
    for level in levels:
        strips = level['strips']
        for a, b in zip(strips, strips[1:]):
            for side in range(2):
                ca, cb = a['cells'][side], b['cells'][side]
                changes.append(dict(lod=level['lod'], side=side,
                    station=a['zmax'], adjacent=a['zmax'] == b['zmin'],
                    beforeU=ca['u'], afterU=cb['u'], beforeV=ca['v'], afterV=cb['v'],
                    sameLongitudinalGradient=ca['u'][:2] == cb['u'][:2],
                    sameTransverseProjection=ca['v'] == cb['v']))
    (OUT / 'unit-trial.json').write_text(json.dumps(dict(
        recipe='BridgeBuilder/RepeatUnits/3', style='Lift04', units=[unit]), indent=2))
    repeating = copy.deepcopy(unit)
    repeating.update(id='Lift04/MeasuredBedRepeatUV', repeatAxes='UV', finiteSourceGrid=False)
    (OUT / 'unit-repeat-uv-trial.json').write_text(json.dumps(dict(
        recipe='BridgeBuilder/RepeatUnits/3', style='Lift04', units=[repeating]), indent=2))
    (OUT / 'continuous-projection-audit.json').write_text(json.dumps(dict(
        sourceHash=digest(evidence), boundaries=changes,
        status='Measured unit recipe only; no continuous receiver mapping accepted.',
        restrictions=['No reset-only receiver partitions', 'No full-span pixel expansion',
                      'No uniform-pitch replacement for measured stations'],
        nativeFiltering='Original-atlas boundary difference remains unaccepted; run all-channel/mip comparison.'), indent=2))
    print(f'Wrote measured trial and {len(changes)} boundary comparisons to {OUT}')


if __name__ == '__main__':
    main()
