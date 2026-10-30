"""Verify the native pavement source bindings and mask across every stored mip.

Decode original BC7 blocks, without resampling, baking, or changing game resources.
The exact worldspace triplet identifies the pavement layer; a whole Surface is not
automatically eligible for replacement because its name contains 'Pathway'.
"""
import argparse
import hashlib
import io
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
SURFACE = '16be9bdc3ead50651a57c398efa6ed2e'

def decode(raw):
    version, width, height = struct.unpack_from('<3H', raw)
    assert version == 2
    mips, size = raw[6:8]
    fmt = raw[8:8 + size].decode()
    assert fmt in ('RGBA_BC7_UNorm', 'RGBA_BC7_SRGB')
    at = 8 + size
    filter_mode, wrap, aniso = raw[at:at + 3]
    at += 3
    result = []
    for mip in range(mips):
        w, h = max(1, width >> mip), max(1, height >> mip)
        length = ((w + 3) // 4) * ((h + 3) // 4) * 16
        blocks = raw[at:at + length]
        assert len(blocks) == length
        header = [124, 0x81007, h, w, length, 0, 1] + [0] * 11
        header += [32, 4, int.from_bytes(b'DX10', 'little'), 0, 0, 0, 0, 0, 0x1000, 0, 0, 0, 0]
        dds = b'DDS ' + struct.pack('<31I', *header) + struct.pack('<5I', 98, 3, 0, 1, 0) + blocks
        pixels = np.asarray(Image.open(io.BytesIO(dds)).convert('RGBA'))
        blue = pixels[:, :, 2]
        result.append(dict(mip=mip, width=w, height=h, blueMin=int(blue.min()),
            blueMax=int(blue.max()), blueZero=int(np.count_nonzero(blue == 0)),
            blueFull=int(np.count_nonzero(blue == 255)), blueMixed=int(np.count_nonzero((blue > 0) & (blue < 255)))))
        at += length
    assert at == len(raw)
    return dict(version=version, format=fmt, filter=filter_mode, wrap=wrap,
        anisotropy=aniso, mips=result)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    assets = json.loads((ROOT / 'artifacts/layer-decoupling/shared/outputs/native-archetype-audit/asset-counts.json').read_text())['assets']
    source = assets[SURFACE]
    verified = {}
    for cid in [SURFACE, *source['references']]:
        item = assets[cid]
        with zipfile.ZipFile(item['archive']) as archive:
            raw = archive.read(item['entry'])
            assert archive.read(item['entry'] + '.cid').decode('utf-8-sig').strip() == cid
        sha = hashlib.sha256(raw).hexdigest()
        if 'sha256' in item:
            assert sha == item['sha256']
        verified[cid] = dict(archive=item['archive'], entry=item['entry'], bytes=len(raw), sha256=sha)
        if cid == source['explicitTextureBindings']['_MaskMap']:
            mask = decode(raw)
    result = dict(surfaceCid=SURFACE, template=source['template'],
        bindings=source['explicitTextureBindings'], verified=verified, baseMask=mask,
        worldspaceLayer='Paved pathway texture family, blended by original base-mask blue under native shader 581.',
        replacementBoundary='Only that evidenced layer is classified here. Preserve the base layer and its masks; no blanket Surface replacement.',
        limits=['This does not classify every third-party pathway material.',
                'Original nonconstant blend weights require a native independently sampled representation.',
                'BC7 block decoding and source hashes are not game rendering or skinning acceptance.'])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(surface=SURFACE, mips=len(mask['mips']), mip0=mask['mips'][0])))

if __name__ == '__main__':
    main()
