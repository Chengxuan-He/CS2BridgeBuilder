"""Build Draw01 chart bindings from actual source faces and existing domain IDs."""
import collections
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'artifacts/agent-material-env/Draw01/python-deps'))
sys.path.insert(0, str(ROOT / 'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays
from shapely.geometry import Polygon
from shapely.ops import unary_union

OUT = ROOT / 'artifacts/prepared-materials/Draw01/native-domain-charts'
NUMBERS = ROOT / 'assets/HardcodedNumbers/Draw01'


def read_bbn(path):
    raw = path.read_bytes()
    assert hashlib.sha256(raw[:-32]).digest() == raw[-32:]
    magic, version, *counts = struct.unpack_from('<II6i', raw)
    assert magic == 0x314e4242 and version == 1
    arrays, offset = [], 32
    for count, dtype in zip(counts, ('<i4','<u4','<i8','<u8','<f4','<f8')):
        arrays.append(np.frombuffer(raw, dtype, count, offset))
        offset += count * np.dtype(dtype).itemsize
    assert offset == len(raw) - 32
    return arrays


def domains(path):
    ints, _, _, _, floats, _ = read_bbn(path)
    ints, floats = decode_arrays(ints, floats)
    assert ints[0] == -2
    at, coordinate, result = 2, 0, []
    for index in range(ints[1]):
        a, b, c, count = map(int, ints[at:at+4]); at += 4
        faces = ints[at:at+abs(count)].tolist(); at += abs(count)
        bounds = None if count < 0 else floats[coordinate:coordinate+4].tolist()
        if bounds is not None: coordinate += 4
        result.append(dict(index=index, anchors=[a,b,c], faces=faces, bounds=bounds))
    assert at == len(ints) and coordinate == len(floats)
    return result


def ring(points):
    points = [tuple(map(float, p)) for p in points[:-1]]
    first = min(range(len(points)), key=points.__getitem__)
    forward = points[first:] + points[:first]
    backward = [forward[0]] + list(reversed(forward[1:]))
    return min(forward, backward)


def normalize(polygon):
    return tuple([tuple(ring(polygon.exterior.coords))] + sorted(tuple(ring(r.coords)) for r in polygon.interiors))


def native_charts(positions, uv, faces):
    # An edge is continuous only when both original position and UV endpoints
    # match exactly. A sampler key or UV-space overlap cannot join source charts.
    vertices = [tuple(map(float, p)) + tuple(map(float, q)) for p, q in zip(positions, uv)]
    parent = list(range(len(faces)))
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]; i = parent[i]
        return i
    edges = collections.defaultdict(list)
    for index, face in enumerate(faces):
        for a, b in zip(face, np.roll(face, -1)):
            edge = tuple(sorted((vertices[a], vertices[b])))
            edges[edge].append(index)
    for connected in edges.values():
        if len(connected) == 2:
            parent[find(connected[1])] = find(connected[0])
    groups = collections.defaultdict(list)
    for i in range(len(faces)): groups[find(i)].append(i)
    result, face_regions = [], collections.defaultdict(list)
    for members in groups.values():
        triangles = [Polygon(uv[faces[i]]) for i in members]
        selected = [p for p in triangles if p.area > 0]
        if not selected: continue
        coverage = unary_union(selected)
        parts = list(coverage.geoms) if coverage.geom_type == 'MultiPolygon' else [coverage]
        for p in parts:
            assert p.is_valid
            chart = len(result); result.append(p)
            for face, triangle in zip(members, triangles):
                if triangle.area > 0 and p.covers(triangle):
                    face_regions[face].append(chart)
    # Robust polygon union may round an intersection onto one side of an
    # original edge. Retain the exact source triangle for such a face instead
    # of expanding a boundary with a tolerance or losing the face's coverage.
    for face, vertices in enumerate(faces):
        triangle = Polygon(uv[vertices])
        if triangle.area > 0 and not face_regions[face]:
            face_regions[face].append(len(result)); result.append(triangle)
    return result, face_regions


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    all_charts, indices, mesh_records = [], {}, []
    shapes = []
    for path in sorted(NUMBERS.glob('UvDomain_*.bin')):
        key = path.stem.removeprefix('UvDomain_')
        render, mesh_index = key.rsplit('_', 1)
        render = render.replace('_Mesh', ' Mesh')
        mesh_index = int(mesh_index)
        native_path = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        native = np.load(native_path)
        prefix = f'mesh{mesh_index}_'
        uv = native[prefix+'uv0'][:, :2].astype(float)
        faces = native[prefix+'faces']; positions = native[prefix+'positions']
        charts, face_charts = native_charts(positions, uv, faces)
        local_to_global = {}
        domain_records = domains(path)
        for domain in domain_records:
            ids = sorted({chart for face in domain['faces'] for chart in face_charts[face]})
            mapped = []
            for index in ids:
                identity = normalize(charts[index])
                if identity not in indices:
                    indices[identity] = len(all_charts); all_charts.append(identity); shapes.append(charts[index])
                local_to_global[index] = indices[identity]
                mapped.append(indices[identity])
            domain['charts'] = sorted(set(mapped))
        mesh_records.append(dict(render=render, mesh=mesh_index,
            sourceSha256=hashlib.sha256(native_path.read_bytes()).hexdigest(),
            domainSha256=hashlib.sha256(path.read_bytes()).hexdigest(), domains=domain_records))
        print(render, mesh_index, len(domain_records), 'domains', len(charts), 'source charts', flush=True)
    # No generated coordinates or sampler-specific source images are stored.
    ints = [1, len(all_charts)] + [0]*len(all_charts) + [len(mesh_records)]
    for mesh in mesh_records:
        ints.append(len(mesh['domains']))
        for domain in mesh['domains']:
            ints.append(len(domain['charts'])); ints.extend(domain['charts'])
    doubles = []
    for index, loops in enumerate(all_charts):
        ints[2+index] = len(ints); ints.append(len(loops))
        for loop in loops:
            ints.extend((len(doubles), len(loop)))
            doubles.extend(v for point in loop for v in point)
    raw = struct.pack('<II6i', 0x314e4242, 1, len(ints), 0, 0, 0, 0, len(doubles))
    raw += np.asarray(ints, dtype='<i4').tobytes() + np.asarray(doubles, dtype='<f8').tobytes()
    raw += hashlib.sha256(raw).digest()
    resource = NUMBERS / 'Draw01UvCharts.bin'
    resource.write_bytes(raw)
    read_ints, _, _, _, _, read_doubles = read_bbn(resource)
    assert np.array_equal(read_ints, ints) and np.array_equal(read_doubles, doubles)
    source = '''// Generated by tools/PreparedMaterials/Draw01/BuildUvCharts.py.
// Embedded numerical arrays: Draw01UvCharts
using System;
namespace BridgeBuilder.Bridges;
internal static class Draw01UvCharts
{
    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        int mesh = (renderName, meshIndex) switch
        {
'''
    source += ''.join(f'            ("{m["render"]}", {m["mesh"]}) => {i},\n' for i, m in enumerate(mesh_records))
    source += '''            _ => -1
        };
        return mesh < 0 ? Array.Empty<MovableUvChart>() : Data.Items[mesh][domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly MovableUvChart[][][] Items = Read();
        private static MovableUvChart[][][] Read()
        {
            var data = HardcodedNumbers.Load("Draw01UvCharts");
            var i = data.I32;
            if (i[0] != 1) throw new InvalidOperationException("Invalid Draw01 UV chart schema.");
            var charts = new MovableUvChart[i[1]];
            for (int c = 0; c < charts.Length; c++)
                charts[c] = MovableUvChart.Read(i, data.F64, i[2 + c]);
            int at = 2 + charts.Length;
            var meshes = new MovableUvChart[i[at++]][][];
            for (int m = 0; m < meshes.Length; m++)
            {
                var domains = meshes[m] = new MovableUvChart[i[at++]][];
                for (int d = 0; d < domains.Length; d++)
                {
                    var references = domains[d] = new MovableUvChart[i[at++]];
                    for (int c = 0; c < references.Length; c++) references[c] = charts[i[at++]];
                }
            }
            return meshes;
        }
    }
}
'''
    (ROOT / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01UvCharts.cs').write_text(source)
    summary = dict(meshes=len(mesh_records), domains=sum(len(m['domains']) for m in mesh_records),
        mappedDomains=sum(bool(d['charts']) for m in mesh_records for d in m['domains']),
        uniqueReferencedCharts=len(all_charts), sourceLoops=sum(len(c) for c in all_charts),
        sourcePoints=len(doubles)//2, resourceBytes=len(raw), textureWrites=0)
    (OUT / 'domain-charts.json').write_text(json.dumps(dict(summary=summary, meshes=mesh_records,
        charts=all_charts, resourceSha256=hashlib.sha256(raw).hexdigest()), indent=2)+'\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__': main()
