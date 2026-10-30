"""Record original packed frame attributes for every measured road triangle."""
import ctypes as ct
import json
from pathlib import Path
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry


def packed(reader,path):
    raw=path.read_bytes()
    version,count,flags=struct.unpack_from('<HII',raw)
    assert version==1
    headers=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
    offset=10+count*88+sum(h[-1]*40 for h in headers)+sum(h[16] for h in headers)
    result={}
    for attribute in range(14):
        for mesh,h in enumerate(headers):
            size=h[2+attribute]
            data=raw[offset:offset+size];offset+=size
            if mesh!=0 or attribute not in (1,2) or not size:continue
            fmt=(h[0]>>(attribute*4))&15
            dim=((h[1]>>(attribute*2))&3)+1
            assert (attribute,fmt,dim) in ((1,5,2),(2,0,1))
            if flags&2:
                capacity=reader.dll.decompress_calc_bound(data,len(data),0)
                buffer=ct.create_string_buffer(capacity)
                assert reader.dll.decompress_data(data,len(data),buffer,capacity,0)==capacity
                data=buffer.raw
            if flags&1:
                buffer=ct.create_string_buffer(h[18]*4)
                assert reader.dll.decompress_meshopt_vertex_attribute(data,len(data),h[18],4,buffer)==0
                data=buffer.raw
            assert len(data)==h[18]*4
            result[attribute]=np.frombuffer(data,dtype='<u4')
    return result


def main():
    output=ROOT/'artifacts/material-remap/PedestrianDraw02'
    recipe=json.loads((output/'native-panel-recipe.json').read_text())
    metadata=json.loads((ROOT/'artifacts/prepared-materials/PedestrianDraw02/source-consolidation/native-charts.json').read_text())
    arrays=np.load(ROOT/'artifacts/prepared-materials/PedestrianDraw02/source-consolidation/native-charts.npz')
    reader=NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
    decode=reader.dll.math_array_oct15_to_fp32tangents
    decode.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p]
    decode.restype=None
    records=[]
    for level in recipe['levels']:
        source=packed(reader,Path(level['geometry']))
        tangents=np.empty((len(source[2]),4),dtype='<f4')
        decode(source[2].ctypes.data,len(source[2]),tangents.ctypes.data)
        assert np.isfinite(tangents).all()
        record=next(r for r in metadata['records'] if r['family']==0 and r['mesh']==0 and r['lod']==level['lod'])
        faces=arrays[record['arrayPrefix']+'_faces']
        panels=[]
        for panel in level['panels']:
            ids=np.unique(faces[[t['face'] for t in panel['triangles']]])
            end_sign=1 if panel['physicalMin'][1]>=0 else -1
            target_sign=end_sign if panel['bone']==0 else -end_sign
            t=tangents[ids,:3].astype(np.float64)
            t/=np.linalg.norm(t,axis=1)[:,None]
            angles=np.degrees(np.arccos(np.clip(t[:,0]*target_sign,-1,1)))
            panels.append(dict(firstFace=panel['firstFace'],normalBits=[int(n) for n in np.unique(source[1][ids])],
                tangentBits=[int(n) for n in np.unique(source[2][ids])],
                canonicalTargetTangent=[target_sign,0,0,-1],
                originalFrameAngleDegrees=dict(min=float(angles.min()),max=float(angles.max()),
                    median=float(np.median(angles)),p95=float(np.quantile(angles,.95))),
                sourceFrames=[dict(face=t['face'],tangents=tangents[faces[t['face']]].tolist()) for t in panel['triangles']]))
        records.append(dict(lod=level['lod'],panels=panels))
    (output/'native-panel-frames.json').write_text(json.dumps(dict(levels=records,
        decoder='PipelinePlugin.dll math_array_oct15_to_fp32tangents, the native MeshTangent.Unpack Float32x1 path.',
        limitation='Native decoded source frames; interpolation and final receiver frame still require validation.'),indent=2)+'\n')
    print(json.dumps([dict(lod=r['lod'],normalBits=sorted({n for p in r['panels'] for n in p['normalBits']}),
        tangentBits=sorted({n for p in r['panels'] for n in p['tangentBits']})) for r in records]))


if __name__=='__main__':main()
