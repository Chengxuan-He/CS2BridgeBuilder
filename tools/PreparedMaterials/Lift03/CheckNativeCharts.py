"""Validate Lift03 native chart arrays against original mesh UV/index streams."""
from collections import Counter
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tools/GeometryMetaprogram'))
from Lift03NativeUvCharts import meshes
from ExportRecipes import numbers

OUT = ROOT/'artifacts/prepared-materials/Lift03/native-charts'


def main():
    report = json.loads((OUT/'report.json').read_text())
    fixture = (ROOT/'artifacts/uv-density/layout-plans.bin').read_bytes()
    assert hashlib.sha256(fixture).hexdigest() == report['fixtureSha256']
    streams = {s['stream']:s for s in report['streams']}
    examined = []
    for render, lod, mesh, source_uv, source_faces, source_positions in meshes(fixture):
        key = render.replace(' ', '_')+'_'+str(mesh)
        record = streams[key]
        data = numbers(OUT/(key+'.bin'))
        ints = data['I32']
        version, vertices, faces, count = ints[:4]
        assert version == 1
        uv = np.array(data['F32'], dtype='<f4').reshape(-1,2)
        assert len(uv) == vertices
        original_points, original_indices = np.unique(source_uv, axis=0, return_inverse=True)
        assert uv.tobytes() == original_points.astype('<f4').tobytes()
        tri = np.array(ints[4:4+faces*3], dtype='<i4').reshape(-1,3)
        assert np.all((tri >= 0) & (tri < vertices))
        original_triangles = np.unique(np.sort(original_indices[source_faces], axis=1), axis=0)
        assert np.array_equal(np.sort(tri, axis=1), original_triangles)
        at = 4+faces*3
        covered = set()
        for _ in range(count):
            nt, ne, flags = ints[at:at+3]
            at += 3
            members = ints[at:at+nt]
            at += nt
            boundary = np.array(ints[at:at+ne*2], dtype='<i4').reshape(-1,2)
            at += ne*2
            covered.update(members)
            edges = Counter()
            for t in tri[list(members)]:
                for a,b in zip(t, np.roll(t,-1)):
                    edges[int(a),int(b)] += 1
            expected = Counter({edge:n-edges[edge[::-1]] for edge,n in edges.items() if n > edges[edge[::-1]]})
            assert expected == Counter(map(tuple,boundary))
            if flags & 1:
                q = uv[tri[list(members)]].astype(np.float64)
                u, v = q[:,1]-q[:,0], q[:,2]-q[:,0]
                area = ((u[:,0]*v[:,1]-u[:,1]*v[:,0])/2).sum()
                span = q.max((0,1))-q.min((0,1))
                # All native Float32 UVs are retained. This check adds no tolerance.
                assert area == span.prod()
        assert at == len(ints)
        q = uv[tri].astype(np.float64)
        u,v = q[:,1]-q[:,0],q[:,2]-q[:,0]
        nondegenerate = set(np.flatnonzero(u[:,0]*v[:,1]-u[:,1]*v[:,0] != 0).tolist())
        assert covered == nondegenerate
        assert hashlib.sha256(source_uv.tobytes()).hexdigest() == record['nativeUvDigest']
        assert hashlib.sha256(source_faces.tobytes()).hexdigest() == record['nativeIndicesDigest']
        examined.append(dict(stream=key, originalUvBitsPreserved=True, originalUvTrianglesPreserved=True,
                             completeNondegenerateCoverage=True, exactBoundaryCheck=True,
                             charts=count, uniqueUvTriangles=faces))
    bindings = json.loads((OUT/'sampler-chart-bindings.json').read_text())
    inventory = json.loads((ROOT/'artifacts/uv-density/structure-tessellated-sources.json').read_text())
    expected = {u['key']:u['sourceCid'] for u in inventory['units'] if u['style']=='Lift03'}
    assert len(bindings) == len(expected) == 5954
    assert {b['key']:b['sourceCid'] for b in bindings} == expected
    for binding in bindings:
        assert binding['uses']
        for use in binding['uses']:
            assert streams[use['stream']]['sourceCid'] == binding['sourceCid']
            assert use['charts']
            assert all(0 <= c < streams[use['stream']]['chartCount'] for c in use['charts'])
    result = dict(status='PASS', streams=examined, exactSamplerSourceBindings=len(bindings),
                  arraysBytes=sum(s['bbnBytes'] for s in streams.values()),
                  sourceNativeUvPreserved=True, pixelFilesCreated=0, encodedTextures=0,
                  runtimeIntegration=False, nativeRendered=False,
                  note='Source topology and data transport verified; no claim of periodicity, '
                       'receiver containment, native shader all-mip behavior or cold-load acceptance.')
    (OUT/'validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='streams'},indent=2))


if __name__=='__main__':
    main()
