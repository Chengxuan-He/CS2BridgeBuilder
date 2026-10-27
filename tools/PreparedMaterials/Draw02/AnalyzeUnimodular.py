"""CPU-only Draw02 lattice study; never changes or encodes material assets."""
import argparse
import hashlib
import itertools
import json
from pathlib import Path
import struct
import numpy as np


def density(j):
    # Same conservative local metric as MovableTextureFootprint, in binary32.
    j = np.asarray(j, dtype=np.float32)
    lengths = np.sqrt(np.sum(j*j, axis=0, dtype=np.float32))
    cosine = abs(np.dot(j[:, 0], j[:, 1]) / (lengths[0]*lengths[1]))
    factor = np.sqrt(np.float32(1) + min(np.float32(1), cosine))
    return np.maximum(4, 4*np.ceil(lengths*factor/np.float32(4))).astype(int)


def gauss(j):
    m = np.eye(2, dtype=np.int64)
    for _ in range(50):
        b = j @ m
        if np.dot(b[:, 1], b[:, 1]) < np.dot(b[:, 0], b[:, 0]):
            m = m[:, [1, 0]]
            b = j @ m
        k = round(np.dot(b[:, 0], b[:, 1])/np.dot(b[:, 0], b[:, 0]))
        if not k:
            return m
        m[:, 1] -= k*m[:, 0]
    raise ValueError('Lattice reduction did not terminate')


def fold(q):
    p = q - np.floor(q)
    reverse = np.sum(p, axis=-1) > 1
    return np.where(reverse[..., None], 1-p, p), reverse


