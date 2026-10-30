"""Measure complete native half-floor units, preserving the near panel breakpoint."""
import hashlib
import json
from pathlib import Path
import numpy as np
from PrepareNativeObjects import ROOT, GAME
from VerifyDensity import native_bytes, hash_text


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    out = ROOT / 'artifacts/material-remap/Lift01/half-floor'
    inputs = out / 'inputs'
    inputs.mkdir(parents=True, exist_ok=True)
    old = json.loads((ROOT / 'artifacts/prepared-materials/Lift01/repeat-units-v2/manifest.json').read_text())['units'][0]
    channels = []
    for source in old['sourceChannels']:
        if source['kind'] == 'absent':
            continue
        vt = native_bytes(GAME, source['cid'], 'VT')
        assert sha(vt) == source['sha256']
        cid = hash_text(vt[3:19])
        raw = native_bytes(GAME, cid, 'Blob')
        path = inputs / (cid + '.Texture')
        path.write_bytes(raw)
        channels.append(dict(name=source['name'], cid=cid, path=str(path), sha256=sha(raw)))
    raw = native_bytes(GAME, old['sourceCid'], 'Blob')
    assert sha(raw) == old['sourceSha256']
    descriptor = inputs / 'Original.Surface'
    descriptor.write_bytes(raw)
    units = []
    for lod, suffix, ids in [(0, '', [259058, 259059, 259060, 259061]), (1, '_LOD1', [165669, 165670])]:
        path = ROOT / 'artifacts/uv-density/native' / ('LiftBridge01' + suffix + ' Mesh.npz')
        mesh = np.load(path)
        faces = mesh['mesh0_faces'][ids]
        positions, uv = mesh['mesh0_positions'][faces], mesh['mesh0_uv0'][faces, :2]
        assert np.all(positions[:, :, 1] == 9.800048828125)
        assert np.array_equal(positions[:, :, 0].min(), -3) and positions[:, :, 0].max() == 0
        assert positions[:, :, 2].min() == 68 and positions[:, :, 2].max() == 72
        evidence = dict(lod=lod, npzSha256=sha(path.read_bytes()), faces=ids,
                        positions=positions.tolist(), uv=uv.tolist(), finiteU=True, repeatedVMetres=4,
                        domain='Complete authored half floor; near inner 2 m plus outer 1 m, mid merged 3 m.')
        evidence_path = inputs / ('evidence-' + str(lod) + '.json')
        evidence_path.write_text(json.dumps(evidence, indent=2))
        bands = []
        # Each original pair is a separate affine panel, not a newly detected tile.
        for pair in range(0, len(ids), 2):
            p, t = positions[pair:pair+2].reshape(-1, 3), uv[pair:pair+2].reshape(-1, 2)
            bands.append(dict(start=float(-p[:, 0].max()/3), end=float(-p[:, 0].min()/3),
                              min=t.min(axis=0).tolist(), max=t.max(axis=0).tolist(), repeatY=1))
        bands.sort(key=lambda b: b['start'])
        unit = dict(id='Lift01-half-floor-lod' + str(lod), sourceCid=old['sourceCid'], sourceDigest=old['sourceSha256'],
                    min=uv.reshape(-1, 2).min(axis=0).tolist(), max=uv.reshape(-1, 2).max(axis=0).tolist(),
                    physicalSize=[3, 4], repeatAxes='V', cutout=True, bands=bands,
                    descriptor=dict(path=str(descriptor), sha256=sha(raw)), channels=channels,
                    evidence=dict(path=str(evidence_path), sha256=sha(evidence_path.read_bytes())), aliases=[])
        units.append(unit)
    (inputs / 'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift01', units=units), indent=2))
    print(inputs / 'manifest.json')


if __name__ == '__main__':
    main()
