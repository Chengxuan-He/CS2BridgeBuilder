from pathlib import Path
import ctypes as C, hashlib, json
root=Path(__file__).resolve().parents[2]
sourceRoot=root/'tools/MaterialShaders'
out=root/'work/custom160';out.mkdir(exist_ok=True)
dll=C.WinDLL('d3dcompiler_47.dll');compile=dll.D3DCompile
compile.argtypes=[C.c_void_p,C.c_size_t,C.c_char_p,C.c_void_p,C.c_void_p,C.c_char_p,C.c_char_p,C.c_uint,C.c_uint,C.POINTER(C.c_void_p),C.POINTER(C.c_void_p)];compile.restype=C.c_long

def blob(p):
 if not p:return b''
 vt=C.cast(p,C.POINTER(C.POINTER(C.c_void_p))).contents
 ptr=C.WINFUNCTYPE(C.c_void_p,C.c_void_p)(vt[3])(p);n=C.WINFUNCTYPE(C.c_size_t,C.c_void_p)(vt[4])(p)
 return C.string_at(ptr,n)
def release(p):
 if p:
  vt=C.cast(p,C.POINTER(C.POINTER(C.c_void_p))).contents
  C.WINFUNCTYPE(C.c_ulong,C.c_void_p)(vt[2])(p)
shared='\n'.join((sourceRoot/name).read_text(encoding='utf-8-sig') for name in ['MaterialFunctions.hlsl','NativeInstanceSkeleton.hlsl','NativeDBuffer.hlsl'])
report=[]
for kind in ['SkeletonDecal','WorldLayeredPbr']:
 evaluation='BBEvaluateDecal(i.uv.xy,float4(1,1,1,1))' if kind=='SkeletonDecal' else 'BBEvaluateLayered(i.uv.xy,i.uv.zw,float4(1,1,1,1))'
 source=shared+'\n'+(sourceRoot/(kind+'.hlsl')).read_text(encoding='utf-8-sig')+'''
struct Input {float4 position:SV_POSITION;float4 uv:TEXCOORD0;};
struct Output {float4 color:SV_Target0;float4 normal:SV_Target1;float4 mask:SV_Target2;};
Output main(Input i) {BBMaterialSample s=EVALUATION;Output o;o.color=float4(s.albedo,s.alpha);o.normal=float4(s.normalTS,1);o.mask=float4(s.metallic,s.occlusion,0,s.smoothness);return o;}
'''.replace('EVALUATION',evaluation)
 raw=source.encode();code=C.c_void_p();error=C.c_void_p()
 try:
  hr=compile(raw,len(raw),kind.encode(),None,None,b'main',b'ps_5_0',1<<15,0,C.byref(code),C.byref(error))
  if hr<0:raise RuntimeError(blob(error).decode())
  payload=blob(code);(out/(kind+'.hlsl')).write_text(source,encoding='utf-8');(out/(kind+'.dxbc')).write_bytes(payload)
  report.append(dict(program=kind,bytes=len(payload),sourceSha256=hashlib.sha256(raw).hexdigest(),programSha256=hashlib.sha256(payload).hexdigest()))
 finally:release(code);release(error)
(out/'compile.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
