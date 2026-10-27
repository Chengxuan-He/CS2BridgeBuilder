"""Record the finite Lift02 native repeat trial without promoting a failed edge check."""
from pathlib import Path
import hashlib
import io
import json
import struct

ROOT = Path(__file__).resolve().parents[3]
BASE = ROOT / 'artifacts/prepared-materials/Lift02/repeat-units-v2'


def text(reader):
    length, shift = 0, 0
    while True:
        value = reader.read(1)[0]
        length |= (value & 127) << shift
        if value < 128:
            break
        shift += 7
    return reader.read(length).decode()


def colors(raw):
    reader = io.BytesIO(raw)
    assert struct.unpack('<Hi?', reader.read(7)) == (1, 1, False)
    result = {}
    for index, size in enumerate((4, 4, 16, 16)):
        assert reader.read(1) == b'\xff'
        count, = struct.unpack('<i', reader.read(4))
        for _ in range(count):
            key, value = text(reader), reader.read(size)
            if index == 3:
                result[key] = struct.unpack('<4f', value)
    return result


def main():
    trial = BASE / 'trial'
    product = json.loads((trial / 'report.json').read_bytes())
    check = json.loads((trial / 'native-check.json').read_bytes())
    evidence = json.loads((BASE / 'evidence.json').read_bytes())
    unit = product['units'][0]
    assert unit['periods'] == [1, 8]
    style = trial / 'output/Lift02'
    descriptor = style / 'RepeatUnits' / unit['key'] / 'Repeat.Surface'
    mapping = colors(descriptor.read_bytes())
    assert mapping['_UV0Scale'][:2] == (1.0, 0.125)
    density = [322 * mapping['_UV0Scale'][0] / evidence['physicalSize'][0],
               271 * mapping['_UV0Scale'][1] / evidence['physicalSize'][1]]
    assert density == evidence['sourcePixelsPerMetre']
    files = [dict(path=str(p.relative_to(style)), bytes=p.stat().st_size,
                  sha256=hashlib.sha256(p.read_bytes()).hexdigest()) for p in sorted(style.rglob('*')) if p.is_file()]
    levels = check['levels']
    native = [l for l in levels if l['mip'] < 9]
    coarse = [l for l in levels if l['mip'] >= 9]
    report = dict(style='Lift02', status='candidate-rejected-for-original-atlas-boundary-difference',
                  nativeHeaders=check['nativeHeaders'], completeGraph=check['completeGraph'],
                  originalSourceMips=13, preparedMipCount=9, originalMipBlocksVerified=True,
                  periodCount=unit['periods'], outputDimensions=[322, 271],
                  materialUVScale=list(mapping['_UV0Scale']), sourceAndOutputPixelsPerMetre=density,
                  fileCount=len(files), totalBytes=sum(f['bytes'] for f in files), files=files,
                  producedTextures=product['producedTextures'],
                  nativeMipPixelCenterMaxError=max(l['maxLinearError'] for l in native),
                  originalAtlasBoundaryMaxError=max(l['maxOriginalAtlasBoundaryDifference'] for l in native),
                  coarserOriginalAtlasVsClampedUnitMaxError=max(l['maxLinearError'] for l in coarse),
                  coarserLevelScope='Source levels 9-12 compared to the 1x1-clamped unit; separately reported, not missing output mips.',
                  channelBoundaryMaxError={c: max(l['maxOriginalAtlasBoundaryDifference'] for l in native if l['channel'] == c)
                                           for c in sorted({l['channel'] for l in native})},
                  selfPeriodComparisonIsAcceptance=False, published=False,
                  pipeline={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in [
                      ROOT/'artifacts/agent-material-env/Lift02/unity/Assets/Editor/PrepareRepeatUnits.cs',
                      ROOT/'artifacts/agent-material-env/Lift02/unity/Assets/RepeatUnit.compute',
                      ROOT/'tools/PreparedMaterials/Lift02/RepeatUnitCheck.Editor.cs',
                      ROOT/'tools/PreparedMaterials/Lift02/RepeatUnitCheck.compute']},
                  localization='No user-visible text changed.')
    (BASE / 'trial-summary.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({k:report[k] for k in ('status','fileCount','totalBytes','sourceAndOutputPixelsPerMetre','originalAtlasBoundaryMaxError')}))


if __name__ == '__main__':
    main()
