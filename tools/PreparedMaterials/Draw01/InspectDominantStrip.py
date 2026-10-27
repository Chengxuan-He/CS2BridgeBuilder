"""Inspect actual Draw01 domain 102 against its measured source axes and full mips."""
import hashlib
import json
import struct
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'artifacts/agent-material-env/Draw01/python-deps'))
import numpy as np
from PIL import Image

OUT = ROOT / 'artifacts/prepared-materials/Draw01/actual-retile'


def read(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def sample(pixels, uv, srgb):
    h, w = pixels.shape[:2]
    xy = np.asarray(uv) * [w, h] - .5
    lo = np.floor(xy).astype(int)
    t = xy - lo
    result = np.zeros(4)
    for dy in range(2):
        for dx in range(2):
            v = pixels[np.clip(lo[1]+dy, 0, h-1), np.clip(lo[0]+dx, 0, w-1)].astype(float)/255
            if srgb:
                v[:3] = np.where(v[:3] <= .04045, v[:3]/12.92, ((v[:3]+.055)/1.055)**2.4)
            result += v * (t[0] if dx else 1-t[0]) * (t[1] if dy else 1-t[1])
    return result


def main():
    native_path = ROOT / 'artifacts/uv-density/native/DrawBridge01 Mesh.npz'
    native = np.load(native_path)
    ids = native['mesh0_faces'][[164, 165]].reshape(-1)
    positions = native['mesh0_positions'][ids].astype(float)
    original_uv = native['mesh0_uv0'][ids, :2].astype(float)
    assert np.ptp(positions[:, 1]) == 0
    span = np.ptp(positions[:, 0])
    cases = read(OUT/'polar-transition.json')['domain102']
    weights = np.asarray([[1, 0, 0], [0, 1, 0], [0, 0, 1], [.173, .317, .51], [.793, .107, .1]])
    comparisons = []
    rows = []
    for case in cases:
        o = case['new']
        bounds = np.asarray(o['bounds'], dtype=np.float32).astype(float)
        q = np.asarray(o['points'], dtype=np.float32).astype(float)
        actual = bounds[:2] + q*(bounds[2:]-bounds[:2])
        expected = original_uv.copy()
        expected[:, 1] = bounds[1] + (original_uv[:, 1]-bounds[1])*(span+o['delta'])/span
        # This horizontal transverse bar keeps its Z width and material U axis.
        # V remains anchored at the original centreline and follows physical X length.
        error = np.max(np.abs(actual-expected))*4096
        assert error < .01, (o['delta'], error)
        rows.append(dict(delta=o['delta'], scenario=o['scenario'], status=case['after'],
                         uRange=[float(q[:, 0].min()), float(q[:, 0].max())],
                         vRange=[float(q[:, 1].min()), float(q[:, 1].max())],
                         source4096PixelError=float(error)))
        if case['after'] == 'original':
            for at in (0, 3):
                comparisons.extend(zip(weights@actual[at:at+3], weights@expected[at:at+3]))
    source_manifest = ROOT/'artifacts/agent-material-env/Draw01/repeat-units-control-corrected/extracted-source/manifest.json'
    channels = []
    for channel in read(source_manifest)['units'][0]['channels']:
        path = Path(channel['path'])
        raw = path.read_bytes()
        assert hashlib.sha256(raw).hexdigest() == channel['sha256']
        width, height = struct.unpack_from('<HH', raw, 2)
        assert struct.unpack_from('<H', raw)[0] == 5 and raw[9] in (108, 109)
        assert struct.unpack_from('<i', raw, 14)[0] == -1
        offset = 18
        levels = []
        for mip in range(raw[8]):
            w, h = max(1, width >> mip), max(1, height >> mip)
            size = ((w+3)//4)*((h+3)//4)*16
            pixels = np.asarray(Image.frombytes('RGBA', (w, h), raw[offset:offset+size], 'bcn', (7, 'BC7')))
            offset += size
            error = max(float(np.max(np.abs(sample(pixels, a, raw[9] == 108)-sample(pixels, b, raw[9] == 108)))) for a, b in comparisons)
            profile = np.asarray([[sample(pixels, [u, v], raw[9] == 108)
                                   for v in np.linspace(bounds[1], bounds[3], 33)]
                                  for u in np.linspace(bounds[0], bounds[2], 9)])
            levels.append(dict(mip=mip, width=w, height=h, maxLinearComponentError=error,
                               candidateVBoundaryMaxComponentDifference=float(np.max(np.abs(profile[:, 0]-profile[:, -1]))),
                               sourceVMaxComponentRange=float(np.max(np.ptp(profile, axis=1)))))
        assert offset == len(raw)
        channels.append(dict(name=channel['name'], sourceSha256=channel['sha256'], mips=levels))
    report = dict(status='PASS', geometrySha256=hashlib.sha256(native_path.read_bytes()).hexdigest(),
                  sourceSpanMetres=float(span), cases=rows, samplesPerMip=len(comparisons), channels=channels,
                  scope='Actual emitted coordinates compared with measured X-length/Z-width material axes. Full existing BC7 mip decoding and CPU bilinear sampling only for directly reusable contraction cases; not native rendering or arbitrary positive extension acceptance.',
                  normalOrientation='The measured source plane is horizontal and its material axes remain Z/X under pure X scaling. The corrected polar frame requires no in-plane normal/tangent rotation for this case; no pixel normal encoding is changed.',
                  periodEvidence='Source V profiles and endpoint differences are diagnostic only. No independently authored repeat period or base/detail decomposition has been identified for this finite steel strip; its receiver chart is not a unit definition.',
                  pixelsBaked=0)
    (OUT/'dominant-strip-phase-mips.json').write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(dict(status=report['status'], cases=len(rows), channels=len(channels),
                         channelMips=sum(len(c['mips']) for c in channels), samplesPerMip=len(comparisons),
                         maxSource4096PixelError=max(r['source4096PixelError'] for r in rows),
                         maxLinearComponentError=max(m['maxLinearComponentError'] for c in channels for m in c['mips']))))


if __name__ == '__main__':
    main()
