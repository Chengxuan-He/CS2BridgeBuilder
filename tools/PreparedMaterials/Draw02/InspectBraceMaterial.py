"""Inspect the existing measured transverse-brace family; never produce runtime assets."""
import collections
import gzip
import hashlib
import json
from pathlib import Path
import sys

import numpy as np
from PIL import Image, ImageDraw

repo, research, output = map(Path, sys.argv[1:])
repo, research, output = repo.resolve(), research.resolve(), output.resolve()
output.mkdir(parents=True, exist_ok=True)
tool = repo / 'tools/GeometryMetaprogram/Draw02TransverseMembers.py'
# Reuse the existing offline membership measurements without executing its writer.
code = tool.read_text().split("\nlines = ")[0]
assert 'output.write_text' not in code
code = code.replace('tables.append((len(v), coefficients, digest))',
    'captured.append((lod,v.copy(),triangles.copy(),{k:list(ids) for k,(ids,weights) in members.items()}))\n    tables.append((len(v), coefficients, digest))')
sys.argv = [str(tool), str(research), str(output / 'unused.cs')]
scope = {'captured': []}
exec(compile(code, str(tool), 'exec'), scope)
manifest = repo / 'artifacts/prepared-materials/Draw02/repeat-units-v2/trial-result-corrected/manifest.json'
channels = json.loads(manifest.read_text())['units'][0]['channels']
source_images = {}
channel_records = []
for channel in channels:
    path = Path(channel['path'])
    raw = path.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == channel['sha256']
    assert raw[9] in (108, 109) and channel['mips'] == 13
    offset = 18
    images = []
    hashes = []
    for mip in range(13):
        side = max(1, 4096 >> mip)
        size = ((side + 3) // 4)**2 * 16
        blocks = raw[offset:offset+size]
        assert len(blocks) == size
        images.append(Image.frombytes('RGBA', (side, side), blocks, 'bcn', 7))
        hashes.append(hashlib.sha256(blocks).hexdigest())
        offset += size
    assert offset == len(raw)
    source_images[channel['name']] = images
    channel_records.append(dict(name=channel['name'], source=str(path), sha256=channel['sha256'],
                                original=channel['provenance'], mipBlockSha256=hashes))

families = []
face_membership = {}
for lod, vertices, faces, members in scope['captured']:
    render = 'DrawBridge02' + ('_LOD1' if lod else '') + ' Mesh'
    native = np.load(repo / 'artifacts/uv-density/native' / (render+'.npz'))
    assert np.array_equal(vertices, native['mesh0_positions'])
    assert np.array_equal(faces, native['mesh0_faces'])
    uv = native['mesh0_uv0']
    family_faces = set()
    for (role, station), indices in members.items():
        if role != 'brace':
            continue
        selected = np.flatnonzero(np.isin(faces, indices).all(axis=1))
        family_faces.update(map(int, selected))
        source_uv = uv[np.unique(faces[selected])]
        families.append(dict(render=render,lod=lod,station=station,faceIds=selected.tolist(),
                             vertices=indices,minimum=vertices[indices].min(0).tolist(),
                             maximum=vertices[indices].max(0).tolist(),
                             uvMinimum=source_uv.min(0).tolist(),uvMaximum=source_uv.max(0).tolist()))
        if lod == 0 and station == 4:
            for channel, images in source_images.items():
                # Display source UV-space, preserving source raster orientation.
                image = images[0].copy()
                draw = ImageDraw.Draw(image)
                for face in faces[selected]:
                    points = [(float(uv[i,0])*4096,float(uv[i,1])*4096) for i in face]
                    draw.line(points+[points[0]], fill=(255,0,255,255), width=2)
                image.save(output / (channel+'-brace-uv.png'))
                images[0].save(output / (channel+'-source.png'))
            coords = vertices[indices]
            diagram = Image.new('RGB',(1300,600),'white');draw=ImageDraw.Draw(diagram)
            for face in faces[selected]:
                points=[(float(vertices[i,0])*70+650,570-float(vertices[i,1])*52) for i in face]
                draw.line(points+[points[0]],fill=(25,50,100),width=1)
            diagram.save(output/'brace-station4-xy.png')
    face_membership[render] = family_faces

counts = collections.Counter()
classification = repo/'artifacts/prepared-materials/Draw02/structure-units/polar-retile-regions.json.classifications.ndjson.gz'
with gzip.open(classification,'rt') as stream:
    for line in stream:
        row=json.loads(line)
        if row['render'] not in face_membership or row['mesh'] != 0:
            continue
        ids=[face for face,fragment in row['sourceFaces']]
        if any(i in face_membership[row['render']] for i in ids):
            assert all(i in face_membership[row['render']] for i in ids)
            counts[(row['render'],row['status'])]+=1
result=dict(scope='Existing measured transverse brace family only. Source diagnostics, not a new repeat unit or generation proof.',
            membershipToolSha256=hashlib.sha256(tool.read_bytes()).hexdigest(),
            channels=channel_records,members=families,
            emittedUnits=[dict(render=k[0],status=k[1],count=v) for k,v in counts.items()])
(output/'source-evidence.json').write_text(json.dumps(result,indent=2))
print(json.dumps(dict(members=len(families),emittedUnits=result['emittedUnits'])))

