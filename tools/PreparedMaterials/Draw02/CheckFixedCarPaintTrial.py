"""Check actual paired native units; operator correctness is not unit quality."""
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
units = json.loads((OUT/'paint-producer-report.json').read_text())['units']
recipe = json.loads((OUT/'paint-composition-recipe.json').read_text())
minimum = np.array(recipe['sourceRegion']['min'], np.float32)
extent = np.array(recipe['sourceRegion']['max'], np.float32)-minimum
rows = []; outside_count = 0
for base, painted in zip(units[0]['channels'], units[1]['channels']):
    assert base['name'] == painted['name']
    assert (base['width'], base['height'], base['mips']) == (painted['width'], painted['height'], painted['mips'])
    name = base['name']; control = name == '_ControlMask'
    if control: assert base['cid'] == painted['cid']
    for mip in range(base['mips']):
        width, height = max(1, base['width'] >> mip), max(1, base['height'] >> mip)
        a = np.fromfile(OUT/f'paint-decoded/control{name}/{mip}.rgba32f', '<f4').reshape(height, width, 4)
        b = np.fromfile(OUT/f'paint-decoded/painted{name}/{mip}.rgba32f', '<f4').reshape(height, width, 4)
        qx = np.clip(2*(np.arange(width, dtype=np.float32)+.5)/width-.5, 0, 1)
        qy = (np.arange(height, dtype=np.float32)+.5)/height
        u = minimum[0]+qx*extent[0]
        v = minimum[1]+np.where(qy < .25, qy+.75, qy-.25)*extent[1]
        lods = base['sourceLodByMip'][mip*4:mip*4+4]
        assert len(set(lods)) == 1
        level = min(12., max(0., lods[0])); lower = int(np.floor(level)); upper = min(12, lower+1)
        mask = np.zeros((height, width), bool)
        if not control:
            for source_mip in {lower, upper}:
                for entry in recipe['sourceMipRules'][source_mip]['masks']:
                    x0, y0, x1, y1 = np.array(entry['sourceUvRect'], np.float32)
                    mask |= ((v[:, None] >= y0) & (v[:, None] < y1) & (u[None, :] >= x0) & (u[None, :] < x1))
        changed = np.any(a.view('<u4') != b.view('<u4'), axis=2)
        assert not np.any(changed & ~mask), (name, mip, np.argwhere(changed & ~mask)[:8])
        count = int(np.sum(~mask)); outside_count += count
        rows.append(dict(channel=name, mip=mip, sourceLod=level, outsideMaskPixels=count,
            changedPixels=int(changed.sum()), controlPayloadIdentical=control))
assert any(r['changedPixels'] > 0 for r in rows if r['channel'] == '_BaseColorMap')
report = dict(passed=True, outsideMaskChannelPixels=outside_count,
    controlPayloadIdentical=True, rows=rows, paintedKey=units[1]['key'],
    status='Actual native-output mask isolation and Control sharing passed. Original-reference filtering, image quality and full owner/package budgets are not accepted by this check.')
(OUT/'paint-unit-isolation-check.json').write_text(json.dumps(report, indent=2)+'\n')
print('PASS actual four-channel/11-mip paired outputs:', outside_count, 'outside-mask channel pixels bit-identical; Control payload identical.')
