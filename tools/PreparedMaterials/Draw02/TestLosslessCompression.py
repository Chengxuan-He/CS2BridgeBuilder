"""Read-only native BC7 block recompression experiment, with bytewise round trips."""
import ctypes as c,json,time
from pathlib import Path
root=Path(__file__).resolve().parents[3];out=root/'artifacts/material-remap/Draw02/near-cells'
dll=c.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
for name,args in [('decompress_calc_bound',[c.c_void_p,c.c_int64,c.c_int]),('compress_calc_bound',[c.c_int64,c.c_int]),('decompress_data',[c.c_void_p,c.c_int64,c.c_void_p,c.c_int64,c.c_int]),('compress_data',[c.c_void_p,c.c_int64,c.c_void_p,c.c_int64,c.c_int,c.c_int])]:
    fn=getattr(dll,name);fn.argtypes=args;fn.restype=c.c_int64
rows=[]
for item in json.loads((out/'producer-report.json').read_text())['products']:
    if not item['unit'].startswith('lod0-'):continue
    payload=Path(item['path']).read_bytes()[18:];n=dll.decompress_calc_bound(payload,len(payload),0);raw=c.create_string_buffer(n)
    assert dll.decompress_data(payload,len(payload),raw,n,0)==n
    cap=dll.compress_calc_bound(n,0);encoded=c.create_string_buffer(cap);restored=c.create_string_buffer(n)
    for level in (11,19,22):
        start=time.perf_counter();count=dll.compress_data(raw,n,encoded,cap,0,level);assert count>0
        assert dll.decompress_data(encoded,count,restored,n,0)==n and restored.raw==raw.raw
        row=dict(unit=item['unit'],cid=item['cid'],channel=item['channel'],level=level,rawBytes=n,compressedBytes=count,originalBytes=len(payload),seconds=time.perf_counter()-start,byteIdenticalRoundTrip=True)
        rows.append(row);print(row,flush=True)
(out/'lossless-compression-check.json').write_text(json.dumps(rows,indent=2)+'\n')
