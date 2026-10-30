"""Measure source-versus-candidate filtered samples; never imply acceptance.

This independent BC7 decoder checks stored pixels and mip exhaustion. Native
shader/tangent and generated-owner loading checks are separate requirements.
"""
import ctypes as ct
import io
import json
import struct
from pathlib import Path
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/PedestrianDraw02'


def levels(path):
    raw = path.read_bytes()
    version, width, height, depth = struct.unpack_from('<4H', raw)
    assert version == 5 and depth == 1 and raw[10] == 2
    count, fmt = raw[8:10]
    assert fmt in (108, 109)
    compressed = struct.unpack_from('<i', raw, 14)[0]
    payload = raw[18:]
    size = sum(((max(1,width>>i)+3)//4)*((max(1,height>>i)+3)//4)*16 for i in range(count))
    if compressed != -1:
        assert compressed == len(payload)
        lib = ct.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
        lib.decompress_data.argtypes = [ct.c_void_p,ct.c_int64,ct.c_void_p,ct.c_int64,ct.c_int]
        lib.decompress_data.restype = ct.c_int64
        target = ct.create_string_buffer(size)
        assert lib.decompress_data(payload,len(payload),target,size,0) == size
        payload = target.raw
    assert len(payload) == size
    result, at = [], 0
    for mip in range(count):
        w,h=max(1,width>>mip),max(1,height>>mip)
        length=((w+3)//4)*((h+3)//4)*16
        header=[124,0x81007,h,w,length,0,1]+[0]*11
        header += [32,4,int.from_bytes(b'DX10','little'),0,0,0,0,0,0x1000,0,0,0,0]
        dds=b'DDS '+struct.pack('<31I',*header)+struct.pack('<5I',98,3,0,1,0)+payload[at:at+length]
        pixels=np.asarray(Image.open(io.BytesIO(dds)).convert('RGBA')).astype(np.float64)/255
        if fmt == 108:
            pixels[:,:,:3]=np.where(pixels[:,:,:3]<=.04045,pixels[:,:,:3]/12.92,((pixels[:,:,:3]+.055)/1.055)**2.4)
        result.append(pixels)
        at += length
    return result


def sample(pixels, uv, repeat):
    h,w=pixels.shape[:2]
    xy=uv*[w,h]-.5
    low=np.floor(xy).astype(np.int64)
    f=xy-low
    result=np.zeros((len(uv),4))
    for dx,dy in ((0,0),(1,0),(0,1),(1,1)):
        x,y=low[:,0]+dx,low[:,1]+dy
        if repeat: x,y=x%w,y%h
        else: x,y=np.clip(x,0,w-1),np.clip(y,0,h-1)
        weight=(f[:,0] if dx else 1-f[:,0])*(f[:,1] if dy else 1-f[:,1])
        result += pixels[y,x]*weight[:,None]
    return result


def main():
    recipe=json.loads((OUT/'native-panel-recipe.json').read_text())
    report=json.loads((OUT/'native-panel-candidate-report.json').read_text())['units'][0]
    panel=next(p for p in recipe['levels'][0]['panels'] if p['firstFace']==8556)
    q,uv=[],[]
    # Interior triangle samples prove the exact piecewise source mapping rather
    # than fitting a rectangle. Include vertices/edges as a separate error scope.
    interior=[]
    for t in panel['triangles']:
        for i in range(33):
            for j in range(33-i):
                bary=np.array([i,j,32-i-j])/32
                q.append(1-bary@np.asarray(t['unit']))
                uv.append(bary@np.asarray(t['sourceUv']))
                interior.append(bool(np.all(bary>0)))
    q,uv,interior=np.asarray(q),np.asarray(uv),np.asarray(interior)
    output_uv=q.copy();output_uv[:,0]=.25+.5*q[:,0]
    rows=[]
    for channel in recipe['channels']:
        source=levels(Path(channel['path']))
        product=next(c for c in report['channels'] if c['name']==channel['name'])
        candidate=levels(OUT/'candidate/PedestrianDraw02/Shared'/(product['cid']+'.Texture'))
        mips=[]
        for mip, pixels in enumerate(source):
            actual=sample(candidate[min(mip,len(candidate)-1)],output_uv,True)
            expected=sample(pixels,uv,False)
            error=abs(actual-expected)
            mips.append(dict(sourceMip=mip,candidateMip=min(mip,len(candidate)-1),
                maxAbsolute=float(error.max()), meanAbsolute=float(error.mean()),
                interiorMaxAbsolute=float(error[interior].max())))
        rows.append(dict(channel=channel['name'],samples=len(q),mips=mips))
    result=dict(status='MEASURED_NOT_ACCEPTED',units=1,channels=rows,
        limitations=['Comparisons use equal integer mip indices before the shared source-density mip correction.',
                    'Source tail mips 11 and 12 are compared to the candidate exhausted mip 10 explicitly.',
                    'Bilinear stored BC7 pixel evidence only, not native rendering or tangent-frame acceptance.'])
    (OUT/'native-panel-candidate-filter-check.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps([(r['channel'],r['mips'][0],r['mips'][-1]) for r in rows]))


if __name__ == '__main__':
    main()
