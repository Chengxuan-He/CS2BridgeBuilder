"""One full evidenced 4 m cycle, rephased to place the finite half-cycle inside.

This is an unpainted private development candidate. It is neither sampling
acceptance nor a claim that all source mip texels survive without resampling.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
plan_path = OUT/'mapping-plan.json'
plan = json.loads(plan_path.read_text())
old = json.loads((ROOT/'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/manifest.json').read_text())['units'][0]
lo, hi = plan['sharedUnit']['min'], plan['sharedUnit']['max']
panels = []
for target_a, target_b, source_a, source_b in [(0, .25, .75, 1), (.25, 1, 0, .75)]:
    target = [[0, target_a], [1, target_a], [1, target_b], [0, target_b]]
    source = [[lo[0], lo[1]+source_a*(hi[1]-lo[1])],
              [hi[0], lo[1]+source_a*(hi[1]-lo[1])],
              [hi[0], lo[1]+source_b*(hi[1]-lo[1])],
              [lo[0], lo[1]+source_b*(hi[1]-lo[1])]]
    for ids in ((0, 1, 2), (0, 2, 3)):
        panels.append(dict(target=sum([target[i] for i in ids], []), source=sum([source[i] for i in ids], [])))
# This is one known period, not four new triangle-derived semantic units.
# The panels only express two phase pieces of an original rectangular source.
unit = dict(id='Draw02/fixed-car-full-period-phase-quarter-unpainted',
    sourceCid=old['sourceCid'], sourceDigest=old['sourceDigest'], min=lo, max=hi,
    physicalSize=[3, 4], repeatAxes='V', sourceGradientSampling=True,
    evidence=dict(path=str(plan_path), sha256=hashlib.sha256(plan_path.read_bytes()).hexdigest()),
    descriptor=old['descriptor'], panels=panels,
    channels=[dict(name=c['name'], cid=c['cid'], path=c['path'], sha256=c['sha256']) for c in old['channels']])
(OUT/'phase-trial-manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3', style='Draw02', units=[unit]), indent=2)+'\n')
checks = []
for r in plan['rows']:
    if r['part'] < 2:
        continue
    # New continuous phase q = old normalized phase + 1/4. No vertex-local frac.
    gradient = r['v'][0]/(hi[1]-lo[1])
    offset = (r['v'][1]-lo[1])/(hi[1]-lo[1])+.25
    values = [gradient*r['end']*z+offset for z in (80, 82)]
    if r['part'] == 2:
        # Actual terminal silhouette can start after Z80 in LOD1. Both endpoints
        # remain well within the whole texture, so native finite clamping is safe.
        assert min(values) > 0 and max(values) < 1
    checks.append(dict(lod=r['lod'], end=r['end'], lane=r['lane'], part=r['part'],
        affineV=[gradient, offset], repeatV=r['repeatV'], finiteEndpointRange=values if r['part'] == 2 else None))
(OUT/'phase-trial-mapping.json').write_text(json.dumps(dict(periodMetres=4, phaseOffset=.25,
    mapping=checks, qualityAccepted=False, paintRemoved=False,
    warning='Panel sampling and native mip construction resample the source; no all-mip texel identity claim.'), indent=2)+'\n')
print('PASS: one whole 4 m period, four offline panels; all finite short V endpoints interior. Paint is not yet removed.')
