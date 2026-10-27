"""Export measured Lift05 receiver cases for native representation development.

This is diagnostic input, not a bake recipe or permission to repeat chart islands.
It consumes the actual ReceiverCheck binary and preserves its mapped source UVs.
"""
import collections
import hashlib
import io
import json
import struct
from pathlib import Path

from BuildUvCharts import ROOT, domains, inputs


def main():
    folder = ROOT / 'artifacts/prepared-materials/Lift05/structure-groups'
    output = ROOT / 'artifacts/prepared-materials/Lift05/remaining-cases'
    output.mkdir(parents=True, exist_ok=True)
    paths = {
        'receivers': folder / 'receivers.json.receivers.bin',
        'regions': folder / 'receivers.json.regions.json',
        'membership': folder / 'group-membership.json',
        'charts': ROOT / 'artifacts/prepared-materials/Lift05/domain-charts/charts.json',
        'sources': ROOT / 'artifacts/prepared-materials/Lift05/rebuild-native-v1/inputs/native-inputs.json',
        'fixture': ROOT / 'artifacts/uv-density/layout-plans.bin',
    }
    evidence = {k: {'path': str(v), 'sha256': hashlib.sha256(v.read_bytes()).hexdigest()}
                for k, v in paths.items()}
    membership = json.loads(paths['membership'].read_text())
    assert membership['receiverEvidence']['sha256'] == evidence['receivers']['sha256']
    assert membership['chartEvidence']['sha256'] == evidence['charts']['sha256']
    groups = {v['id']: v for v in membership['nonOriginal']}
    charts = json.loads(paths['charts'].read_text())
    streams = {(v['name'], v['mesh']): v['domains'] for v in charts['streams']}
    materials = json.loads(paths['sources'].read_text())
    sources = {v['cid']: v for v in materials['sources']}
    bindings = {(v['render'], v['materialSlot']): sources[v['surfaceCid']]
                for v in materials['bindings']}
    geometry = {(name, mesh): (positions, uv, faces)
                for name, mesh, positions, uv, faces in inputs()}
    domain_faces = {key: domains(*key) for key in geometry}
    reader = io.BytesIO(paths['receivers'].read_bytes())

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
    cases = {}
    total = direct_count = 0
    while reader.tell() < len(reader.getbuffer()):
        name = text()
        mesh, delta, count = read('ifi')
        for receiver_index in range(count):
            domain, repeat, direct, n = read('i??i')
            raw = read('f' * (n * 2))
            mapped = [list(raw[i:i + 2]) for i in range(0, len(raw), 2)]
            total += 1
            direct_count += int(direct)
            if direct:
                continue
            identity = f'{name}|{mesh}|{domain}'
            category = groups.get(identity, {}).get('category', 'original-chart-without-direct-reuse')
            counts[(name, mesh, category)] += 1
            # Existing evidenced units have their own native tests. Export the
            # other actual receivers, including both widening and contraction.
            if category.startswith('Lift05.'):
                continue
            key = (name, mesh, category, delta)
            if key in cases:
                continue
            positions, uv, faces = geometry[(name, mesh)]
            face_ids = domain_faces[(name, mesh)][domain]
            ids = sorted(set(int(i) for f in face_ids for i in faces[f]))
            source = bindings[(name, mesh)]
            cases[key] = {
                'render': name, 'mesh': mesh, 'delta': delta,
                'receiverIndex': receiver_index, 'domain': domain,
                'category': category, 'repeatOnly': repeat, 'canUseSource': direct,
                'sourceSurfaceCid': source['cid'], 'sourceDigest': source['sha256'],
                'nativeSource': source['native'], 'sourceFaceIndices': face_ids,
                'sourceVertices': [{'id': i, 'position': positions[i].tolist(), 'uv': uv[i].tolist()}
                                   for i in ids],
                'sourceTriangles': faces[face_ids].tolist(),
                'authoredCharts': [charts['charts'][i] for i in streams[(name, mesh)][domain]],
                'mappedSourceUvs': mapped,
                'limit': 'Mapped UVs are measured receiver outputs, not a new periodic unit. '
                         'Target geometry is reproducible from fixture, render, mesh and delta. '
                         'No triangle repetition, arbitrary crop or normal equivalence is established.',
            }
    regions = json.loads(paths['regions'].read_text())
    metric_names = ('receiverUnits', 'needsPrepared', 'mappedUnits', 'additionalPreparedCoverage')
    metrics = {k: sum(r[k] for r in regions) for k in metric_names}
    assert total == metrics['receiverUnits']
    assert total - direct_count == metrics['needsPrepared'] == sum(counts.values())
    metrics.update(directSourceReuse=direct_count,
                   remainingAfterKnownUnits=metrics['needsPrepared'] - metrics['additionalPreparedCoverage'])
    report = {
        'style': 'Lift05', 'scope': 'Finite six-delta actual-plan receiver sample; no saved bridge generated.',
        'evidence': evidence, 'metrics': metrics,
        'nonDirectCountsByOriginalDomainCategory': [dict(render=n, mesh=m, category=c, receivers=v)
            for (n, m, c), v in sorted(counts.items())],
        'representativeCount': len(cases), 'cases': list(cases.values()),
        'completeOwnerMaterialBytes': None, 'productionGenerationVerified': False,
    }
    path = output / 'cases.json'
    path.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest(),
                      'metrics': metrics, 'representativeCount': len(cases)}))


if __name__ == '__main__':
    main()
