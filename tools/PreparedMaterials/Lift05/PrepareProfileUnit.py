"""Evidence and one-time input for the complete native LOD1 crossbeam period."""
import copy
import hashlib
import json
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, inputs, domains
from shapely.geometry import Polygon, box
from shapely.ops import unary_union


def ref(path):
    return dict(path=str(path), sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    out = ROOT / 'artifacts/material-resume/Lift05/profile-unit'
    (out / 'input').mkdir(parents=True, exist_ok=True)
    name, mesh, positions, uv, faces = next(v for v in inputs() if v[:2] == ('LiftBridge05_LOD1 Mesh', 0))
    source_faces = [9892, 9893]
    adjacent_faces = [9434, 9435]
    lo = [0.6162109375, 0.890625]
    hi = [0.748046875, 0.8955078125]
    native = uv[faces[source_faces]].astype(float)
    support = unary_union([Polygon(t) for t in native])
    assert support.equals(box(*lo, *hi))
    # The neighbouring authored panel ends where this one starts. Its U jumps
    # by exactly one measured period at identical XYZ and V, positive evidence
    # of actual native repetition rather than a convenient texture crop.
    points = {}
    for group, fs in [('unit', source_faces), ('adjacent', adjacent_faces)]:
        for vertex in np.unique(faces[fs]):
            points.setdefault(tuple(float(x) for x in positions[vertex]), {})[group] = uv[vertex].astype(float).tolist()
    seam = []
    for p, maps in points.items():
        if len(maps) != 2:
            continue
        a, b = maps['adjacent'], maps['unit']
        assert a[1] == b[1] and abs(a[0] - b[0]) == hi[0] - lo[0]
        seam.append(dict(position=list(p), adjacentUv=a, unitUv=b, nativePeriodJump=1))
    assert len(seam) >= 1
    period = float(np.ptp(positions[faces[source_faces]][:, :, 0]))
    assert period == 4
    base_path = ROOT / 'artifacts/prepared-materials/Lift05/structure-groups/input/manifest.json'
    unit = copy.deepcopy(json.loads(base_path.read_text())['units'][0])
    # V is an authored finite profile. Its physical edge lengths differ because
    # the coarse panel is not rectangular in XYZ; only U has a physical period.
    p = positions[faces[source_faces]].reshape(-1, 3).astype(float)
    v_edge = float(np.linalg.norm(p[1] - p[2]))
    unit.update(id='Lift05.UpperCrossbeam.NativeProfile', min=lo, max=hi,
        physicalSize=[period, v_edge], repeatAxes='U', originalFaces=source_faces,
        measuredTexelsPerMetre=[135.0, None], runtimeTexelsPerMetre=[135.0, None],
        status='COMPLETE_NATIVE_PANEL_U_PERIOD_FINITE_V', aliases=[])
    evidence = dict(style='Lift05', render=name, materialSlot=mesh, sourceCid=unit['sourceCid'],
        sourceDigest=unit['sourceDigest'], nativeMesh=ref(ROOT / 'artifacts/uv-density/native' / (name + '.npz')),
        domainResource=ref(ROOT / 'assets/HardcodedNumbers/Lift05/UvDomain_LiftBridge05_LOD1_Mesh_0.bin'),
        sourceFaces=source_faces, sourceDomains=[8307, 8308],
        positions=positions[faces[source_faces]].tolist(), sourceUvs=native.tolist(),
        fullNativeRectangle=True, uvMin=lo, uvMax=hi, sourcePixels=[540, 20],
        repeatAxes=['U'], finiteAxes=['V'], physicalPeriodU=period,
        adjacentFaces=adjacent_faces, sharedPhysicalSeam=seam,
        finiteVPhysicalEdgeReference=v_edge,
        reuse='One complete-profile payload serves the old near strip as a V subrange and the measured LOD1 profile. The old near coverage rectangle does not expand.',
        nativeOutput='Native Repeat texture with finite-axis guards and per-use finite-V mapping; no texture-global per-axis override is assumed.',
        fullMipProvenance=ref(ROOT / 'artifacts/prepared-materials/Lift05/repeat-unit-trial-v2/extraction.json'),
        sourceChannels=unit['channels'])
    ep = out / 'source-evidence.json'
    ep.write_text(json.dumps(evidence, indent=2) + '\n')
    unit['evidence'] = ref(ep)
    for channel in unit['channels']:
        assert ref(Path(channel['path']))['sha256'] == channel['sha256']
    (out / 'input/manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift05', units=[unit]), indent=2) + '\n')
    print(json.dumps(dict(manifest=str(out / 'input/manifest.json'), sourcePixels=[540,20], seamVertices=len(seam))))


if __name__ == '__main__':
    main()
