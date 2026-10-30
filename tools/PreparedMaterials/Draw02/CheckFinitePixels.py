"""Inspect the uncompressed RGBA trial stage before native BC7 encoding."""
import ctypes as c,json,struct
from pathlib import Path
import numpy as np
from PIL import Image
root=Path(__file__).resolve().parents[3];out=root/'artifacts/material-remap/Draw02/near-cells'
dll=c.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
dll.decompress_calc_bound.argtypes=[c.c_void_p,c.c_int64,c.c_int];dll.decompress_calc_bound.restype=c.c_int64
dll.decompress_data.argtypes=[c.c_void_p,c.c_int64,c.c_void_p,c.c_int64,c.c_int];dll.decompress_data.restype=c.c_int64
report=json.loads((out/'producer-report.json').read_text());rows=[]
for row in report['products']:
    data=Path(row['path']).read_bytes();compressed=struct.unpack_from('<i',data,14)[0]
    raw=data[18:]
    if compressed>=0:
        n=dll.decompress_calc_bound(raw,len(raw),0);buf=c.create_string_buffer(n)
        assert dll.decompress_data(raw,len(raw),buf,n,0)==n;raw=buf.raw
    w,h=row['width'],row['height'];at=0;counts=[]
    expected=sum(max(1,w>>i)*max(1,h>>i)*4 for i in range(row['mips']))
    if len(raw)!=expected:
        raise ValueError('This checker requires the pre-BC7 RGBA trial report; use FiniteNativeCheck for the encoded candidate.')
    for mip in range(row['mips']):
        a=np.frombuffer(raw[at:at+w*h*4],np.uint8).reshape(h,w,4)
        counts.append(int(np.all(a==[255,0,255,255],axis=2).sum()))
        if mip==0 and row['channel']=='_BaseColorMap':Image.fromarray(a).save(out/(row['unit']+'-candidate.png'))
        at+=w*h*4;w=max(1,w//2);h=max(1,h//2)
    rows.append(dict(unit=row['unit'],channel=row['channel'],uncoveredByMip=counts))
    print(row['unit'],row['channel'],sum(counts),counts)
(out/'coverage-check.json').write_text(json.dumps(rows,indent=2)+'\n')
