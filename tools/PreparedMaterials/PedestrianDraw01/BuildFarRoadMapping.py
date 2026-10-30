"""Extract finite LOD2 walking charts from retained original-mesh evidence.

No textures are produced. Every affine chart is checked against both original
triangles, and remains a distinct appearance domain rather than a repeat unit.
"""
import argparse
import hashlib
import json
import struct
from pathlib import Path
from ExportRecipes import numbers


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('output', type=Path)
    parser.add_argument('--research', type=Path, required=True,
                        help='Retained original geometry research directory, containing movable-skin/.')
    args = parser.parse_args()
    repo = args.repository.resolve()
    evidence = repo / 'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json'
    audit = json.loads(evidence.read_text())
    level = audit['levels'][2]
    assert level['geometrySHA256'] == hashlib.sha256((args.research / 'movable-skin/PedestrianDrawBridge01_LOD2 Mesh.Geometry').read_bytes()).hexdigest()
    faces = level['walkwayFaces']
    assert len(faces) == 16
    rows = []
    for first, second in zip(faces[::2], faces[1::2]):
        assert first['bone'] == second['bone']
        positions = first['positions'] + second['positions']
        uv = first['uv0'] + second['uv0']
        x0, x1 = min(p[0] for p in positions), max(p[0] for p in positions)
        z0, z1 = min(p[2] for p in positions), max(p[2] for p in positions)
        corners = {(p[0], p[2]): t for p, t in zip(positions, uv)}
        assert len(corners) == 4
        base = corners[x0, z0]
        gradients = []
        for axis in range(2):
            dx = (corners[x1, z0][axis] - base[axis]) / (x1 - x0)
            dz = (corners[x0, z1][axis] - base[axis]) / (z1 - z0)
            gradients.append([f32(dx), 0., f32(dz), f32(base[axis] - dx*x0 - dz*z0)])
        error = max(abs(sum(p[i]*g[i] for i in range(3)) + g[3] - t[axis])
                    for p, t in zip(positions, uv) for axis, g in enumerate(gradients))
        assert error < 1e-7, (first['face'], error)
        rows.append(dict(faces=[first['face'], second['face']], bone=first['bone'],
                         bounds=[x0,x1,z0,z1], uvMin=[min(t[a] for t in uv) for a in range(2)],
                         uvMax=[max(t[a] for t in uv) for a in range(2)],
                         u=gradients[0], v=gradients[1], maximumVertexUvError=error))
    rows.sort(key=lambda r:r['bounds'][2])
    assert [r['bone'] for r in rows] == [0,0,2,2,1,1,0,0]
    physical = numbers(repo/'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_PedestrianDraw01RoadPartitions_cs.bin')
    for index, r in enumerate(rows):
        r['sourceBounds'] = list(r['bounds'])
        # The existing generator reconciles all LODs to the detailed motion gap.
        # Stretch each finite terminal chart to that already-authorized endpoint;
        # retaining its old affine slope would sample outside its original island.
        if index == 3:
            r['bounds'][3] = -physical['F32'][0]
        elif index == 4:
            r['bounds'][2] = physical['F32'][0]
        old0,old1 = r['sourceBounds'][2:]
        new0,new1 = r['bounds'][2:]
        zscale = (old1-old0)/(new1-new0)
        zoffset = old0-new0*zscale
        for key in ('u','v'):
            g = r[key]
            g[3] = f32(g[3]+g[2]*zoffset)
            g[2] = f32(g[2]*zscale)
    width_checks = []
    # Exercise the finite-source width map used by Patches. Positions follow the
    # corresponding full-width affine deformation; UVs must remain authored UVs.
    for delta in (0., 2., 8., 24.):
        errors = []
        for r in rows:
            half = r['bounds'][1]
            scale = f32(half / f32(half + delta))
            for face in faces:
                if face['face'] not in r['faces']:
                    continue
                for position, uv in zip(face['positions'], face['uv0']):
                    x = f32(position[0] * f32(f32(half + delta) / half))
                    old0,old1 = r['sourceBounds'][2:]
                    new0,new1 = r['bounds'][2:]
                    z = f32(new0+(position[2]-old0)*(new1-new0)/(old1-old0))
                    for axis, key in enumerate(('u','v')):
                        g = r[key]
                        sampled = f32(f32(f32(g[0] * scale)*x) + f32(g[2]*z))
                        sampled = f32(sampled + g[3])
                        errors.append(abs(sampled - uv[axis]))
        assert max(errors) < 2e-7
        width_checks.append(dict(halfWidthDelta=delta,maximumUvError=max(errors),vertexChannelSamples=len(errors)))
    ints = [1,len(rows)] + [v for r in rows for v in [r['bone'], *r['faces']]]
    floats = [v for r in rows for v in r['bounds']+r['uvMin']+r['uvMax']+r['u']+r['v']]
    payload = b'BBN1'+struct.pack('<i6i',1,len(ints),0,0,0,len(floats),0)
    payload += struct.pack('<'+'i'*len(ints),*ints)+struct.pack('<'+'f'*len(floats),*floats)
    payload += hashlib.sha256(payload).digest()
    resource = repo / 'assets/HardcodedNumbers/PedestrianDraw01/Types_PedestrianDraw01_FarRoadMapping.bin'
    resource.write_bytes(payload)
    args.output.mkdir(parents=True,exist_ok=True)
    report = dict(style='PedestrianDraw01',lod=2,surface='0f4e5a3c788ce4bbd5f18bfff2224ea1',
                  evidenceSHA256=hashlib.sha256(evidence.read_bytes()).hexdigest(),
                  geometrySHA256=level['geometrySHA256'],resourceSHA256=hashlib.sha256(payload).hexdigest(),
                  charts=rows,widthChecks=width_checks, mapping='Original finite chart projections; no texture extraction or periodicity claim.',
                  limitations='Source-coordinate verification only; selected-road dispatch and native rendering require integration.')
    (args.output/'far-road-mapping.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps(dict(charts=len(rows),bytes=len(payload),maximumUvError=max(r['maximumVertexUvError'] for r in rows))))


if __name__ == '__main__':
    main()
