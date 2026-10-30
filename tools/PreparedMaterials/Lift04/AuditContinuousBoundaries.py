"""Separate physical/normal-frame boundaries from affine-fit-only road partitions."""
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/material-remap/Lift04'
source = json.loads((BASE / 'near-domain-groups.json').read_text())
rows = []
for level in source['levels']:
    groups = level['domains']
    for i, before in enumerate(groups):
        for j, after in enumerate(groups):
            if before['max'][2] != after['min'][2]:
                continue
            if (before['min'][0] < -1) != (after['min'][0] < -1):
                continue
            same_bone = before['bone'] == after['bone']
            same_tangent = before['tangent'] == after['tangent']
            planar = before['min'][1] == before['max'][1] == after['min'][1] == after['max'][1]
            rows.append(dict(lod=level['lod'], before=i, after=j,
                station=before['max'][2], sameBone=same_bone,
                sameNativeTangent=same_tangent, sameHorizontalPlane=planar,
                sameSurface=True, beforeU=before['u'], afterU=after['u'],
                classification='affine-fit boundary only; no physical/material cut evidence'
                    if same_bone and same_tangent and planar else
                    'native frame or motion boundary; verify independent cut eligibility'))
output = BASE / 'offline-road-unit/continuous-boundary-classification.json'
output.write_text(json.dumps(dict(boundaries=rows,
    conclusion='The twenty-domain fit is not a compliant runtime partition by itself. '
    'Same-Surface coplanar boundaries without frame/motion changes must not be retained merely to fit UV phase.'), indent=2))
print(len(rows), 'adjacent boundaries;', sum('only' in r['classification'] for r in rows), 'fit-only boundaries')
