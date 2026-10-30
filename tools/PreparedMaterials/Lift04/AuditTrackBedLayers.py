"""Decode every native subway bed mask mip and identify the shader blend layer."""
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'artifacts/material-remap/environments/Lift04/python-deps'))
import texture2ddecoder

report = json.loads((ROOT / 'artifacts/material-remap/Lift04/appearance-support.json').read_text())
refs = report['nativeBedSource']['texture_reference_bytes']
# Reverse each byte's hexadecimal nibbles to match AssetDatabase Hash128 text.
cids = {name: ''.join(s[i+1] + s[i] for i in range(0, len(s), 2)) for name, s in refs.items()}
archive_path = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/Game/Blob.cok')
with zipfile.ZipFile(archive_path) as archive:
    matches = {archive.read(e).decode().strip(): e[:-4] for e in archive.namelist()
               if e.endswith('.cid') and archive.read(e).decode().strip() in cids.values()}
    assert set(matches) == set(cids.values())
    entry = matches[cids['_MaskMap']]
    raw = archive.read(entry)
    canvas = Image.new('RGB', (1536, 1080), 'white')
    draw = ImageDraw.Draw(canvas)
    for i, (channel, cid) in enumerate(cids.items()):
        content = archive.read(matches[cid])
        ver, w, h, _ = struct.unpack_from('<3HB', content)
        n = content[7]
        assert ver == 2 and content[8:8+n].decode().startswith('RGBA_BC7')
        at = 11 + n
        size = ((w + 3)//4)*((h + 3)//4)*16
        rgba = np.frombuffer(texture2ddecoder.decode_bc7(content[at:at+size], w, h), np.uint8).reshape(h,w,4)
        preview = Image.fromarray(rgba[:,:,[2,1,0]], 'RGB')
        preview.thumbnail((512,512))
        x, y = (i%3)*512, (i//3)*540
        canvas.paste(preview, (x,y+24))
        draw.text((x+4,y+4), channel+' '+str((w,h)), fill='black')
    canvas.save(ROOT / 'artifacts/material-remap/Lift04/track-bed-channels.png')
version, width, height, mip_count = struct.unpack_from('<3HB', raw)
assert version == 2
text_length = raw[7]
fmt = raw[8:8 + text_length].decode()
assert fmt == 'RGBA_BC7_UNorm'
filtering, wrap, aniso = raw[8 + text_length:11 + text_length]
offset = 11 + text_length
mips = []
for mip in range(mip_count):
    w, h = max(1, width >> mip), max(1, height >> mip)
    size = ((w + 3) // 4) * ((h + 3) // 4) * 16
    # texture2ddecoder returns BGRA, so decoded channel 0 is source B.
    pixels = np.frombuffer(texture2ddecoder.decode_bc7(raw[offset:offset+size], w, h), np.uint8).reshape(h, w, 4)
    blue = pixels[:, :, 0]
    values, counts = np.unique(blue, return_counts=True)
    mips.append(dict(mip=mip, width=w, height=h,
                     blueHistogram={str(int(v)): int(c) for v, c in zip(values, counts)},
                     allWorldspaceActive=bool(np.all(blue > 0)),
                     allWorldspaceOpaque=bool(np.all(blue == 255))))
    offset += size
assert offset == len(raw)
shader = ROOT / 'artifacts/layer-decoupling/shared/outputs/native-repeat-mechanism/581-133.asm'
text = shader.read_text()
assert 'mad r7.xyz, r9.zzzz, r8.xyzx, r7.xyzx' in text
assert 'mad r10.xyz, r9.zzzz, r11.xyzx, r10.xyzx' in text
assert 'mad r9.xyz, r9.zzzz, r11.xyzx, r9.wxyw' in text
original = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'
assert hashlib.sha256(original.read_bytes()).hexdigest() == report['sourceAuditSHA256']
records = json.loads(original.read_text())['selected']
receivers = {}
for key in ('120985', '120981'):
    receiver = records[key]['data']
    assert receiver['m_MeshCount'] == 1 and len(receiver['m_SurfaceAssets']) == 1
    assert receiver['m_Bounds']['max']['y'] - receiver['m_Bounds']['min']['y'] < .00001
    parts = receiver['m_SurfaceAssets'][0]
    cidbytes = struct.pack('<4i', *(parts['m_GuidPart'+str(i)] for i in range(1,5)))
    cid = ''.join(format(b & 15,'x')+format(b >> 4,'x') for b in cidbytes)
    assert cid == report['nativeBedSource']['cid']
    receivers[key] = receiver
lane = records['120946']['data']
assert {e['m_Mesh']['m_PathID'] for e in lane['m_Meshes']} == {120951,120954,120952,121053,120953}
output = ROOT / 'artifacts/material-remap/Lift04/track-bed-layer-evidence.json'
output.write_text(json.dumps(dict(surface='a3522ead5e61e02569f0884fb4607eec',
    textureCids=cids, textureEntries=matches, maskEntry=entry,
    maskSHA256=hashlib.sha256(raw).hexdigest(), width=width, height=height,
    format=fmt, filter=filtering, wrap=wrap, aniso=aniso, mips=mips,
    shaderSHA256=hashlib.sha256(shader.read_bytes()).hexdigest(),
    renderer='Shader 581 fragment 133 samples base at network UV, worldspace at absolute XZ times 0.4000000059604645; Mask B blends base/world color, tangent-converted normal, and mask channels.',
    receivers=receivers, independentLane=lane,
    actualInputAppearanceType='Ballast', receiverGeometryRole='TrackBed',
    prototypeSupport='Lift04 accepts this actual Ballast input and supplies its own prototype bed appearance; the output prototype appearance need not itself be gravel.',
    visualInspection='All six decoded channels inspected. Base is fine granular grey bed plus flat grey regions with matching fine/flat normal; worldspace is coarse gravel. No rail, sleeper or marking motif appears in either albedo. Mask has no independent inactive worldspace area.',
    dispatch='For the verified flat bed receivers 120985 and 120981 using exact Surface a3522ead5e61e02569f0884fb4607eec, base and worldspace jointly constitute Ballast appearance on a TrackBed geometry role. Replace that complete receiver appearance with the supported Lift04 prototype bed appearance, leaving independent lane rail/sleeper meshes untouched. B=254 residual does not require retaining selected base when both layers are the supported type. Do not generalize by shader/template alone.',
    limitations='Mip texture evidence covers all possible sampled UVs; it does not prove skinning-compatible native layered composition or output persistence.'), indent=2))
print(output)
