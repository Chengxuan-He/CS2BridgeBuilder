"""Compare native Lift05 repeating profiles with the actual embedded width recipe.

This check does not bake, change production data or classify a failed mapping as
stretching. It measures whether original atlas reuse can preserve a source period
after the existing geometry recipe. Run with the bundled Python and NumPy.
"""
import hashlib
import json
import re
import zipfile
from pathlib import Path

import numpy as np
from ExportRecipes import numbers

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Lift05'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def motion(lod, count):
    source = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RoofRings.g.cs'
    resource = ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_Lift05RoofRings_g_cs.bin'
    code = source.read_text()
    body = code.split(f'[] ReadPackedMotion{lod}()')[1].split('return values;')[0]
    expected = int(re.search(r'Weight\)\[(\d+)\]', body)[1])
    pattern = (r'for \(int i = 0; i < (\d+); i\+\+\)\s*'
               r'values\[(\d+) \+ i\] = \(d.I32\[(\d+) \+ i \* (\d+)\], '
               r'd.I32\[(\d+) \+ i \* (\d+)\], (-?)d.F32\[(\d+) \+ i \* (\d+)\]\);')
    data = numbers(resource)
    weights = np.zeros(count, dtype=np.float32)
    assigned = 0
    for n, start, a, da, b, db, sign, w, dw in re.findall(pattern, body):
        n, start, a, da, b, db, w, dw = map(int, (n, start, a, da, b, db, w, dw))
        assert start == assigned
        for i in range(n):
            first, last = data['I32'][a+i*da], data['I32'][b+i*db]
            assert 0 <= first <= last < count
            value = data['F32'][w+i*dw] * (-1 if sign else 1)
            weights[first:last+1] = value
        assigned += n
    assert assigned == expected, (assigned, expected)
    return weights, {str(p.relative_to(ROOT)): digest(p) for p in (source, resource)}


def main():
    policy_path = ROOT / 'artifacts/prepared-materials/Lift05/non-tiling/classification.json'
    policy = json.loads(policy_path.read_text())
    source_path = ROOT / 'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/summary.json'
    native = json.loads(source_path.read_text())
    results, provenance = [], {}
    for lod in range(2):
        level = native['levels'][lod]
        with zipfile.ZipFile(level['surface']['archive']) as archive:
            assert hashlib.sha256(archive.read(level['geometryEntry'])).hexdigest() == level['geometrySha256']
            assert hashlib.sha256(archive.read(level['surface']['entry'])).hexdigest() == level['surface']['sha256']
        array_path = ROOT / f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz'
        manifest = json.loads((array_path.parent / 'manifest.json').read_text())
        assert digest(array_path) == next(f['sha256'] for f in manifest['files'] if f['path'] == array_path.name)
        arrays = np.load(array_path)
        positions, faces, uv = arrays['mesh0_positions'], arrays['mesh0_indices'], arrays['mesh0_uv0']
        weights, hashes = motion(lod, len(positions))
        provenance.update(hashes)
        mesh = next(m for m in policy['meshes'] if m['render'] == level['render'] and m['mesh'] == 0)
        for part in mesh['mixedProfileEvidence']:
            ids = faces[part['faces']].ravel()
            xyz, source_uv, rates = positions[ids], uv[ids], weights[ids]
            assert np.array_equal(xyz.reshape(-1, 3, 3), part['positions'])
            span = float(np.ptp(xyz[:, 0]))
            assert abs(span-4) < .00001
            # Width parameter is the existing half-width delta. This is a test
            # scenario, not a new archetype constant or geometry correction.
            delta = np.float32(3)
            changed = xyz[:, 0] + rates * delta
            new_span = float(np.ptp(changed))
            pixels = float(np.ptp(source_uv[:, 0])) * 4096
            results.append(dict(lod=lod, seedFace=part['seedFace'], faces=part['faces'],
                sourceSurface=level['surface']['cid'], sourceSpan=span, generatedSpan=new_span,
                halfWidthDelta=float(delta), sourcePixels=pixels,
                sourceTexelsPerMetre=pixels/span, unchangedUvTexelsPerMetre=pixels/new_span,
                sourceU=source_uv[:, 0].tolist(), widthWeights=rates.tolist(),
                retainsDensityWithUnchangedUvs=new_span == span,
                requiredPeriodCount=new_span/span))
    assert results and any(not r['retainsDensityWithUnchangedUvs'] for r in results)
    OUT.mkdir(parents=True, exist_ok=True)
    report = dict(style='Lift05', scope='Native repeating roof profiles under the actual roof width recipe; not a complete final render capture.',
        provenance={str(p.relative_to(ROOT)): digest(p) for p in (policy_path, source_path)},
        recipeProvenance=provenance, profiles=results,
        changedProfiles=sum(not r['retainsDensityWithUnchangedUvs'] for r in results),
        conclusion='Retaining the original Surface is possible; unchanged UVs stretch these evidenced tiling profiles. Native atlas-region reset representation must be solved without replacement materials or additional period cuts.',
        localization='No user-facing text changed.')
    (OUT / 'original-profile-mapping.json').write_text(json.dumps(report, indent=2)+'\n')
    print(f"PASS source/recipe verification: {len(results)} profiles, {report['changedProfiles']} change U density with unchanged UVs.")


if __name__ == '__main__':
    main()
