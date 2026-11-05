"""Clone the measured native network descriptor with pure asphalt base PBR.

Worldspace references and every non-base-binding byte remain original. The
existing pure mask is already blue-zero at every mip, so no texture is added.
"""
import json
import struct
import zipfile
from StageCleanCap import ROOT, GAME, sha, identity, cid_bytes


def main():
    out = ROOT / 'artifacts/material-remap/Draw01/bridgehead35'
    source_entry = 'Road_0c1d8d6afba9b274ba12424460a2cdcc.Material'
    with zipfile.ZipFile(GAME / 'Content/Game/Blob.cok') as archive:
        source = archive.read(source_entry)
    assert len(source) == 250
    assert sha(source) == '99f48ff7b6296957b5170c7fdf14ce65912c1de8eb81b15ba7c38b5189be1572'
    assert struct.unpack_from('<Hi?', source) == (1, 17, False)
    validation = json.loads((out / 'head-mask-validation.json').read_text())
    assert validation['nativeBytesIdentical'] and all(mip['blueMax'] == 0 for mip in validation['levels'])
    target_bindings = json.loads((ROOT / 'artifacts/material-remap/Draw01/asphalt-repeat34/bindings.json').read_text())
    original = dict(_BaseColorMap='9a6506aaede935752b5c4c0362c95f6e',
                    _NormalMap='a142c18b42577de52815d1afd054660b',
                    _MaskMap='37ceea0e8c2bd24599e237236e6ae338')
    candidate = source
    changed_offsets = set()
    for name, cid in original.items():
        token = cid_bytes(cid)
        assert candidate.count(token) == 1
        at = candidate.index(token)
        changed_offsets.update(range(at, at + 16))
        candidate = candidate.replace(token, cid_bytes(target_bindings[name]))
        assert (ROOT / 'assets/Materials/Draw01/Shared' / (target_bindings[name] + '.Texture')).is_file()
    assert all(a == b or i in changed_offsets for i, (a, b) in enumerate(zip(source, candidate)))
    key = sha(b'Draw01/NativeNetworkHeadAsphalt/1' + candidate)
    cid = identity(candidate)
    destination = ROOT / 'assets/Materials/Draw01/RepeatUnits' / key
    destination.mkdir(parents=True, exist_ok=True)
    (destination / 'Repeat.Surface').write_bytes(candidate)
    (destination / 'Repeat.Surface.cid').write_text(cid)
    (out / 'native-head-descriptor.json').write_text(json.dumps(dict(
        key=key, surfaceCid=cid, template=17, bytes=len(candidate), source=source_entry,
        sourceSha256=sha(source), sha256=sha(candidate), bindings={name: target_bindings[name] for name in original},
        allOtherBytesIdentical=True, newTextureBytes=0, nativeConsumerAcceptance=False), indent=2) + '\n')
    print(key, cid, '250 descriptor bytes; 0 new texture bytes')


if __name__ == '__main__':
    main()
