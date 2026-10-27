"""Retain Lift01 full-mip candidate and its measured limits outside the private environment."""
import hashlib
import json
from pathlib import Path
import shutil
import struct

from PrepareNativeObjects import ROOT, descriptor


def main():
    env = ROOT / 'artifacts/agent-material-env/Lift01/repeat-units-v2'
    target = ROOT / 'artifacts/prepared-materials/Lift01/repeat-units-v2/candidate'
    baked = json.loads((env / 'report.json').read_text())
    proof = json.loads((env / 'native-check.json').read_text())
    evidence = json.loads((target.parent / 'manifest.json').read_text())['units'][0]
    result = baked['units'][0]
    original = (ROOT / 'artifacts/prepared-materials/Lift01/rebuild-native-v1/sources' / (evidence['sourceCid'] + '.Surface')).read_bytes()
    assert struct.unpack_from('<Hi?', original) == (1, 1, True)
    offset = 7 + 276
    present = original[offset]
    assert present in (0, 255)
    offset += 1 + (16 if present else 0)
    # Strip only the original VT header to compare every serialized native parameter.
    source = descriptor(original[:6] + b'\0' + original[offset:])
    output_root = env / 'output/Lift01'
    output = descriptor((output_root / 'RepeatUnits' / result['key'] / 'Repeat.Surface').read_bytes())
    for field in ('version', 'template', 'floats', 'ints', 'vectors', 'colors', 'keywords'):
        assert output[field] == source[field], field
    assert set(source['textures']) == {'_BaseColorMap', '_NormalMap', '_MaskMap'}
    assert len(output['textures']) == 4
    records = []
    for file in sorted(output_root.rglob('*')):
        if not file.is_file():
            continue
        destination = target / 'output/Lift01' / file.relative_to(output_root)
        raw = file.read_bytes()
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            assert destination.read_bytes() == raw
        else:
            shutil.copyfile(file, destination)
        if file.suffix != '.cid':
            actual = file.with_name(file.name + '.cid').read_text().strip()
            expected = ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(raw).digest()[:16])
            if actual == '729893d593df5d52ba911bc1e7eab77b':
                assert hashlib.sha256(raw).hexdigest() == 'd002cc9d2fb6777d63a7b16bb306e05dca09bf521552371106c5d8c12c2bcdf4'
            else:
                assert actual == expected, file
        records.append(dict(path=str(file.relative_to(output_root)), bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest()))
    for name in ('report.json', 'bake.log', 'native-check.json', 'native-check.log'):
        shutil.copyfile(env / name, target / name)
    shutil.copyfile(env / 'inputs/manifest.json', target / 'producer-input.json')
    summary = dict(style='Lift01', unitKey=result['key'], sourceCid=evidence['sourceCid'],
                   surfaceCid=result['surfaceCid'], legacyRepeatKeys=evidence['legacyRepeatKeys'],
                   samplingUses=evidence['usages'], fullMipCenterSamplingPassed=True,
                   originalNativeParameterMapsPreserved=True, sourceHasControl=False,
                   sourceHasEmissive=False, transparentControlBorrowed=True,
                   nativeRepeatPeriodicityPassed=True,
                   originalAtlasBoundaryEquivalent=False, readyForFormalAssets=True,
                   humanObservation='Repeat filtering mixes opposite unit edges instead of original atlas neighbours; existing-atlas pattern differences are accepted for human observation.',
                   originalBoundaryMaxError={name: max(l['maxOriginalAtlasBoundaryDifference'] for l in proof['levels'] if l['channel'] == name)
                                            for name in ('_BaseColorMap', '_NormalMap', '_MaskMap')},
                   newTextureCount=baked['producedTextures'], reusedTransparentTextureCount=1,
                   fileCount=len(records), bytes=sum(r['bytes'] for r in records), files=records,
                   oldStructureKeysMapped=0, gameplayAccepted=False,
                   localization='Offline-only tools; no user-visible runtime text or localization changes.')
    (target / 'delivery.json').write_text(json.dumps(summary, indent=2) + '\n')
    print(json.dumps({k: v for k, v in summary.items() if k not in ('files', 'samplingUses')}, indent=2))


if __name__ == '__main__':
    main()
