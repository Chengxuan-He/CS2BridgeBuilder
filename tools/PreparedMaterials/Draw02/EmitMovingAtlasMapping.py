"""Pack measured appearance boundaries and absolute original-atlas mappings."""
import hashlib, json, struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
plan = json.loads((ROOT / 'artifacts/material-remap/Draw02/near-cells/native-atlas-wrap-plan.json').read_text())
integers = [0] * 18
floats = []
for level in plan:
    domains, slabs = level['domains'], level['boundarySlabs']
    intervals = [item for slab in slabs for item in slab['intervals']]
    dio = len(integers)
    integers.extend(d['bone'] for d in domains)
    sio = len(integers)
    cursor = 0
    for slab in slabs:
        integers.extend([slab['bone'], cursor, len(slab['intervals'])])
        cursor += len(slab['intervals'])
    iio = len(integers)
    integers.extend(d['domain'] for d in intervals)
    dfo = len(floats)
    for domain in domains:
        lo, hi = domain['sourceBounds']
        stages = {s['kind']: s for s in domain['stages']}
        sign = 1 if domain['bone'] == 1 else -1
        # The missing outer solid terminal has no geometry and is never selected.
        hinge = stages.get('hinge', stages['repeat'])
        floats.extend(domain['u'] + [lo[0], hi[0], lo[1], hi[1]] +
                      [stages['centre']['absoluteZ'][0], hinge['absoluteZ'][1]] +
                      [v for name in ('centre', 'repeat', 'hinge')
                       for v in (stages.get(name, hinge)['v'][1], stages.get(name, hinge)['v'][2])])
    sfo = len(floats)
    for slab in slabs:
        floats.extend(slab['absoluteZ'])
    ifo = len(floats)
    for interval in intervals:
        floats.extend(interval['left'] + interval['right'])
    integers[level['lod'] * 9:level['lod'] * 9 + 9] = [len(domains), len(slabs), len(intervals), dio, sio, iio, dfo, sfo, ifo]
# Original full carriageway half width and the finite/repeating domain endpoints.
floats.extend([6.0999755859375, 4., 76., 78.69921875])
blob = struct.pack('<8I', 0x314e4242, 1, len(integers), 0, 0, 0, len(floats), 0)
blob += struct.pack('<%di' % len(integers), *integers) + struct.pack('<%df' % len(floats), *floats)
blob += hashlib.sha256(blob).digest()
target = ROOT / 'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02MovingAtlasAppearance_cs.bin'
target.write_bytes(blob)
print(target.name, len(blob), hashlib.sha256(blob).hexdigest())
