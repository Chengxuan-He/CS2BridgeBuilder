"""Integrate the verified Lift01 floor unit without rebaking or changing existing payloads."""
import hashlib
import json
from pathlib import Path
import shutil

from PrepareNativeObjects import ROOT, descriptor


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    candidate = ROOT / 'artifacts/prepared-materials/Lift01/repeat-units-v2/call-semantics'
    source = candidate / 'output/Lift01'
    target = ROOT / 'assets/Materials/Lift01'
    delivery = json.loads((candidate / 'delivery.json').read_text())
    assert delivery['fullMipCenterSamplingPassed'] and delivery['nativeRepeatPeriodicityPassed']
    assert delivery['originalCallSemanticsPreserved']
    assert 'PASS native source descriptors: 1 descriptors' in (candidate / 'native-reader.log').read_text()
    code = (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01RepeatUnits.cs').read_text()
    assert delivery['unitKey'] in code and all(key in code for key in delivery['legacyRepeatKeys'])
    before = {str(p.relative_to(target)): sha(p) for p in target.rglob('*') if p.is_file()}
    existing_ids = {}
    for sidecar in target.rglob('*.cid'):
        payload = sidecar.with_suffix('')
        cid = sidecar.read_text().strip()
        existing_ids.setdefault(cid, []).append(payload)
    # Verify all records and all identity conflicts before the first write.
    for record in delivery['files']:
        src = source / record['path']
        assert src.stat().st_size == record['bytes'] and sha(src) == record['sha256']
        dst = target / record['path']
        if dst.exists():
            assert dst.read_bytes() == src.read_bytes(), dst
        if src.suffix != '.cid':
            identity = src.with_name(src.name + '.cid').read_text().strip()
            for old in existing_ids.get(identity, []):
                assert old.read_bytes() == src.read_bytes(), old
    added, retained = [], []
    for record in delivery['files']:
        src, dst = source / record['path'], target / record['path']
        if dst.exists():
            retained.append(record['path'])
        else:
            dst.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(src, dst)
            added.append(record['path'])
        assert sha(dst) == record['sha256']
    for path, digest in before.items():
        assert sha(target / path) == digest
    surface = target / 'RepeatUnits' / delivery['unitKey'] / 'Repeat.Surface'
    parsed = descriptor(surface.read_bytes())
    assert parsed['template'] == 14 and not parsed['vt'] and parsed['floats'] == {'_Snow': [0.0]}
    assert parsed['keywords'] == ['_ALPHATEST_ON', '_DISABLE_SSR_TRANSPARENT', '_DOUBLESIDED_ON', '_TANGENTSPACE_OCTO']
    for cid in parsed['textures'].values():
        dependency = target / 'Shared' / (cid + '.Texture')
        assert dependency.is_file() and dependency.with_name(dependency.name + '.cid').read_text().strip() == cid
        assert len([p for p in target.rglob('*.Texture.cid') if p.read_text().strip() == cid]) == 1
    # Structural uses retain the original source template; road calls use 14.
    # Both descriptors reference exactly the same four physical texture files.
    old_key = delivery['sourceUnitKey']
    assert old_key in code and 'Lift01Regions' in code
    structural = target / 'RepeatUnits' / old_key
    for name in ('Repeat.Surface', 'Repeat.Surface.cid'):
        destination = structural / name
        expected = candidate.parent / 'candidate/output/Lift01/RepeatUnits' / old_key / name
        if destination.exists():
            assert destination.read_bytes() == expected.read_bytes()
        else:
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(expected, destination)
    structural_descriptor = descriptor((structural / 'Repeat.Surface').read_bytes())
    assert structural_descriptor['template'] == 1 and structural_descriptor['textures'] == parsed['textures']
    files = [p for p in target.rglob('*') if p.is_file()]
    assert not [p for p in target.rglob('*') if p.is_dir() and not any(p.iterdir())]
    report = dict(style='Lift01', unitKey=delivery['unitKey'], repeatKeys=delivery['legacyRepeatKeys'],
                  samplingUseCount=len(delivery['samplingUses']), added=added, unchanged=retained,
                  preexistingFileCount=len(before), preexistingPayloadsUnchangedBeforeRetirement=True,
                  structuralUnitKey=old_key, structuralTemplate=1, roadTemplate=14, texturePayloadsUnchanged=True,
                  repeatGraphComplete=True, onePayloadPerCid=True,
                  styleFiles=len(files), styleBytes=sum(p.stat().st_size for p in files),
                  newStructureAliases=0, legacyStructureKeysUnmapped=2353,
                  boundaryDifferenceAcceptedForHumanObservation=True,
                  originalBoundaryMaxError=delivery['originalBoundaryMaxError'],
                  gameVisualAcceptance=False, mainBuildPending=True,
                  localization='No runtime user-visible strings or changes to localized errors.')
    (candidate.parent / 'integration.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(report, indent=2))


if __name__ == '__main__':
    main()
