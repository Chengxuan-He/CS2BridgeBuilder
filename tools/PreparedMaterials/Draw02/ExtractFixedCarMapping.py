"""Measure fixed carriageway domains before replacing the legacy road recipe.

Uses actual up-facing source charts, including the middle LOD's asymmetric
terminal silhouette. Does not rasterize the span or alter production assets.
"""
import hashlib
import json
import ctypes as c
import zipfile
import struct
from pathlib import Path
import numpy as np
from EmitFarMapping import interior_line

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism'
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
OUT.mkdir(parents=True, exist_ok=True)
rows = []
decoder_source = (SOURCE/'audit.py').read_text().split('def analyze')[0]
decoder_source = decoder_source.replace('(0,4,13)', '(0,1,2,4,13)').replace("formats={0:'<f4',1:'<f2',10:'<u4',11:'<i4'}", "formats={0:'<f4',1:'<f2',5:'<i2',10:'<u4',11:'<i4'}")
decoder_source = decoder_source.replace("{0:'positions',4:'uv',13:'bones'}", "{0:'positions',1:'normalPacked',2:'tangentPacked',4:'uv',13:'bones'}")
decoder = {'__file__': str(SOURCE/'audit.py')}
exec(decoder_source, decoder)
game = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
dll = c.CDLL(str(game/'Plugins/x86_64/PipelinePlugin.dll'))
fn = dll.math_array_oct15_to_fp32tangents
fn.argtypes = [c.c_void_p, c.c_int64, c.c_void_p]
fn.restype = None
for lod in (0, 1):
    path = SOURCE/f'lod{lod}-native.npz'
    mesh = np.load(path)
    p, uv, f, bones = (mesh['mesh0_'+k] for k in ('positions', 'uv', 'indices', 'bones'))
    p, uv = p.astype(float), uv.astype(float)
    evidence = json.loads((SOURCE/f'lod{lod}-analysis.json').read_text())
    with zipfile.ZipFile(game/'Content/BridgesAndPorts/Blob.cok') as archive:
        raw = archive.read(evidence['archiveEntry'])
    assert hashlib.sha256(raw).hexdigest() == evidence['sha256']
    native = decoder['decode'](raw)[0]
    packed = np.ascontiguousarray(native['tangentPacked']).copy().view('<u4').reshape(-1)
    tangents = np.zeros((len(packed), 4), np.float32)
    fn(packed.ctypes.data, len(packed), tangents.ctypes.data)
    q, tex = p[f], uv[f]
    normals = np.cross(q[:, 1]-q[:, 0], q[:, 2]-q[:, 0])
    for end in (1, -1):
        z = q[:, :, 2]*end
        eligible = (bones[f[:, 0], 0] == 0) & (normals[:, 1] > 0) & (z.min(1) >= 78.74)
        eligible &= (q[:, :, 0].min(1) >= -6.401) & (q[:, :, 0].max(1) <= 6.401)
        for part in range(4):
            if part == 0:
                match = (tex[:, :, 0].min(1) >= .1035) & (tex[:, :, 0].max(1) <= .2503) & (z.max(1) < 79.95)
            elif part == 1:
                match = (tex[:, :, 0].min(1) >= .2619) & (tex[:, :, 0].max(1) <= .4090) & (z.min(1) > 79.94) & (z.max(1) < 80.02)
            else:
                match = (tex[:, :, 0].min(1) >= .3251) & (tex[:, :, 0].max(1) <= .4722)
                match &= ((z.min(1) >= 80) & (z.max(1) <= 82)) if part == 2 else ((z.min(1) >= 82) & (z.max(1) <= 90))
            faces = np.where(eligible & match)[0]
            groups = {}
            for face in faces:
                x = q[face, :, 0]
                groups.setdefault((float(x.min()), float(x.max())), []).append(int(face))
            assert len(groups) == 4, (lod, end, part, groups)
            for lane, ((xmin, xmax), ids) in enumerate(sorted(groups.items())):
                assert len(ids) == (4 if part == 3 else 2), (lod, end, part, lane, ids)
                vi = np.unique(f[ids]); points, st = p[vi], uv[vi]
                u, ucheck = interior_line(points[:, 0], st[:, 0])
                if part == 3:
                    # Source resets at Z86. Measure one actual 4 m cell, then
                    # verify the second cell independently without fitting resets.
                    cell = [i for i in ids if (q[i, :, 2]*end).max() <= 86]
                    ci = np.unique(f[cell]); cp, ct = p[ci], uv[ci]
                    v, vcheck = interior_line(cp[:, 2], ct[:, 1])
                    source_min, source_max = st.min(0), st.max(0)
                    period = source_max[1]-source_min[1]
                    for face in ids:
                        fi = f[face]; station = int(round(((p[fi, 2]*end).min()-82)/4))
                        actual = np.float32(np.float32(p[fi, 2]+end*station*-4)*v[0]+v[1])
                        assert np.array_equal(actual.astype(np.float16).view(np.uint16),
                            uv[fi, 1].astype(np.float16).view(np.uint16))
                else:
                    v, vcheck = interior_line(points[:, 2], st[:, 1])
                    source_min, source_max = st.min(0), st.max(0)
                # The lower silhouette is a measured edge, not a face-Boolean.
                left_z = float((points[points[:, 0] == xmin, 2]*end).min())
                right_z = float((points[points[:, 0] == xmax, 2]*end).min())
                lower_slope = (right_z-left_z)/(xmax-xmin)
                rows.append(dict(lod=lod, end=end, lane=lane, part=part, faces=ids,
                    x=[xmin, xmax], lowerZ=[lower_slope, left_z-lower_slope*xmin],
                    upperZ=float((points[:, 2]*end).max()), u=u.tolist(), v=v.tolist(),
                    sourceMin=source_min.tolist(), sourceMax=source_max.tolist(),
                    repeatV=part == 3, finiteU=True, supportedTypes=['Asphalt'],
                    uStorageCheck=ucheck, vStorageCheck=vcheck,
                    packedTangents=[f'{v:08x}' for v in np.unique(packed[vi])],
                    tangents=np.unique(tangents[vi], axis=0).tolist()))

