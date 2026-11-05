"""Record exact native source faces inside the already evidenced RoadEU asphalt band.

This is an offline identity-keyed candidate, not permission to classify arbitrary
network meshes by a bounding box at runtime.
"""
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT / 'artifacts/material-remap/shared/draw01-bridgehead35'
OUT = ROOT / 'artifacts/material-remap/Draw01/bridgehead35'
ROAD = '1d7dad9f64c9fe55a809f1080c39fb92'
LIMIT = 0.249755859375


def main():
    bindings = json.loads((SOURCE / 'actual-bindings.json').read_text())
    extraction = json.loads((SOURCE / 'native-network-extraction.json').read_text())
    entries = {item['cid']: item for item in extraction['found']}
    geometries = sorted({item['geometry'] for item in bindings if ROAD in item['surfaces']})
    result = []
    for cid in geometries:
        path = SOURCE / 'native-network' / (cid + '.Geometry')
        mesh_file = json.loads(path.with_suffix('.Geometry.puv.json').read_text())
        for mesh in mesh_file['meshes']:
            asphalt, retained = [], []
            for start in range(0, len(mesh['indices']), 3):
                ids = mesh['indices'][start:start + 3]
                # PUV JSON uses round-trip Single text. Restore the original
                # Float32 before exact boundary comparisons (0.24975586 text
                # represents the authored 0.249755859375 bit pattern).
                u = [struct.unpack('<f', struct.pack('<f', mesh['uv'][i][0]))[0] for i in ids]
                (asphalt if min(u) >= 0 and max(u) <= LIMIT else retained).append(start // 3)
            result.append(dict(geometry=cid, geometrySha256=hashlib.sha256(path.read_bytes()).hexdigest(),
                               source=entries[cid]['entry'], surface=ROAD, mesh=mesh['slot'],
                               vertexCount=len(mesh['positions']), triangleCount=len(mesh['indices']) // 3,
                               asphaltTriangles=asphalt, retainedTriangles=retained))
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'native-asphalt-face-membership.json').write_text(json.dumps(dict(
        status='Measured source-face candidate; native UV adapter validation pending',
        sourceRule='Existing MovableRoadLayerIdentity.WholeReceiver exact RoadEU family, U 0..0.249755859375',
        meshes=result), indent=2) + '\n')
    # Stage typed resource bytes separately from production until the native
    # projection adapter is verified. Each record is vertexCount, triangleCount,
    # selectedCount, followed by the immutable original triangle ordinals.
    values = []
    offsets = []
    for mesh in result:
        offsets.append(len(values))
        values.extend([mesh['vertexCount'], mesh['triangleCount'], len(mesh['asphaltTriangles'])])
        values.extend(mesh['asphaltTriangles'])
    raw = struct.pack('<II6i', 0x314e4242, 1, len(values), 0, 0, 0, 0, 0)
    raw += struct.pack('<' + 'i' * len(values), *values)
    (OUT / 'Types_Draw01_HeadAsphaltFaces.bin').write_bytes(raw + hashlib.sha256(raw).digest())
    (OUT / 'native-asphalt-record-offsets.json').write_text(json.dumps(dict(
        records=[dict(geometry=mesh['geometry'], offset=offset)
                 for mesh, offset in zip(result, offsets)]), indent=2) + '\n')
    print(json.dumps([dict(source=m['source'], asphalt=len(m['asphaltTriangles']),
                           retained=len(m['retainedTriangles'])) for m in result]))


if __name__ == '__main__':
    main()
