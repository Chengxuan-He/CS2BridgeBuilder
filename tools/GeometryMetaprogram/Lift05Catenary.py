"""Record the prototype-only overhead assembly; selected supply is generated separately.

Usage: python Lift05Catenary.py RESEARCH_DIRECTORY [--install]
Membership is reviewed in the original component inventory, not inferred at runtime.
"""
from pathlib import Path
import hashlib
import json
import struct
import sys
import numpy as np
from UvDensityAudit import NativeGeometry

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT/'artifacts/material-remap/Lift05/catenary74'
RESEARCH = Path(sys.argv[1])
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    inventory = json.loads((RESEARCH/'lift05-main-parts.json').read_text())
    codec = NativeGeometry(GAME/'Plugins/x86_64/PipelinePlugin.dll')
    rows, values = [], []
    for lod, group_range in enumerate(((825, 902), (534, 565), None)):
        item = next(x for x in inventory if x['lod'] == lod and x['mesh'] == 0)
        source = RESEARCH/'movable-skin'/(item['source']+'.Geometry')
        assert hashlib.sha256(source.read_bytes()).hexdigest() == item['sha256']
        native = codec.read(source)[0]
        data = np.load(RESEARCH/f'lift05-main-{lod}-0.npz')
        assert np.array_equal(native['faces'], data['indices'])
        assert np.allclose(native['positions'], data['positions'], rtol=1e-7, atol=1e-5)
        selected = np.zeros(len(data['positions']), dtype=bool) if group_range is None else (
            (data['component'] >= group_range[0]) & (data['component'] <= group_range[1]))
        touched = selected[data['indices']]
        assert np.array_equal(touched.any(1), touched.all(1)), 'Mixed part boundary'
        faces = np.flatnonzero(touched.all(1))
        first = int(faces[0]) if len(faces) else 0
        assert np.array_equal(faces, np.arange(first, first+len(faces)))
        values += [len(data['positions']), len(data['indices']), first, len(faces)]
        parts = [p for p in item['parts'] if group_range and group_range[0] <= p['id'] <= group_range[1]]
        # Source bone labels independently identify the overhead moving-deck assembly.
        bone, bones = -1, []
        for line in source.with_suffix('.mesh-0.obj').read_text().splitlines():
            fields = line.split()
            if fields and fields[0] == 'g': bone = int(fields[1][5:]) if fields[1].startswith('bone_') else -1
            elif fields and fields[0] == 'f': bones.append(bone)
        row = dict(lod=lod, geometry=item['source'], sha256=item['sha256'],
                   sourceVertices=len(data['positions']), sourceTriangles=len(data['indices']),
                   firstFace=first, count=len(faces), components=parts,
                   bones=sorted(set(np.asarray(bones)[faces].tolist())))
        if len(faces):
            p=native['positions'][native['faces'][faces]]
            row.update(minimum=p.min((0,1)).tolist(), maximum=p.max((0,1)).tolist())
        rows.append(row)
    assert [r['count'] for r in rows] == [1472, 515, 0]
    raw=b'BBN1'+struct.pack('<7i',1,len(values),0,0,0,0,0)+struct.pack('<%di'%len(values),*values)
    raw += hashlib.sha256(raw).digest()
    (OUT/'Types_Lift05_Catenary.bin').write_bytes(raw)
    if '--install' in sys.argv:
        (ROOT/'assets/HardcodedNumbers/Lift05/Types_Lift05_Catenary.bin').write_bytes(raw)
    (OUT/'source-membership.json').write_text(json.dumps(dict(levels=rows,
        decision='Replace prototype overhead assembly with actual selected-road native supply. Do not remove lift cables, towers, road surfaces or selected auxiliaries.',
        far='Native far source has no overhead assembly; all 1824 far source faces retain previous disposition.'),indent=2)+'\n')
    print('Measured prototype catenary faces:', [r['count'] for r in rows])


if __name__ == '__main__': main()
