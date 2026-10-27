"""Extract true Draw01 source UV coverage; no pixels are copied or rebaked."""
import hashlib
import json
import struct
import sys
from pathlib import Path
import numpy as np

root=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(root/'artifacts/agent-material-env/Draw01/python-deps'))
from shapely.geometry import Polygon, box
from shapely.ops import unary_union

out=root/'artifacts/prepared-materials/Draw01/native-charts'
out.mkdir(parents=True,exist_ok=True)
mapping=json.loads((root/'artifacts/prepared-materials/Draw01/source-sharing/sampling-map.json').read_text())
primary='97291ab7f7938fc35cc670b15f2bca36'
units=[u for u in mapping['samplers'] if u['sourceMaterial']==primary]
sources=[]
polygons=[]
for name in ('DrawBridge01 Mesh','DrawBridge01_LOD1 Mesh'):
    path=root/'artifacts/uv-density/native'/f'{name}.npz'
    native=np.load(path)
    uv=native['mesh0_uv0'][:,:2].astype(np.float64)
    faces=native['mesh0_faces']
    unique={}
    for face in faces:
        q=uv[face]
        p=Polygon(q)
        if p.area==0:continue
        key=tuple(sorted(map(tuple,q)))
        unique.setdefault(key,p)
    union=unary_union(list(unique.values()))
    parts=list(union.geoms) if union.geom_type=='MultiPolygon' else [union]
    sources.append(dict(name=name,mesh=0,npzSha256=hashlib.sha256(path.read_bytes()).hexdigest(),
        triangles=len(faces),uniqueUvTriangles=len(unique),coverageRegions=len(parts)))
    polygons.extend(unique.values())
coverage=unary_union(polygons)
parts=list(coverage.geoms) if coverage.geom_type=='MultiPolygon' else [coverage]
parts.sort(key=lambda p:p.bounds)
regions=[]
for index,p in enumerate(parts):
    bounds=p.bounds
    rectangle=p.symmetric_difference(box(*bounds)).area==0
    regions.append(dict(id=index,bounds=list(bounds),rectangle=rectangle,area=p.area,
        exterior=list(map(list,p.exterior.coords)),holes=[list(map(list,h.coords)) for h in p.interiors]))
records=[]
for u in units:
    original=u['originalSampling']
    triangle=original['triangle']
    if triangle:
        q=np.asarray([original[n] for n in ('a','b','c')],dtype=np.float32).astype(float)
        shape=Polygon(q)
    else:shape=box(*np.asarray(original['bounds'],dtype=np.float32).astype(float))
    containing=[i for i,p in enumerate(parts) if p.covers(shape)]
    # Do not bridge actual atlas gaps or round bounds to force chart membership.
    records.append(dict(key=u['key'],sourceMaterial=primary,sourceDigest=u['sourceDigest'],
        sourceTriangle=triangle,chart=containing[0] if len(containing)==1 else None,
        sourceRegionCovered=bool(containing),float32Bits=u['float32Bits'],
        originalSampling=original))
rectangles={r['chart'] for r in records if r['chart'] is not None and regions[r['chart']]['rectangle']}
# BBN1 contains region boundaries and old sampler-to-region mapping only. These
# are numerical source facts; no triangulated image or new runtime texture exists.
ints=[1,len(regions),len(records)];floats=[]
for region in regions:
    rings=[region['exterior']]+region['holes']
    ints.extend([region['id'],int(region['rectangle']),len(rings)])
    floats.extend(region['bounds'])
    for ring in rings:
        ints.extend([len(floats)//2,len(ring)])
        floats.extend(v for point in ring for v in point)
for record in records:ints.append(-1 if record['chart'] is None else record['chart'])
# Intersections of authored half-float edges can have non-Float32 coordinates.
# Retain the measured double result instead of moving the chart boundary.
raw=struct.pack('<II6i',0x314e4242,1,len(ints),0,0,0,0,len(floats))
raw+=np.asarray(ints,dtype='<i4').tobytes()+np.asarray(floats,dtype='<f8').tobytes()
raw+=hashlib.sha256(raw).digest()
(out/'Draw01NativeCharts.bin').write_bytes(raw)
result=dict(schema='Draw01/NativeUvCharts/1',sourceMaterial=primary,sources=sources,
    textureResources=[r for r in mapping['textureResources'] if r['cid'] in units[0]['textureResources']],
    regions=regions,samplers=records,
    summary=dict(samplers=len(records),coveredSamplers=sum(r['sourceRegionCovered'] for r in records),
        sourceRegions=len(regions),rectangularRegions=sum(r['rectangle'] for r in regions),
        samplersInRectangularRegions=sum(r['chart'] in rectangles for r in records),
        distinctRectanglesUsed=len(rectangles),nativeBbnBytes=len(raw),newTextures=0,
        boundaryStorage='Float64',boundaryFloat32LossyComponents=int(np.sum(np.asarray(floats)!=np.asarray(floats,dtype=np.float32)))),
    boundarySemantics='Exact union of authored source UV triangles, not a bounding-box approximation. Rectangular coverage does not by itself prove seamless pixel repetition. Nonrectangular regions retain holes and cannot be replaced with their bounding box.',
    integration='Bind unchanged native Surface. Common code may preserve affine atlas coordinates while within covered source region; finite seams need measured valid region and preserved density/normal/mip behavior. Do not use per-triangle baked files.',
    limitations='Coverage is measured from the two listed mesh0 sources. Corrected/generated UVs outside authored coverage remain explicit. No mip/filter acceptance is inferred from topology.')
(out/'charts.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result['summary'],indent=2))
