"""Adapt evidenced Lift01 floor inputs to the shared full-mip RepeatUnits/2 producer."""
import argparse
import hashlib
import json
from pathlib import Path
import struct

from PrepareNativeObjects import GAME, ROOT
from VerifyDensity import native_bytes, hash_text


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('--output', type=Path, required=True)
    p.add_argument('--descriptor', type=Path)
    a = p.parse_args()
    out = a.output.resolve()
    evidence_root = ROOT / 'artifacts/prepared-materials/Lift01/repeat-units-v2'
    inventory = json.loads((evidence_root / 'manifest.json').read_text())
    measured = inventory['units'][0]
    prior_native = json.loads((ROOT / 'artifacts/prepared-materials/Lift01/native-object-candidates/native-check.json').read_text())
    verified_sources = {t['cid']: t for t in prior_native['textures']}
    channels = []
    checks = []
    for source in measured['sourceChannels']:
        if source['kind'] == 'absent':
            continue
        vt = native_bytes(GAME, source['cid'], 'VT')
        assert digest(vt) == source['sha256']
        cid = hash_text(vt[3:19])
        raw = native_bytes(GAME, cid, 'Blob')
        native = verified_sources[cid]
        assert digest(raw) == native['sha256'] and native['mips'] == 13
        destination = out / 'source' / (cid + '.Texture')
        destination.parent.mkdir(parents=True, exist_ok=True)
        if destination.exists():
            assert destination.read_bytes() == raw
        else:
            destination.write_bytes(raw)
        channels.append(dict(name=source['name'], cid=cid, sha256=digest(raw), path=str(destination)))
        checks.append(dict(name=source['name'], originalVtCid=source['cid'], originalTextureCid=cid,
                           bytes=len(raw), sha256=digest(raw), mips=13,
                           nativeRawMipSha256=native['rawMipSha256'], preservedOriginalSource=True))
    descriptor = None
    if a.descriptor:
        raw = a.descriptor.read_bytes()
        version, template, vt = struct.unpack_from('<Hi?', raw)
        assert version == 1 and template == measured['sourceTemplate'] and vt, 'Use the original Lift01 VT cutout descriptor.'
        assert digest(raw) == measured['sourceSha256'], 'The source descriptor must match the measured floor source.'
        descriptor = dict(path=str(a.descriptor.resolve()), sha256=digest(raw))
    unit = dict(id=measured['id'], sourceCid=measured['sourceCid'], sourceDigest=measured['sourceSha256'],
                min=measured['min'], max=measured['max'], physicalSize=measured['physicalSize'], cutout=True,
                evidence=dict(path=str(evidence_root / 'prototype-evidence.json'), sha256=measured['geometryEvidenceSha256']),
                descriptor=descriptor, channels=channels, aliases=measured['legacyRepeatKeys'])
    manifest = dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift01', units=[unit])
    (out / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
    result = dict(style='Lift01', readyForProducer=descriptor is not None,
                  sourceChannels=checks, sourceFilesAreAuthoringInputsOnly=True,
                  textureReencoding=False, oldBakeRead=False,
                  missing=[] if descriptor else ['The original measured Lift01 floor Surface descriptor.'])
    (out / 'input-verification.json').write_text(json.dumps(result, indent=2) + '\n')
    (evidence_root / 'input-verification.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
