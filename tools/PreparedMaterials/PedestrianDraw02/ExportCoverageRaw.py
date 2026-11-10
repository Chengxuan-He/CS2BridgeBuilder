import json,struct,hashlib
from pathlib import Path
root=Path('E:/CS2BridgeBuilder');s=(root/'tools/GeometryMetaprogram/UvDensityAudit.py').read_text();a=s.index('        for attribute in range(14):');b=s.index('        return meshes',a)
s=s[:a]+'''        widths={0:4,1:2,2:1,3:1,4:2,5:2,6:1,7:1,8:2,9:2,10:4,11:4}
        for attribute in range(14):
            for mesh,h in zip(meshes,headers):
                size=h[2+attribute]
                if not size:continue
                fmt=(h[0]>>(attribute*4))&15;dim=((h[1]>>(attribute*2))&3)+1
                data=block(size,h[18],widths[fmt]*dim)
                mesh.setdefault('raw',[]).append((attribute,fmt,dim,data))
'''+s[b:];ns={'__name__':'raw_fixture'};exec(s,ns);reader=ns['NativeGeometry'](Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
for x in json.loads((root/'tools/PreparedMaterials/PedestrianDraw02/ClosureUvParts.json').read_text())['meshes']:
 p=Path(x['geometry']);assert hashlib.sha256(p.read_bytes()).hexdigest()==x['geometrySha256'];m=reader.read(p)[0];ix=m['faces'].astype('<i4').tobytes();attrs=m['raw'];count=len(attrs[0][3])//12
 raw=struct.pack('<4i',count,len(ix)//4,4,len(attrs))+ix
 for a,f,d,v in attrs:raw+=struct.pack('<4i',a,f,d,len(v))+v
 (root/f"artifacts/material-remap/PedestrianDraw02/coverage54/lod{x['lod']}-raw.bin").write_bytes(raw)
 print(x['lod'],count,[(a,f,d) for a,f,d,v in attrs])
