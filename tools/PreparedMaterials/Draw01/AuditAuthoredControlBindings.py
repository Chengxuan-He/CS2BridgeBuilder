"""Read Draw01's real serialized binding map separately from its active VT layer."""
import hashlib
import io
import json
import struct
import zipfile
from pathlib import Path
from AuditStructureReuse import count, text

root=Path(__file__).resolve().parents[3]
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
evidence=root/'artifacts/prepared-materials/Draw01/repeat-control-native-audit'
proof=json.loads((evidence/'audit.json').read_text())['sources'][0]
source=next(s for s in json.loads((root/'artifacts/uv-density/structure-draw01-footprint.json').read_text())['sources'] if s['cid']==proof['cid'])
matches=[]
for family in ('Game','BridgesAndPorts'):
    for archive in (game/'Content'/family).glob('Blob*.cok'):
        with zipfile.ZipFile(archive) as z:
            if source['name'] not in z.namelist():continue
            data=z.read(source['name'])
            assert hashlib.sha256(data).hexdigest()==proof['sha256']
            assert z.read(source['name']+'.cid').decode().strip()==proof['cid']
            reader=io.BytesIO(data)
            version,template,vt=struct.unpack('<Hi?',reader.read(7));assert version==1 and vt
            reader.read(276)
            marker=reader.read(1)[0];assert marker in (0,255)
            if marker:reader.read(16)
            for size in (4,4,16,16):
                for _ in range(count(reader)):
                    text(reader);reader.read(size)
            bindings=[]
            for _ in range(count(reader)):
                property=text(reader);marker=reader.read(1)[0];assert marker in (0,255)
                cid=None if not marker else ''.join(f'{v&15:x}{v>>4:x}' for v in reader.read(16))
                bindings.append(dict(property=property,cid=cid))
            for _ in range(count(reader)):text(reader)
            assert reader.tell()==len(data)
            matches.append(dict(archive=str(archive),entry=source['name'],sha256=proof['sha256'],template=template,isVT=vt,
                bindings=bindings,tryGetTextureControlWouldBeTrue=any(b['property']=='_ControlMask' and b['cid'] is not None for b in bindings),
                vtControlLayer=proof['vtControlLayer'],decodedControlPresent='_ControlMask' in proof['decodedChannels']))
assert matches
result=dict(sources=matches,gameAssembly=str(game/'Managed/Colossal.IO.AssetDatabase.dll'),
    gameAssemblySHA256=hashlib.sha256((game/'Managed/Colossal.IO.AssetDatabase.dll').read_bytes()).hexdigest(),
    decompiledSHA256=hashlib.sha256((evidence/'SurfaceAsset.current.cs').read_bytes()).hexdigest())
(evidence/'authored-control-bindings.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
