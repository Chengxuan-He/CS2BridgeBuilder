"""Verify the exact native pathway input and its worldspace-layer blend mask."""
import argparse
import hashlib
import io
import json
import struct
import sys
import zipfile
from pathlib import Path
import numpy as np
from PIL import Image
from ExportSharedSampling import descriptor, string


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('research', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    args.output.parent.mkdir(parents=True,exist_ok=True)
    dump = args.research / 'PedestrianDrawBridge01.txt'
    text = dump.read_text(encoding='utf-8-sig')
    piece = text[text.index('m_Piece: PathwayPavement800cm Mesh'):]
    piece = piece[:piece.index('m_Piece -> PathwayPavement800cm Mesh')]
    identity = '16be9bdc3ead50651a57c398efa6ed2e'
    entry = 'PathwayPavement3_6fd7952d1e5e7cd4da112709a3f8a403.Material'
    assert identity in piece and '<PedestrianLane>' in piece
    inventory_path = args.repository / 'artifacts/layer-decoupling/shared/outputs/native-archetype-audit/asset-counts.json'
    inventory = json.loads(inventory_path.read_text())
    # The audit stores the asset table separately from summary counters.
    assets = inventory['assets']
    archive_path = args.game_data / 'Content/Game/Blob.cok'
    with zipfile.ZipFile(archive_path) as archive:
        raw = archive.read(entry)
        assert archive.read(entry+'.cid').decode().strip() == identity
        template, header, _, bindings, keywords = descriptor(raw)
        assert template == 17 and header is None
        assert bindings['_WorldspaceAlbedo'] == '81526dc63dccf7656b7cdef1b67aca93'
        records = {}
        for prop, cid in bindings.items():
            name = assets[cid]['entry']
            data = archive.read(name)
            assert archive.read(name+'.cid').decode().strip() == cid
            stream = io.BytesIO(data)
            version, width, height = struct.unpack('<3H',stream.read(6))
            mip_count = stream.read(1)[0]
            fmt = string(stream)
            filtering, wrap, aniso = struct.unpack('<3B',stream.read(3))
            assert version == 2 and fmt in ('RGBA_BC7_UNorm','RGBA_BC7_SRGB')
            levels = []
            for level in range(mip_count):
                w,h = max(1,width>>level),max(1,height>>level)
                size = ((w+3)//4)*((h+3)//4)*16
                block = stream.read(size)
                assert len(block) == size
                if level == 0:
                    Image.frombytes('RGBA',(w,h),block,'bcn',7).save(args.output.parent / (prop+'.png'))
                if prop == '_MaskMap':
                    image = Image.frombytes('RGBA',(w,h),block,'bcn',7)
                    blue = np.array(image)[:,:,2]
                    values,counts = np.unique(blue,return_counts=True)
                    levels.append(dict(level=level,width=w,height=h,
                        blueHistogram={str(int(v)):int(n) for v,n in zip(values,counts)}))
            assert stream.tell() == len(data)
            records[prop] = dict(cid=cid,entry=name,sha256=digest(data),width=width,
                height=height,mipCount=mip_count,format=fmt,filter=filtering,wrap=wrap,
                anisotropy=aniso,maskBlueMips=levels)
    shader = args.repository / 'artifacts/layer-decoupling/shared/outputs/native-repeat-mechanism/581-133.asm'
    geometry_entry = 'PathwayPavement800cm_ee17d998b9cbb852d9a6f1f45773e32d.Geometry'
    with zipfile.ZipFile(args.game_data/'Content/Game/Blob_BridgesAndPorts.cok') as archive:
        geometry_raw = archive.read(geometry_entry)
    geometry_path = args.output.parent/geometry_entry
    geometry_path.write_bytes(geometry_raw)
    sys.path.insert(0,str(args.repository/'tools/GeometryMetaprogram'))
    from UvDensityAudit import NativeGeometry
    meshes = NativeGeometry(args.game_data/'Plugins/x86_64/PipelinePlugin.dll').read(geometry_path)
    geometry = []
    for mesh in meshes:
        geometry.append({key:value.tolist() for key,value in mesh.items()})
    assert len(meshes) == 1
    mesh = meshes[0]
    positions, uv, faces = mesh['positions'], mesh['uv0'], mesh['faces']
    assert positions.shape == (40,3) and faces.shape == (32,3)
    assert np.all(np.abs(positions[:,1]) < 0.00001)
    assert np.array_equal(positions[:,[0,2]].min(0),[-4,-32])
    assert np.array_equal(positions[:,[0,2]].max(0),[4,32])
    assert np.array_equal(uv.min(0),[0,0]) and np.array_equal(uv.max(0),[0.5,1])
    triangle_points = positions[faces]
    normals = np.cross(triangle_points[:,1]-triangle_points[:,0],triangle_points[:,2]-triangle_points[:,0])
    assert np.all(normals[:,1] > 0) and abs(float(normals[:,1].sum()/2)-512) < 0.0001
    shader_text = shader.read_text()
    for instruction in (
        'mul r6.zw, r5.yyyz, cb4[6].xxxx',
        'sample_b_indexable(texture2d)(float,float,float,float) r9.xyzw, v4.xyxx, t4.xyzw, s4, cb1[79].y',
        'mad r7.xyz, r9.zzzz, r8.xyzx, r7.xyzx',
        'mad r10.xyz, r9.zzzz, r11.xyzx, r10.xyzx',
        'mad r9.xyz, r9.zzzz, r11.xyzx, r9.wxyw'):
        assert instruction in shader_text
    report = dict(style='PedestrianDraw01',sourcePrefabDumpSHA256=digest(dump.read_bytes()),
        piece='PathwayPavement800cm Mesh',inputSurfaceCid=identity,entry=entry,
        surfaceSHA256=digest(raw),template=template,keywords=keywords,channels=records,
        appearanceType='Pathway',appearanceLayer='base-and-worldspace',
        selector='Exact source Surface identity and verified worldspace channel family; not PathwayPrefab or shader-wide inference.',
        blend='Native shader 581 blends worldspace color, tangent-transformed normal and mask with base _MaskMap.b. Both appearances are supported Pathway on this measured receiver, so replace both with prototype appearance. A blend value below one does not imply unsupported content.',
        maskScope='Whole authored base texture; histogram covers every texel of every authored mip.',
        wholeSurfaceReplacementAllowed=True,
        replacementScope='Exact input CID on the measured walking receiver only; no shader/template or PathwayPrefab-wide inference.',
        contentInspection=dict(method='Decoded and visually inspected all six complete native mip-zero channel images, together with decoded original receiver geometry.',
            base='Gray square walking pavers with aligned grout normal detail; no traffic paint, track, grass or curb appearance in U=0..0.5 across all V.',
            worldspace='Gray diagonal square walking pavers and matching grout normal detail; no separate unsupported appearance.',
            unusedRegion='The base atlas has a flat gray right strip beyond U=0.75. It lies outside the measured receiver U=0..0.5.',
            receiver='Single upward walking sheet, 40 vertices, 32 triangles, 512 square metres, x=-4..4 and z=-32..32. No curb sides or separate planted strip.',
            channelImages=[prop+'.png' for prop in bindings],
            semanticReview='Visual source-content assessment, not a machine classifier; bound to recorded source hashes and actual mesh footprint.'),
        receiverGeometry=dict(entry=geometry_entry,sha256=digest(geometry_raw),meshes=geometry),
        shaderEvidence=dict(path=str(shader),sha256=digest(shader.read_bytes()),lines='106-145'),
        limitations='One measured native pathway Surface only. Arbitrary pathway inputs need their own source-layer evidence. This is not rendering, composition or independent-loading acceptance.')
    args.output.parent.mkdir(parents=True,exist_ok=True)
    args.output.write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(surface=identity,channels=len(records),maskMips=records['_MaskMap']['maskBlueMips'])))


if __name__ == '__main__':
    main()
