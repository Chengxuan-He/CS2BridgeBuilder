"""Deduplicate evidenced atlas units and measure original normal seam limits.

This is a producer preflight, not a filtered-material acceptance certificate.
Finite terminals keep their original Surface; only the repeated interiors enter.
"""
import hashlib
import json
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
EVIDENCE = ROOT / 'artifacts/material-remap/Draw02/near-cells'
OUT = ROOT / 'artifacts/material-remap/Draw02/offline-atlas-units'


def read(name):
    return json.loads((EVIDENCE / name).read_text())


def sample(image, u, v):
    h, w, _ = image.shape
    x, y = np.asarray(u) * w - .5, np.asarray(v) * h - .5
    ix, iy = np.floor(x).astype(int), np.floor(y).astype(int)
    fx, fy = x - ix, y - iy
    a = image[np.clip(iy, 0, h-1), np.clip(ix, 0, w-1)]
    b = image[np.clip(iy, 0, h-1), np.clip(ix+1, 0, w-1)]
    c = image[np.clip(iy+1, 0, h-1), np.clip(ix, 0, w-1)]
    d = image[np.clip(iy+1, 0, h-1), np.clip(ix+1, 0, w-1)]
    return (a*(1-fx[..., None])+b*fx[..., None])*(1-fy[..., None]) + (c*(1-fx[..., None])+d*fx[..., None])*fy[..., None]


def normal(rgba):
    xy = np.stack((rgba[..., 0]*rgba[..., 3], rgba[..., 1]), axis=-1)*2-1
    xyz = np.concatenate((xy, np.sqrt(np.maximum(0, 1-np.sum(xy*xy, axis=-1)))[..., None]), axis=-1)
    return xyz/np.linalg.norm(xyz, axis=-1)[..., None]


def main():
    units = {}
    def add(lo, hi, use):
        key = tuple(lo)+tuple(hi)
        if key not in units:
            extent = (np.array(hi)-lo)*4096
            size = (np.ceil(extent/4)*4).astype(int)
            identity = hashlib.sha256(json.dumps(key).encode()).hexdigest()
            units[key] = dict(key=identity, sourceSurface='8a28ab8d9526e258efbcb81c3bbc936b',
                minimum=lo, maximum=hi, sourceExtentPixels=extent.tolist(),
                contentGrid=size.tolist(), periodMetresV=4, repeatU=False, repeatV=True,
                finiteU=True, uses=[], normalSeamMips=[])
        units[key]['uses'].append(use)
    for level in read('native-atlas-wrap-plan.json'):
        for d in level['domains']:
            add(*d['sourceBounds'], dict(kind='moving-carriageway', lod=level['lod'], domain=d['index'], family=d['family']))
    for i, d in enumerate(read('walk-atlas-plan.json')):
        add(d['sourceMinimum'], d['sourceMaximum'], dict(kind='moving-sidewalk', domain=i, lod=d['lod']))
    for d in read('fixed-walk-atlas-plan.json'):
        if d['repeatV']:
            p = d['profile']
            add(p[4:6], p[6:8], dict(kind='fixed-sidewalk', domain=d['domain'], lod=d['lod']))
    # Finite transverse ranges are views of one source, not new content units.
    # Consolidate the same original V cycle, retaining absolute U in each view.
    groups = {}
    for unit in units.values():
        key = (unit['minimum'][1], unit['maximum'][1])
        groups.setdefault(key, []).append(unit)
    consolidated = []
    for key, views in groups.items():
        lo = [min(v['minimum'][0] for v in views), key[0]]
        hi = [max(v['maximum'][0] for v in views), key[1]]
        extent = (np.asarray(hi)-lo)*4096
        consolidated.append(dict(minimum=lo, maximum=hi,
            sourceExtentPixels=extent.tolist(), contentGrid=(np.ceil(extent/4)*4).astype(int).tolist(),
            repeatU=False, repeatV=True, periodMetresV=4,
            views=[dict(key=v['key'], minimum=v['minimum'], maximum=v['maximum']) for v in views],
            note='One shared finite-U payload per source V cycle; view-specific original U projection remains separate. Unused transverse texels are original atlas samples, not a new repeated transverse pattern.'))
    for mip in range(13):
        size = max(1, 4096 >> mip)
        path = EVIDENCE / 'all-source-mips' / '_NormalMap' / f'{mip}.rgba32f'
        image = np.fromfile(path, dtype=np.float32).reshape(size, size, 4).astype(float)
        for unit in units.values():
            lo, hi = unit['minimum'], unit['maximum']
            u = np.linspace(lo[0], hi[0], max(33, int(np.ceil((hi[0]-lo[0])*size))*2+1))
            low = normal(sample(image, u, np.full_like(u, lo[1])))
            high = normal(sample(image, u, np.full_like(u, hi[1])))
            jump = np.linalg.norm(low-high, axis=-1)
            unit['normalSeamMips'].append(dict(mip=mip, samples=len(u),
                maxDecodedNormalJump=float(jump.max()),
                unavoidableContinuousBoundaryErrorLowerBound=float(jump.max()/2),
                sourceDecodedSha256=hashlib.sha256(path.read_bytes()).hexdigest()))
    for name in ('_BaseColorMap', '_MaskMap', '_ControlMask'):
        for mip in range(13):
            size = max(1, 4096 >> mip)
            path = EVIDENCE/'all-source-mips'/name/f'{mip}.rgba32f'
            image = np.fromfile(path, dtype=np.float32).reshape(size, size, 4).astype(float)
            for unit in units.values():
                lo, hi = unit['minimum'], unit['maximum']
                u = np.linspace(lo[0], hi[0], max(33, int(np.ceil((hi[0]-lo[0])*size))*2+1))
                low = sample(image, u, np.full_like(u, lo[1]))
                high = sample(image, u, np.full_like(u, hi[1]))
                unit.setdefault('channelSeamMips', []).append(dict(channel=name, mip=mip,
                    maxComponentJump=np.max(np.abs(low-high), axis=0).tolist(),
                    unavoidableContinuousBoundaryErrorLowerBound=(np.max(np.abs(low-high), axis=0)/2).tolist(),
                    sourceDecodedSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                    space='GPU-decoded float32 linear RGBA; base RGB sampled through native sRGB source; no UNorm8 readback quantization'))
    result = dict(schema='Draw02OfflineAtlasUnits/1', units=list(units.values()), consolidatedUnits=consolidated,
        readyForProduction=False,
        interpretation='Original authored UV reset has two one-sided samples. A continuous filtered Repeat resource has one seam value; half their distance is a necessary boundary error lower bound, not an accepted tolerance. No normal reconstruction, compression or filter error has been hidden by this measurement.',
        remaining=['all channels and alpha', 'guard panels and native sampler', 'original mip LOD correspondence', 'native resource production and cold load', 'owner and official package graphs'])
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/'plan.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(dict(views=len(units), contentUnits=len(consolidated), uses=sum(len(x['uses']) for x in units.values()),
        maxNormalBoundaryLowerBound=max(y['unavoidableContinuousBoundaryErrorLowerBound'] for x in units.values() for y in x['normalSeamMips']))))


if __name__ == '__main__':
    main()
