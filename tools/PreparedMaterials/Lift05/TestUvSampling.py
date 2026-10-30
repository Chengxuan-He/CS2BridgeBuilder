"""Check source-part provenance, native LOD agreement and real captured dispatch."""
import collections
import gzip
import hashlib
import json
from pathlib import Path
from ExportRecipes import numbers
from BuildUvCharts import ROOT
from shapely.geometry import Polygon


def main():
    path = ROOT / 'artifacts/prepared-materials/Lift05/non-tiling/classification.json'
    report = json.loads(path.read_text())
    resource = ROOT / 'assets/HardcodedNumbers/Lift05/Lift05UvSampling.bin'
    values = numbers(resource)['I32']
    assert values[:2] == (2, 11)
    at, flags = 2, {}
    for mesh in report['meshes']:
        n = values[at]; at += 1
        assert n == mesh['domainCount']
        for d, flag in enumerate(values[at:at+n]):
            assert flag in (0, 2, 3)
            flags[(mesh['render'], mesh['mesh'], d)] = flag
        at += n
    assert at == len(values)
    expected_profiles = {1489,1490,1626,1627,1848,1849,1857,1858,7881,7882,8074,8075,8268,8269,8307,8308,8317,8318}
    assert {('LiftBridge05_LOD1 Mesh', 0, d) for d in expected_profiles} <= {key for key, value in flags.items() if value == 2}
    assert sum(value == 2 for value in flags.values()) == 149
    assert all(flags[('LiftBridge05_LOD1 Mesh', 0, d)] == 3 for d in (1799,1819))
    profile = ROOT / 'artifacts/material-resume/Lift05/profile-unit'
    native = json.loads((profile / 'Lift05.UpperCrossbeam.NativeProfile-native-check.json').read_text())
    assert len(native['levels']) == 30 and native['nativeHeaders'] and native['completeGraph']
    assert all(level['maxLinearError'] <= .005 and level['maxRepeatSeamError'] <= .0001 and level['maxOldNearTransformError'] <= .0001 for level in native['levels'])
    bounds = numbers(ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_ProfileRegion.bin')['F64']
    assert bounds == (.6162109375, .890625, .748046875, .8955078125)
    old_bounds = numbers(ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_Lift05RepeatRegions_cs.bin')['F64'][:4]
    assert old_bounds == (.6162109375, .892578125, .748046875, .8955078125)

    assert flags[('LiftBridge05 Mesh', 0, 2133)] == 2  # U density is preserved; finite V is retained
    assert not flags[('LiftBridge05_LOD1 Mesh', 0, 12386)]
    near, lod1 = [next(m for m in report['meshes'] if m['render'] == name and m['mesh'] == 0)
                  for name in ('LiftBridge05 Mesh', 'LiftBridge05_LOD1 Mesh')]
    assert len(near['finitePartEvidence']) == len(lod1['finitePartEvidence']) == 16
    for first, second in zip(near['finitePartEvidence'], lod1['finitePartEvidence']):
        assert first['id'] == second['id']
        assert Polygon(first['uvBoundary']).symmetric_difference(Polygon(second['uvBoundary'])).area == 0
        assert first['singleCover'] and second['singleCover']
    capture = ROOT / 'artifacts/prepared-materials/shared-nontiling/actual-final2/styles/Lift05.regions.json.classifications.ndjson.gz'
    outcomes, transitions = collections.Counter(), collections.Counter()
    for line in gzip.open(capture, 'rt'):
        row = json.loads(line)
        retained = flags[(row['render'], row['mesh'], row['originalDomain'])] == 3
        status = 'source-retained' if retained else row['status']
        outcomes[status] += 1
        transitions[(row['status'], status)] += 1
    out = ROOT / 'artifacts/material-resume/Lift05/source-policy-check.json'
    out.parent.mkdir(parents=True, exist_ok=True)
    result = dict(resourceSha256=hashlib.sha256(resource.read_bytes()).hexdigest(),
        testedSourceDomains=len(flags), nativeLodPartPairs=16, genuineRepeatSeedsPreserved=True,
        capturedInputSha256=hashlib.sha256(capture.read_bytes()).hexdigest(),
        counterfactualOutcomes=dict(outcomes), transitions={f'{a}->{b}': n for (a,b),n in transitions.items()},
        scope='Static original-source checks and current flags joined to the previous real capture. This is not a fresh runtime capture or native visual acceptance.',
        localization='No user-visible strings changed; internal diagnostics and technical identities only.')
    out.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(result))


if __name__ == '__main__':
    main()
