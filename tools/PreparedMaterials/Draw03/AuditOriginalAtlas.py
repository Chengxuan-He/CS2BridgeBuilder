"""Read-only original-channel paint audit and exact native compression measurement."""
import ctypes
import json
import zipfile
import io
import struct
import numpy as np
from ComposeRoadPeriods import PROOF, ROOT, CIDS, NAMES, Writer, digest, patch_donors
from InspectWearSources import bc7, GAME
from PIL import Image


def paint_caches():
    matches=[]
    for path in (ROOT/'assets/Materials/Shared/MidMips').rglob('*.MidMips'):
        raw=path.read_bytes()
        stream=io.BytesIO(raw)
        def read(fmt):
            return struct.unpack('<'+fmt,stream.read(struct.calcsize('<'+fmt)))
        tile,levels,group,stacks,groups,entries=read('6i')
        categories=[]
        for _ in range(stacks):
            for _ in range(read('i')[0]):
                width,height,n,first,tiles,disabled,pad=read('5i?i')
                categories.append((n,disabled))
        members=set()
        def identity():
            return ''.join(f'{b:02x}'[::-1] for b in stream.read(16))
        for n,disabled in categories:
            if disabled: continue
            for _ in range(n):
                members.add(identity())
                mask,ordinal,duplicates=read('ihi')
                for _ in range(duplicates): members.add(identity())
                for _ in range(levels): read('ii')
        tiles=read('i')[0]
        alignment=stream.tell()
        assert read('q')[0]==alignment
        assert stream.tell()+tiles*(tile+16)**2*5//4==len(raw)
        if 'd765f1348645cc68318b5ac29c61abde' in members:
            matches.append(dict(path=str(path.relative_to(ROOT)),bytes=len(raw)+32,
                sha256=digest(raw),memberCount=len(members),levels=levels))
    return matches


def main():
    output = ROOT / 'artifacts/material-remap/Draw03/original-atlas-audit'
    output.mkdir(parents=True, exist_ok=True)
    scan = json.loads((PROOF / 'authoring-texture-scan.json').read_text())
    originals, pixels = [], []
    for cid in CIDS:
        entry = next(row for row in scan['found'] if row['cid'] == cid)
        with zipfile.ZipFile(entry['path']) as archive:
            raw = archive.read(entry['entry'])
        assert digest(raw) == entry['sha256']
        originals.append(raw)
        pixels.append(next(bc7(raw))[1])
    rows = []
    for name, bottom, top in [('lane', 802, 1210), ('gap', 786, 850)]:
        roi = pixels[0][bottom:top, 3084:4052]
        seeds = np.all(roi[..., :3] > 180, axis=2)
        mask = np.zeros_like(seeds)
        for y, x in zip(*np.where(seeds)):
            by = (bottom + y) // 4 * 4 - bottom
            bx = x // 4 * 4
            mask[max(0, by-4):min(len(roi), by+8), max(0, bx-4):min(968,bx+8)] = True
        dy, dx, patches = patch_donors(mask, pixels[0], bottom)
        channels = []
        for prop, image in zip(NAMES, pixels):
            region = image[bottom:top, 3084:4052]
            before, donor = region[seeds], image[dy[seeds], dx[seeds]]
            delta = np.abs(before.astype(int)-donor.astype(int))
            channels.append(dict(property=prop, seedMean=before.mean(0).tolist(),
                donorMean=donor.mean(0).tolist(), meanAbsDelta=delta.mean(0).tolist(),
                maximumAbsDelta=delta.max(0).tolist(), identicalPixels=int(np.all(delta==0,axis=1).sum())))
            Image.fromarray(region).save(output / (name + prop + '.png'))
        rows.append(dict(domain=name,seedPixels=int(seeds.sum()),channels=channels,patches=patches))
    codec = Writer(GAME, output)
    compression=[]
    for level in (11, 18, 22):
        raw=originals[0][14:]
        bound=codec.bound(len(raw),0)
        encoded=ctypes.create_string_buffer(bound)
        size=codec.encode(raw,len(raw),encoded,bound,0,level)
        assert 0<size<=bound
        restored=ctypes.create_string_buffer(len(raw))
        assert codec.decode(encoded,size,restored,len(raw),0)==len(raw)
        assert restored.raw==raw
        compression.append(dict(level=level,textureAndCidBytes=size+18+32,lossless=True))
    report=dict(originalBaseCompression=compression,paintChannelStatistics=rows,paintCacheClosure=paint_caches(),
        interpretation='Different normal/mask donor values do not alone prove paint affects these channels; inspect spatial correlation and native source before selecting unchanged references.',
        reachability='No unused atlas block deletion is authorized by this audit. Native shader sample_b uses cb0[79].y; its allowed range and anisotropic footprint require proof before any unreachable-region optimization.')
    (output/'audit.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps(report))


if __name__=='__main__':
    main()