repeat = [r for r in rows if r['repeatV']]
assert len(rows) == 64 and len(repeat) == 16
assert len({tuple(r['sourceMin']+r['sourceMax']) for r in repeat}) == 1
result = dict(schema='Draw02FixedCar/1', sourceSurface='8a28ab8d9526e258efbcb81c3bbc936b',
    rows=rows, sourceMeshes=[dict(path=str(SOURCE/f'lod{lod}-native.npz'),
        sha256=hashlib.sha256((SOURCE/f'lod{lod}-native.npz').read_bytes()).hexdigest()) for lod in (0, 1)],
    sharedUnit=dict(min=repeat[0]['sourceMin'], max=repeat[0]['sourceMax'], repeatAxes='V',
        finiteAxis='U', physicalPeriod=4, sourcePixels=[602, 752]),
    paintComposition=dict(channels=['_BaseColorMap', '_NormalMap', '_MaskMap'],
        preserveChannels=['_ControlMask'], sourceMip0Size=[4096, 4096],
        sourcePixelRects=[[1344, 2002, 1360, 2754], [1904, 2002, 1924, 2754]],
        mipMaskRule='Round each minimum down and maximum up to the source mip texel grid.',
        mipDonorRule='First: min(1640,1360+2^mip); second: max(1640,1904-2^mip); snap to source mip texel center.',
        meaning='Localized composition from adjacent source texels, not recovery of unknown underlying asphalt.'),
    qualityAccepted=False)
(OUT/'mapping-plan.json').write_text(json.dumps(result, indent=2)+'\n')
# All four physical roles retain independently measured source projections.
values = []
for lod in (0, 1):
    for end in (1, -1):
        for part in (0, 1):
            group = [r for r in rows if r['lod'] == lod and r['end'] == end and r['part'] == part]
            assert len({tuple(r['lowerZ']) for r in group}) == 1 and group[0]['lowerZ'][0] == 0
            assert len({r['upperZ'] for r in group}) == 1
            assert all(a['x'][1] == b['x'][0] for a, b in zip(group, group[1:]))
    for row in (r for r in rows if r['lod'] == lod):
        assert len(row['tangents']) == 1
        values += row['x']+row['lowerZ']+[row['upperZ']]+row['u']+row['v']+row['sourceMin']+row['sourceMax']+row['tangents'][0]
values.append(6.4000244140625)
moving = json.loads((ROOT/'artifacts/material-remap/Draw02/near-cells/native-atlas-wrap-plan.json').read_text())
for level in moving:
    values.append(max(s['absoluteZ'][1] for d in level['domains'] for s in d['stages']))
blob = struct.pack('<8I', 0x314e4242, 1, 0, 0, 0, 0, len(values), 0)
blob += struct.pack('<%df' % len(values), *values)
blob += hashlib.sha256(blob).digest()
(ROOT/'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02FixedCarAtlasAppearance_cs.bin').write_bytes(blob)
print('PASS: 64 source domains; 16 repeated views share one 602 x 752 source rectangle.')
