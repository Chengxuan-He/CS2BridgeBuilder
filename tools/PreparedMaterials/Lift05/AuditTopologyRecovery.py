"""Measure exact source-footprint recovery without changing packaged charts."""
import collections
import hashlib
import io
import json
import struct
from BuildUvCharts import ROOT, inputs, domains, extract, Polygon
from shapely.geometry import Point, LineString


def main():
    source = ROOT / 'artifacts/prepared-materials/Lift05/structure-groups/receivers.json.receivers.bin'
    output = ROOT / 'artifacts/prepared-materials/Lift05/topology-recovery'
    output.mkdir(parents=True, exist_ok=True)
    bindings = {}
    reports = []
    for name, mesh, positions, uv, faces in inputs():
        old, old_map, old_missing = extract(positions, uv, faces)
        new, new_map, new_missing = extract(positions, uv, faces, recover_overlaps=True)
        shapes = []
        for chart in new:
            shape = Polygon()
            for loop in chart['loops']:
                shape = shape.symmetric_difference(Polygon(loop))
            shapes.append(shape)
        recovered = set(new_map) - set(old_map)
        assert set(old_map) <= set(new_map)
        # Previously established footprints remain byte-for-byte unchanged.
        for face, index in old_map.items():
            assert old[index] == new[new_map[face]]
        for domain, face_ids in enumerate(domains(name, mesh)):
            if not any(face in recovered for face in face_ids):
                continue
            refs = sorted({new_map[f] for f in face_ids if f in new_map})
            bindings[(name, mesh, domain)] = [shapes[i] for i in refs]
        reports.append(dict(render=name, mesh=mesh, beforeCharts=len(old), afterCharts=len(new),
                            beforeMissingParts=old_missing, afterMissingParts=new_missing,
                            recoveredFaces=len(recovered)))
    reader = io.BytesIO(source.read_bytes())
    def read(fmt):
        return struct.unpack('<' + fmt, reader.read(struct.calcsize('<' + fmt)))
    def text():
        length = shift = 0
        while True:
            value, = read('B')
            length |= (value & 127) << shift
            if value < 128:
                return reader.read(length).decode()
            shift += 7
    counts = collections.Counter()
    examples = []
    while reader.tell() < len(reader.getbuffer()):
        name = text()
        mesh, delta, count = read('ifi')
        for _ in range(count):
            domain, repeat, direct, n = read('i??i')
            values = read('f' * (n * 2))
            if direct or repeat or (name, mesh, domain) not in bindings:
                continue
            shapes = bindings[(name, mesh, domain)]
            points = [values[i:i + 2] for i in range(0, len(values), 2)]
            contained = True
            for i in range(0, n, 3):
                triangle = Polygon(points[i:i + 3])
                if triangle.area == 0:
                    unique = sorted(set(points[i:i + 3]))
                    triangle = Point(unique[0]) if len(unique) == 1 else LineString(unique)
                if not any(shape.covers(triangle) for shape in shapes):
                    contained = False
                    break
            counts[(name, mesh, contained)] += 1
            if contained:
                examples.append(dict(render=name, mesh=mesh, delta=delta, domain=domain))
    report = dict(style='Lift05', sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                  geometryChanged=False, packagedChartsChanged=False, repeatInferred=False,
                  exactOriginalTriangleUnion=True, streams=reports,
                  receivers=[dict(render=n, mesh=m, contained=c, count=v)
                             for (n, m, c), v in sorted(counts.items())],
                  recoveredContainedReceivers=len(examples), examples=examples,
                  limit='Offline source footprint result only. Native runtime containment and '
                        'persisted owner rendering still require verification.')
    path = output / 'audit.json'
    path.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k not in ('examples', 'streams')}))


if __name__ == '__main__':
    main()
