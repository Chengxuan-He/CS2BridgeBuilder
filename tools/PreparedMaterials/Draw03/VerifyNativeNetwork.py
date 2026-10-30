"""Recheck Draw03's native-network retention evidence against actual source inputs.

This validates source identity and finite negative-V authoring, not game rendering.
Shared native integration must separately verify graph-context gating and raw upload.
"""
from pathlib import Path
import argparse
import collections
import hashlib
import json
import re
import zipfile
import numpy as np

ROOT = Path(__file__).resolve().parents[3]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    evidence_path = ROOT / 'artifacts/prepared-materials/Draw03/non-tiling/evidence.json'
    evidence = json.loads(evidence_path.read_text())
    source_path = ROOT / 'src/BridgeBuilder/Bridges/Types/Draw03/Draw03NativeMaterials.cs'
    source = source_path.read_text().split('internal static string? ObjectDescriptor')[0]
    bindings = dict(re.findall(r'"([^"]+ Mesh)" => "([0-9a-f]{32})"', source))
    assert len(bindings) == 15
    reconstruct = (ROOT / 'src/BridgeBuilder/Bridges/Types/Draw03/Draw03ApproachSources.g.cs').read_text()
    declarations = re.findall(r'Create<(NetPiecePrefab|RenderPrefab)>\("([^"]+)", "([0-9a-f]{32})", new\[\] \{ "([0-9a-f]{32})" \}', reconstruct)
    actual = {name: (kind, geometry, surface) for kind, name, geometry, surface in declarations}
    inventory = json.loads((ROOT / 'artifacts/prepared-materials/Draw03/rebuild-native-v1/native-rebuild.json').read_text())
    surfaces = {s['cid']: s for s in inventory['sourceSurfaces']}
    records = []
    for row in evidence['meshes']:
        name = row['render']
        if name not in bindings:
            continue
        assert row['mesh'] == 0 and row['surfaceCid'] == bindings[name]
        kind, geometry, cid = actual[name]
        assert cid == row['surfaceCid'] and geometry == row['geometryCid']
        assert kind == ('RenderPrefab' if '_LOD' in name else 'NetPiecePrefab')
        native = ROOT / 'artifacts/uv-density/native' / (name + '.npz')
        if not native.exists():
            native = ROOT / 'artifacts/uv-density/native-additional' / (name + '.npz')
        recorded_hash = evidence['inputs'].get(str(native.relative_to(ROOT)), evidence['inputs'].get(str(native)))
        assert recorded_hash == sha(native), name
        data = np.load(native)
        uv = data['mesh0_uv0'][:, :2]
        faces = data['mesh0_faces']
        used = uv[np.unique(faces)]
        assert np.isfinite(used).all()
        assert np.all((used[:, 0] >= 0) & (used[:, 0] <= 1))
        assert np.all((used[:, 1] > -1) & (used[:, 1] < 0))
        assert row['periodicSeamCharts'] == 0
        original = surfaces[cid]
        with zipfile.ZipFile(original['archive']) as archive:
            payload = archive.read(original['entry'])
        assert hashlib.sha256(payload).hexdigest() == original['sha256']
        descriptor = original['descriptor']
        assert descriptor['template'] in (17, 33) and not descriptor['virtualTextures']
        assert evidence['sources'][cid]['sourceDigest'] == original['sha256']
        records.append(dict(render=name, sourceCid=cid, geometryCid=geometry,
                            template=descriptor['template'], graphSourceType=kind,
                            sourceMeshSha256=sha(native), sourceSurfaceSha256=original['sha256'],
                            sourceDomains=row['domains'], sourceFaces=len(faces),
                            uvMin=used.min(axis=0).tolist(), uvMax=used.max(axis=0).tolist()))
    assert len(records) == 15
    shaders = {}
    for path, digest in evidence['inputs'].items():
        if Path(path).name in ('581-106.asm', '581-133.asm'):
            assert sha(Path(path)) == digest
            shaders[path] = digest
    assert len(shaders) == 2
    vertex = Path(next(p for p in shaders if p.endswith('581-106.asm'))).read_text()
    assert 'frc r4.w, v3.y' in vertex and 'movc r0.y, r1.w, r2.y, r4.w' in vertex
    observed = collections.Counter()
    unresolved = collections.Counter()
    observer = ROOT / 'artifacts/prepared-materials/shared-nontiling/actual-final2/observer.json.units.ndjson'
    with observer.open() as stream:
        for line in stream:
            unit = json.loads(line)
            if unit['style'] != 'Draw03':
                continue
            if unit['name'] in bindings:
                assert unit['mesh'] == 0 and unit['implementation'] == 'Draw03ApproachGeometry'
                observed[unit['name']] += 1
                if not unit['canUseSource']:
                    unresolved[unit['name']] += 1
    assert sum(unresolved.values()) == 22766
    report = dict(schema=1, sourceEvidenceSha256=sha(evidence_path),
                  runtimePredicateSha256=sha(source_path), shaders=shaders, meshes=records,
                  sourceDomains=sum(r['sourceDomains'] for r in records),
                  baselineChangedUses=dict(observed), baselineUnresolvedUses=dict(unresolved),
                  baselineUnresolvedTotal=sum(unresolved.values()),
                  sourceUvRule='Both authored axes finite; retain negative V for native shader 581. Worldspace layer stays native.',
                  limits='Source and baseline attribution only; shared graph/native upload and human rendering checks remain separate.')
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(meshes=len(records), domains=report['sourceDomains'],
                          baselineUnresolved=report['baselineUnresolvedTotal'])))


if __name__ == '__main__':
    main()
