"""Check the measured native asphalt pixels, not just UV-density algebra.

Pass the decoded draw03-main-albedo.png. Requires Pillow and NumPy.
The original image is read only; no game assets are modified.
"""
from pathlib import Path
import hashlib
import struct
import sys
import numpy as np
from PIL import Image

source = Path(sys.argv[1])
assert hashlib.sha256(source.read_bytes()).hexdigest() == '9dee2d00cb198ed250a167a33293c8a485d4e1dbbe507ffb038ba1499cb580a4'
resource = Path(__file__).resolve().parents[2] / 'assets/HardcodedNumbers/Draw03/Types_Draw03_Draw03RoadWear_cs.bin'
b = resource.read_bytes()
assert hashlib.sha256(b[:-32]).digest() == b[-32:]
counts = struct.unpack_from('<6i', b, 8)
offset = 32 + sum(c * size for c, size in zip(counts[:4], (4, 4, 8, 8)))
values = struct.unpack_from('<' + 'f' * counts[4], b, offset)
height = int(values[9])
bottom = int(values[3] - height * values[11])
pixels = np.asarray(Image.open(source).convert('RGB'))[::-1].astype(float)
tile = pixels[bottom:bottom+height, int(values[1]):int(values[5])].mean(2)
assert tile.shape == (32, 296)
assert tile.max() < 180, 'Paint or bright wear contaminated the asphalt crop'

# At mip 2 the former four-row unit averages to one row. Repeating that row
# cannot contain any transverse grain, regardless of a correct repeat count.
def mip_variation(crop):
    tiled = np.tile(crop, (256 // len(crop), 1))
    mip = tiled.reshape(64, 4, 74, 4).mean((1, 3))
    return np.abs(np.diff(mip, axis=0)).mean()
old = pixels[818:822, 3084:3380].mean(2)
assert mip_variation(old) < 1e-8
assert mip_variation(tile) > 1, 'Mip-filtered tile still degenerates to a stripe'
print(f'PASS native 296x32 dark asphalt: mean={tile.mean():.2f}, max={tile.max():.0f}; '
      f'mip-2 transverse variation old={mip_variation(old):.2f}, new={mip_variation(tile):.2f}')
