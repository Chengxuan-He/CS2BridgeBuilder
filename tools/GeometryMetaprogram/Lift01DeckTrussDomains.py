"""Extract Lift01 deck truss face ownership before affine construction.

The outer U-frame uprights are separated from their central webs. Internal
transverse-web end collars belong to the same central assembly. Non-zero coordinate tests are offline identification only. Output is a
reviewable exact face/vertex manifest, not a runtime spatial predicate.
"""
import json
from pathlib import Path
import sys
import numpy as np

research, inventory, output = map(Path, sys.argv[1:])
output.mkdir(parents=True, exist_ok=True)
for lod in (0, 1):
    data = np.load(research / f'lift01-main-{lod}.npz')
    v, f = data['positions'], data['indices']
    parts = json.loads((inventory / f'original-members-lod{lod}.json').read_text())['parts']
    center, edge, records = set(), set(), []
    for p in parts:
        lo, hi = p['minimum'], p['maximum']
        crossing = (p['family'] == 'crossing'
                    and p['currentRecipe'] in ('JoinedFrames', 'TransverseParts')
                    and hi[1] < 14 and max(abs(lo[0]), abs(hi[0])) < 6)
        inner = (p['family'] == 'side' and lo[1] > 5.8 and hi[1] < 7.4
                 and max(abs(lo[0]), abs(hi[0])) < 3.31)
        if not (crossing or inner):
            continue
        ids = np.flatnonzero(np.isin(f, p['vertices']).all(1))
        # Authored outer upright faces lie entirely outside the central web.
        # The source drawing and exact boundary coordinates are retained alongside
        # this extraction; these predicates do not enter generation code.
        x = v[f[ids], 0]
        outer = ((x.min(1) > 3.3) | (x.max(1) < -3.3)) if crossing and p['currentRecipe'] == 'JoinedFrames' else np.zeros(len(ids), bool)
        c, e = ids[~outer].tolist(), ids[outer].tolist()
        assert not (center | edge).intersection(ids.tolist())
        center.update(c)
        edge.update(e)
        records.append(dict(family=p['family'], ordinal=p['ordinal'],
                            centerFaces=c, edgeFaces=e, minimum=lo, maximum=hi))
    cv = set(f[sorted(center)].ravel().tolist())
    ev = set(f[sorted(edge)].ravel().tolist())
    shared = sorted(cv & ev)
    # The two original quantized U-frame inner planes bound the same nominal
    # construction. Keep both values explicit until the interface audit selects
    # its final datum; do not silently absorb the difference in a tolerance.
    result = dict(lod=lod, centerFaces=sorted(center), edgeFaces=sorted(edge),
                  centerVertices=sorted(cv), edgeVertices=sorted(ev),
                  sharedVertices=shared,
                  sharedPositions=v[shared].tolist(), parts=records,
                  measuredInnerPlanes=[3.39190673828125, 3.3919677734375],
                  status='Exact source-domain candidate; connection sweep and final affine datum pending')
    (output / f'deck-domains-lod{lod}.json').write_text(json.dumps(result), encoding='utf-8')
    print(f'LOD{lod}: {len(records)} members, {len(center)} central faces, '
          f'{len(edge)} edge faces, {len(shared)} shared source vertices')
