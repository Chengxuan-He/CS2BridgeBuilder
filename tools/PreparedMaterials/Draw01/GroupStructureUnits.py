"""One complete Draw01 domain grouping; reuse only already verified genuine units."""
import collections
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, read_bbn
from shapely.geometry import Polygon

OUT = ROOT / 'artifacts/prepared-materials/Draw01/structure-units'
AUDIT = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json'
BASE = ROOT / 'artifacts/prepared-materials/Draw01'


def load(path):
    return json.loads(path.read_text(encoding='utf-8-sig'))


def ref(path):
    return dict(path=str(path.resolve()), sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def save(name, value):
    (OUT / name).write_text(json.dumps(value, indent=2) + '\n')


def cid(value):
    data = b''.join(struct.pack('<i', value['m_GuidPart' + str(i)]) for i in range(1, 5))
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


def signature(triangle, values):
    return triangle, np.asarray(values, dtype='<f4').tobytes()


def main():
    global OUT
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--receivers', type=Path, default=BASE / 'receiver-plans.json.receivers.json')
    parser.add_argument('--output', type=Path, default=OUT)
    parser.add_argument('--no-integrate', action='store_true', help='Write evidence only; preserve runtime files.')
    args = parser.parse_args()
    OUT = args.output
    OUT.mkdir(parents=True, exist_ok=True)
    chart_path = BASE / 'native-domain-charts/domain-charts.json'
    charts = load(chart_path)
    source_map = load(BASE / 'source-sharing/sampling-map.json')
    sources = {s['cid']: s for s in source_map['sources']}
    renders = {}
    for selected in load(AUDIT)['selected'].values():
        data = selected['data']
        if 'm_SurfaceAssets' in data:
            renders[data['m_Name']] = [cid(s) for s in data['m_SurfaceAssets']]
    needed = {renders[m['render']][m['mesh']] for m in charts['meshes']} - sources.keys()
    game = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')
    for family in ('Game', 'BridgesAndPorts'):
        for archive in (game / family).glob('Blob*.cok'):
            if not needed:
                break
            with zipfile.ZipFile(archive) as z:
                for name in z.namelist():
                    if not name.endswith('.Surface.cid'):
                        continue
                    identity = z.read(name).decode().strip()
                    if identity in needed:
                        raw = z.read(name[:-4])
                        sources[identity] = dict(cid=identity, sha256=hashlib.sha256(raw).hexdigest(),
                            template=struct.unpack_from('<i', raw, 2)[0], archive=str(archive), entry=name[:-4])
                        needed.remove(identity)
    assert not needed, needed
    requirements = load(BASE / 'genuine-unit-requirements/manifest.json')['roadCandidates']
    products = {u['id']: u for u in load(BASE / 'reviewed-units/report.json')['units']}
    known = [dict(id=u['candidateId'], digest=u['sourceDigest'], bounds=u['region']['min'] + u['region']['max'],
        physicalPeriod=u['proposedPhysicalPeriodMetres'], key=products[u['candidateId']]['key']) for u in requirements]
    geometry = {}
    domain_lookup = collections.defaultdict(list)
    domain_faces = {}
    corrections, _, _, _, correction_uv, _ = read_bbn(ROOT / 'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01UndersideUv_cs.bin')
    records = []
    for mesh in charts['meshes']:
        name, mi = mesh['render'], mesh['mesh']
        source = sources[renders[name][mi]]
        native_path = ROOT / 'artifacts/uv-density/native' / (name + '.npz')
        native = np.load(native_path)
        assert hashlib.sha256(native_path.read_bytes()).hexdigest() == mesh['sourceSha256']
        uv = native[f'mesh{mi}_uv0'][:, :2].astype(float)
        faces = native[f'mesh{mi}_faces']
        if mi == 0 and name in ('DrawBridge01 Mesh', 'DrawBridge01_LOD1 Mesh'):
            offset, start, count = (2, 0, 176) if name == 'DrawBridge01 Mesh' else (178, 352, 158)
            uv[corrections[offset:offset + count]] = correction_uv[start:start + count * 2].reshape(-1, 2)
        geometry[name, mi] = uv
        for domain in mesh['domains']:
            triangle = domain['bounds'] is None
            values = uv[domain['anchors']].flatten() if triangle else domain['bounds']
            points = uv[faces[domain['faces']]].reshape(-1, 2)
            low, high = points.min(axis=0), points.max(axis=0)
            sampler_points = uv[domain['anchors']] if triangle else np.asarray(domain['bounds']).reshape(2, 2)
            repeat = next((u for u in known if source['sha256'] == u['digest'] and
                np.all(low >= u['bounds'][:2]) and np.all(high <= u['bounds'][2:]) and
                np.all(sampler_points >= u['bounds'][:2]) and np.all(sampler_points <= u['bounds'][2:])), None)
            record = dict(id=f'{name}|{mi}|{domain["index"]}', sourceCid=source['cid'], sourceDigest=source['sha256'],
                render=name, mesh=mi, domain=domain['index'], faces=len(domain['faces']), charts=domain['charts'],
                bounds=[*low, *high], unit=repeat['id'] if repeat else None, uniqueReceivers=0,
                uniqueContainedReceivers=0, ambiguousReceiverMatches=0)
            records.append(record)
            domain_faces[record['id']] = set(domain['faces'])
            domain_lookup[name, mi, signature(triangle, values)].append(record)
    polygons = [Polygon(c[0], c[1:]) for c in charts['charts']]
    observations_path = args.receivers
    observations = load(observations_path)
    statistics = collections.Counter()
    for observation in observations:
        tri = observation['Triangle']
        values = [v for k in ('a', 'b', 'c') for v in observation[k]] if tri else observation['bounds']
        matching = domain_lookup[observation['name'], observation['mesh'], signature(tri, values)]
        if 'domain' in observation:
            matching = [d for d in matching if d['domain'] == observation['domain']]
        assert matching
        if len(matching) != 1:
            statistics['ambiguousObservations'] += 1
            for d in matching:
                d['ambiguousReceiverMatches'] += 1
            continue
        d = matching[0]
        if 'domain' in observation:
            assert observation['sourceFace'] in domain_faces[d['id']], 'Receiver face does not belong to its declared parent domain'
        d['uniqueReceivers'] += 1
        statistics['uniqueObservations'] += 1
        q = np.asarray(observation['points'], dtype=np.float32).astype(float)
        if tri:
            a, b, c = (np.asarray(observation[k], dtype=np.float32).astype(float) for k in ('a', 'b', 'c'))
            atlas = a + q @ np.asarray((b-a, c-a))
        else:
            bounds = np.asarray(observation['bounds'], dtype=np.float32).astype(float)
            atlas = bounds[:2] + q * (bounds[2:] - bounds[:2])
        contained = not observation['RepeatOnly'] and all(any(polygons[i].covers(Polygon(atlas[j:j+3]))
            for i in d['charts']) for j in range(0, len(atlas), 3))
        d['uniqueContainedReceivers'] += int(contained)
    groups = collections.defaultdict(list)
    for d in records:
        if d['unit']:
            kind, label = 'REPEAT_BASE', d['unit']
        elif d['uniqueReceivers'] and d['uniqueContainedReceivers'] == d['uniqueReceivers'] and not d['ambiguousReceiverMatches']:
            kind, label = 'ORIGINAL', 'finite-receiver-contained'
        else:
            kind = 'NONREPEATING'
            label = ('zero-area-or-no-chart' if not d['charts'] else
                'receiver-identity-ambiguous' if d['ambiguousReceiverMatches'] else
                'unobserved-transform' if not d['uniqueReceivers'] else 'outside-authored-chart')
        d['classification'], d['constraint'] = kind, label
        # Keep each render/LOD explicit; source digest sharing alone is not part identity.
        groups[d['sourceDigest'], d['render'], d['mesh'], kind, label].append(d['id'])
    save('membership.json', dict(style='Draw01', nativeAudit=ref(AUDIT), domainCharts=ref(chart_path),
        receiverFixture=ref(observations_path), sources=list(sources.values()), statistics=dict(statistics), domains=records,
        scope='Complete 25-mesh, 22939-domain original BBN inventory. Receiver counts include only uniquely identified existing observations; ambiguous matches are recorded separately, never duplicated as receivers. Width fixtures are finite and do not prove arbitrary-width coverage.'))
    save('original-domains.json', dict(schema='BridgeBuilder/StructureDomains/1', style='Draw01',
        domains=[dict(id=d['id'], sourceDigest=d['sourceDigest'], receivers=d['uniqueReceivers']) for d in records]))
    used = {d['unit'] for d in records if d['unit']}
    unit_records = [dict(id=u['id'], sourceDigest=u['digest'], semanticPattern='Verified original ' + u['id'] + ' pattern; shared unit already prepared with native full-mip validation.',
        bounds=u['bounds'], physicalPeriod=u['physicalPeriod'], bindingKey=u['key'],
        evidence=ref(BASE / 'genuine-unit-requirements/manifest.json'),
        mipEvidence=ref(BASE / f'reviewed-units/native-check-{u["id"]}.json')) for u in known if u['id'] in used]
    reasons = {
        'finite-receiver-contained': 'All uniquely identified finite-fixture receivers stay inside their own native chart; unchanged Surface or verified descriptor-only conversion preserves the native graph. This is not an arbitrary-width proof.',
        'zero-area-or-no-chart': 'Source UV coverage has no two-dimensional chart; rank-deficient mapping cannot establish a two-axis periodic unit. Preserve rigid original uses; changed singular frames require explicit native handling.',
        'receiver-identity-ambiguous': 'Saved receiver observations omit parent domain IDs and this original frame matches several domains. Do not assign another chart by sampler equality. Re-record parent domain IDs before certifying changed receiver coverage; no additional periodic unit is evidenced for this atlas island.',
        'unobserved-transform': 'This native domain has no uniquely identified changed receiver in the retained fixture. Preserve compatible native references, but do not claim its varying-width coverage or synthesize a repeat unit from its bounds.',
        'outside-authored-chart': 'A uniquely identified transformed receiver exits this authored chart. Its original atlas island is outside the four verified patterns. Requires measured repeated base/detail evidence or a native nonperiodic mapping; a per-triangle reflected crop is not a valid unit.'}
    entries = []
    for i, ((digest, render, mesh, kind, label), ids) in enumerate(groups.items()):
        group = dict(id=f'group-{i}', sourceDigest=digest, render=render, mesh=mesh, kind=kind,
            domains=ids, evidence=ref(OUT / 'membership.json'))
        if kind.startswith('REPEAT_'):
            group['unit'] = label
        else:
            group['reason'] = reasons[label]
        entries.append(group)
    save('manifest.json', dict(schema='BridgeBuilder/StructureUnits/1', style='Draw01',
        inventory=ref(OUT / 'original-domains.json'), units=unit_records, groups=entries))
    # Register only measured genuine-unit rectangles, not their chart bounding boxes.
    selected = [u for u in known if u['id'] in used]
    floats = [v for u in selected for v in u['bounds']]
    raw = struct.pack('<II6i', 0x314e4242, 1, 0, 0, 0, 0, 0, len(floats)) + struct.pack('<' + 'd'*len(floats), *floats)
    raw += hashlib.sha256(raw).digest()
    if not args.no_integrate:
        (ROOT / 'assets/HardcodedNumbers/Draw01/Draw01RepeatRegions.bin').write_bytes(raw)
    source = '''// Generated by tools/PreparedMaterials/Draw01/GroupStructureUnits.py.
// Embedded numerical arrays: Draw01RepeatRegions
using System.Collections.Generic;
namespace BridgeBuilder.Bridges;
internal static partial class MovableRepeatUnits
{
    static partial void Draw01Regions(string sourceDigest, List<Region> regions)
    {
'''
    source += f'        if (sourceDigest != "{selected[0]["digest"]}") return;\n        var values = Draw01RegionData.Values.F64;\n'
    for i, unit in enumerate(selected):
        source += f'        regions.Add(new Region("{unit["key"]}", values[{i*4}], values[{i*4+1}], values[{i*4+2}], values[{i*4+3}]));\n'
    source += '''    }
    private static class Draw01RegionData
    {
        static Draw01RegionData() { }
        internal static readonly HardcodedNumbers Values = HardcodedNumbers.Load("Draw01RepeatRegions");
    }
}
'''
    if not args.no_integrate:
        (ROOT / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01RepeatRegions.cs').write_text(source)
    summary = dict(domains=len(records), meshes=len(charts['meshes']), groups=len(entries),
        reusedUnits=[u['id'] for u in selected], newBakes=0,
        classifications=dict(collections.Counter(d['classification'] for d in records)), observations=dict(statistics))
    save('summary.json', summary)
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
