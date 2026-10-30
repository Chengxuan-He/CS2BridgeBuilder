"""Read-only native Lift03 auxiliary surface-layer audit; no material generation."""
import hashlib,json,io,struct,zipfile
from pathlib import Path
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Lift03'
RESEARCH=Path(r'C:\Users\admin\Documents\Codex\2026-10-15\c-users-admin-documents-codex-cs2bridgebuilder\work\bridge-research')
GAME=Path(r'D:\Program Files (x86)\Steam\steamapps\common\Cities Skylines II\Cities2_Data')
audit=json.loads((RESEARCH/'all-movable-audit.json').read_text())
net=json.loads((RESEARCH/'all-net-data.json').read_text())
surface='a3522ead5e61e02569f0884fb4607eec'
source=audit['assets'][surface]
refs={k:''.join(v[i+1]+v[i] for i in range(0,len(v),2)) for k,v in source['texture_reference_bytes'].items()}
with zipfile.ZipFile(GAME/source['package']) as z:
 material=z.read(source['entry']);assert hashlib.sha256(material).hexdigest()==source['sha256']
 # The native descriptor has no numeric overrides; inherited scale comes from template 17.
 assert struct.unpack_from('<Hi?',material)==(1,17,False)
 assert all(material[7+5*i]==255 and struct.unpack_from('<i',material,8+5*i)[0]==0 for i in range(4))
 matches={z.read(n).decode('utf-8-sig').strip():n[:-4] for n in z.namelist() if n.endswith('.cid')}
 raw=z.read(matches[refs['_MaskMap']])
version,w,h,count=struct.unpack_from('<3HB',raw);assert version==2
n=raw[7];assert raw[8:8+n].decode()=='RGBA_BC7_UNorm'
offset=11+n;mips=[]
for mip in range(count):
 width,height=max(1,w>>mip),max(1,h>>mip);size=((width+3)//4)*((height+3)//4)*16
 header=struct.pack('<7I',124,0x81007,height,width,size,0,1)+bytes(44)+struct.pack('<8I',32,4,0x30315844,0,0,0,0,0)+struct.pack('<5I',0x1000,0,0,0,0)
 im=Image.open(io.BytesIO(b'DDS '+header+struct.pack('<5I',98,3,0,1,0)+raw[offset:offset+size]));blue=np.asarray(im)[:,:,2]
 values,counts=np.unique(blue,return_counts=True)
 mips.append(dict(mip=mip,width=width,height=height,blueHistogram={str(int(k)):int(v) for k,v in zip(values,counts)},allActive=bool(np.all(blue>0)),allOpaque=bool(np.all(blue==255))))
 offset+=size
assert offset==len(raw)
def cid(v):return ''.join(f'{b&15:x}{b>>4:x}' for b in struct.pack('<4i',*[v[f'm_GuidPart{i}'] for i in range(1,5)]))
pieces=[]
for key in ['121075','121076','121072','121074']:
 d=net[key];assert cid(d['m_SurfaceAssets'][0])==surface
 pieces.append(dict(pathId=key,name=d['m_Name'],surface=surface))
shader=ROOT/'artifacts/layer-decoupling/shared/outputs/native-repeat-mechanism/581-133.asm';s=shader.read_text()
for instruction in ['mad r7.xyz, r9.zzzz, r8.xyzx, r7.xyzx','mad r10.xyz, r9.zzzz, r11.xyzx, r10.xyzx','mad r9.xyz, r9.zzzz, r11.xyzx, r9.wxyw']:assert instruction in s
result=dict(surface=surface,surfaceEntry=source['entry'],surfaceSHA256=hashlib.sha256(material).hexdigest(),pieces=pieces,textureCids=refs,textureEntries={k:matches[v] for k,v in refs.items()},maskSHA256=hashlib.sha256(raw).hexdigest(),mips=mips,shaderSHA256=hashlib.sha256(shader.read_bytes()).hexdigest(),appearanceLayer='Base fine grey bed and worldspace gravel jointly constitute Ballast; original Mask blue blends these two families.',dispatch='Match exact source Surface and original texture families; authorize complete Ballast receiver replacement for Lift03 Auxiliary LOD0/1. TrackBed names a geometry role, not a second appearance type. Native rail/sleeper lane meshes remain independent. B254 alone does not require retaining selected base because both layers belong to supported Ballast.', additionalEvidence='artifacts/material-remap/Lift04/track-bed-layer-evidence.json',scope='Native source resources and full texture mip domain. Not a generated runtime sample, selected arbitrary road classification, native render comparison or independent-load acceptance.')
(OUT/'auxiliary-layer-evidence.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(surface=surface,mips=len(mips),blueRanges=[list(x['blueHistogram']) for x in mips]),indent=2))
