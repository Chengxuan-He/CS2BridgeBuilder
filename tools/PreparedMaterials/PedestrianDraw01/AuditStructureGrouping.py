"""Group every original PedestrianDraw01 domain once; do not infer new texture units."""
from collections import Counter, defaultdict
import hashlib
import json
from pathlib import Path
import sys

import numpy as np

from ExportRecipes import numbers
sys.path.insert(0, str(Path(__file__).resolve().parents[3]/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays

ROOT = Path(__file__).resolve().parents[3]
STYLE = 'PedestrianDraw01'
OUT = ROOT / 'artifacts/prepared-materials/PedestrianDraw01/structure-units-v1'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def reference(path):
    return dict(path=str(path.resolve()), sha256=digest(path))


def save(name, value):
    path = OUT / name
    path.write_text(json.dumps(value, indent=2) + '\n')
    return path


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    shared = ROOT / 'artifacts/prepared-materials/PedestrianDraw01/shared-sampling'
    chart_path = shared / 'native-charts.json'
    charts = json.loads(chart_path.read_text())
    chart_lookup = {c['key']: c for c in charts['charts']}
    bindings = json.loads((shared / 'native-bindings.json').read_text())['nativeDependencyGraph']
    repeat = ROOT / 'artifacts/prepared-materials/PedestrianDraw01/repeat-units-v2'
    units = json.loads((repeat / 'trial-v1/persistent-manifest.json').read_text())['units']
    integration_path = repeat / 'single-cycle-v1/integration.json'
    integration = json.loads(integration_path.read_text())
    assert integration['published'] and integration['mipDataVerified']
    reuse_path = OUT / 'original-reuse-check.json'
    reuse = json.loads(reuse_path.read_text())
    assert reuse['status'] == 'PASS' and reuse['sourceSurfaceIdentityPreserved']
    assert reuse['scenarioDomains'] == 1380 and reuse['scenarioFaces'] == 2760
    evidence, domains, grouped = [], [], defaultdict(list)
    for mesh in charts['meshes']:
        native_path = ROOT / 'artifacts/uv-density/native' / (mesh['render'] + '.npz')
        assert digest(native_path) == mesh['nativeMeshSHA256']
        native = np.load(native_path)
        prefix = f'mesh{mesh["mesh"]}_'
        faces, uvs, positions = (native[prefix + name] for name in ('faces', 'uv0', 'positions'))
        path = ROOT / f'assets/HardcodedNumbers/{STYLE}' / ('UvDomain_' + mesh['render'].replace(' ', '_') + f'_{mesh["mesh"]}.bin')
        assert digest(path) == mesh['measuredDomainSHA256']
        data = numbers(path)
        ints, floats = (a.tolist() for a in decode_arrays(data['I32'], data['F32']))
        assert ints[0] == -2 and ints[1] == len(mesh['domainCharts'])
        at, fp = 2, 0
        source = bindings[mesh['sourceCid']]['sha256']
        rigid_pillar = 'Pillar' in mesh['render'] and 'Base' not in mesh['render']
        rigid_glass = mesh['mesh'] == 1
        if rigid_pillar:
            # The measured support producer translates each disconnected column
            # by its side. Every native face has the same side on all vertices.
            sides = np.sign(positions[faces, 0])
            assert np.all(sides != 0) and np.all(sides == sides[:, :1])
        for chart_domain in mesh['domainCharts']:
            a, b, c, count = ints[at:at+4]
            at += 4
            members = ints[at:at+abs(count)]
            at += abs(count)
            if count < 0:
                domain_uv = uvs[[a, b, c], :2]
            else:
                x0, y0, x1, y1 = floats[fp:fp+4]
                fp += 4
                domain_uv = np.array([[x0, y0], [x1, y0], [x1, y1], [x0, y1]])
            # Actual triangle vertices must belong to the same filled measured
            # rectangle as the domain representation. A chart bbox is not used.
            actual_uv = uvs[faces[list(members)], :2].reshape(-1, 2)
            unit_index = None
            for i in (1, 0):
                unit = units[i]
                if source == unit['sourceDigest'] and all(np.all(q >= unit['min']) and np.all(q <= unit['max']) for q in (domain_uv, actual_uv)):
                    unit_index = i
                    break
            if unit_index is not None:
                kind, category = 'REPEAT_BASE', units[unit_index]['id']
            elif rigid_glass:
                kind, category = 'ORIGINAL', 'rigid-glass-native-template-2'
            elif rigid_pillar:
                kind, category = 'ORIGINAL', 'rigid-side-columns'
            elif 'PillarBase' in mesh['render']:
                kind, category = 'NONREPEATING', 'stretched-foundation-mixed-native-islands'
            elif '_LOD2' in mesh['render']:
                kind, category = 'NONREPEATING', 'far-lod-512-atlas-mixed-authored-detail'
            else:
                keys = chart_domain['chartKeys']
                category = 'main-body-mixed-chart-domain' if len(keys) != 1 else 'main-body-unproven-pattern-island'
                kind = 'NONREPEATING'
            identity = f'{mesh["render"]}|{mesh["mesh"]}|{chart_domain["domain"]}'
            domains.append(dict(id=identity, sourceDigest=source, receivers=len(members)))
            grouped[(source, kind, category)].append(identity)
            evidence.append(dict(id=identity, sourceCid=mesh['sourceCid'], sourceFaces=list(members),
                chartKeys=chart_domain['chartKeys'], domainUv=domain_uv.tolist(),
                actualUvBounds=actual_uv.min(0).tolist() + actual_uv.max(0).tolist(), classification=category,
                mapping='Native UV retained under rigid translation' if kind == 'ORIGINAL' else
                    'Original triangles contained in an evidenced filled repeat rectangle' if kind == 'REPEAT_BASE' else
                    'No demonstrated periodic replacement; do not widen coverage from the bounding box'))
        assert at == len(ints) and fp == len(floats)

    reasons = {
        'rigid-glass-native-template-2': 'Mesh 1 is uniformly translated by PedestrianDraw01Glass.Create at LOD0/1; native UVs and template 2 glass/interior texture references remain unchanged. No cutout conversion or new pixels are permitted.',
        'rigid-side-columns': 'PedestrianDraw01SupportGeometry uses part.Sign for non-footing columns. Every source triangle is verified on one nonzero side; rigid translation preserves triangle edges and native UV density at all three LODs.',
        'stretched-foundation-mixed-native-islands': 'Footing X coordinates stretch with source X/4.0821533203125. Original atlas islands include distinct base faces; the two walking-surface periods are neither the same source digest nor the same pattern. Requires measured foundation pattern periods and per-island native coverage, not road-unit aliases.',
        'far-lod-512-atlas-mixed-authored-detail': 'LOD2 uses a separate 512-pixel atlas with merged frame, housing, base and deck details. High-detail 4096 road units have a different source digest and cannot substitute it. Requires LOD2-specific semantic pattern identification or a verified unchanged native mapping for each width-transformed part.',
        'main-body-mixed-chart-domain': 'This original domain includes multiple disconnected native charts with distinct atlas boundaries. One bounding rectangle would cross other atlas islands. Requires per-part semantic unit evidence and exact chart coverage; no blanket crop or arbitrary face repeat is valid.',
        'main-body-unproven-pattern-island': 'Native faces identify a steel/frame, machinery, housing, rope or deck atlas island outside the two evidenced walking-surface units. Native adjacency alone proves neither a physical repeat period nor full-channel periodic content. Requires measured pattern/part membership or production rigid/native-footprint proof before another unit can be prepared.'}
    code_dir = ROOT / 'src/BridgeBuilder/Bridges/Types/PedestrianDraw01'
    evidence_path = save('membership-evidence.json', dict(style=STYLE, sourceCharts=reference(chart_path),
        sourceBindings=reference(shared / 'native-bindings.json'),
        productionOriginalReuse=reference(reuse_path),
        productionOriginalReuseFixture=reference(OUT / 'original-reuse-fixture.json'),
        productionOriginalReuseRunner=reference(Path(__file__).parent / 'OriginalReuseCheck/Program.cs'),
        runtimeSources=[reference(code_dir / name) for name in ('PedestrianDraw01Glass.g.cs', 'PedestrianDraw01SupportGeometry.g.cs',
            'PedestrianDraw01StructureWidthPlan.cs', 'PedestrianDraw01RepeatUnits.cs')],
        inventoryScope='All recorded original domains in all eleven native mesh/material streams. Receivers count original member triangles, not generated receivers or arbitrary widths.', domains=evidence))
    inventory_path = save('original-domains.json', dict(schema='BridgeBuilder/StructureDomains/1', style=STYLE, domains=domains,
        evidence=reference(evidence_path), finiteScope='Original prototype geometry; generated-width coverage is not asserted.'))
    proposed = [dict(id=u['id'], sourceDigest=u['sourceDigest'], bounds=u['min'] + u['max'],
        physicalPeriod=u['physicalSize'], semanticPattern='Measured native fixed walking-surface pattern from faces ' + str([1035, 1036] if i == 0 else [13243, 13244]),
        evidence=reference(repeat / f'fixed-region-{i}-evidence.json'), mipEvidence=reference(integration_path)) for i, u in enumerate(units)]
    groups = []
    for (source, kind, category), members in sorted(grouped.items()):
        group = dict(id=category + '-' + source[:12], kind=kind, sourceDigest=source, domains=members,
                     evidence=reference(evidence_path))
        if kind.startswith('REPEAT_'):
            group['unit'] = category
        else:
            group['reason'] = reasons[category]
        groups.append(group)
    save('manifest.json', dict(schema='BridgeBuilder/StructureUnits/1', style=STYLE,
        inventory=reference(inventory_path), units=proposed, groups=groups))
    print(json.dumps(dict(domains=len(domains), receivers=sum(d['receivers'] for d in domains),
        classifications=dict(Counter(e['classification'] for e in evidence)), newBakes=0)))


if __name__ == '__main__':
    main()
