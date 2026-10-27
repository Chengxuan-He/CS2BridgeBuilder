"""Restore the fixed-end caller's cutout/double-sided semantics without pixel conversion."""
import io
import json
import struct
import sys
from IntegrateStructures import ROOT, cid, count, read_string, refs, sha, string_bytes

base = ROOT / 'artifacts/prepared-materials/Lift02/repeat-units-v2'
old_key = '22704d8d067abe0783be1e32837dc37532e163f37492e3214ae35da6ae8374c0'
style = ROOT / 'assets/Materials/Lift02'
original = base / 'trial/output/Lift02/RepeatUnits' / old_key / 'Repeat.Surface'
raw = original.read_bytes()
assert sha(raw) == '234ce7c266d847798fa3c068c725e1a5f6a1677a858af2e71ee9f23d69ac1bd2'
r = io.BytesIO(raw)
assert struct.unpack('<Hi?', r.read(7)) == (1, 1, False)
maps = []
for size in (4,4,16,16):
    values = {}
    for _ in range(count(r)):
        name = read_string(r)
        values[name] = r.read(size)
    maps.append(values)
maps[0]['_Snow'] = struct.pack('<f', 0)
assert struct.unpack('<4f', maps[3]['_UV0Scale']) == (1,0.125,0,0)
texture_start = r.tell()
for _ in range(count(r)):
    read_string(r)
    assert r.read(1) == b'\xff'
    r.read(16)
texture_bytes = raw[texture_start:r.tell()]
keywords = [read_string(r) for _ in range(count(r))]
assert r.tell() == len(raw)
for value in ('_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO'):
    if value not in keywords:
        keywords.append(value)
def dictionary(values):
    return b'\xff' + struct.pack('<i',len(values)) + b''.join(string_bytes(k)+v for k,v in values.items())
converted = struct.pack('<Hi?',1,14,False) + b''.join(dictionary(m) for m in maps) + texture_bytes
converted += b'\xff' + struct.pack('<i',len(keywords)) + b''.join(string_bytes(k) for k in keywords)
assert refs(raw) == refs(converted)
surface_cid = cid(bytes.fromhex(sha(converted))[:16])
unit_key = sha(string_bytes('BridgeBuilder/RepeatUnits/2') + string_bytes(surface_cid))
stage = base / 'cutout-descriptor/RepeatUnits' / unit_key / 'Repeat.Surface'
stage.parent.mkdir(parents=True,exist_ok=True)
stage.write_bytes(converted)
stage.with_suffix('.Surface.cid').write_text(surface_cid)
report = {'oldUnitKey':old_key,'unitKey':unit_key,'surfaceCid':surface_cid,'sha256':sha(converted),
          'template':14,'snow':0,'keywords':keywords,'UV0Scale':[1,0.125,0,0],
          'unchangedTextureReferences':refs(converted),'texturePayloadsModified':0,
          'callerEvidence':['Lift02RoadAppearance.Patches: repeat:true','MovableSurfaceAtlas: repeatCutout=patch.Repeat || source!=owner.Asset','PrepareRepeat.Surface(true): template14, Snow0, AlphaTest, DoubleSided'],
          'status':'staged-native-validation-required'}
if '--install' in sys.argv:
    log = base / 'cutout-descriptor/native.log'
    assert 'PASS native source descriptors:' in log.read_text(errors='replace')
    backup = ROOT / 'artifacts/backups/Lift02/repeat-cutout-descriptor'
    old = style / 'RepeatUnits' / old_key / 'Repeat.Surface'
    mapping = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift02/Lift02RepeatUnits.cs'
    for file in (old,old.with_suffix('.Surface.cid'),mapping):
        if file.exists():
            destination = backup / file.relative_to(ROOT)
            destination.parent.mkdir(parents=True,exist_ok=True)
            if destination.exists():
                assert destination.read_bytes() == file.read_bytes()
            else:
                destination.write_bytes(file.read_bytes())
    dest = style / stage.relative_to(base / 'cutout-descriptor')
    dest.parent.mkdir(parents=True,exist_ok=True)
    dest.write_bytes(converted)
    dest.with_suffix('.Surface.cid').write_text(surface_cid)
    text = mapping.read_text()
    assert old_key in text or unit_key in text
    mapping.write_text(text.replace(old_key,unit_key))
    assert dest.read_bytes() == converted
    assert old.parent.resolve().is_relative_to(style.resolve())
    if old.exists():
        assert old.read_bytes() == raw
        old.unlink()
        old.with_suffix('.Surface.cid').unlink()
        old.parent.rmdir()
    report['status'] = 'installed-native-reader-validated'
    files = [p for p in style.rglob('*') if p.is_file()]
    report['formalFileCount'] = len(files)
    report['formalBytes'] = sum(p.stat().st_size for p in files)
    report['formalFiles'] = [{'path':p.relative_to(style).as_posix(),'sha256':sha(p.read_bytes()),'bytes':p.stat().st_size} for p in files]
    for identity in refs(converted):
        payload = style / 'Shared' / (identity+'.Texture')
        assert payload.read_bytes() == (base/'trial/output/Lift02/Shared'/payload.name).read_bytes()
    report['textureBytesEqualVerifiedTrial'] = True
(base/'cutout-descriptor.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k != 'formalFiles'}))
