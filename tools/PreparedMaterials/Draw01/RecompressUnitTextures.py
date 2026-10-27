"""One bounded native ZSTD level-22 trial, without altering any mip bytes."""
import ctypes as C
import hashlib
import json
import struct
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ENV = ROOT / 'artifacts/agent-material-env/Draw01'
OUT = ENV / 'repeat-units-single-period/lossless-level22'
DLL = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')


def main():
    lib = C.CDLL(str(DLL))
    decode = lib.decompress_data
    decode.argtypes = [C.c_void_p,C.c_int64,C.c_void_p,C.c_int64,C.c_int]
    encode = lib.compress_data
    encode.argtypes = decode.argtypes+[C.c_int]
    bound = lib.compress_calc_bound
    bound.argtypes = [C.c_int64,C.c_int]
    for method in (decode,encode,bound): method.restype=C.c_int64
    OUT.mkdir(parents=True,exist_ok=True)
    rows=[]
    for batch in ['repeat-units-control-corrected','repeat-units-single-period']:
        report=json.loads((ENV/batch/'report.json').read_text())
        for unit in report['units']:
            for channel in unit['channels']:
                source=ENV/batch/'output/Draw01/Shared'/(channel['cid']+'.Texture')
                data=source.read_bytes()
                assert struct.unpack_from('<H',data)[0]==5
                w,h=struct.unpack_from('<HH',data,2)
                mips=data[8]
                # These once-prepared units are native R8G8B8A8 UNorm/SRGB.
                assert data[9] in (4,8), data[9]
                size=sum(max(1,w>>m)*max(1,h>>m)*4 for m in range(mips))
                stored=struct.unpack_from('<i',data,14)[0]
                decoded=C.create_string_buffer(size)
                if stored==-1: raw=data[18:]
                else:
                    assert stored==len(data)-18
                    assert decode(data[18:],stored,decoded,size,0)==size
                    raw=decoded.raw
                assert len(raw)==size
                capacity=bound(size,0)
                compressed=C.create_string_buffer(capacity)
                length=encode(raw,size,compressed,capacity,0,22)
                assert 0<length<=capacity
                assert decode(compressed,length,decoded,size,0)==size and decoded.raw==raw
                candidate=data[:14]+struct.pack('<i',length)+compressed.raw[:length]
                if len(candidate)>=len(data): candidate=data
                digest=hashlib.sha256(candidate).digest()
                cid=''.join(f'{x&15:x}{x>>4:x}' for x in digest[:16])
                (OUT/(cid+'.Texture')).write_bytes(candidate)
                (OUT/(cid+'.Texture.cid')).write_text(cid)
                rows.append(dict(unit=unit['id'],channel=channel['name'],oldCid=channel['cid'],newCid=cid,
                                 oldBytes=len(data),newBytes=len(candidate),rawSha256=hashlib.sha256(raw).hexdigest(),
                                 width=w,height=h,mips=mips,bitExact=True))
    result=dict(level=22,oldBytes=sum(r['oldBytes'] for r in rows),newBytes=sum(r['newBytes'] for r in rows),channels=rows)
    (OUT/'report.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k!='channels'}))


if __name__=='__main__':main()
