"""Identify original native channel families behind the retained Draw03 gap corpus.

This is provenance evidence, not current runtime coverage or repeat-unit evidence.
No asset, geometry, material descriptor, or producer snapshot is modified.
"""
import hashlib
import json
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/prepared-materials/Draw03'
GAPS = BASE / 'repeat-units-v2/structure-groups/shared-gap-inputs.json'
SOURCES = BASE / 'rebuild-native-v1/native-rebuild.json'
CHECKER = ROOT / 'tools/PreparedMaterials/Draw03/ReceiverCheck/Program.cs'
OUT = BASE / 'current-source-audit.json'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def main():
    gaps = json.loads(GAPS.read_text(encoding='utf-8'))
    sources = json.loads(SOURCES.read_text(encoding='utf-8'))['sourceSurfaces']
    archives = {}
    families = {}
    result = []
    try:
        for digest in sorted({e['sourceDigest'] for e in gaps['examples']}):
            aliases = [s for s in sources if s['sha256'] == digest]
            assert aliases, digest
            verified = []
            for s in aliases:
                archive = archives.setdefault(s['archive'], None)
                if archive is None:
                    archive = archives[s['archive']] = zipfile.ZipFile(s['archive'])
                payload = archive.read(s['entry'])
                assert sha(payload) == digest, s['entry']
                verified.append({'cid': s['cid'], 'entry': s['entry'], 'bytes': len(payload)})
            descriptor = aliases[0]['descriptor']
            channels = descriptor['textureProperties']
            identity = json.dumps(channels, sort_keys=True, separators=(',', ':'))
            family = families.setdefault(identity, f'channels-{len(families) + 1}')
            examples = [e for e in gaps['examples'] if e['sourceDigest'] == digest]
            result.append({
                'sourceDigest': digest, 'channelFamily': family,
                'template': descriptor['template'],
                'virtualTextures': descriptor['virtualTextures'],
                'channels': channels, 'keywords': descriptor['keywords'],
                'properties': descriptor['properties'],
                'verifiedNativeSurfaceAliases': verified,
                'historicalGroups': [{k: e[k] for k in ('group', 'render', 'mesh', 'outsideCount')} for e in examples],
                'representative': examples[0],
            })
    finally:
        for archive in archives.values():
            if archive is not None:
                archive.close()
    checker = CHECKER.read_text(encoding='utf-8-sig')
    report = {
        'schema': 'Draw03StructureSourceProvenance/1',
        'status': 'not-human-acceptance-ready',
        'scope': gaps['scope'],
        'limitations': [
            'Retained receiver corpus is historical; do not treat its counts as current runtime failures.',
            'Legacy ReceiverCheck omits ExactCoordinates, ExactPoints and ExactSourceUvs when constructing chart Units.',
            'Original source channels are finite atlas data; shared channel identity does not prove repeatability.',
            'No new native representability or generated owner byte closure is established by this report.',
        ],
        'sourceDigestCount': len(result), 'channelFamilyCount': len(families),
        'historicalGroupCount': len(gaps['examples']),
        'historicalOutsideCount': sum(e['outsideCount'] for e in gaps['examples']),
        'legacyCheckerExactFieldMentions': {name: name in checker for name in ('ExactCoordinates', 'ExactPoints', 'ExactSourceUvs')},
        'inputs': {str(p.relative_to(ROOT)): sha(p.read_bytes()) for p in (GAPS, SOURCES, CHECKER)},
        'sources': result,
    }
    OUT.write_text(json.dumps(report, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k: report[k] for k in ('sourceDigestCount', 'channelFamilyCount', 'historicalGroupCount', 'historicalOutsideCount', 'legacyCheckerExactFieldMentions')}))


if __name__ == '__main__':
    main()
