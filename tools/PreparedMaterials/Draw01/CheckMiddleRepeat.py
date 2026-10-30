"""Check middle-unit Jacobians/mips and bounded composition against same-grid control."""
import ctypes as ct,json,math,struct,sys
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
tag='middle8-directional' if '--directional' in sys.argv else 'middle8'
m=json.loads((OUT/(tag+'-manifest.json')).read_text())['units'][0]
r=json.loads((OUT/(tag+'-report.json')).read_text());painted,control=r['units']
affines=[];panels=[]
for p in m['panels']:
    q=np.array(p['target']).reshape(3,2);uv=np.array(p['source']).reshape(3,2)
    a=np.linalg.solve(np.column_stack([q,np.ones(3)]),uv)
    affines.append(a);panels.append((q,a))
for g in m['guardPolygons']:
    q=np.array(g['target']).reshape(-1,2)
    for i in range(1,len(q)-1):panels.append((q[[0,i,i+1]],affines[g['adjacentPanel']]))
dll=ct.CDLL('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Plugins/x86_64/PipelinePlugin.dll')
dll.decompress_data.argtypes=[ct.c_void_p,ct.c_int64,ct.c_void_p,ct.c_int64,ct.c_int];dll.decompress_data.restype=ct.c_int64
def load(c):
    data=(OUT/(tag+'-products/Draw01/Shared')/(c['cid']+'.Texture')).read_bytes()
    size=sum(w*h*4 for w,h in zip(c['mipWidths'],c['mipHeights']));count=struct.unpack_from('<i',data,14)[0]
    if count==-1:raw=data[18:]
    else:
        buf=ct.create_string_buffer(size);assert dll.decompress_data(data[18:],count,buf,size,0)==size;raw=buf.raw
    assert len(raw)==size;return np.frombuffer(raw,dtype=np.uint8)
density=[];masks=[]
c=painted['channels'][0]
for level,(w,h) in enumerate(zip(c['mipWidths'],c['mipHeights'])):
    x=(np.arange(w,dtype=np.float32)+np.float32(.5))/np.float32(w)
    y=(np.arange(h,dtype=np.float32)+np.float32(.5))/np.float32(h)
    xx,yy=np.meshgrid(x,y);found=np.zeros((h,w),bool);mask=np.zeros_like(found)
    for q,a in panels:
        q=q.astype(np.float32);a=a.astype(np.float32)
        def edge(v0,v1):return (v1[0]-v0[0])*(yy-v0[1])-(v1[1]-v0[1])*(xx-v0[0])
        sign=1 if np.linalg.det(np.array([q[1]-q[0],q[2]-q[0]]))>0 else -1
        inside=(sign*edge(q[0],q[1])>=0)&(sign*edge(q[1],q[2])>=0)&(sign*edge(q[2],q[0])>=0)&~found
        u=a[2,0]+a[0,0]*xx+a[1,0]*yy;v=a[2,1]+a[0,1]*xx+a[1,1]*yy
        lod=math.log2(max(np.linalg.norm(a[0]*4096/w),np.linalg.norm(a[1]*4096/h)))
        footprint=.5*2**max(0,lod)/4096
        bounded=(u>=m['paintRect'][0]-footprint-1e-7)&(u<=m['paintRect'][1]+footprint+1e-7)&(v>=m['paintRect'][2]-footprint-1e-7)&(v<=m['paintRect'][3]+footprint+1e-7)
        mask[inside]=bounded[inside];found|=inside
    assert found.all(),('CPU reconstructed sampling panel gap',level,int((~found).sum()))
    masks.append(mask)
    for i,a in enumerate(affines):
        dx=a[0]*4096/w;dy=a[1]*4096/h
        density.append({'mip':level,'panel':i,'width':w,'height':h,'sourceTexelJacobian':np.column_stack([dx,dy]).tolist(),'singularValues':np.linalg.svd(np.column_stack([dx,dy]),compute_uv=False).tolist(),'sourceTexelsPerOutputX':float(np.linalg.norm(dx)),'sourceTexelsPerOutputY':float(np.linalg.norm(dy)),'sourceLod':math.log2(max(np.linalg.norm(dx),np.linalg.norm(dy)))})
assert max(max(d['sourceTexelsPerOutputX'],d['sourceTexelsPerOutputY']) for d in density if d['mip']==0)<=1+1e-12
rows=[]
for a,b in zip(painted['channels'],control['channels']):
    assert a['name']==b['name'];aa,bb=load(a),load(b);offset=0
    for level,(w,h) in enumerate(zip(a['mipWidths'],a['mipHeights'])):
        size=w*h*4;x=aa[offset:offset+size].reshape(h,w,4);y=bb[offset:offset+size].reshape(h,w,4);offset+=size
        diff=np.abs(x.astype(int)-y.astype(int));outside=diff[~masks[level]]
        rows.append({'channel':a['name'],'mip':level,'changedTexels':int(np.any(diff!=0,axis=2).sum()),'outsideChangedTexels':int(np.any(outside!=0,axis=1).sum()),'outsideMaximumByteError':int(outside.max()) if outside.size else 0,'maximumComponentDelta':diff.max(axis=(0,1)).tolist()})
result={'status':'MASK_ISOLATION_PASS' if all(x['outsideChangedTexels']==0 for x in rows) else 'MASK_ISOLATION_FAIL','sourceSampling':'Actual source Clamp / Trilinear / anisotropy16, SampleGrad; no pre-clamp UV.','contentMin':c['contentMin'],'contentMax':c['contentMax'],'baseGridNoAxisDownsampling':True,'baseGridMaximumDirectionalFootprint':max(d['singularValues'][0] for d in density if d['mip']==0),'baseGridAllDirectionsNoDownsampling':max(d['singularValues'][0] for d in density if d['mip']==0)<=1,'coarseMipInterpretation':'Jacobians are in full-resolution source texels. Compare with the actual source LOD footprint, not directly with 1. sourceLod is nominal max-column isotropic LOD; native SampleGrad anisotropic hardware selection need not equal it. Filter equivalence is not certified.','density':density,'maskRows':rows,'samplingQualityAccepted':False,'scope':'Each authored source panel/mip Jacobian and defined paint-mask isolation only; not original-vs-unit filtered equivalence, mid transition coverage or budget acceptance.'}
(OUT/(tag+'-validation.json')).write_text(json.dumps(result,indent=2));print(result['status'],len(rows),'mask rows',len(density),'panel/mip Jacobians')
