"""Validate the standalone Lift05 chart binding resource against its source mesh evidence."""
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np


repo, directory = map(Path, sys.argv[1:])
record = json.loads((directory/'charts.json').read_text())
raw = (directory/'Lift05NativeCharts.bin').read_bytes()
assert hashlib.sha256(raw[:-32]).digest() == raw[-32:]
magic, version, ni, nu, nl, nul, nf, nd = struct.unpack_from('<II6i', raw)
assert (magic, version, nl, nul, nf) == (0x314e4242, 1, 0, 0, 0)
assert len(raw) == 64+ni*4+nu*4+nd*8
ints = list(struct.unpack_from(f'<{ni}i', raw, 32))
words = list(struct.unpack_from(f'<{nu}I', raw, 32+ni*4))
doubles = list(struct.unpack_from(f'<{nd}d', raw, 32+ni*4+nu*4))
schema, ns, nr, nt = ints[:4]
assert schema == 2 and ns == len(record['sources']) and nr == len(record['regions']) and nt == len(record['samplers'])
for index, cid in enumerate(record['sourceOrder']):
    assert struct.pack('<4I', *words[index*4:index*4+4]).hex() == cid
at = 4
for region in record['regions']:
    index, source, rectangle, rings = ints[at:at+4]; at += 4
    assert index == region['id'] and record['sourceOrder'][source] == region['sourceSurfaceCid']
    assert bool(rectangle) == region['rectangle']
    expected = [region['exterior']]+region['holes']
    assert rings == len(expected)
    for ring in expected:
        start, count = ints[at:at+2]; at += 2
        assert count == len(ring)
        assert doubles[2*start:2*(start+count)] == [v for point in ring for v in point]
native = {}
for e in record['evidence']:
    path = repo/'artifacts/uv-density/native'/(e['render']+'.npz')
    assert hashlib.sha256(path.read_bytes()).hexdigest() == e['sourceNpZSha256']
    native.setdefault(e['render'], np.load(path))
matched = 0
for index, sampler in enumerate(record['samplers']):
    source, kind, mesh_entry, face, count = ints[at:at+5]; at += 5
    assert record['sourceOrder'][source] == sampler['sourceSurfaceCid']
    assert kind == int(sampler['kind']=='fixed-road-repeat')
    assert ints[at:at+count] == sampler['chartIds']; at += count
    key = struct.pack('<8I', *words[ns*4+index*8:ns*4+(index+1)*8]).hex()
    assert key == sampler.get('key', '0'*64)
    expected = sampler.get('exactNativeFace')
    if expected is None:
        assert mesh_entry == face == -1
        continue
    e = record['evidence'][mesh_entry]
    assert e['render'] == expected['render'] and e['materialSlot'] == expected['mesh'] and face == expected['face']
    n = native[e['render']]; prefix = f"mesh{e['materialSlot']}"
    ids = n[prefix+'_faces'][face]
    assert ids.tolist() == expected['vertexIndices']
    q = n[prefix+'_uv0'][ids, :2]
    sample = sampler['originalSampling']
    intended = np.asarray([sample[k] for k in ('a','b','c')], dtype=np.float32)
    assert sorted(map(tuple,q)) == sorted(map(tuple,intended))
    matched += 1
assert at == len(ints) and matched == record['summary']['exactNativeFaceSamplers']
assert all(r['coveredByNativeSource'] for r in record['samplers'])
result = dict(status='PASS', resourceBytes=len(raw), resourceSha256=hashlib.sha256(raw).hexdigest(),
              sourceMeshEvidence=len(record['evidence']), exactNativeFaceChecks=matched,
              binarySamplingRecords=nt, binaryRegions=nr, newTexturePayloads=0,
              nativeRenderAcceptance=False, packageAcceptance=False)
(directory/'check.json').write_text(json.dumps(result, indent=2)+'\n')
print(json.dumps(result, indent=2))
