"""Copy read-only Draw03 native inputs into an isolated integration fixture."""
from pathlib import Path
import argparse
import hashlib
import json
import struct
import zipfile
import numpy as np

ROOT = Path(__file__).resolve().parents[3]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--evidence', type=Path, required=True,
                        help='Fresh VerifyNativeNetwork.py output for this verification run')
    args = parser.parse_args()
    args.output.mkdir(parents=True, exist_ok=True)
    evidence = json.loads(args.evidence.read_text())
    inventory = json.loads((ROOT / 'artifacts/prepared-materials/Draw03/rebuild-native-v1/native-rebuild.json').read_text())
    surfaces = {s['cid']: s for s in inventory['sourceSurfaces']}
    rows = []
    families = ['BottomFirst300cm', 'BottomSecond300cm', 'BottomLast300cm', 'BottomEdge350cm', 'Side50cm']
    for row in evidence['meshes']:
        name, cid = row['render'], row['sourceCid']
        original = surfaces[cid]
        with zipfile.ZipFile(original['archive']) as archive:
            raw = archive.read(original['entry'])
        assert hashlib.sha256(raw).hexdigest() == row['sourceSurfaceSha256']
        (args.output / (cid + '.Surface')).write_bytes(raw)
        source = ROOT / 'artifacts/uv-density/native' / (name + '.npz')
        if not source.exists():
            source = ROOT / 'artifacts/uv-density/native-additional' / (name + '.npz')
        assert hashlib.sha256(source.read_bytes()).hexdigest() == row['sourceMeshSha256']
        data = np.load(source)
        p, uv, faces = data['mesh0_positions'], data['mesh0_uv0'], data['mesh0_faces']
        assert uv.dtype == np.float16 and uv.shape[1] == 2
        fixture = struct.pack('<ii', len(p), faces.size) + p.astype('<f4').tobytes() + uv.astype('<f2').tobytes() + faces.astype('<i4').tobytes()
        filename = name.replace(' ', '_') + '.mesh'
        (args.output / filename).write_bytes(fixture)
        family = next(i for i, token in enumerate(families) if token in name)
        lod = 1 if '_LOD1' in name else 2 if '_LOD2' in name else 0
        rows.append(dict(render=name, cid=cid, family=family, lod=lod, template=row['template'], mesh=filename))
    (args.output / 'inputs.json').write_text(json.dumps(dict(rows=rows), indent=2) + '\n')
    print(f'Prepared {len(rows)} source mesh/header pairs; no game registration or generation.')


if __name__ == '__main__':
    main()
