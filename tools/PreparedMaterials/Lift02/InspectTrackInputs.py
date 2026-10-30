"""Read original track surfaces and complete source mask mips; no generated assets."""
from pathlib import Path
import hashlib, io, json, struct, zipfile
import numpy as np
from PIL import Image
from ExportSharedSampling import descriptor,read_string

ROOT=Path(__file__).resolve().parents[3]
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
RESEARCH=ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research'
OUT=ROOT/'artifacts/material-remap/Lift02/source-mapping/track-inputs'
IDS=['a3522ead5e61e02569f0884fb4607eec','be8e4a86010f693e8d121c139d53e9a6','514691f970b5843a8093eee79c2bd22e','25fb1c9826d4752435921f06ec5c57fc']
def main():
    OUT.mkdir(parents=True,exist_ok=True)
    audit=json.loads((RESEARCH/'all-movable-audit.json').read_text())
    rows=[]; wanted=set(); archives={}
    def archive(path):
        key=str(path)
        if key not in archives:archives[key]=zipfile.ZipFile(path)
        return archives[key]
    for identity in IDS:
        a=audit['assets'][identity]
        path=GAME/a['package']
        z=archive(path)
        raw=z.read(a['entry']); template,vt,_,_,bindings,keywords=descriptor(raw)
        rows.append(dict(cid=identity,entry=a['entry'],sha256=hashlib.sha256(raw).hexdigest(),template=template,vt=vt,bindings=bindings,keywords=keywords))
        wanted.update(bindings.values())
    textures={}
    for family in ('Game','BridgesAndPorts'):
        for path in (GAME/'Content'/family).glob('*.cok'):
            z=archive(path); names=set(z.namelist())
            for name in names:
                if not name.endswith('.cid'):continue
                identity=z.read(name).decode().strip()
                if identity not in wanted:continue
                raw=z.read(name[:-4]); r=io.BytesIO(raw)
                version,w,h=struct.unpack('<3H',r.read(6))
                depth=struct.unpack('<H',r.read(2))[0] if version>=4 else 1
                mips=r.read(1)[0]; fmt=r.read(1)[0] if version>=3 else read_string(r)
                dimension=r.read(1)[0] if version>=4 else 2
                if version>=2:r.read(3)
                record=dict(cid=identity,archive=str(path),entry=name[:-4],sha256=hashlib.sha256(raw).hexdigest(),width=w,height=h,format=fmt,mips=[])
                assert version<=4 and depth==1 and fmt in (108,109,'RGBA_BC7_UNorm','RGBA_BC7_SRGB') and dimension==2,(name,version,w,h,depth,mips,fmt,dimension)
                at=r.tell()
                is_mask=any(row['bindings'].get('_MaskMap')==identity for row in rows)
                for mip in range(mips):
                    mw,mh=max(1,w>>mip),max(1,h>>mip); size=((mw+3)//4)*((mh+3)//4)*16
                    if is_mask or mip==0:
                        im=Image.frombytes('RGBA',(mw,mh),raw[at:at+size],'bcn',(7,'BC7')); p=np.asarray(im)
                        if is_mask:
                            blue=p[:,:,2]
                            record['mips'].append(dict(level=mip,width=mw,height=mh,minimum=p.min((0,1)).tolist(),maximum=p.max((0,1)).tolist(),blueZero=int((blue==0).sum()),blueFull=int((blue==255).sum()),bluePartial=int(((blue>0)&(blue<255)).sum())))
                        if mip==0:
                            im.transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(OUT/(identity+'.png'))
                    at+=size
                assert at==len(raw)
                textures[identity]=record
    assert wanted==set(textures),wanted-set(textures)
    shader=RESEARCH/'shader-programs/581-133.asm'
    asm=shader.read_text()
    assert 'mad r7.xyz, r9.zzzz, r8.xyzx, r7.xyzx' in asm
    assert 'mad r10.xyz, r9.zzzz, r11.xyzx, r10.xyzx' in asm
    assert 'mad r9.xyz, r9.zzzz, r11.xyzx, r9.wxyw' in asm
    result=dict(sources=rows,textures=list(textures.values()),shader=dict(path=str(shader),sha256=hashlib.sha256(shader.read_bytes()).hexdigest(),evidence='Lines 116-145: Mask B (r9.z) blends base/worldspace albedo, tangent-frame normals and mask parameters.'),scope='Original Surface descriptor and source mask pixels only. Names identify candidate track assets, not semantic pixel labels. Selected receiver-domain coverage must be verified separately.')
    (OUT/'evidence.json').write_text(json.dumps(result,indent=2)+'\n')
    for z in archives.values():z.close()
    print(json.dumps(rows,indent=2))
if __name__=='__main__':main()
