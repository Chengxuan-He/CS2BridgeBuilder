"""Install the measured Lift05 road unit without rebaking any texture."""
import hashlib
import json
from pathlib import Path
import struct
from MapSharedSampling import surface

ROOT = Path(__file__).resolve().parents[3]


def text(value):
    raw = value.encode('utf-8')
    n, prefix = len(raw), bytearray()
    while n >= 128:
        prefix.append((n & 127) | 128)
        n >>= 7
    return bytes(prefix) + bytes([n]) + raw


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def native_hash(raw):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(raw).digest()[:16])


def cid_bytes(value):
    return bytes(int(value[i + 1] + value[i], 16) for i in range(0, 32, 2))


def write(path, raw):
    if path.exists():
        assert path.read_bytes() == raw, f'Conflicting existing material: {path}'
    else:
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(raw)


def main():
    evidence = ROOT / 'artifacts/prepared-materials/Lift05/repeat-unit-trial-v2'
    source = evidence / 'output/Lift05'
    destination = ROOT / 'assets/Materials/Lift05'
    result = json.loads((evidence / 'bake.json').read_text())['units'][0]
    original = source / 'RepeatUnits' / result['key'] / 'Repeat.Surface'
    native = surface(original.read_bytes())
    assert native['template'] == 0 and not native['isVT']
    assert all(not v for v in native['properties'].values())
    # Old fixed-road Repeat uses template 14, Snow=0 and these exact native flags.
    # Sharing the prototype's texels does not replace road cutout/double-sided semantics.
    raw = bytearray(struct.pack('<Hi?', 1, 14, False))
    raw += struct.pack('<Bi', 255, 1) + text('_Snow') + struct.pack('<f', 0)
    for _ in range(3):
        raw += struct.pack('<Bi', 255, 0)
    raw += struct.pack('<Bi', 255, len(native['textures']))
    for name, cid in native['textures'].items():
        raw += text(name) + b'\xff' + cid_bytes(cid)
    keywords = ['_ALPHATEST_ON', '_DISABLE_SSR_TRANSPARENT', '_DOUBLESIDED_ON', '_TANGENTSPACE_OCTO']
    raw += struct.pack('<Bi', 255, len(keywords))
    for key in keywords:
        raw += text(key)
    raw = bytes(raw)
    surface_cid = native_hash(raw)
    unit_key = digest(text('BridgeBuilder/RepeatUnits/2') + text(surface_cid))
    copied = []
    for path in (source / 'Shared').glob('*'):
        target = destination / 'Shared' / path.name
        write(target, path.read_bytes())
        copied.append(str(target.relative_to(destination)))
    target = destination / 'RepeatUnits' / unit_key / 'Repeat.Surface'
    write(target, raw)
    write(Path(str(target) + '.cid'), surface_cid.encode())
    parsed = surface(raw)
    assert parsed['template'] == 14
    assert parsed['textures'] == native['textures']
    assert parsed['properties']['floats'] == {'_Snow': '00000000'}
    assert parsed['keywords'] == keywords
    for cid in parsed['textures'].values():
        payload = destination / 'Shared' / (cid + '.Texture')
        assert payload.exists() and Path(str(payload) + '.cid').read_text().strip() == cid
    alias = result['aliases'][0]
    code = f'''namespace BridgeBuilder.Bridges;

internal static partial class MovableRepeatUnits
{{
    // The four fixed-road regions share one measured source strip. Their original
    // UV phase/orientation remain on the receivers; road template 14 preserves
    // double-sided alpha-test and Snow=0 semantics without rebaking the texels.
    static partial void Lift05(string samplerKey, ref string? unitKey)
    {{
        if (samplerKey == "{alias}")
            unitKey = "{unit_key}";
    }}
}}
'''
    code_path = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RepeatUnits.cs'
    write(code_path, code.encode())
    files = [p for p in destination.rglob('*') if p.is_file()]
    assert not any(p.is_dir() and not any(p.iterdir()) for p in destination.rglob('*'))
    report = dict(style='Lift05', status='INTEGRATED_FOR_HUMAN_VISUAL_ACCEPTANCE',
        oldRepeatKey=alias, unitKey=unit_key, surfaceCid=surface_cid,
        roadTemplate=14, snow=0, keywords=keywords, sourceTextureBytesChanged=0,
        sourceDescriptorSha256=digest(original.read_bytes()), installedDescriptorSha256=digest(raw),
        fileCount=len(files), bytes=sum(p.stat().st_size for p in files),
        localReferencesResolved=len(parsed['textures']), structureAliasesAdded=0,
        atlasBoundary='Observed color/normal/mask differences retained for human observation under AGENTS existing-atlas pattern exception.',
        files=[dict(path=p.relative_to(destination).as_posix(), bytes=p.stat().st_size,
                    sha256=digest(p.read_bytes())) for p in sorted(files)])
    (evidence / 'installed.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps({k:report[k] for k in ('unitKey','fileCount','bytes','roadTemplate','localReferencesResolved')}))


if __name__ == '__main__':
    main()
