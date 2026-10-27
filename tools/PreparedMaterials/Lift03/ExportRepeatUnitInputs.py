"""Extract exact built-in authoring textures for a private Lift03 repeat candidate."""
import argparse
import hashlib
import json
import struct
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def sha(data):
    return hashlib.sha256(data).hexdigest()


def hash_text(data):
    return ''.join(f'{v & 15:x}{v >> 4:x}' for v in data)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--game-data', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=True)
    evidence_path = ROOT / 'artifacts/prepared-materials/Lift03/repeat-units-v2/unit-evidence.json'
    evidence = json.loads(evidence_path.read_text())
    inputs = json.loads((ROOT / 'artifacts/prepared-materials/Lift03/rebuild-native-v1/input/source-input.json').read_text())
    mapping = json.loads((ROOT / 'artifacts/prepared-materials/Lift03/shared-source-map/mapping.json').read_text())
    source = next(x for x in inputs['sources'] if x['cid'] == evidence['sourceCid'])
    with zipfile.ZipFile(source['archive']) as archive:
        descriptor = archive.read(source['entry'])
    assert sha(descriptor) == evidence['sourceSha256']
    descriptor_path = output / (source['cid'] + '.Surface')
    descriptor_path.write_bytes(descriptor)
    channel_cids = {'_BaseColorMap': '8c8c7c348b3a3114bc610b7dcc7f305f',
                    '_NormalMap': 'a32d521e1446ca2597246c951d16b853',
                    '_MaskMap': '814870826c030baed22b96d33efbe4b1'}
    wanted = {}
    for name, cid in channel_cids.items():
        row = next(t for t in mapping['textures'].values() if t['nativeCid'] == cid)
        with zipfile.ZipFile(row['nativeArchive']) as archive:
            vt = archive.read(row['nativeEntry'])
        assert sha(vt) == row['nativePayloadSha256'] and vt[:3] == b'\x01\x00\xff'
        wanted[hash_text(vt[3:19])] = dict(name=name, vtCid=cid, vtSha256=sha(vt))
    scanned, found = [], {}
    for path in sorted((args.game_data / 'Content').rglob('Blob*.cok')):
        scanned.append(str(path))
        with zipfile.ZipFile(path) as archive:
            for entry in archive.namelist():
                if not entry.endswith('.Texture.cid'):
                    continue
                cid = archive.read(entry).decode().strip()
                if cid not in wanted:
                    continue
                raw = archive.read(entry[:-4])
                if cid in found:
                    assert found[cid]['sha256'] == sha(raw)
                version, width, height = struct.unpack_from('<HHH', raw)
                mip_offset = 8 if version >= 4 else 6
                mips = raw[mip_offset]
                assert mips == max(width, height).bit_length(), 'Incomplete authored mip chain'
                destination = output / (cid + '.Texture')
                destination.write_bytes(raw)
                found[cid] = dict(**wanted[cid], cid=cid, sha256=sha(raw), path=str(destination),
                                  archive=str(path), entry=entry[:-4], width=width, height=height,
                                  mips=mips, format=raw[mip_offset+1], bytes=len(raw))
    report = dict(style='Lift03', archivesScanned=scanned, sourceChannels=list(found.values()),
                  missingAuthoringCids=sorted(set(wanted)-set(found)), rawSourceBytesCopied=True,
                  sourceMipsGenerated=False, boundaryAcceptance='pending-candidate-native-check')
    (output / 'input-verification.json').write_text(json.dumps(report, indent=2)+'\n')
    if set(wanted) != set(found):
        print(json.dumps(report, indent=2))
        raise SystemExit('Missing authoring textures; preserve native VT graph instead of generating mips.')
    channels = [{k: row[k] for k in ('name','cid','sha256','path')} for row in found.values()]
    units = []
    for measured in evidence['units']:
        units.append(dict(id=measured['id'], sourceCid=source['cid'], sourceDigest=source['sha256'],
                          min=measured['min'], max=measured['max'], physicalSize=measured['physicalSize'],
                          evidence=dict(path=str(evidence_path), sha256=sha(evidence_path.read_bytes())),
                          descriptor=dict(path=str(descriptor_path), sha256=sha(descriptor)),
                          channels=channels, aliases=[v['legacyRepeatKey'] for v in measured['variants']]))
    (output / 'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/2', style='Lift03', units=units), indent=2)+'\n')
    print(json.dumps(dict(found=len(found), archivesScanned=len(scanned), channels=list(found.values())), indent=2))


if __name__ == '__main__':
    main()
