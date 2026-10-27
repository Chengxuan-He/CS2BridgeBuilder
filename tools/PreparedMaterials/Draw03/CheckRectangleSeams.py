"""Check unchanged prototype faces and bounded seam interpolation, not shader equivalence."""
from pathlib import Path
import json
import struct
import hashlib
import numpy as np
from RectangleSeams import partition

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/prepared-materials/Draw03/native-charts'
manifest=json.loads((OUT/'manifest.json').read_bytes())
checked=fragments=0
max_uv=max_bary_area=0.
representative=None
representative_area=0.
for mesh in manifest['meshes']:
    blob=(OUT/mesh['file']).read_bytes()
    assert hashlib.sha256(blob).hexdigest()==mesh['sha256']
    ni,_,_,_,nf,_=struct.unpack_from('<6i',blob,8)
    ints=np.frombuffer(blob,'<i4',ni,32)
    fs=np.frombuffer(blob,'<f4',nf,32+4*ni).reshape(-1,4)
    count,charts=ints[1:3]
    fc=ints[3:3+count]
    flags=[];at=3+count
    for ci in range(charts):
        flags.append(int(ints[at]));at+=2+int(ints[at+1])*2
    source=ROOT/'artifacts/uv-density/native'/(mesh['render']+'.npz')
    if not source.exists():source=ROOT/'artifacts/uv-density/native-additional'/(mesh['render']+'.npz')
    data=np.load(source);prefix=f"mesh{mesh['mesh']}_"
    faces=data[prefix+'faces'];uv=data[prefix+'uv0'][:,:2]
    for face,ci in zip(faces,fc):
        if not flags[ci]&1:continue
        original=uv[face].astype(float)
        pieces=partition(original,fs[ci],1)
        if not pieces:continue # UV-degenerate authored triangles are separate.
        assert len(pieces)==1
        max_uv=max(max_uv,float(abs(pieces[0]['sourceUv']-pieces[0]['barycentric']@original).max()))
        checked+=1;fragments+=len(pieces)
        bounds=fs[ci].astype(float)
        q=(original-bounds[:2])/(bounds[2:]-bounds[:2])
        e,f=q[1]-q[0],q[2]-q[0]
        normalized_area=abs(e[0]*f[1]-e[1]*f[0])
        if normalized_area>representative_area:
            representative_area=normalized_area
            representative=(original,bounds)

# A measured native face supplies the shape/atlas region. Translation here is a
# seam-algorithm fixture, not a claim that a production width plan was evaluated.
original,bounds=representative
span=bounds[2:]-bounds[:2]
seam_cases=[]
for offset in ((.5,.25),(-.5,-.25),(1.,0.),(-1.,0.)):
    projected=original+np.asarray(offset)*span
    pieces=partition(projected,bounds,4)
    area=0.
    for piece in pieces:
        b=piece['barycentric']
        area+=abs(float(np.linalg.det(b)))
        expected=b@projected-np.asarray(piece['tile'])*span
        max_uv=max(max_uv,float(abs(expected-piece['sourceUv']).max()))
        assert np.all(b>=-1e-14) and np.all(b<=1+1e-14)
        assert np.all(piece['sourceUv']>=bounds[:2]-1e-14)
        assert np.all(piece['sourceUv']<=bounds[2:]+1e-14)
    max_bary_area=max(max_bary_area,abs(area-1))
    seam_cases.append({'normalizedOffset':offset,'fragments':len(pieces),'barycentricArea':area})
assert max_uv<1e-12 and max_bary_area<1e-12
assert any(c['fragments']>1 for c in seam_cases)
report={'prototypeFacesChecked':checked,'prototypeFragments':fragments,'boundedSeamCases':seam_cases,
        'maximumSourceUvArithmeticError':max_uv,'maximumBarycentricAreaError':max_bary_area,
        'textureWrites':0,'scope':'Geometry/UV arithmetic only; not actual width-plan, native filtering, normal, mip or game acceptance.',
        'inputs':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in (Path(__file__),Path(__file__).with_name('RectangleSeams.py'),OUT/'manifest.json')}}
(OUT/'seam-verification.json').write_text(json.dumps(report,indent=2),encoding='utf-8')
print(json.dumps(report,indent=2))
