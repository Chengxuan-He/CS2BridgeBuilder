"""Decode original Lift01 floor frames with the installed native octahedral codec."""
import ctypes as ct
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry


def main():
    plugin=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
    research=ROOT.parent/'2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/movable-skin'
    reader=NativeGeometry(plugin)
    dll=reader.dll
    evidence=json.loads((ROOT/'artifacts/layer-decoupling/Lift01/outputs/native-repeat-mechanism/audit.json').read_text())
    rows=[]
    for lod, suffix, faces in [(0,'',[259058,259059,259060,259061]),(1,'_LOD1',[165669,165670]),(2,'_LOD2',[320,321,342,343,708,709,730,731])]:
        if lod<2: faces=[f['face'] for f in evidence['reports'][lod]['sourceFaces']]
        name='LiftBridge01'+suffix+' Mesh'
        raw=(research/(name+'.Geometry')).read_bytes()
        assert hashlib.sha256(raw).hexdigest()==evidence['reports'][lod]['geometrySHA256']
        version,count,flags=struct.unpack_from('<HII',raw)
        assert version==1
        headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
        offset=10+count*88+sum(h[-1]*40+h[16] for h in headers)
        attrs={}
        for attr in range(14):
            for mi,h in enumerate(headers):
                size=h[2+attr]; data=raw[offset:offset+size];offset+=size
                if not size or attr not in (1,2) or mi!=0:continue
                fmt=(h[0]>>(attr*4))&15;dim=((h[1]>>(attr*2))&3)+1
                assert (attr,fmt,dim) in ((1,5,2),(2,0,1))
                if flags&2:
                    capacity=dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(capacity)
                    assert dll.decompress_data(data,len(data),buffer,capacity,0)==capacity
                    data=buffer.raw
                if flags&1:
                    buffer=ct.create_string_buffer(h[18]*4)
                    assert dll.decompress_meshopt_vertex_attribute(data,len(data),h[18],4,buffer)==0
                    data=buffer.raw
                assert len(data)==h[18]*4
                dest=ct.create_string_buffer(h[18]*(12 if attr==1 else 16))
                fn=getattr(dll,'math_array_oct16_to_fp32normals' if attr==1 else 'math_array_oct15_to_fp32tangents')
                fn.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p];fn.restype=None
                fn(data,h[18],dest)
                attrs[attr]=np.frombuffer(dest.raw,'<f4').reshape(h[18],3 if attr==1 else 4)
        mesh=np.load(ROOT/'artifacts/uv-density/native'/(name+'.npz'))
        for face in faces:
            ids=mesh['mesh0_faces'][face]
            rows.append(dict(lod=lod,face=face,indices=ids.tolist(),positions=mesh['mesh0_positions'][ids].tolist(),
                uv=mesh['mesh0_uv0'][ids,:2].tolist(),normals=attrs[1][ids].tolist(),tangents=attrs[2][ids].tolist()))
    out=ROOT/'artifacts/material-remap/Lift01/floor-native-frames.json'
    out.write_text(json.dumps(dict(nativeCodecSHA256=hashlib.sha256(plugin.read_bytes()).hexdigest(),faces=rows),indent=2))
    print(out)
    for lod in range(3):
        selected=[r for r in rows if r['lod']==lod]
        print(lod, 'faces',len(selected),'N',np.unique(np.concatenate([r['normals'] for r in selected]),axis=0).tolist(),'T',np.unique(np.concatenate([r['tangents'] for r in selected]),axis=0).tolist())


if __name__=='__main__':main()
