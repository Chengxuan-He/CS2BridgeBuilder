"""Diagnostic template-byte probe only; use Rebuild-NativeMaterials.ps1 for production.

Production conversion must explicitly retain native source template defaults.
This probe only isolates unchanged texture CIDs and mip data; its output is not
a production material and must not be copied into assets/Materials.
"""
import argparse
import hashlib
import io
import json
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
OUT = ROOT/'artifacts/prepared-materials/Lift01/native-object-candidates'
RECIPE = 'BridgeBuilder/Lift01/NativeObjectDescriptor/1'


def text(r):
    n, shift = 0, 0
    while True:
        b = r.read(1)[0]
        n |= (b & 127) << shift
        if b < 128:
            return r.read(n).decode('utf-8')
        shift += 7


def count(r):
    marker = r.read(1)
    if marker == b'\0':
        return 0
    assert marker == b'\xff'
    return struct.unpack('<i', r.read(4))[0]


def cid(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in raw)


def descriptor(raw):
    r = io.BytesIO(raw)
    version, template, vt = struct.unpack('<Hi?', r.read(7))
    assert version == 1 and not vt, 'Network descriptor requires separate VT handling'
    maps = {}
    for name, fmt in [('floats', '<f'), ('ints', '<i'), ('vectors', '<4f'), ('colors', '<4f')]:
        values = {}
        for _ in range(count(r)):
            key = text(r)
            values[key] = list(struct.unpack(fmt, r.read(struct.calcsize(fmt))))
        maps[name] = values
    bindings = {}
    for _ in range(count(r)):
        name = text(r)
        present = r.read(1)
        assert present in (b'\0', b'\xff')
        bindings[name] = cid(r.read(16)) if present == b'\xff' else None
    keywords = [text(r) for _ in range(count(r))]
    assert r.tell() == len(raw), 'Unconsumed native descriptor'
    return dict(version=version, template=template, vt=vt, **maps, textures=bindings, keywords=keywords)


def put(path, raw):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists():
        assert path.read_bytes() == raw, 'Immutable candidate differs: ' + str(path)
    else:
        path.write_bytes(raw)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, default=OUT)
    args = parser.parse_args()
    out = args.output.resolve()
    native = json.loads((ROOT/'artifacts/prepared-materials/Lift01/shared-source/native-sources.json').read_text())
    mapping = json.loads((ROOT/'artifacts/prepared-materials/Lift01/shared-source/source-sampler-map.json').read_text())
    sources = {s['cid']: s for s in native['sources'] if s['template'] == 33}
    found = {}
    for family in ('Game', 'BridgesAndPorts'):
        for archive_path in (GAME/'Content'/family).glob('Blob*.cok'):
            with zipfile.ZipFile(archive_path) as z:
                for entry in z.namelist():
                    if not entry.endswith('.Surface.cid'):
                        continue
                    identity = z.read(entry).decode().strip()
                    if identity not in sources:
                        continue
                    raw = z.read(entry[:-4])
                    if identity in found:
                        assert found[identity][0] == raw
                    found[identity] = (raw, str(archive_path), entry[:-4])
    assert len(found) == 3
    rows = []
    for identity, (raw, archive, entry) in sorted(found.items()):
        before = descriptor(raw)
        assert before['template'] == 33
        candidate = raw[:2] + struct.pack('<i', 0) + raw[6:]
        after = descriptor(candidate)
        assert {k:v for k,v in before.items() if k != 'template'} == {k:v for k,v in after.items() if k != 'template'}
        assert candidate[:2] == raw[:2] and candidate[6:] == raw[6:]
        digest = hashlib.sha256(candidate).hexdigest()
        identity_out = hashlib.sha256((RECIPE+'/'+digest).encode()).hexdigest()[:32]
        relative = Path('NativeSources')/identity_out/'Object.Surface'
        put(out/'payload'/relative, candidate)
        put(out/'payload'/(str(relative)+'.cid'), identity_out.encode())
        put(out/'originals'/(identity+'.Surface'), raw)
        samplers = [dict((k,s[k]) for k in ('key','sourceCid','sourceDigest','triangle','bounds','a','b','c','channels'))
                    for s in mapping['samplers'] if s['sourceCid'] == identity]
        assert len(samplers) == sources[identity]['samplers']
        rows.append(dict(sourceCid=identity, sourceSha256=hashlib.sha256(raw).hexdigest(), sourceArchive=archive,
                         sourceEntry=entry, outputPath=('payload/'+relative.as_posix()), outputCid=identity_out,
                         outputSha256=digest, bytes=len(candidate), descriptor=after, samplers=samplers,
                         changedByteOffsets=[i for i,(a,b) in enumerate(zip(raw,candidate)) if a != b]))
    report = dict(recipe=RECIPE, style='Lift01', sourceTemplate=33, targetTemplate=0,
                  texturesCreated=0, texturesDecoded=0,
                  descriptorBytes=sum(r['bytes'] for r in {r['outputCid']:r for r in rows}.values()),
                  descriptorCount=len({r['outputCid'] for r in rows}),
                  samplerCount=sum(len(r['samplers']) for r in rows), sources=rows,
                  nativeReaderVerified=False, nativeRenderingVerified=False,
                  uvSemantics='Original atlas UVs; sampler coordinates retained without modulo/fold/clamp',
                  placement='Candidate only; not copied to assets/Materials')
    out.mkdir(parents=True, exist_ok=True)
    (out/'manifest.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k != 'sources'}))
    for row in rows:
        print(json.dumps({k:v for k,v in row.items() if k not in ('samplers',)}))


if __name__ == '__main__':
    main()
