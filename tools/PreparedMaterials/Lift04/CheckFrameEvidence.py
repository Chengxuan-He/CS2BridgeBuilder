"""Compare production Lift04 planar frames with their native axis-aligned gradients."""
import gzip
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/prepared-materials/Lift04/actual-retile'
native_path = ROOT / 'artifacts/uv-density/native/LiftBridge04 Mesh.npz'
native = np.load(native_path)
capture = json.loads((OUT / 'frame-evidence.json').read_text())
units = {}
with gzip.open(ROOT / 'artifacts/prepared-materials/shared-current/receivers/Lift04.units.ndjson.gz', 'rt') as stream:
    for line in stream:
        row = json.loads(line)
        if row['name'] == 'LiftBridge04 Mesh' and row['mesh'] == 0 and row['originalDomain'] in (3, 28):
            units[row['delta'], row['originalDomain']] = row

records = []
for case in capture['results']:
    rows = case['vertices']
    ids = [row['index'] for row in rows]
    source = np.array([row['source'] for row in rows], dtype='float32').astype(float)
    target = np.array([row['target'] for row in rows], dtype='float32').astype(float)
    uv = np.array([row['sourceUv'] for row in rows], dtype='float32').astype(float)
    mapped = np.array([row['mappedUv'] for row in rows], dtype='float32').astype(float)
    assert np.array_equal(source.astype('float32'), native['mesh0_positions'][ids])
    assert np.array_equal(uv.astype('float32'), native['mesh0_uv0'][ids])
    assert np.array_equal(source[:, 1:], target[:, 1:])
    # The native surface is exactly XY (domain 3) or XZ (domain 28), without
    # out-of-plane rotation. Solve the measured two-coordinate native gradient.
    axes = [0, 1] if case['domain'] == 3 else [0, 2]
    p = source[:, axes]
    affine = np.linalg.lstsq(np.column_stack((p, np.ones(len(p)))), uv, rcond=None)[0]
    assert np.max(np.abs(np.column_stack((p, np.ones(len(p)))) @ affine - uv)) < 1e-12
    expected = (uv + (target[:, axes] - source[:, axes]) @ affine[:2]).astype('float32')
    bounds = np.array(case['bounds'], dtype='float32').astype(float); spans = bounds[2:] - bounds[:2]
    normalized = (expected.astype(float) - bounds[:2]) / spans
    unit = units[case['delta'], case['domain']]
    current = np.array(unit['points'])
    frame_normalized = {i: np.array(row['normalizedUv']) for i, row in zip(ids, rows)}
    expected_current = np.array([frame_normalized[i] for face in case['indices'] for i in face])
    assert np.max(np.abs(current - expected_current)) < 1e-6
    def gradient(values):
        return np.linalg.lstsq(np.column_stack((target[:, axes], np.ones(len(p)))), values, rcond=None)[0][:2]
    original_gradient = affine[:2]
    current_gradient = gradient(mapped)
    expected_gradient = gradient(expected.astype(float))
    records.append(dict(domain=case['domain'], delta=case['delta'], axes=axes,
        sourceTargetYZUnchanged=True, fixtureNativeFloat32Parity=True,
        capturedUnitFrameParity=True, originalGradient=original_gradient.tolist(),
        productionGradient=current_gradient.tolist(), axisAnchoredFloat32Gradient=expected_gradient.tolist(),
        originalUvMetric=(original_gradient.T @ original_gradient).tolist(),
        productionUvMetric=(current_gradient.T @ current_gradient).tolist(),
        axisAnchoredUvMetric=(expected_gradient.T @ expected_gradient).tolist(),
        productionNormalizedURange=[float(current[:,0].min()),float(current[:,0].max())],
        axisAnchoredNormalizedURange=[float(normalized[:,0].min()),float(normalized[:,0].max())],
        axisAnchoredUvs=expected.tolist(), productionDomain=unit))

def sha(path): return hashlib.sha256(path.read_bytes()).hexdigest()
evidence = dict(style='Lift04', records=records,
    hashes={str(p.relative_to(ROOT)):sha(p) for p in [native_path, OUT/'frame-evidence.json',
        ROOT/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll', ROOT/'artifacts/uv-density/actual-plans.bin']},
    conclusion='Production frame preserves a UV metric while rotating the native planar pattern with a selected diagonal edge. These axis-aligned planes have unchanged Y/Z; a source-axis-anchored gradient keeps U in the finite profile for every captured width. This is evidence for a shared frame investigation, not a general mapping replacement.',
    limits='Only two exact native domains and four deltas. No native texture, mip, normal interpretation or persisted output acceptance is claimed. A shared fix must cover non-axis-aligned planes, real rotation and fragment paths without inventing a universal world-axis rule.',
    productionFilesChanged=False, textureBakes=0, localization='No runtime UI text changed.')
(OUT/'frame-orientation-evidence.json').write_text(json.dumps(evidence, indent=2)+'\n')
print(json.dumps([{k:r[k] for k in ('domain','delta','productionNormalizedURange','axisAnchoredNormalizedURange')} for r in records],indent=2))
