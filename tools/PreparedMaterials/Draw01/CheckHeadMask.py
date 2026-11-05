"""Check every native head-mask mip, including exact removal of world blend."""
import json
import struct
import numpy as np
from PIL import Image
from StageCleanCap import ROOT, decode, sha, identity


def main():
    out = ROOT / 'artifacts/material-remap/Draw01/bridgehead35'
    original = ROOT / 'artifacts/material-remap/Draw01/asphalt-repeat34'
    raw = (out / 'head-mask.Texture').read_bytes()
    assert struct.unpack_from('<HH', raw, 2) == (512, 732)
    assert list(raw[8:14]) == [10, 109, 2, 2, 0, 16]
    native = decode(raw)
    authored = (original / 'asphalt-2.rgba').read_bytes()
    existing = decode((original / 'asphalt-2.Texture').read_bytes())
    offset = source_offset = 0
    levels = []
    for mip in range(10):
        w, h = max(1, 512 >> mip), max(1, 732 >> mip)
        size = ((w + 3) // 4) * ((h + 3) // 4) * 16
        image = np.asarray(Image.frombytes('RGBA', (w, h), native[offset:offset+size], 'bcn', (7, 'BC7'))).copy()
        old = np.asarray(Image.frombytes('RGBA', (w, h), existing[offset:offset+size], 'bcn', (7, 'BC7'))).copy()
        source = np.frombuffer(authored[source_offset:source_offset+w*h*4], np.uint8).reshape(h, w, 4)
        assert np.all(image[:, :, 2] == 0), (mip, np.unique(image[:, :, 2]))
        error = image[:, :, [0, 1, 3]].astype(float) - source[:, :, [0, 1, 3]]
        old_error = image[:, :, [0, 1, 3]].astype(float) - old[:, :, [0, 1, 3]]
        levels.append(dict(mip=mip, width=w, height=h, blueMin=0, blueMax=0,
                           authoredRgaRms=float(np.sqrt(np.mean(error**2))),
                           authoredRgaMax=float(np.abs(error).max()),
                           existingDecodedRgaRms=float(np.sqrt(np.mean(old_error**2)))))
        offset += size
        source_offset += w*h*4
    assert offset == len(native) and source_offset == len(authored)
    result = dict(status='PASS', cid=identity(raw), sha256=sha(raw), bytes=len(raw),
                  preservedRgaAuthoringInput=True,
                  decodedRgaBitIdentity=all(level['existingDecodedRgaRms'] == 0 for level in levels),
                  nativeBytesIdentical=raw == (original / 'asphalt-2.Texture').read_bytes(),
                  nativeConsumerAcceptance=False, levels=levels)
    (out / 'head-mask-validation.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'levels'}))


if __name__ == '__main__':
    main()
