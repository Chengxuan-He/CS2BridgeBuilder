"""Check all authored road charts and the actual candidate-33 owner's road UVs.

This deliberately tests the whole moving-road domain, not just end caps.
Run with the existing Draw01 Python dependencies on PYTHONPATH.
"""
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

from StageCleanCap import ROOT, sha
from StageCleanFarCaps import levels

OUT = ROOT / 'artifacts/material-remap/Draw01/yellow-line33'
OWNER = ROOT / 'artifacts/material-remap/shared/draw01-yellow-line33'


def yellow(image):
    rgb = image.astype(int)
    return (rgb[:, :, 0] - rgb[:, :, 2] > 20) & (rgb[:, :, 1] - rgb[:, :, 2] > 10)


def coverage(polygons, width):
    image = Image.new('1', (width, width))
    draw = ImageDraw.Draw(image)
    for polygon in polygons:
        draw.polygon([tuple(p) for p in np.asarray(polygon) * width - .5], fill=1)
    return np.asarray(image)


def main():
    report = json.loads((OUT / 'clean-far-report.json').read_text())
    channel = report['channels'][0]
    raw = (OUT / 'staged/Draw01/Shared' / (channel['cid'] + '.Texture')).read_bytes()
    assert sha(raw) == channel['sha256']
    original = json.loads((OUT / 'far-cap-source-evidence.json').read_text())['source']['channels'][0]
    before = list(levels(Path(original['path']).read_bytes()))
    after = list(levels(raw))
    groups = json.loads((ROOT / 'artifacts/material-remap/Draw01/far-appearance.json').read_text())['groups']
    groups = [g for g in groups if 'fixed-sidewalk' not in g['group']]
    assert len(groups) == 12
    owner_path = OWNER / 'Mechanism LOD2 Appearance Group 1.puv.json'
    owner = json.loads(owner_path.read_text())
    road = [m for m in owner['meshes'] if m['slot'] in (1, 2)]
    assert [len(m['indices']) // 3 for m in road] == [64, 466]
    triangles = []
    for mesh in road:
        uv = np.asarray(mesh['uv'])[:, :2]
        triangles.extend(uv[tri] for tri in np.asarray(mesh['indices']).reshape(-1, 3))
    checks = []
    for name, polygons in [('actualOwnerSlots1And2', triangles)] + [(g['group'], [g['uv']]) for g in groups]:
        result = []
        for mip, (old, new) in enumerate(zip(before, after)):
            mask = coverage(polygons, new.shape[0])
            remaining = int((yellow(new) & mask).sum())
            assert remaining == 0, (name, mip, remaining)
            result.append(dict(mip=mip, sampledPixels=int(mask.sum()), originalYellowPixels=int((yellow(old) & mask).sum()), remainingYellowPixels=remaining))
        checks.append(dict(domain=name, levels=result))
    assert all(level['outsideMaskBitExact'] for c in report['channels'] for level in c['levels'])
    result = dict(status='PASS', owner='bd3ca3559-c4c6-4153-9899-5dbc3efd9c19', ownerGeometryEvidenceSha256=sha(owner_path.read_bytes()), actualRoadTriangles=530, authoredRoadCharts=12, mips=10, checks=checks, geometryChanges=0, nativeVisualAcceptance=False)
    (OUT / 'full-road-validation.json').write_text(json.dumps(result, indent=2) + '\n')
    print('PASS: 530 actual road triangles, all 12 source road charts, all 10 mips; no remaining yellow-class pixels.')


if __name__ == '__main__':
    main()
