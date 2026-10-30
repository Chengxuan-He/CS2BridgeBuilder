"""Verify actual Lift05 tram receivers and decode their unchanged source channels."""
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'artifacts/material-remap/environments/Lift05/cache/python-deps'))
import texture2ddecoder
sys.path.insert(0, str(ROOT / 'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry

OUT = ROOT / 'artifacts/material-remap/Lift05'
AUDIT = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'


def digest(data):
    return hashlib.sha256(data).hexdigest()


def main():
    source = AUDIT.read_bytes()
    assert digest(source) == '47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'
    audit = json.loads(source)
    records = audit['selected']
    def cid(parts):
        raw = struct.pack('<4i', *(parts['m_GuidPart'+str(i)] for i in range(1, 5)))
        return ''.join(format(b & 15, 'x')+format(b >> 4, 'x') for b in raw)
    receivers = {str(i): records[str(i)]['data'] for i in (121111, 121107)}
    ids = {cid(p['m_SurfaceAssets'][0]) for p in receivers.values()}
    assert ids == {'1d7dad9f64c9fe55a809f1080c39fb92'}
    surface = audit['assets'][next(iter(ids))]
    refs = {k: ''.join(s[i+1]+s[i] for i in range(0, len(s), 2))
            for k, s in surface['texture_reference_bytes'].items()}
    archive_path = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data') / surface['package']
    channels = {}
    canvas = Image.new('RGB', (1536, 1080), 'white')
    draw = ImageDraw.Draw(canvas)
    with zipfile.ZipFile(archive_path) as archive:
        raw_surface = archive.read(surface['entry'])
        assert digest(raw_surface) == surface['sha256']
        entries = {}
        geometry_ids = {cid(p['m_GeometryAsset']) for p in receivers.values()}
        for name in archive.namelist():
            if name.endswith('.cid'):
                identity = archive.read(name).decode().strip()
                if identity in set(refs.values()) | geometry_ids:
                    entries[identity] = name[:-4]
        assert set(entries) == set(refs.values()) | geometry_ids
        codec = NativeGeometry(archive_path.parents[2] / 'Plugins/x86_64/PipelinePlugin.dll')
        for key, receiver in receivers.items():
            identity = cid(receiver['m_GeometryAsset'])
            raw = archive.read(entries[identity])
            path = ROOT / 'artifacts/material-remap/environments/Lift05/cache' / (identity+'.Geometry')
            path.write_bytes(raw)
            meshes = codec.read(path)
            assert len(meshes) == 1
            mesh = meshes[0]
            receiver['decodedSource'] = dict(cid=identity,entry=entries[identity],sha256=digest(raw),
                positions=mesh['positions'].tolist(),uv=mesh['uv0'].tolist(),faces=mesh['faces'].tolist(),
                uMin=float(mesh['uv0'][:,0].min()),uMax=float(mesh['uv0'][:,0].max()))
        for index, (channel, identity) in enumerate(refs.items()):
            raw = archive.read(entries[identity])
            version, width, height, count = struct.unpack_from('<3HB', raw)
            n = raw[7]
            fmt = raw[8:8+n].decode()
            assert version == 2 and fmt.startswith('RGBA_BC7')
            at = 11+n
            mips = []
            for mip in range(count):
                w, h = max(1, width >> mip), max(1, height >> mip)
                size = ((w+3)//4)*((h+3)//4)*16
                pixels = np.frombuffer(texture2ddecoder.decode_bc7(raw[at:at+size], w, h), np.uint8).reshape(h,w,4)[:,:,[2,1,0,3]]
                coverage = {}
                if channel == '_MaskMap':
                    for key, receiver in receivers.items():
                        r = receiver['decodedSource']
                        lo=max(0,int(np.floor(r['uMin']*w-.5)))
                        hi=min(w,int(np.ceil(r['uMax']*w+.5))+1)
                        values=pixels[:,lo:hi,2]
                        coverage[key]=dict(firstColumn=lo,endColumn=hi,blueMin=int(values.min()),blueMax=int(values.max()))
                mips.append(dict(level=mip, width=w, height=h,receiverUFootprint=coverage,
                    minimum=pixels.min(axis=(0,1)).tolist(), maximum=pixels.max(axis=(0,1)).tolist(),
                    maskBlueHistogram={str(int(k)):int(v) for k,v in zip(*np.unique(pixels[:,:,2],return_counts=True))} if channel=='_MaskMap' else None))
                if mip == 0:
                    preview = Image.fromarray(pixels[:,:,:3], 'RGB')
                    preview.thumbnail((512,512))
                    x,y = (index%3)*512,(index//3)*540
                    canvas.paste(preview,(x,y+24)); draw.text((x+4,y+4),channel,fill='black')
                at += size
            assert at == len(raw)
            channels[channel] = dict(cid=identity,entry=entries[identity],sha256=digest(raw),format=fmt,
                filter=raw[8+n],wrap=raw[9+n],anisotropy=raw[10+n],mips=mips)
    OUT.mkdir(parents=True,exist_ok=True)
    canvas.save(OUT/'tram-input-channels.png')
    result = dict(style='Lift05',auditSHA256=digest(source),surfaceCid=next(iter(ids)),surface=surface,
        receivers=receivers,channels=channels,
        actualAppearanceType='Asphalt',
        visualInspection='The complete decoded base-color footprints of both original receivers (U 0..0.249755859375) are dark grey fine road surfacing; the worldspace color is granular asphalt. The cobbled atlas strip lies outside both original receiver footprints. TrackLane and PedestrianLane describe traffic roles, not separate TrackBed/Pathway material families.',
        conclusion='The verified native tram receivers support Asphalt appearance. Base and worldspace within these measured footprints belong to that same type, so non-unit blend weights alone do not prohibit complete same-type replacement. Keep independent rail/sleeper lane visuals and unsupported input appearances on their own sources. Do not generalize this identification to the entire Road atlas or arbitrary receivers.',
        scope='Original native sources only; no baking, production assets, mapping changes or acceptance claim.',
        localization='No user-visible runtime text changed.')
    (OUT/'tram-input-layers.json').write_text(json.dumps(result,indent=2)+'\n')
    print('PASS original Surface/receiver identity and all six complete BC7 mip chains.')


if __name__ == '__main__':
    main()