def native_binding_sizes(root, key):
    """Read descriptor references and actual existing native Texture headers."""
    data = (root/'Structure'/key/'Structure.Surface').read_bytes()
    if struct.unpack_from('<H', data)[0] != 1 or data[6] != 0:
        raise ValueError('Expected native non-VT structural Surface')
    at = 7
    def count():
        nonlocal at
        marker = data[at]; at += 1
        if marker == 0: return 0
        if marker != 255: raise ValueError('Bad map marker')
        n = struct.unpack_from('<i', data, at)[0]; at += 4
        return n
    def text():
        nonlocal at
        length = shift = 0
        while True:
            b = data[at]; at += 1; length |= (b & 127) << shift
            if not b & 128: break
            shift += 7
        value = data[at:at+length].decode(); at += length
        return value
    for stride in (4, 4, 16, 16):
        for _ in range(count()): text(); at += stride
    result = []
    for _ in range(count()):
        name = text(); marker = data[at]; at += 1
        if marker != 255: raise ValueError('Missing structural channel')
        cid = ''.join(f'{b & 15:x}{b >> 4:x}' for b in data[at:at+16]); at += 16
        path = root/'Shared'/(cid+'.Texture')
        raw = path.read_bytes()
        if struct.unpack_from('<H', raw)[0] != 5: raise ValueError('Texture version differs')
        width, height = struct.unpack_from('<HH', raw, 2)
        result.append(dict(channel=name,cid=cid,width=width,height=height,bytes=len(raw)))
    return result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('output', type=Path)
    args = p.parse_args()
    inventory_path = args.repository/'artifacts/uv-density/structure-tessellated-sources.json'
    inventory = json.loads(inventory_path.read_text())
    native = json.loads((args.repository/'artifacts/prepared-materials/Draw02/unimodular-native-inputs.json').read_text())
    sizes = {s['cid']: [(l['width'], l['height']) for d in s['backing'] for l in d['layers']]
             for s in native['sources']}
    units = [u for u in inventory['units'] if u['style'] == 'Draw02']
    triangles = []
    for u in units:
        if not u['triangle']:
            continue
        a, b, c = [np.array(u[k], dtype=np.float32) for k in ('a', 'b', 'c')]
        j = np.stack((b-a, c-a), axis=1)
        channel_j = [np.array(size, dtype=np.float32)[:, None]*j for size in sizes[u['sourceCid']]]
        size = np.max([density(x) for x in channel_j], axis=0)
        triangles.append((u, j, channel_j, size, np.linalg.cond(channel_j[0].astype(float))))
    selected = []
    for row in sorted(triangles, key=lambda t: int(np.prod(t[3])), reverse=True)[:3] + sorted(triangles, key=lambda t: t[4], reverse=True)[:3]:
        if row[0]['key'] not in [r[0]['key'] for r in selected]:
            selected.append(row)
    small = [np.array([[a, b], [c, d]], dtype=np.int64)
             for a, b, c, d in itertools.product(range(-8, 9), repeat=4) if abs(a*d-b*c) == 1]
    rng = np.random.default_rng(4261)
    points = rng.uniform(-32, 32, (20000, 2)).astype(np.float32)
    # Synthetic Float32 stress coordinates, not measured receiver vertices.
    seam = np.array([[x, y] for x in (0, 1, -1, 8) for y in (.2, .7)] +
                    [[.2, .8], [.7, .3], [1e-7, .7], [-1e-7, .7]], dtype=np.float32)
    points = np.concatenate((points, seam))
    rows = []
    for u, j, channels, old_size, condition in selected:
        bindings = native_binding_sizes(args.repository/'artifacts/prepared-current-structures-canonical/Draw02', u['key'])
        if any([b['width'], b['height']] != old_size.tolist() for b in bindings):
            raise ValueError('Metric estimate does not match current prepared texture dimensions')
        reduced = gauss(channels[0].astype(float))
        candidates = small + [reduced @ m for m in small if np.max(np.abs(m)) <= 2]
        def score(m):
            new_size = np.max([density(x.astype(float) @ m) for x in channels], axis=0)
            return (int(np.prod(new_size)), int(np.max(np.abs(m))), tuple(new_size)), new_size
        m = min(candidates, key=lambda m: score(m)[0])
        proposed = score(m)[1]
        inverse = np.rint(np.linalg.inv(m)).astype(np.int64)
        assert abs(round(np.linalg.det(m))) == 1 and np.array_equal(m @ inverse, np.eye(2, dtype=int))
        old_fold, old_parity = fold(points)
        new_q = (points @ inverse.T.astype(np.float32)).astype(np.float32)
        reconstructed = (new_q @ m.T.astype(np.float32)).astype(np.float32)
        new_fold, new_parity = fold(reconstructed)
        displacement = (new_fold-old_fold) @ channels[0].T
        errs = np.linalg.norm(displacement, axis=1)
        bad = int(np.argmax(errs))
        q_prime = np.array([.2, .3])
        correct_fold, _ = fold(m @ q_prime)
        naive_fold, _ = fold(q_prime)
        correct_uv = np.array(u['a'], dtype=np.float32) + j @ correct_fold
        naive_uv = np.array(u['a'], dtype=np.float32) + j @ m @ naive_fold
        rows.append(dict(key=u['key'], sourceCid=u['sourceCid'], a=u['a'], b=u['b'], c=u['c'],
                         currentNativeBindings=bindings,
                         nativeChannelDimensions=sizes[u['sourceCid']], oldSize=old_size.tolist(),
                         proposedLocalDensitySize=proposed.tolist(), matrix=m.tolist(), inverse=inverse.tolist(),
                         localPixelReduction=1-float(np.prod(proposed))/float(np.prod(old_size)),
                         condition=float(condition), sourcePixelParallelogramArea=abs(float(np.linalg.det(channels[0]))),
                         float32Stress=dict(samples=len(points),maxSourcePixelDisplacement=float(errs[bad]),
                                            parityChanges=int(np.count_nonzero(old_parity != new_parity)),
                                            worstInput=points[bad].tolist(),worstReconstructed=reconstructed[bad].tolist(),
                                            maxUnitCoordinateError=float(np.max(np.abs(reconstructed-points)))),
                         wrongTriangleReparameterization=dict(qPrime=q_prime.tolist(), correctSourceUv=correct_uv.tolist(),
                                                             naiveSourceUv=naive_uv.tolist(), sourcePixelDisplacement=float(np.linalg.norm((correct_uv-naive_uv)*sizes[u['sourceCid']][0])))))
    # Pixel filters do not commute with a shear, even if ideal T(M M^-1 q)=T(q).
    # Old 2x2 mip kernel acts along axes. Shear maps frequency (1,0) to (1,-1).
    n = 4
    report = dict(scope='CPU analytical sampling/density feasibility; no image encoding or new assets.',
                  inventory=str(inventory_path.relative_to(args.repository)),inventorySHA256=hashlib.sha256(inventory_path.read_bytes()).hexdigest(),
                  currentStyleUnits=len(units),currentTriangleUnits=len(triangles),
                  candidateSearch='GL(2,Z) coefficients [-8,8], plus local [-2,2] changes around a Gauss-reduced basis; not a global optimum proof.',
                  selected=rows,
                  mipCounterexample=dict(matrix=[[1,-1],[0,1]],frequency=[1,0],size=n,
                                         oldAmplitude=float(np.cos(np.pi/n)),newAmplitude=float(np.cos(np.pi/n)**2)),
                  decision='Potential local-density allocation reductions exist, but not a drop-in equivalent native bilinear/mip implementation. Exact torus composition and transformed fold/normal parity are mandatory; unchanged native filtering is not invariant under shear.')
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2)+'\n')
    print(json.dumps(dict(currentStyleUnits=len(units),currentTriangleUnits=len(triangles),samples=len(rows),
                          reductions=[r['localPixelReduction'] for r in rows],output=str(args.output))))


if __name__ == '__main__':
    main()
