"""Locate original Lift04 repeat Texture identities in every installed builtin archive."""
import hashlib
import json
from pathlib import Path
import zipfile

from VerifyCoverage import hash_text

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def main():
    env = ROOT / 'artifacts/agent-material-env/Lift04/repeat-units-v2'
    manifest = json.loads((env / 'native-inputs.json').read_text())
    channels = []
    for c in manifest['units'][0]['channels']:
        raw = Path(c['path']).read_bytes()
        assert raw[:3] == b'\x01\0\xff'
        channels.append(dict(property=c['name'], vtCid=c['cid'],
                             authoringCid=hash_text(raw[3:19]), matches=[]))
    requested = {c['authoringCid'] for c in channels}
    archives = []
    for path in sorted((GAME / 'Content').rglob('*.cok')):
        if not path.name.startswith(('Blob', 'VT')):
            continue
        with zipfile.ZipFile(path) as z:
            count = 0
            for entry in z.namelist():
                if not entry.endswith('.cid'):
                    continue
                count += 1
                cid = z.read(entry).decode().strip()
                if cid not in requested:
                    continue
                payload_name = entry[:-4]
                raw = z.read(payload_name)
                ext = Path(payload_name).suffix
                target = env / 'inputs' / (cid + ext)
                if target.exists():
                    assert target.read_bytes() == raw
                else:
                    target.write_bytes(raw)
                match = dict(archive=str(path), entry=payload_name, nativeKind=ext.lstrip('.'),
                             bytes=len(raw), sha256=hashlib.sha256(raw).hexdigest(), path=str(target))
                for c in channels:
                    if c['authoringCid'] == cid:
                        c['matches'].append(match)
            archives.append(dict(path=str(path), cidCount=count))
    result = dict(style='Lift04', channels=channels, archives=archives,
                  archiveCount=len(archives), scannedCidCount=sum(a['cidCount'] for a in archives),
                  ordinaryTextureFound=all(any(m['nativeKind'] == 'Texture' for m in c['matches']) for c in channels),
                  localization='Authoring evidence only; no user-visible strings or localization changes.')
    report = ROOT / 'artifacts/prepared-materials/Lift04/rebuild-native/authoring-source-search.json'
    report.write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps({k: v for k, v in result.items() if k != 'archives'}, indent=2))


if __name__ == '__main__':
    main()
