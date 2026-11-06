"""Recover unchanged native paint mip blocks and supply explicit object PBR layers.

Writes a separate candidate; it does not edit installed assets or saved owners.
"""
from pathlib import Path
import argparse, ctypes, hashlib, importlib.util, json, struct, zipfile

ROOT = Path(__file__).resolve().parents[3]
SOURCE = '4b5500b1b5c270890d99149f7f62e51a'

def identity(data):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in hashlib.sha256(data).digest()[:16])

def text(s):
    b=s.encode(); assert len(b)<128
    return bytes([len(b)])+b

def token(cid):
    return bytes(int(cid[i+1]+cid[i],16) for i in range(0,32,2))

def main():
    p=argparse.ArgumentParser();p.add_argument('game',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    spec=importlib.util.spec_from_file_location('extract',ROOT/'tools/PrepareRepeatUnits/ExtractVtSources.py')
    module=importlib.util.module_from_spec(spec);spec.loader.exec_module(module)
    def read(archive,cid):
        with zipfile.ZipFile(a.game/'Content/Game'/archive) as z:
            names=[n[:-4] for n in z.namelist() if n.endswith('.cid') and z.read(n).decode().strip()==cid]
            assert len(names)==1,(archive,cid,names)
            return z.read(names[0])
    source=read('Blob.cok',SOURCE)
    assert hashlib.sha256(source).hexdigest()=='6bb8e926e34c3b39f8ad0cff6d8a893b5c8e305c0ace1370438d2614f799895f'
    backing=module.cid(source[83:99]);decoder=module.Decoder(a.game)
    base,evidence=decoder.recover(dict(kind='VTTexture',name='_BaseColorMap'),read('VT.cok',backing),SOURCE)
    # Lossless native compression changes only the storage envelope, never mip blocks.
    lib=decoder.library;bound=lib.compress_calc_bound;bound.argtypes=[ctypes.c_int64,ctypes.c_int];bound.restype=ctypes.c_int64
    encode=lib.compress_data;encode.argtypes=[ctypes.c_void_p,ctypes.c_int64,ctypes.c_void_p,ctypes.c_int64,ctypes.c_int,ctypes.c_int];encode.restype=ctypes.c_int64
    raw=base[18:];buf=ctypes.create_string_buffer(bound(len(raw),0));size=encode(raw,len(raw),buf,len(buf),0,11)
    assert 0<size<=len(buf)
    check=ctypes.create_string_buffer(len(raw));assert decoder.decode(buf,size,check,len(raw),0)==len(raw) and check.raw==raw
    base=base[:14]+struct.pack('<i',size)+buf.raw[:size]
    # RGBAHalf retains the exact neutral value in the native AG normal encoding.
    # Mask white keeps the existing Metallic=0 / Smoothness=.35 scalars and AO=1;
    # zero control prevents unintended palette recoloring.
    def constant(rgba):
        return struct.pack('<4H6Bi',5,1,1,1,1,48,2,2,1,1,-1)+struct.pack('<4e',*rgba)
    payloads={'_BaseColorMap':base,'_NormalMap':constant((1,.5,1,.5)),
              '_MaskMap':constant((1,1,1,1))}
    # Preserve all old explicit material parameters and keywords, changing only
    # VT storage to fully bound regular textures. No color or alpha adjustment.
    legacy=ROOT/'assets/Materials/Draw01/NativeSources/d765f1348645cc68318b5ac29c61abde/Object.Surface'
    old=(legacy if legacy.exists() else ROOT/'assets/Materials/Draw01/Paint/SkinnedPaint.Surface').read_bytes()
    assert hashlib.sha256(old).hexdigest()=='d765f1348645cc68318b5ac29c61abdea9d8c90c663a2e62a056b8019c05519f'
    assert old[6]==1
    at=7+276;at+=1 if old[at]==0 else 17
    props=old[at:];needle=b'\xff'+struct.pack('<i',0)+b'\xff'+struct.pack('<i',2)+text('_TANGENTSPACE_OCTO')
    pos=props.index(needle)
    bindings={name:identity(value) for name,value in payloads.items()}
    bindings['_ControlMask']='09426c88de2bf8983a7e1943ea8a21ad'
    texture_map=b'\xff'+struct.pack('<i',len(bindings))+b''.join(text(k)+b'\xff'+token(cid) for k,cid in bindings.items())
    surface=old[:6]+b'\x00'+props[:pos]+texture_map+props[pos+5:]
    a.output.mkdir(parents=True,exist_ok=True)
    for name,data in {'SkinnedPaint.Surface':surface,**{identity(v)+'.Texture':v for v in payloads.values()}}.items():
        (a.output/name).write_bytes(data);(a.output/(name+'.cid')).write_text(identity(data))
    report=dict(sourceSurface=SOURCE,sourceBacking=backing,surfaceCid=identity(surface),baseMipEvidence=evidence,
        channels={k:dict(cid=identity(v),bytes=len(v),sha256=hashlib.sha256(v).hexdigest()) for k,v in payloads.items()},
        sourceMipBlocksUnchanged=True,geometryUvColorAlphaUnchanged=True,normal=[1,.5,1,.5],mask=[1,1,1,1],control=[0,0,0,0])
    (a.output.parent/'complete-paint-report.json').write_text(json.dumps(report,indent=2));print(json.dumps(report))

if __name__=='__main__':main()
