"""Check Draw01 domain-local chart bindings against saved receiver observations."""
import collections
import json
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, OUT, read_bbn
from shapely.geometry import Polygon


def signature(triangle, values):
    return triangle, np.asarray(values, dtype='<f4').tobytes()


def main():
    data = json.loads((OUT/'domain-charts.json').read_text())
    charts = [Polygon(c[0], c[1:]) for c in data['charts']]
    by_domain_shape = collections.defaultdict(list)
    corrected, _, _, _, correction_uv, _ = read_bbn(ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01UndersideUv_cs.bin')
    covered_faces = zero_area_faces = domains_without_chart = 0
    for record in data['meshes']:
        source = np.load(ROOT/'artifacts/uv-density/native'/(record['render']+'.npz'))
        prefix = f'mesh{record["mesh"]}_'
        original_uv = source[prefix+'uv0'][:, :2].astype(float)
        faces = source[prefix+'faces']
        mapped_uv = original_uv.copy()
        if record['mesh'] == 0 and record['render'] in ('DrawBridge01 Mesh','DrawBridge01_LOD1 Mesh'):
            offset, start, count = (2, 0, 176) if record['render']=='DrawBridge01 Mesh' else (178, 352, 158)
            mapped_uv[corrected[offset:offset+count]] = correction_uv[start:start+count*2].reshape(-1,2)
        for domain in record['domains']:
            associated = [charts[i] for i in domain['charts']]
            if not associated: domains_without_chart += 1
            for face in domain['faces']:
                triangle = Polygon(original_uv[faces[face]])
                if triangle.area == 0:
                    zero_area_faces += 1
                else:
                    assert any(c.covers(triangle) for c in associated), (record['render'],domain['index'],face)
                    covered_faces += 1
            triangle = domain['bounds'] is None
            values = mapped_uv[domain['anchors']].flatten() if triangle else domain['bounds']
            by_domain_shape[(record['render'],record['mesh'],signature(triangle,values))].append(domain)
    observations = json.loads((ROOT/'artifacts/prepared-materials/Draw01/receiver-plans.json.receivers.json').read_text())
    counts = collections.Counter()
    by_render = collections.defaultdict(collections.Counter)
    cases = []
    for observation in observations:
        triangle = observation['Triangle']
        values = [v for field in ('a','b','c') for v in observation[field]] if triangle else observation['bounds']
        key = observation['name'], observation['mesh'], signature(triangle,values)
        domains = by_domain_shape.get(key, [])
        counts['observations'] += 1
        if not domains:
            counts['unmatchedObservations'] += 1
            continue
        counts['matchedObservations'] += 1
        counts['matchingDomains'] += len(domains)
        q = np.asarray(observation['points'], dtype=np.float32).astype(float)
        if triangle:
            a,b,c = (np.asarray(observation[f], dtype=np.float32).astype(float) for f in ('a','b','c'))
            atlas = a+q @ np.array((b-a,c-a))
        else:
            bounds = np.asarray(observation['bounds'], dtype=np.float32).astype(float)
            atlas = bounds[:2]+q*(bounds[2:]-bounds[:2])
        tests = [Polygon(atlas[i:i+3]) for i in range(0,len(atlas),3)]
        # Fixtures did not save source domain indices. Require EVERY matching
        # domain to prove coverage, never choose a convenient cross-chart match.
        covered = all(all(p.area == 0 or any(charts[c].covers(p) for c in domain['charts']) for p in tests) for domain in domains)
        eligible = covered and not observation['RepeatOnly']
        kind = 'nativeChartCovered' if eligible else 'unresolved'
        counts[kind] += 1; by_render[observation['name']][kind] += 1
        if eligible and not observation['canUseSource']:
            counts['newConservativeChartCovered'] += 1
            by_render[observation['name']]['newConservativeChartCovered'] += 1
            cases.append(dict(render=observation['name'],mesh=observation['mesh'],lod=observation['lod'],
                delta=observation['delta'],scenario=observation['scenario'],left=observation['left'],right=observation['right'],
                domainIndices=[d['index'] for d in domains]))
    result = dict(status='PASS', sourceFaceCoverage=covered_faces,zeroUvAreaFaces=zero_area_faces,
        domainsWithoutTwoDimensionalCharts=domains_without_chart,counts=dict(counts),
        byRender={k:dict(v) for k,v in by_render.items()},newCoverageCases=cases,
        interpretation='Observed receiver containment only; every possible matching source domain must cover the receiver because old fixtures omit domain indices. RepeatOnly is excluded. Production native Float32 density, filtering and saved-asset checks remain the shared integration responsibility.')
    (OUT/'receiver-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k not in ('newCoverageCases','byRender')},indent=2))


if __name__=='__main__': main()
