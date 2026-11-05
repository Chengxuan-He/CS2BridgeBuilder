"""Read an existing owner's saved geometry and match exact prototype catenary corners.

Usage: python CheckCatenaryOwner.py RESEARCH_DIRECTORY EXISTING_OWNER_DIRECTORY
This diagnostic never creates, repairs or recovers an owner.
"""
import hashlib
import json
from collections import Counter
from pathlib import Path
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry


def signatures(mesh, faces):
    corners = np.concatenate((mesh['positions'][:, 1:3], mesh['uv0'][:, :2]), axis=1).astype('<f4')
    return Counter(tuple(sorted(corners[v].tobytes() for v in face)) for face in faces)


def main():
    research, owner = map(Path, sys.argv[1:])
    imported = Path('C:/Users/admin/AppData/LocalLow/Colossal Order/Cities Skylines II/ImportedData').resolve()
    assert owner.resolve().parent == imported and owner.name.startswith('b') and owner.is_dir()
    out = ROOT/'artifacts/material-remap/Lift05/catenary74'
    levels = json.loads((out/'source-membership.json').read_text())['levels']
    codec = NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
    results = []
    for lod in (0, 1):
        row = levels[lod]
        source = codec.read(research/'movable-skin'/(row['geometry']+'.Geometry'))[0]
        faces = source['faces'][row['firstFace']:row['firstFace']+row['count']]
        expected, found, files = signatures(source, faces), Counter(), []
        pattern = 'Mechanism*.Geometry' if lod == 0 else 'Mechanism LOD1*.Geometry'
        for file in (owner/'Geometry').glob(pattern):
            if lod == 0 and ' LOD' in file.name: continue
            for mesh in codec.read(file): found.update(signatures(mesh, mesh['faces']))
            files.append(dict(name=file.name, sha256=hashlib.sha256(file.read_bytes()).hexdigest()))
        matched = sum((expected & found).values())
        results.append(dict(lod=lod, expectedFaces=len(faces), exactSourceYZUvTriangleMatches=matched, files=files))
        print(lod, 'exact Y/Z/sourceUV face matches', matched, 'of', len(faces))
    (out/'saved-owner73-provenance.json').write_text(json.dumps(dict(owner=owner.name,
        method='Exact Float32 Y/Z and original UV per corner, permutation-independent triangle multiset including multiplicity. X excluded because width mapping changes it.',
        results=results), indent=2)+'\n')


if __name__ == '__main__': main()
