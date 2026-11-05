"""Offline complete Draw02 lane pattern and matching solid background.

New bounded authored road repeats, not claimed as original whole-lane atlas units.
The original two grating strips and middle solid band retain their physical widths.
"""
from pathlib import Path
import ctypes,hashlib,json,math,struct
import numpy as np
from PIL import Image
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw02/lane-units-v2';OUT.mkdir(exist_ok=True)
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')
SOURCE=ROOT/'artifacts/shared-material-pipeline/vt-full-source-check-corrected'
HASHES=dict(_BaseColorMap='ced2ea798ceb53decc736ab3c314aca9f91c415cddad6fd1b324a9c293fedb7a',_NormalMap='909e829672e561eea495b40ba7e70639eac965f78316f7da705e6fbdc3336b57',_MaskMap='10cd794384edf0f86f92cbdaf1f6ac67cd42779dfe31e8fe1d3292dbb7503fbf',_ControlMask='0b3a7dc94b0022b6e526299fbab6595814fbd89ca28eb78ca44c5f9b38b04b1d')
def sha(b):return hashlib.sha256(b).hexdigest()
def read(name):
 raw=(SOURCE/(HASHES[name]+'.Texture')).read_bytes();assert sha(raw)==HASHES[name]
 ver,w,h,depth,mips,fmt,dim,filt,wrap,aniso,size=struct.unpack_from('<4H6Bi',raw)
 assert (ver,w,h,depth,mips,dim,size)==(5,4096,4096,1,13,2,-1)
 assert fmt==(108 if name=='_BaseColorMap' else 109)
 return np.asarray(Image.frombytes('RGBA',(w,h),raw[18:18+w*h],'bcn',(7,'BC7')))
def linear(p,color):
 p=p.astype(np.float64)/255
 if color:p[...,:3]=np.where(p[...,:3]<=.04045,p[...,:3]/12.92,((p[...,:3]+.055)/1.055)**2.4)
 return p
def encode(p,color):
 p=np.clip(p,0,1).copy()
 if color:p[...,:3]=np.where(p[...,:3]<=.0031308,p[...,:3]*12.92,1.055*p[...,:3]**(1/2.4)-.055)
 return np.rint(p*255).astype(np.uint8)
def sample(p,u,v,color):
 x=u*4096-.5;y=v*4096-.5;ix=np.floor(x).astype(int);iy=np.floor(y).astype(int);fx=(x-ix)[None,:,None];fy=(y-iy)[:,None,None]
 return encode((linear(p[iy[:,None],ix],color)*(1-fx)+linear(p[iy[:,None],ix+1],color)*fx)*(1-fy)+(linear(p[(iy+1)[:,None],ix],color)*(1-fx)+linear(p[(iy+1)[:,None],ix+1],color)*fx)*fy,color)
def weights(old,new):
 lo=np.arange(new)*old/new;hi=np.arange(1,new+1)*old/new
 return np.maximum(0,np.minimum(hi[:,None],np.arange(old)+1)-np.maximum(lo[:,None],np.arange(old)))/(old/new)
