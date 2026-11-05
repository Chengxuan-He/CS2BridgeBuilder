"""Retain original cap BC7 blocks outside the measured paint/filter footprint.

Run after Draw01PrepareFiniteEnds.Run with yellow-line32/near-cap-manifest.json.
This stages resources only; the original atlas and formal materials are untouched.
"""
import ctypes as ct
import hashlib
import json
import struct
import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Draw01/yellow-line32'
OLD = ROOT / 'assets/Materials/Draw01'
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
LIB = ct.CDLL(str(GAME / 'Plugins/x86_64/PipelinePlugin.dll'))
for name, args in [('compress_calc_bound', [ct.c_int64, ct.c_int]),
                   ('compress_data', [ct.c_void_p, ct.c_int64, ct.c_void_p, ct.c_int64, ct.c_int, ct.c_int]),
                   ('decompress_data', [ct.c_void_p, ct.c_int64, ct.c_void_p, ct.c_int64, ct.c_int])]:
    fn = getattr(LIB, name)
    fn.argtypes, fn.restype = args, ct.c_int64


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def identity(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(raw).digest()[:16])


def cid_bytes(cid):
    return bytes(int(cid[i+1] + cid[i], 16) for i in range(0, 32, 2))


def decode(raw):
    w, h = struct.unpack_from('<HH', raw, 2)
    assert raw[:2] == b'\x05\x00' and raw[9] in (108, 109)
    n = sum(((max(1, w >> m)+3)//4)*((max(1, h >> m)+3)//4)*16 for m in range(raw[8]))
    count = struct.unpack_from('<i', raw, 14)[0]
    data = raw[18:]
    if count != -1:
        assert count == len(data)
        target = ct.create_string_buffer(n)
        assert LIB.decompress_data(data, len(data), target, n, 0) == n
        data = target.raw
    assert len(data) == n
    return data


def encode(header, raw):
    n = LIB.compress_calc_bound(len(raw), 0)
    target = ct.create_string_buffer(n)
    count = LIB.compress_data(raw, len(raw), target, n, 0, 11)
    assert 0 < count <= n
    result = header[:14] + struct.pack('<i', count) + target.raw[:count]
    assert decode(result) == raw
    return result


def write(raw, folder, name=None):
    cid = identity(raw)
    path = OUT / 'staged/Draw01' / folder / (name or (cid + '.Texture'))
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(raw)
    Path(str(path)+'.cid').write_text(cid)
    return cid, path


def main():
    original = json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/grid-bake-report.json').read_text())['units'][3]
    baked = json.loads((OUT/'near-cap-report.json').read_text())['units'][0]
    recipe = json.loads((OUT/'near-cap-manifest.json').read_text())['units'][0]
    descriptor = (OLD/'RepeatUnits'/original['key']/'Repeat.Surface').read_bytes()
    channels = []
    for old, new in zip(original['channels'], baked['channels']):
        assert old['name'] == new['name']
        before = (OLD/'Shared'/(old['cid']+'.Texture')).read_bytes()
        after = (OUT/'products/Draw01/Shared'/(new['cid']+'.Texture')).read_bytes()
        assert before[:14] == after[:14]
        source, candidate = decode(before), decode(after)
        result = bytearray(source)
        offset = 0
        levels = []
        for mip in range(before[8]):
            w, h = max(1, old['width'] >> mip), max(1, old['height'] >> mip)
            bw, bh = (w+3)//4, (h+3)//4
            # Exact producer grid and its actual source mip/filter support.
            lod = new['sourceLodByMip'][mip]
            footprint = .5 * 2**max(0, lod) / 4096
            u = (2524 + (np.arange(w)+.5)/w*1816) / 4096
            v = recipe['min'][1] + (np.arange(h)+.5)/h*(recipe['max'][1]-recipe['min'][1])
            hit = ((u >= recipe['paintU'][0]-footprint) & (u <= recipe['paintU'][1]+footprint))[None, :] & ((v >= recipe['paintV'][0]-footprint) & (v <= recipe['paintV'][1]+footprint))[:, None]
            padded = np.pad(hit, ((0,bh*4-h),(0,bw*4-w)))
            mask = padded.reshape(bh,4,bw,4).any(axis=(1,3)).reshape(-1)
            a = np.frombuffer(source[offset:offset+bw*bh*16], dtype='u1').reshape(-1,16)
            b = np.frombuffer(candidate[offset:offset+bw*bh*16], dtype='u1').reshape(-1,16)
            merged = a.copy(); merged[mask] = b[mask]
            assert np.array_equal(a[~mask], merged[~mask])
            result[offset:offset+bw*bh*16] = merged.tobytes()
            pixels = np.asarray(Image.frombytes('RGBA',(w,h),merged.tobytes(),'bcn',(7,'BC7'))).astype(int)
            if mip == 0 and old['name'] == '_BaseColorMap':
                Image.fromarray(pixels.astype('uint8')).save(OUT/'clean-cap-color.png')
                yellow = (pixels[:,:,0]-pixels[:,:,2]>35)&(pixels[:,:,1]-pixels[:,:,2]>20)
                assert not yellow[:,454:1362].any(), 'Embedded yellow remains in cap content.'
            levels.append(dict(mip=mip,width=w,height=h,sourceLod=lod,maskBlocks=int(mask.sum()),unchangedBlocks=int((~mask).sum()),outsideMaskBitExact=True))
            offset += bw*bh*16
        assert offset == len(result)
        raw = before if bytes(result) == source else encode(before, bytes(result))
        cid, path = write(raw, 'Shared')
        assert descriptor.count(cid_bytes(old['cid'])) == 1
        descriptor = descriptor.replace(cid_bytes(old['cid']), cid_bytes(cid))
        channels.append(dict(name=old['name'],oldCid=old['cid'],newCid=cid,oldBytes=len(before),newBytes=len(raw),oldSha256=sha(before),newSha256=sha(raw),levels=levels))
    key = sha(b'Draw01/CleanFixedCap/1' + descriptor)
    cid, _ = write(descriptor, 'RepeatUnits/'+key, 'Repeat.Surface')
    report = dict(key=key,surfaceCid=cid,oldKey=original['key'],channels=channels,geometryChanges=0,paintMeshChanges=0,sourceAtlasChanges=0)
    (OUT/'clean-cap-report.json').write_text(json.dumps(report,indent=2)+'\n')
    print(key, 'texture byte delta', sum(c['newBytes']-c['oldBytes'] for c in channels))


def prepare():
    manifest=json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/manifest.json').read_text())
    unit=manifest['units'][3]
    assert unit['id']=='fixed-carriageway'
    unit.update(id='clean-fixed-cap',paintU=[3849/4096,3872/4096],paintV=[2/4096,1156/4096],donorU=[3700/4096,3723/4096])
    evidence=dict(sourceCid=unit['sourceCid'],sourceDigest=unit['sourceDigest'],nativeFaces=[2674,2675],
        sourcePaintPixels=dict(x=[3849,3871],y=[2,1155]),donorPixelsX=[3700,3722],donorV='unchanged',
        operation='All-channel translation from the same native cap substrate; not recovery of unknown paint-free original pixels.',
        finiteAxis='U',repeatAxis='V',physicalPeriodMeters=4,sourceGridPreserved=True,
        mipPolicy='Each authored mip supplies its own donor; only paint plus half-mip-texel filter footprint changes. Original BC7 blocks outside that footprint remain bit-exact.')
    path=OUT/'near-cap-source-evidence.json';path.write_text(json.dumps(evidence,indent=2)+'\n')
    unit['evidence']=dict(path=str(path),sha256=sha(path.read_bytes()))
    manifest['units']=[unit]
    (OUT/'near-cap-manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')


if __name__ == '__main__':
    prepare() if '--prepare' in sys.argv else main()
