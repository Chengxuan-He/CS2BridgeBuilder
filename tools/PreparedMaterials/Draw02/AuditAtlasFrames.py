"""Decode original frames with the native codec and quantify projection changes."""
import ctypes as c,hashlib,json,zipfile
from pathlib import Path
import numpy as np

ROOT=Path(__file__).resolve().parents[3];E=ROOT/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism';OUT=ROOT/'artifacts/material-remap/Draw02/near-cells'
source=(E/'audit.py').read_text().split('def analyze')[0]
source=source.replace('(0,4,13)','(0,1,2,4,13)').replace("formats={0:'<f4',1:'<f2',10:'<u4',11:'<i4'}","formats={0:'<f4',1:'<f2',5:'<i2',10:'<u4',11:'<i4'}")
source=source.replace("{0:'positions',4:'uv',13:'bones'}","{0:'positions',1:'normalPacked',2:'tangentPacked',4:'uv',13:'bones'}")
namespace={'__file__':str(E/'audit.py')};exec(source,namespace)
dll=c.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
def decode(values,name,dimensions):
    fn=getattr(dll,name);fn.argtypes=[c.c_void_p,c.c_int64,c.c_void_p];fn.restype=None
    raw=np.ascontiguousarray(values).copy().view('<u4').reshape(-1);result=np.zeros((len(raw),dimensions),np.float32)
    fn(raw.ctypes.data,len(raw),result.ctypes.data)
    return result.astype(float)
groups=[]
for level in json.loads((OUT/'appearance-domain-measurements.json').read_text()):
    for i,d in enumerate(level['domains']):groups.append(dict(lod=level['lod'],kind='moving-car',domain=i,faces=d['faces']))
for file,kind in [('walk-atlas-plan.json','moving-walk'),('fixed-walk-atlas-plan.json','fixed-walk')]:
    for i,d in enumerate(json.loads((OUT/file).read_text())):groups.append(dict(lod=d['lod'],kind=kind,domain=i,faces=d['faces']))
for i,d in enumerate(json.loads((E/'far-spans.json').read_text())['groups']):
    groups.append(dict(lod=2,kind='far',domain=i,faces=d['faces']))
results=[]
with zipfile.ZipFile('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok') as archive:
    for lod in (0,1,2):
        evidence=json.loads((E/f'lod{lod}-analysis.json').read_text());raw=archive.read(evidence['archiveEntry']);assert hashlib.sha256(raw).hexdigest()==evidence['sha256']
        m=namespace['decode'](raw)[0];p=m['positions'].astype(float);f=m['indices'];uv=m['uv'].astype(float)
        normals=decode(m['normalPacked'],'math_array_oct16_to_fp32normals',3)
        tangents=decode(m['tangentPacked'],'math_array_oct15_to_fp32tangents',4)
        for group in (g for g in groups if g['lod']==lod):
            worst=0.;hands=set();packed=set();packedNormals=set();samples=0
            for face in group['faces']:
                ids=f[face];pts=p[ids];tex=uv[ids]
                coefficients=np.linalg.solve(np.column_stack((pts[:,0],pts[:,2],np.ones(3))),tex)
                u=np.array([coefficients[0,0],0,coefficients[1,0]])
                v=np.array([coefficients[0,1],0,coefficients[1,1]])
                for i in ids:
                    direction=np.cross(v,normals[i]);det=np.dot(u,direction)
                    direction=direction/det;direction/=np.linalg.norm(direction)
                    native=tangents[i,:3];native/=np.linalg.norm(native)
                    angle=np.arctan2(np.linalg.norm(np.cross(direction,native)),np.dot(direction,native))*180/np.pi
                    worst=max(worst,float(angle));hands.add(bool(np.sign(det)==np.sign(tangents[i,3])))
                    packed.add(f"{m['tangentPacked'][i:i+1].copy().view('<u4').reshape(-1)[0]:08x}");samples+=1
                    packedNormals.add(f"{m['normalPacked'][i:i+1].copy().view('<u4').reshape(-1)[0]:08x}")
            results.append(dict(**{k:v for k,v in group.items()if k!='faces'},samples=samples,
                maximumProjectedTangentAngleDegrees=worst,handednessMatches=hands=={True},originalPackedTangents=sorted(packed),originalPackedNormals=sorted(packedNormals)))
for kind in sorted(set(r['kind']for r in results)):
    rows=[r for r in results if r['kind']==kind]
    print(kind,'max angular difference',max(r['maximumProjectedTangentAngleDegrees']for r in rows),'all handedness',all(r['handednessMatches']for r in rows))
(OUT/'native-atlas-frame-audit.json').write_text(json.dumps(results,indent=2)+'\n')