def mipchain(p,name):
 levels=[p];normal=name=='_NormalMap';color=name=='_BaseColorMap'
 value=linear(p,color)
 if normal:
  xy=np.stack((value[:,:,0]*value[:,:,3]*2-1,value[:,:,1]*2-1),2)
  value=np.dstack((xy,np.sqrt(np.maximum(0,1-(xy*xy).sum(2)))))
  value/=np.maximum(np.linalg.norm(value,axis=2,keepdims=True),1e-20)
 h,w=p.shape[:2]
 while w>1 or h>1:
  nw,nh=max(1,w//2),max(1,h//2)
  value=np.einsum('ya,abc,xb->yxc',weights(h,nh),value,weights(w,nw),optimize=True)
  if normal:
   value/=np.maximum(np.linalg.norm(value,axis=2,keepdims=True),1e-20)
   q=np.stack((np.ones(value.shape[:2]),(value[:,:,1]+1)/2,(value[:,:,1]+1)/2,(value[:,:,0]+1)/2),2)
  else:q=value
  levels.append(encode(q,color));w,h=nw,nh
 return levels
codec=ctypes.CDLL(str(GAME/'Plugins/x86_64/PipelinePlugin.dll'))
codec.compress_calc_bound.argtypes=[ctypes.c_longlong,ctypes.c_int];codec.compress_calc_bound.restype=ctypes.c_longlong
codec.compress_data.argtypes=[ctypes.c_void_p,ctypes.c_longlong,ctypes.c_void_p,ctypes.c_longlong,ctypes.c_int,ctypes.c_int];codec.compress_data.restype=ctypes.c_longlong
codec.decompress_data.argtypes=[ctypes.c_void_p,ctypes.c_longlong,ctypes.c_void_p,ctypes.c_longlong,ctypes.c_int];codec.decompress_data.restype=ctypes.c_longlong
def write(blob,ext,folder='Shared',filename=None):
 cid=sha(('Draw02/LaneAlignedRoad/1/'+sha(blob)).encode())[:32]
 p=OUT/'Materials/Draw02'/folder/(filename or cid+ext);p.parent.mkdir(parents=True,exist_ok=True)
 if p.exists():assert p.read_bytes()==blob
 else:p.write_bytes(blob);Path(str(p)+'.cid').write_text(cid,encoding='utf-8')
 return cid,len(blob)+32
def texture(levels,color):
 raw=b''.join(p.tobytes() for p in levels);bound=codec.compress_calc_bound(len(raw),0);buf=ctypes.create_string_buffer(bound)
 size=codec.compress_data(raw,len(raw),buf,bound,0,11);assert 0<size<=bound
 restored=ctypes.create_string_buffer(len(raw));assert codec.decompress_data(buf,size,restored,len(raw),0)==len(raw) and restored.raw==raw
 h,w=levels[0].shape[:2]
 return write(struct.pack('<4H6Bi',5,w,h,1,len(levels),4 if color else 8,2,2,0,16,size)+buf.raw[:size],'.Texture')
def cidbytes(cid):return bytes(int(cid[i:i+2][::-1],16) for i in range(0,32,2))
plan=json.loads((ROOT/'artifacts/material-remap/Draw02/near-cells/native-atlas-wrap-plan.json').read_text())[0]
slab=plan['boundarySlabs'][0]
parts=[]
for interval in slab['intervals']:
 lo=max(-3.,interval['left'][1]);hi=min(0.,interval['right'][1])
 if hi>lo:parts.append(dict(low=lo,high=hi,domain=interval['domain'],u=plan['domains'][interval['domain']]['u'],family=plan['domains'][interval['domain']]['family']))
assert len(parts)==5 and [p['family'] for p in parts]==['solid','grating','solid','grating','solid']
density=max(abs(p['u'][0])*4096 for p in parts)
width=math.ceil(3*density);height=827
old=json.loads((ROOT/'artifacts/material-remap/Draw02/offline-atlas-units/producer-report-raw.json').read_text())['units'][0]
original=(ROOT/'assets/Materials/Draw02/RepeatUnits'/old['key']/'Repeat.Surface').read_bytes()
products=[]
for name,physicalWidth,w in [('LanePattern',3.,width),('LaneBackground',72/(abs(parts[2]['u'][0])*4096),72)]:
 # Texture U follows native -X, so sampled normal components keep their source basis.
 x=-3*(np.arange(w)+.5)/w if name=='LanePattern' else parts[2]['high']-(np.arange(w)+.5)/w*physicalWidth
 u=np.zeros(w)
 for p in parts:
  mask=(x>=p['low'])&(x<=p['high']);u[mask]=p['u'][0]*x[mask]+p['u'][1]
 if name=='LaneBackground':u=(2428+(np.arange(w)+.5)*72/w)/4096
 assert (u>0).all()
 v=.27392578125+(np.arange(height)+.5)/height*(.475830078125-.27392578125)
 bindings={};reports={};total=0
 for channel in HASHES:
  pixels=sample(read(channel),u,v,channel=='_BaseColorMap');levels=mipchain(pixels,channel)
  bindings[channel],size=texture(levels,channel=='_BaseColorMap');total+=size
  reports[channel]=[dict(mip=i,width=q.shape[1],height=q.shape[0],sha256=sha(q.tobytes()),alphaMean=float(q[:,:,3].mean()),uSeamMax=np.abs(q[:,0].astype(int)-q[:,-1].astype(int)).max(0).tolist(),vSeamMax=np.abs(q[0].astype(int)-q[-1].astype(int)).max(0).tolist()) for i,q in enumerate(levels)]
  if channel=='_BaseColorMap':Image.fromarray(pixels).save(OUT/(name+'-base.png'));Image.fromarray(pixels[:,:,3]).save(OUT/(name+'-alpha.png'))
 surface=original
 for channel in old['channels']:
  oldid=cidbytes(channel['cid']);assert surface.count(oldid)==1
  surface=surface.replace(oldid,cidbytes(bindings[channel['name']]))
 surfaceCid,size=write(surface,'.Surface','RepeatUnits/'+name,'Repeat.Surface');total+=size
 products.append(dict(name=name,surface=surfaceCid,bindings=bindings,width=w,height=height,physicalSize=[physicalWidth,4],nativeBytes=total,mips=reports))
report=dict(schema='Draw02/LaneAlignedRoad/1',sourceSurface='8a28ab8d9526e258efbcb81c3bbc936b',sourceTextureSha256=HASHES,referenceLane=[-3,0],visualCenter=-1.5,parts=parts,minimumDensity=density,units=products,
 operations='Same donor UV in all four channels. Bounded authored lane and same-source solid background; native -X/-Z sampling orientation. Complete area-integrated periodic texture chains; RGB linear-light; AG normals decoded/normalized/re-encoded below mip zero. Original assets unchanged.',
 acceptance='Offline authored candidate only. Native descriptor/upload, filtered seams, all-LOD consumer and game appearance pending.')
(OUT/'composition.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
print(json.dumps(dict(units=[dict(name=u['name'],size=[u['width'],u['height']],bytes=u['nativeBytes'],surface=u['surface'])for u in products])))
