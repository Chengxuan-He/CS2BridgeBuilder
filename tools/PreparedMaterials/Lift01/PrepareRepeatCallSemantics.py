"""Match Lift01's actual cutout repeat call without altering any texture payload."""
import hashlib
import json
from pathlib import Path
import shutil
import struct

from PrepareNativeObjects import ROOT, descriptor, cid
from ExportRecipes import source_recipes, recipe_key


def text(value):
    raw = value.encode()
    assert len(raw) < 128
    return bytes([len(raw)]) + raw


def main():
    evidence = ROOT / 'artifacts/prepared-materials/Lift01/repeat-units-v2'
    original = evidence / 'candidate'
    target = evidence / 'call-semantics'
    delivery = json.loads((original / 'delivery.json').read_text())
    old_key = delivery['unitKey']
    recipes, code_evidence = source_recipes(ROOT)
    assert len(recipes) == 1 and recipes[0]['cutout'] and recipes[0]['regions'] == list(range(8))
    source_digest = json.loads((evidence / 'manifest.json').read_text())['units'][0]['sourceSha256']
    assert recipe_key(source_digest, recipes[0]) == delivery['legacyRepeatKeys'][0]
    old = original / 'output/Lift01/RepeatUnits' / old_key / 'Repeat.Surface'
    parsed = descriptor(old.read_bytes())
    names = ['_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask']
    keywords = ['_ALPHATEST_ON', '_DISABLE_SSR_TRANSPARENT', '_DOUBLESIDED_ON', '_TANGENTSPACE_OCTO']
    raw = struct.pack('<Hi?', 1, 14, False)
    raw += b'\xff' + struct.pack('<i', 1) + text('_Snow') + struct.pack('<f', 0)
    raw += (b'\xff' + struct.pack('<i', 0)) * 3
    raw += b'\xff' + struct.pack('<i', len(names))
    for name in names:
        identity = parsed['textures'][name]
        raw += text(name) + b'\xff' + bytes(int(identity[i+1] + identity[i], 16) for i in range(0, 32, 2))
    raw += b'\xff' + struct.pack('<i', len(keywords)) + b''.join(text(k) for k in keywords)
    identity = cid(hashlib.sha256(raw).digest()[:16])
    unit_key = hashlib.sha256(text('BridgeBuilder/RepeatUnits/2') + text(identity)).hexdigest()
    new = target / 'output/Lift01/RepeatUnits' / unit_key / 'Repeat.Surface'
    new.parent.mkdir(parents=True, exist_ok=True)
    if new.exists():
        assert new.read_bytes() == raw
    new.write_bytes(raw)
    new.with_name('Repeat.Surface.cid').write_text(identity)
    for file in (original / 'output/Lift01/Shared').iterdir():
        dest = target / 'output/Lift01/Shared' / file.name
        dest.parent.mkdir(parents=True, exist_ok=True)
        if dest.exists():
            assert dest.read_bytes() == file.read_bytes()
        else:
            shutil.copyfile(file, dest)
    after = descriptor(raw)
    assert after['template'] == 14 and after['floats'] == {'_Snow': [0.0]}
    assert after['textures'] == parsed['textures'] and after['keywords'] == keywords
    delivery.update(unitKey=unit_key, surfaceCid=identity, sourceUnitKey=old_key,
                    originalNativeParameterMapsPreserved=False, originalCallSemanticsPreserved=True,
                    oldProducer='tools/GpuBake/PrepareRepeat.Editor.cs Surface(cutout=true)',
                    template=14, snow=0, keywords=keywords, newTextureCount=0,
                    codeEvidence=code_evidence)
    delivery['files'] = [dict(path=str(p.relative_to(target / 'output/Lift01')), bytes=p.stat().st_size,
                             sha256=hashlib.sha256(p.read_bytes()).hexdigest())
                         for p in sorted((target / 'output/Lift01').rglob('*')) if p.is_file()]
    delivery['bytes'] = sum(p['bytes'] for p in delivery['files'])
    (target / 'delivery.json').write_text(json.dumps(delivery, indent=2) + '\n')
    print(json.dumps(dict(unitKey=unit_key, surfaceCid=identity, descriptorBytes=len(raw), newTextureCount=0), indent=2))


if __name__ == '__main__':
    main()
