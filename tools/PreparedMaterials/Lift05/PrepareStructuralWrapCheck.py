"""Extract original raw mesh channels for the isolated native wrap check, no bake."""
import ctypes as ct
import hashlib
import json
import struct
import sys
import zipfile
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
OUT=ROOT/'artifacts/material-remap/Lift05/native-wrap-inputs'


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    codec=NativeGeometry(GAME/'Plugins/x86_64/PipelinePlugin.dll')
    summary=json.loads((ROOT/'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/summary.json').read_text())
    manifests=[]
    for lod,level in enumerate(summary['levels'][:2]):
        with zipfile.ZipFile(level['surface']['archive']) as archive:
            raw=archive.read(level['geometryEntry'])
            assert hashlib.sha256(raw).hexdigest()==level['geometrySha256']
            surface=archive.read(level['surface']['entry'])
            assert hashlib.sha256(surface).hexdigest()==level['surface']['sha256']
        (OUT/'native.Surface').write_bytes(surface)
        version,count,flags=struct.unpack_from('<HII',raw)
        headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
        offset=10+count*88+sum(h[-1]*40 for h in headers)
        def block(size,n,stride,indices=False):
            nonlocal offset
            data=raw[offset:offset+size];offset+=size
            if flags&2:
                size=codec.dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(size)
                assert codec.dll.decompress_data(data,len(data),buffer,size,0)==size
                data=buffer.raw
            if flags&1:
                buffer=ct.create_string_buffer(n*stride)
                method=codec.dll.decompress_meshopt_index_buffer if indices else codec.dll.decompress_meshopt_vertex_attribute
                assert method(data,len(data),n,stride,buffer)==0
                data=buffer.raw
            assert len(data)==n*stride
            return data
        mesh_indices=[]
        for h in headers:
            stride=4 if h[17]&1 else 2
            data=block(h[16],h[19],stride,True)
            mesh_indices.append(struct.unpack('<'+('I' if stride==4 else 'H')*h[19],data))
        attributes=[]
        widths={0:4,1:2,2:1,3:1,4:2,5:2,6:1,7:1,8:2,9:2,10:4,11:4}
        for attr in range(14):
            for mesh,h in enumerate(headers):
                size=h[2+attr]
                if not size:continue
                fmt=(h[0]>>(attr*4))&15;dim=((h[1]>>(attr*2))&3)+1
                data=block(size,h[18],widths[fmt]*dim)
                if mesh==0:attributes.append((attr,fmt,dim,data))
        vc=headers[0][18];ix=mesh_indices[0]
        output=struct.pack('<3i',vc,len(ix),len(attributes))+struct.pack('<%di'%len(ix),*ix)
        for attr,fmt,dim,data in attributes:output+=struct.pack('<4i',attr,fmt,dim,len(data))+data
        path=OUT/f'lod{lod}.bin';path.write_bytes(output)
        manifests.append(dict(lod=lod,nativeSHA256=level['geometrySha256'],fixtureSHA256=hashlib.sha256(output).hexdigest(),
            vertices=vc,indices=len(ix),attributes=[dict(attribute=a,format=f,dimension=d,bytes=len(b)) for a,f,d,b in attributes]))
    (OUT/'manifest.json').write_text(json.dumps(manifests,indent=2)+'\n')
    print(json.dumps(manifests,indent=2))


if __name__=='__main__':main()
