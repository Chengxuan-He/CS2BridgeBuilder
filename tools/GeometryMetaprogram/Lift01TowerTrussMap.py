"""Emit measured lower tower transverse-truss membership (not tower machinery)."""
from pathlib import Path
import json, struct, hashlib, sys
import numpy as np

research, repo = map(Path, sys.argv[1:])
half = 7.6124267578125
groups = []
upper_half = 6.1463623046875
evidence = []
for lod in (0, 1):
    crossing = json.loads((research / f'lift01-crossing-{lod}.json').read_text())
    side = json.loads((research / f'lift01-side-{lod}.json').read_text())
    # The two original transverse tower assemblies: columns, bars, riveted centre
    # connections and their complete inclined members. Housings remain separate.
    cross_ids = list(range(278, 334)) + [334, 335]
    side_ids = []
    for i, p in enumerate(side):
        lo, hi = np.array(p['minimum']), np.array(p['maximum'])
        extent = max(abs(lo[0]), abs(hi[0]))
        if (abs(extent-half) < .00001 and lo[1] >= 15 and hi[1] < 77
                and min(abs(lo[2]), abs(hi[2])) > 80
                and lo[0]*hi[0] > 0):
            side_ids.append(i)
    members = [crossing[i] for i in cross_ids] + [side[i] for i in side_ids]
    ids = sorted(i for p in members for i in p['vertices'])
    assert len(ids) == len(set(ids))
    d = np.load(research / f'lift01-main-{lod}.npz')
    mask = np.isin(d['indices'], ids)
    assert np.all(mask.all(1) == mask.any(1))
    groups.append(ids)
    upper_cross = [i for i in range(90,278) if i not in (169,170,171,263,264,265)]
    machinery_side = set(range(2394,2398)) | set(range(2649,2653)) | set(range(2899,2903)) | set(range(3144,3148))
    upper_side = [i for i in range(2238,3298)
                  if i not in machinery_side and max(abs(side[i]['minimum'][0]), abs(side[i]['maximum'][0])) <= upper_half]
    upper_ids = sorted(j for p in [crossing[i] for i in upper_cross] + [side[i] for i in upper_side] for j in p['vertices'])
    assert len(upper_ids)==len(set(upper_ids)) and not set(ids).intersection(upper_ids)
    upper_mask=np.isin(d['indices'],upper_ids)
    assert np.all(upper_mask.any(1)==upper_mask.all(1))
    groups.append(upper_ids)
    evidence.append(dict(lod=lod, assembly='upper transverse gantry', crossing=upper_cross, side=upper_side,
                         vertices=len(upper_ids), faces=int(upper_mask.all(1).sum()), halfSpan=upper_half))
    evidence.append(dict(lod=lod, crossing=cross_ids, side=side_ids,
                         vertices=len(ids), faces=int(mask.all(1).sum()), halfSpan=half))
ints = [len(g) for g in groups] + [i for g in groups for i in g]
payload = struct.pack('<8i', 0x314e4242, 1, len(ints), 0, 0, 0, 2, 0)
payload += struct.pack('<' + 'i'*len(ints), *ints) + struct.pack('<2f', half, upper_half)
payload += hashlib.sha256(payload).digest()
target = repo / 'assets/HardcodedNumbers/Lift01/Types_Lift01_TowerTrussMap.bin'
target.write_bytes(payload)
out = repo / 'artifacts/material-remap/Lift01/truss62/tower-map.json'
out.write_text(json.dumps(evidence, indent=2), encoding='utf-8')
print([(e['lod'],e.get('assembly','lower transverse tower'),e['vertices'],e['faces']) for e in evidence])
