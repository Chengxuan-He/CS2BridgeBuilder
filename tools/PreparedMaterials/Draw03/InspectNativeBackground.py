"""Read original Texture2D payloads for Draw03's native network asphalt family."""
from pathlib import Path
import hashlib
import json
import struct
import zipfile
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw03/lane-placement/native-background'
CIDS={'BaseColor':'b00176216b0ce36579bce6fc675cfb77','Normal':'9f446790360787756b0e5909db12e200','MaskMap':'375b871b66aaf675dacef141eb2a9810'}

def read():
    result={}; evidence=[]
    archive=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/Game/Blob.cok')
    with zipfile.ZipFile(archive) as z:
        for name,cid in CIDS.items():
            entry=next(n for n in z.namelist() if n.startswith('RoadEUWorldspace_'+name+'_') and n.endswith('.Texture2D'))
            raw=z.read(entry);assert z.read(entry+'.cid').decode().strip()==cid
            version,w,h,mips,length=struct.unpack_from('<HHHBB',raw)
            fmt=raw[8:8+length].decode();filt,wrap,aniso=raw[8+length:11+length]
            assert version==2 and w==h==1024 and fmt.startswith('RGBA_BC7_') and wrap==0
            at=11+length;levels=[]
            for m in range(mips):
                ww,hh=max(1,w>>m),max(1,h>>m);size=((ww+3)//4)*((hh+3)//4)*16
                levels.append(np.asarray(Image.frombytes('RGBA',(ww,hh),raw[at:at+size],'bcn',(7,'BC7'))));at+=size
            assert at==len(raw)
            result[name]=levels
            evidence.append(dict(property=name,cid=cid,archive=str(archive),entry=entry,sha256=hashlib.sha256(raw).hexdigest(),
                width=w,height=h,mips=mips,format=fmt,filter=filt,wrap=wrap,anisotropy=aniso))
    return result,evidence

if __name__=='__main__':
    textures,evidence=read();OUT.mkdir(parents=True,exist_ok=True)
    for name,levels in textures.items():Image.fromarray(levels[0]).save(OUT/(name+'.png'))
    (OUT/'evidence.json').write_text(json.dumps(dict(sourcePiece=120675,sourceLaneComponent=162720,
        sourceSurface='1d7dad9f64c9fe55a809f1080c39fb92',targetSurface='88df662f6bc8058d3ecf8f7a5d7f2239',
        densityCyclesPerMetre=.4000000059604645,textures=evidence,
        scope='Corresponding original asphalt PBR family of the prototype Car Drive Piece 3; not equivalent to the moving object atlas or full network blend.'),indent=2)+'\n')
    print('PASS three original 1024-square BC7 Texture2D payloads, complete mip chains; no new production textures')
