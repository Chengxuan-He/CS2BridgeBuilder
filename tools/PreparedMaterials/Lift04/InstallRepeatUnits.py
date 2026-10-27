"""Install Lift04's measured repeat unit with separate native surface semantics."""
import hashlib
import json
from pathlib import Path
import struct
from InstallBeamRepeatUnit import install as install_beam

ROOT = Path(__file__).resolve().parents[3]


def string(value):
    data = value.encode('utf-8'); size = len(data); prefix = bytearray()
    while size >= 128:
        prefix.append((size & 127) | 128); size >>= 7
    return bytes(prefix) + bytes([size]) + data


def native_maps(data):
    assert data[:7] == struct.pack('<Hi?', 1, 0, False)
    position = 7
    def text():
        nonlocal position
        size = shift = 0
        while True:
            b = data[position]; position += 1; size |= (b & 127) << shift
            if b < 128: break
            shift += 7
        result = data[position:position+size].decode('utf-8'); position += size
        return result
    def count():
        nonlocal position
        marker = data[position]; position += 1
        assert marker in (0,255)
        if not marker: return 0
        result = struct.unpack_from('<i', data, position)[0]; position += 4
        assert result >= 0
        return result
    maps = []
    for width in (4,4,16,16):
        values = {}
        for _ in range(count()):
            name = text(); values[name] = data[position:position+width]; position += width
        maps.append(values)
    bindings = {}
    for _ in range(count()):
        name = text(); marker = data[position]; position += 1
        assert marker == 255
        bindings[name] = data[position:position+16]; position += 16
    keywords = [text() for _ in range(count())]
    assert position == len(data)
    return maps, bindings, keywords


def road_descriptor(original):
    maps, bindings, keywords = native_maps(original)
    # Preserve the old road path's double-sided cutout and world-space snow policy.
    maps[0]['_Snow'] = struct.pack('<f', 0)
    result = bytearray(struct.pack('<Hi?',1,14,False))
    for values in maps:
        result.extend(struct.pack('<Bi',255,len(values)))
        for name,value in values.items(): result.extend(string(name)+value)
    result.extend(struct.pack('<Bi',255,len(bindings)))
    for name,value in bindings.items(): result.extend(string(name)+b'\xff'+value)
    keywords = sorted(set(keywords) | {'_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO'})
    result.extend(struct.pack('<Bi',255,len(keywords)))
    for value in keywords: result.extend(string(value))
    return bytes(result)


def digest(data): return hashlib.sha256(data).hexdigest()


def content_id(data):
    return ''.join(f'{b&15:x}{b>>4:x}' for b in hashlib.sha256(data).digest()[:16])


def write(path, data):
    path.parent.mkdir(parents=True, exist_ok=True)
    if path.exists(): assert path.read_bytes() == data, 'Conflicting prepared material: '+str(path)
    else: path.write_bytes(data)


def main():
    evidence = ROOT/'artifacts/prepared-materials/Lift04/rebuild-native'
    candidate = evidence/'repeat-candidate'
    summary = json.loads((candidate/'summary.json').read_text())
    result = json.loads((candidate/'report.json').read_text())['units'][0]
    unit = json.loads((ROOT/'tools/PreparedMaterials/Lift04/repeat-units-v2.json').read_text())['units'][0]
    structure_key = unit['oldStructureKeys'][0]
    assert len(unit['oldStructureKeys']) == 1 and len(unit['oldRepeatKeys']) == 1
    sampler = next(s for s in json.loads((evidence.parent/'shared-sampling/map.json').read_text())['samplers'] if s['key']==structure_key)
    assert not sampler['triangle'] and sampler['sourceDigest']==unit['sourceDigest']
    assert struct.pack('<4f',*sampler['bounds']) == struct.pack('<4f',*(unit['min']+unit['max']))
    assert result['periods'] == [1,1]
    assert all((c['width'],c['height'],c['mips'])==(638,640,10) for c in result['channels'])
    target = ROOT/'assets/Materials/Lift04'
    for item in summary['files']:
        raw=(candidate/'output/Lift04'/item['path']).read_bytes()
        assert len(raw)==item['bytes'] and digest(raw)==item['sha256']
        write(target/item['path'],raw)
    original=(target/'RepeatUnits'/result['key']/'Repeat.Surface').read_bytes()
    road=road_descriptor(original); road_cid=content_id(road)
    road_key=digest(string('BridgeBuilder/RepeatUnits/2')+string(road_cid))
    road_path=target/'RepeatUnits'/road_key/'Repeat.Surface'
    write(road_path,road);write(Path(str(road_path)+'.cid'),road_cid.encode())
    aliases={unit['oldRepeatKeys'][0]:road_key,structure_key:result['key']}
    code='''namespace BridgeBuilder.Bridges;

// One measured 638 x 640 source rectangle. Native descriptors preserve road
// double-sided cutout versus structural Default semantics; texture CIDs are shared.
internal static partial class MovableRepeatUnits
{
    // Embedded numerical arrays: Types_Lift04_Lift04RoadAppearance_cs
    // Reuse the existing demand-loaded holder; the literals below are array indices.
    static partial void Lift04(string samplerKey, ref string? unitKey)
    {
        unitKey = samplerKey switch
        {
'''
    for key,value in aliases.items(): code+=f'            "{key}" => "{value}",\n'
    code+='''            _ => unitKey
        };
    }
'''
    code+=f'''
    static partial void Lift04Regions(string sourceDigest, System.Collections.Generic.List<Region> regions)
    {{
        if (sourceDigest != "{unit['sourceDigest']}") return;
        var values = Numeric_Types_Lift04_Lift04RoadAppearance_cs.Values.F32;
        regions.Add(new Region("{result['key']}",
            values[0], values[1], values[2], values[3]));
    }}
}}
'''
    code=install_beam(code,target)
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift04/Lift04RepeatUnits.cs').write_text(code)
    files=[]
    for path in sorted(target.rglob('*')):
        if path.is_file(): files.append(dict(path=path.relative_to(target).as_posix(),bytes=path.stat().st_size,sha256=digest(path.read_bytes())))
        elif path.is_dir(): assert any(path.iterdir())
    report=dict(style='Lift04',status='REPEAT_UNIT_INTEGRATED_PENDING_HUMAN_VISUAL_ACCEPTANCE',
                aliases=aliases,structureTemplate=0,roadTemplate=14,newPixelBakes=0,
                structuralRegion=dict(sourceDigest=unit['sourceDigest'],bindingKey=result['key'],
                                      boundsResource='Types_Lift04_Lift04RoadAppearance_cs',boundsIndices=[0,1,2,3]),
                preparedFiles=len(files),preparedBytes=sum(f['bytes'] for f in files),files=files,
                sharedTextureCids=[c['cid'] for c in result['channels']],
                sourceAtlasPackaged=False,chartScope='One proven normalized rectangle; no barycentric triangle aliases.',
                manualObservations=summary['channelErrors'],
                remaining='Other 524 structural recipe keys require the shared native path; no crop fallback restored.',
                localization='No UI text or localization changes.')
    (evidence/'repeat-integration.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ('status','aliases','preparedFiles','preparedBytes','newPixelBakes')},indent=2))


if __name__=='__main__':main()
