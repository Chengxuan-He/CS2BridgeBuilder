"""Record Lift04 original VT and native middle-mip cache inputs for offline decoding."""
import hashlib
import json
from pathlib import Path
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[3]
GAME = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def raw_hash(cid):
    return bytes(int(cid[i + 1] + cid[i], 16) for i in range(0, 32, 2))


def main():
    retained = ROOT / 'artifacts/prepared-materials/Lift04/rebuild-native'
    source = json.loads((retained / 'repeat-unit-evidence.json').read_text())['units'][0]
    lookup = json.loads((retained / 'authoring-source-search.json').read_text())
    env = ROOT / 'artifacts/agent-material-env/Lift04/repeat-units-v2'
    requested = {source['sourceCid']} | {c['cid'] for c in source['channels']}
    native = []
    # The original Surface and its preprocessed VT channels are in this DLC.
    for name in ('Blob.cok', 'VT.cok'):
        archive = GAME / 'Content/BridgesAndPorts' / name
        with zipfile.ZipFile(archive) as z:
            for entry in z.namelist():
                if not entry.endswith('.cid'):
                    continue
                cid = z.read(entry).decode().strip()
                if cid not in requested:
                    continue
                payload = entry[:-4]
                raw = z.read(payload)
                metadata = dict(cid=cid, archive=str(archive), entry=payload, bytes=len(raw),
                                sha256=hashlib.sha256(raw).hexdigest())
                if payload.endswith('.VTTexture'):
                    channel = next(c for c in source['channels'] if c['cid'] == cid)
                    tail = struct.unpack_from('<i', raw, 34)[0]
                    metadata.update(property=channel['property'], width=channel['width'], height=channel['height'],
                                    mipCount=channel['mipCount'], format=channel['format'], tileSize=channel['tileSize'],
                                    requestedMidMipLevels=channel['nativeMidMipField'],
                                    mipTailOffset=38, mipTailBytes=tail, mipTailSHA256=channel['mipTailSHA256'],
                                    tileBlocks=channel['tileBlocks'], stagedPath=str(env / 'inputs' / (cid + '.VTTexture')),
                                    tileTableOffset=38+tail,
                                    nativeMiddleMipSizes=[256, 128, 64], nativeTailSizes=[32, 16, 8, 4, 2, 1])
                native.append(metadata)
    assert {p['cid'] for p in native} == requested
    patterns = {source['sourceCid']: raw_hash(source['sourceCid'])}
    for c in lookup['channels']:
        patterns[c['authoringCid']] = raw_hash(c['authoringCid'])
        patterns[c['vtCid']] = raw_hash(c['vtCid'])
    archive = GAME / 'Content/BridgesAndPorts/MidMips.cok'
    caches = []
    with zipfile.ZipFile(archive) as z:
        for entry in z.namelist():
            if not entry.endswith('.MidMips'):
                continue
            digest = hashlib.sha256(); position = 0; carry = b''; matches = {cid: [] for cid in patterns}
            with z.open(entry) as stream:
                while True:
                    block = stream.read(4*1024*1024)
                    if not block:
                        break
                    digest.update(block)
                    data = carry + block
                    for cid, pattern in patterns.items():
                        at = data.find(pattern)
                        while at >= 0:
                            absolute = position - len(carry) + at
                            if not matches[cid] or absolute > matches[cid][-1]:
                                matches[cid].append(absolute)
                            at = data.find(pattern, at + 1)
                    position += len(block); carry = data[-15:]
            caches.append(dict(archive=str(archive), entry=entry, cid=z.read(entry+'.cid').decode().strip(),
                               bytes=position, sha256=digest.hexdigest(), cidByteOccurrences=matches,
                               interpretation='Byte locations identify candidate native metadata records; shared native parser must decode them before use.'))
    result = dict(style='Lift04', sourceSurfaceCid=source['sourceCid'], sourceUnitPixels=[638,640],
                  authoringTextureFound=False, nativeInputs=native, nativeMiddleMipCaches=caches,
                  manifest=str(env/'native-inputs.json'),
                  sharedRequest='Decode authored VT tile mips, 256/128/64 native middle mip cache, and <=32 mip tail without regenerating missing levels. Preserve native channel format/normal decoding and compare the extracted region edges to the original atlas neighborhood.',
                  status='COMPLETE_SOURCE_FILES_IDENTIFIED_DECODER_VALIDATION_PENDING',
                  localization='Offline evidence only; runtime UI localization preserved.')
    (retained / 'repeat-vt-dependencies.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps(dict(nativeInputCount=len(native), caches=caches), indent=2))


if __name__ == '__main__':
    main()
