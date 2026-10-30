"""Decode original packed normals/tangents using the installed native codec."""
import argparse
import ctypes as ct
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('repository',type=Path);p.add_argument('research',type=Path);p.add_argument('plugin',type=Path);p.add_argument('output',type=Path)
    a=p.parse_args();sys.path.insert(0,str(a.repository/'tools/GeometryMetaprogram'))
    from UvDensityAudit import NativeGeometry
    reader=NativeGeometry(a.plugin);dll=reader.dll
    audit=json.loads((a.repository/'artifacts/layer-decoupling/PedestrianDraw01/outputs/native-repeat-mechanism/audit.json').read_text());rows=[]
    for level in audit['levels']:
        path=a.research/'movable-skin'/(level['source']+'.Geometry');raw=path.read_bytes()
        assert hashlib.sha256(raw).hexdigest()==level['geometrySHA256']
        version,count,flags=struct.unpack_from('<HII',raw);assert version==1
        headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
        offset=10+count*88+sum(h[-1]*40 for h in headers)+sum(h[16] for h in headers)
        attributes={}
        for attr in range(14):
            for mi,h in enumerate(headers):
                size=h[2+attr];data=raw[offset:offset+size];offset+=size
                if not size or attr not in (1,2) or mi!=0:continue
                fmt=(h[0]>>(attr*4))&15;dim=((h[1]>>(attr*2))&3)+1
                assert (attr,fmt,dim) in ((1,5,2),(2,0,1))
                if flags&2:
                    capacity=dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(capacity)
                    assert dll.decompress_data(data,len(data),buffer,capacity,0)==capacity;data=buffer.raw
                if flags&1:
                    buffer=ct.create_string_buffer(h[18]*4)
                    assert dll.decompress_meshopt_vertex_attribute(data,len(data),h[18],4,buffer)==0;data=buffer.raw
                assert len(data)==h[18]*4
                dest=ct.create_string_buffer(h[18]*(12 if attr==1 else 16))
                function=getattr(dll,'math_array_oct16_to_fp32normals' if attr==1 else 'math_array_oct15_to_fp32tangents')
                function.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p];function.restype=None
                function(data,h[18],dest)
                attributes[attr]=np.frombuffer(dest.raw,dtype='<f4').reshape(h[18],3 if attr==1 else 4)
        for bone in (0,1,2):
            for side in (-1,1):
                faces=[f for f in level['walkwayFaces'] if f['bone']==bone and np.mean(np.array(f['positions'])[:,0])*side>0]
                ids=sorted({i for f in faces for i in f['indices']})
                rows.append(dict(lod=level['lod'],bone=bone,side=side,faces=[f['face'] for f in faces],
                    normals=np.unique(attributes[1][ids],axis=0).tolist(),tangents=np.unique(attributes[2][ids],axis=0).tolist(),
                    perFace=[dict(face=f['face'],normals=attributes[1][f['indices']].tolist(),tangents=attributes[2][f['indices']].tolist()) for f in faces]))
    a.output.parent.mkdir(parents=True,exist_ok=True)
    far=json.loads((a.output.parent/'far-road-mapping.json').read_text())
    face_frames={f['face']:f for row in rows if row['lod']==2 for f in row['perFace']}
    far_checks=[]
    for chart in far['charts']:
        expected={tuple(t) for face in chart['faces'] for t in face_frames[face]['tangents']}
        assert len(expected)==1
        normal=np.array([0.,1.,0.]);u,v=np.array(chart['u'][:3]),np.array(chart['v'][:3])
        determinant=np.dot(np.cross(u,v),normal);tangent=np.cross(v,normal)/determinant;tangent/=np.linalg.norm(tangent)
        actual=tuple(tangent.tolist()+[float(np.sign(determinant))]);assert actual in expected
        far_checks.append(dict(faces=chart['faces'],derivedTangent=actual,matchesNativePackedTangent=True))
    a.output.write_text(json.dumps(dict(nativeDecoderSHA256=hashlib.sha256(a.plugin.read_bytes()).hexdigest(),domains=rows,farProjectionFrames=far_checks),indent=2)+'\n')
    print(json.dumps([{k:v for k,v in row.items() if k not in ('faces','perFace')} for row in rows]))


if __name__=='__main__':main()
