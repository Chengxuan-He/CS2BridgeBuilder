"""Verify fresh producer files, template defaults and original Lift01 texture bindings."""
import argparse
import hashlib
import json
from pathlib import Path

from PrepareNativeObjects import ROOT, descriptor


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def floats(material):
    return dict(material['m_SavedProperties']['m_Floats'])


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--manifest', type=Path, required=True)
    parser.add_argument('--producer-report', type=Path, required=True)
    parser.add_argument('--materials', type=Path, required=True)
    parser.add_argument('--defaults', type=Path, required=True)
    parser.add_argument('--report', type=Path, required=True)
    a = parser.parse_args()
    source_manifest = json.loads(a.manifest.read_text())
    producer = json.loads(a.producer_report.read_text())
    source = {s['cid']: s for s in source_manifest['sources']}
    original33 = json.loads((a.defaults / 'template-33.json').read_text())
    template0 = next(t['material'] for t in json.loads((ROOT / 'tools/PrepareLanes/NativeTemplates.json').read_text()) if t['hash'] == 0)
    source_defaults, target_defaults = floats(original33), floats(template0)
    material_files = {str(p.relative_to(a.materials)).replace('\\', '/'): p for p in a.materials.rglob('*') if p.is_file()}
    assert set(material_files) == {f['path'] for f in producer['files']}
    for f in producer['files']:
        raw = material_files[f['path']].read_bytes()
        assert len(raw) == f['bytes'] and digest(raw) == f['sha256']
    conversions = []
    for r in producer['roots']:
        if r['sourceCid'] not in source:
            assert r['operation'] == 'native-vt-paint-descriptor'
            continue
        original = Path(source[r['sourceCid']]['inputPath']).read_bytes()
        assert digest(original) == source[r['sourceCid']]['sha256']
        if r['operation'] == 'builtin-reference':
            assert r['cid'] == r['sourceCid'] and r['path'] is None
            continue
        assert r['sourceTemplate'] == 33 and r['targetTemplate'] == 0
        before = descriptor(original)
        raw = material_files[r['path']].read_bytes()
        after = descriptor(raw)
        expected = {k: v[0] for k, v in before['floats'].items()}
        for key, value in source_defaults.items():
            if key in target_defaults and key not in expected:
                expected[key] = value
        actual = {k: v[0] for k, v in after['floats'].items()}
        assert actual.keys() == expected.keys()
        # Compare serialized Float32 values, not decimal parser presentation.
        import struct
        assert all(struct.pack('<f', actual[k]) == struct.pack('<f', v) for k, v in expected.items())
        assert before['textures'] == after['textures']
        for field in ('ints', 'vectors', 'colors', 'vt', 'version'):
            assert before[field] == after[field]
        assert set(after['keywords']) == set(before['keywords']) | {'_TANGENTSPACE_OCTO'}
        assert r['cid'] == digest(raw)[:32]
        assert material_files[r['path'] + '.cid'].read_text().strip() == r['cid']
        assert sorted(r['builtinDependencies']) == sorted(before['textures'].values())
        conversions.append(dict(sourceCid=r['sourceCid'], outputCid=r['cid'], path=r['path'],
                                bytes=len(raw), explicitFloatCount=len(actual), textureCids=before['textures']))
    assert len(conversions) == 3
    report = dict(style='Lift01', producerSha256=digest(a.producer_report.read_bytes()),
                  manifestSha256=digest(a.manifest.read_bytes()), default33Sha256=digest((a.defaults / 'template-33.json').read_bytes()),
                  roots=producer['roots'], conversions=conversions, sourceCount=len(source),
                  fileCount=len(material_files), bytes=sum(p.stat().st_size for p in material_files.values()),
                  allFileHashesVerified=True, originalTextureCidsPreserved=True,
                  nativeDefaultFloat32ValuesPreserved=True, newTexturePayloads=0,
                  completeStyle=False, nativeRendererVerified=False,
                  limitations=['Native reader result is recorded separately.', 'Graph existence is checked by public producer; original VT mip/render sampling and complete owner persistence still need acceptance.', 'Paint renderer, Repeat and out-of-chart sampling remain incomplete.'])
    a.report.parent.mkdir(parents=True, exist_ok=True)
    a.report.write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k: v for k, v in report.items() if k not in ('roots', 'conversions')}, indent=2))


if __name__ == '__main__':
    main()
