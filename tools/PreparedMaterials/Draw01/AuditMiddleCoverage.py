"""Prove eligible near/middle reuse and enumerate unresolved original middle fields."""
import ctypes as ct,hashlib,json,struct,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/native-atlas-road'
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry
decoder=NativeGeometry(Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll'))
graph=json.loads((ROOT/'artifacts/layer-decoupling/Draw01/outputs/native-archetype-audit/native-source-graph.json').read_text())
recipe=json.loads((OUT/'recipe.json').read_text())
meshes=[np.load(ROOT/'artifacts/uv-density/native'/x['sourceFile']) for x in recipe['lods']]
def attributes(lod):
    path=ROOT/'artifacts/layer-decoupling/Draw01/cache'/(graph['mainMovingLODs'][lod]['geometryCid']+'.Geometry')
    raw=path.read_bytes();version,count,flags=struct.unpack_from('<HII',raw);assert version==1 and flags==3
    hs=[struct.unpack_from('<QI14i5I',raw,10+i*88) for i in range(count)]
    offset=10+count*88+sum(h[-1]*40 for h in hs)+sum(h[16] for h in hs);result={}
    sizes=[4,2,1,1,2,2,1,1,2,2,4,4]
    for a in range(14):
        for mi,h in enumerate(hs):
            size=h[2+a];data=raw[offset:offset+size];offset+=size
            if mi!=0 or not size:continue
            fmt=(h[0]>>(a*4))&15;dim=((h[1]>>(a*2))&3)+1;stride=sizes[fmt]*dim
            capacity=decoder.dll.decompress_calc_bound(data,len(data),0);buffer=ct.create_string_buffer(capacity)
            assert decoder.dll.decompress_data(data,len(data),buffer,capacity,0)==capacity
            decoded=ct.create_string_buffer(h[18]*stride)
            assert decoder.dll.decompress_meshopt_vertex_attribute(buffer,capacity,h[18],stride,decoded)==0
            result[a]={'format':fmt,'dimension':dim,'stride':stride,'rows':[decoded.raw[i*stride:(i+1)*stride] for i in range(h[18])]}
    return result,hashlib.sha256(raw).hexdigest()
attrs=[attributes(i) for i in range(2)]
def row(lod,domain,right,positive):return next(r for r in recipe['lods'][lod]['domains'] if r['domain']==domain and r['right']==right and r['positive']==positive)
def ids(r):return sorted({f for c in r['cells'] for f in c['faces']})
def signature(lod,r):
    a=attrs[lod][0];triangles=[]
    for face in ids(r):
        vertices=[b''.join(struct.pack('<iii',k,v['format'],v['dimension'])+v['rows'][int(i)] for k,v in sorted(a.items())) for i in meshes[lod]['mesh0_faces'][face]]
        # Cyclic equivalence preserves winding; reflection is not allowed.
        triangles.append(min(tuple(vertices[i:]+vertices[:i]) for i in range(3)))
    return sorted(triangles)
proof=[];summary=[]
for r in recipe['lods'][1]['domains']:
    domain=r['domain'];right=r['right'];positive=r['positive'];n=row(0,domain,right,positive)
    mid=ids(r);near=ids(n)
    tangent=attrs[1][0][2]['rows']
    varying=[f for f in mid if len({tangent[int(i)] for i in meshes[1]['mesh0_faces'][f]})>1]
    same=signature(0,n)==signature(1,r)
    item={'domain':domain,'right':right,'positive':positive,'nearFaces':len(near),'middleFaces':len(mid),'allNativeAttributeBytesAndCyclicTriangleMultisetEqual':same,'varyingTangentFaces':varying,'constantTangentFaceCount':len(mid)-len(varying),'middleSourceFaces':mid}
    if domain=='fixed-carriageway':
        assert same
        item['reuse']='Existing near finite/prepared mapping and measured frame; no new resource.'
        item['nearSourceFaces']=near
        item['nativeAttributes']=[{'attribute':k,'format':v['format'],'dimension':v['dimension']} for k,v in attrs[1][0].items()]
        proof.append(item)
    else:summary.append(item)
result={'status':'FIXED_CARRIAGEWAY_EXACT_REUSE_PROVEN','sourceGeometrySha256':[x[1] for x in attrs],'sourceSurfaceCids':[graph['mainMovingLODs'][i]['surfaces'][0] for i in range(2)],'fixedCarriageway':proof,'remainingDomainSummary':summary,'limitations':['Near/middle grating is already consumed; this audit does not certify its filtered unit quality.','Other mid asphalt source tangents and UV field are not replaced by near mappings.','Native source triangle evidence is used only offline, never as runtime prototype-geometry Boolean input.']}
(OUT/'middle-coverage-and-fixedcap-proof.json').write_text(json.dumps(result,indent=2))
print('fixed-cap exact all-native-attribute matches',sum(x['middleFaces'] for x in proof))
print('middle asphalt varying tangent triangles',sum(len(x['varyingTangentFaces']) for x in summary if x['domain']=='asphalt'))
