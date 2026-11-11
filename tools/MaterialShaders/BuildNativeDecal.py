"""Offline source-driven authoring; runtime compiles the contained HLSL. No Editor dependency at runtime."""
from pathlib import Path
import json,struct,sys,ctypes as C,copy
from NativeDecalTranscription import transcribe
import argparse,hashlib
parser=argparse.ArgumentParser(description="Author current-skeleton native DBuffer programs.")
parser.add_argument('--object-shader',type=Path,required=True);parser.add_argument('--decal-shader',type=Path,required=True)
parser.add_argument('--container-seed',type=Path,required=True);parser.add_argument('--output',type=Path,required=True);parser.add_argument('--unitypy',type=Path)
args=parser.parse_args()
if args.unitypy:sys.path.insert(0,str(args.unitypy))
import UnityPy
from UnityPy.helpers import CompressionHelper
root=Path(__file__).resolve().parents[2];out=args.output.resolve();out.mkdir(parents=True,exist_ok=True)
s=json.loads(args.decal_shader.read_text());blob=bytes(s['compressedBlob'])
segments=[CompressionHelper.decompress_lz4(blob[o:o+c],n) for o,c,n in zip(s['offsets'][0],s['compressedLengths'][0],s['decompressedLengths'][0])]
records=[]
for i in range(struct.unpack_from('<i',segments[0])[0]):
 o,n,k=struct.unpack_from('<iii',segments[0],4+i*12);records.append(segments[k][o:o+n])
compiler=C.WinDLL('d3dcompiler_47.dll').D3DCompile
compiler.argtypes=[C.c_void_p,C.c_size_t,C.c_char_p,C.c_void_p,C.c_void_p,C.c_char_p,C.c_char_p,C.c_uint,C.c_uint,C.POINTER(C.c_void_p),C.POINTER(C.c_void_p)];compiler.restype=C.c_long
def data(p):
 if not p:return b''
 v=C.cast(p,C.POINTER(C.POINTER(C.c_void_p))).contents;a=C.WINFUNCTYPE(C.c_void_p,C.c_void_p)(v[3])(p);n=C.WINFUNCTYPE(C.c_size_t,C.c_void_p)(v[4])(p);result=C.string_at(a,n);C.WINFUNCTYPE(C.c_uint,C.c_void_p)(v[2])(p);return result
def replace(entry,compiled):
 k=entry.index(b'DXBC');size=struct.unpack_from('<I',entry,k+24)[0]
 candidates=[p for p in range(24,k,4) if p+4+struct.unpack_from('<I',entry,p)[0]>=k+size and p+4+struct.unpack_from('<I',entry,p)[0]<=len(entry) and struct.unpack_from('<I',entry,p)[0]>size]
 assert len(candidates)==1
 p=candidates[0];end=p+4+struct.unpack_from('<I',entry,p)[0];program=entry[p+4:k]+compiled+entry[k+size:end]
 result=entry[:p]+struct.pack('<I',len(program))+program
 return result+bytes((-len(result))%4)+entry[(end+3)&~3:]
def save(shader,entries,name):
 shader=copy.deepcopy(shader);shader['m_Dependencies']=[];shader['m_ParsedForm']['m_FallbackName']=''
 shader['m_Name']=shader['m_ParsedForm']['m_Name']='BridgeBuilder/Generated/'+name
 packed=bytearray(struct.pack('<i',len(entries)));offset=4+len(entries)*12
 for e in entries:packed+=struct.pack('<iii',offset,len(e),0);offset+=len(e)
 packed+=b''.join(entries);compressed=bytearray([240]);remaining=len(packed)-15
 while remaining>=255:compressed.append(255);remaining-=255
 compressed.append(remaining);compressed+=packed
 shader['compressedBlob']=list(compressed);shader['offsets']=[[0]];shader['compressedLengths']=[[len(compressed)]];shader['decompressedLengths']=[[len(packed)]]
 env=UnityPy.load(str(args.container_seed))
 for obj in env.objects:
  if obj.type.name=='Shader':obj.save_typetree(shader)
  elif obj.type.name=='AssetBundle':
   tree=obj.read_typetree();tree['m_Name']=name;obj.save_typetree(tree)
 internal_name='CAB-'+hashlib.sha256(json.dumps(shader,sort_keys=True,separators=(',',':')).encode()).hexdigest()[:32]
 for bundle in env.files.values():
  assert len(bundle.files)==1
  file=next(iter(bundle.files.values()));file.name=internal_name;bundle.files={internal_name:file}
 result=next(iter(env.files.values())).save(packer='none');(out/(name+'.bundle')).write_bytes(result);return result
def getrecords(shader):
 blob=bytes(shader['compressedBlob']);seg=[CompressionHelper.decompress_lz4(blob[o:o+c],n) for o,c,n in zip(shader['offsets'][0],shader['compressedLengths'][0],shader['decompressedLengths'][0])]
 result=[]
 for i in range(struct.unpack_from('<i',seg[0])[0]):
  o,n,k=struct.unpack_from('<iii',seg[0],4+i*12);result.append(seg[k][o:o+n])
 return result

def without_draw_parameters(raw):
 # The authored fragment no longer reads any UnityPerDraw value: its complete
 # projection and determinant arrive in the current-skeleton vertex varyings.
 cursor=8;blocks=[]
 def string():
  nonlocal cursor
  size=struct.unpack_from('<I',raw,cursor)[0];cursor+=4
  text=raw[cursor:cursor+size];cursor=(cursor+size+3)&~3;return text
 count=struct.unpack_from('<I',raw,4)[0]
 for _ in range(count):
  start=cursor;name=string();size,fields=struct.unpack_from('<II',raw,cursor);cursor+=8
  for _ in range(fields):string();cursor+=24
  structs=struct.unpack_from('<I',raw,cursor)[0];cursor+=4
  assert structs==0,('Unsupported parameter struct',name)
  if name!=b'UnityPerDraw':blocks.append(raw[start:cursor])
 bindings=bytearray(raw[cursor:]);needle=struct.pack('<I',12)+b'UnityPerDraw'
 pos=bindings.find(needle);assert pos>=4 and bindings.find(needle,pos+1)<0
 assert struct.unpack_from('<I',bindings,pos+len(needle))[0]==1
 del bindings[pos:pos+len(needle)+12]
 struct.pack_into('<I',bindings,0,struct.unpack_from('<I',bindings)[0]-1)
 assert len(blocks)==count-1
 return raw[:4]+struct.pack('<I',len(blocks))+b''.join(blocks)+bindings
def compilecode(source,entry,profile):
 raw=source.encode();p=C.c_void_p();e=C.c_void_p();hr=compiler(raw,len(raw),b'CurrentDecal',None,None,entry.encode(),profile.encode(),1<<15,0,C.byref(p),C.byref(e));error=data(e);assert hr>=0,error.decode();code=data(p);dynamic.setdefault(hashlib.sha256(code).hexdigest(),(source,entry,profile,code));return code
base=json.loads(args.object_shader.read_text());br=getrecords(base);modified=list(records);dynamic={}
bp=next(p for p in base['m_ParsedForm']['m_SubShaders'][0]['m_Passes'] if p['m_State']['m_Name']=='GBuffer')
choice=None
for platform,group in enumerate(bp['progVertex']['m_PlayerSubPrograms']):
 for i,v in enumerate(group):
  keys=[base['m_ParsedForm']['m_KeywordNames'][k]for k in v['m_KeywordIndices']]
  if 'DOTS_INSTANCING_ON'not in keys and '_GPU_ANIMATION_PROCEDURAL'in keys:
   if choice is None or len(keys)<choice[0]:choice=(len(keys),platform,i,v)
_,platform,i,v=choice
parameter=bytearray(br[bp['progVertex']['m_ParameterBlobIndices'][platform][i]])
bone=b'colossal_BoneParameters';pos=parameter.index(bone);field=(pos+len(bone)+3)&~3
assert struct.unpack_from('<I',parameter,field+20)[0]==392
struct.pack_into('<I',parameter,field+20,256)
parameter_index=len(modified);modified.append(bytes(parameter))
vertex_index=len(modified);modified.append(replace(br[v['m_BlobIndex']],compilecode('#define BB_NON_DOTS 1\n'+(root/'tools/MaterialShaders/NativeDecalVertex.hlsl').read_text(),'BBDecalVertex','vs_5_0')))
target=copy.deepcopy(s)
bone_property=next(p for p in base['m_ParsedForm']['m_PropInfo']['m_Props'] if p['m_Name']=='colossal_BoneParameters')
target['m_ParsedForm']['m_PropInfo']['m_Props'].append(copy.deepcopy(bone_property))
for p in target['m_ParsedForm']['m_SubShaders'][0]['m_Passes']:
 if p['m_State']['m_Name']!='DBufferMesh':continue
 for platform,group in enumerate(p['progVertex']['m_PlayerSubPrograms']):
  for i,v in enumerate(group):
   keys=[target['m_ParsedForm']['m_KeywordNames'][k]for k in v['m_KeywordIndices']]
   if 'DOTS_INSTANCING_ON'not in keys:v['m_BlobIndex']=vertex_index;p['progVertex']['m_ParameterBlobIndices'][platform][i]=parameter_index
 names={n:i for n,i in p['m_NameIndices']}
 for stage in ['progVertex','progFragment']:
  for cb in p[stage]['m_CommonParameters']['m_ConstantBuffers']:
   if cb['m_NameIndex']==names.get('UnityPerMaterial'):cb['m_Size']=272
for index in [42,43,44,45]:
 record=records[index];start=record.index(b'DXBC');code=record[start:start+struct.unpack_from('<I',record,start+24)[0]]
 disassembler=C.WinDLL('d3dcompiler_47.dll').D3DDisassemble;disassembler.argtypes=[C.c_void_p,C.c_size_t,C.c_uint,C.c_char_p,C.POINTER(C.c_void_p)]
 result=C.c_void_p();assert disassembler(code,len(code),0,None,C.byref(result))>=0
 source=transcribe(data(result).decode().rstrip('\0'),current_projection=True,per_volume_area=True)
 modified[index]=replace(records[index],compilecode(source,'BBFragment','ps_5_0'))
# Retain each native DOTS program header/keyword identity while substituting
# the same per-volume fragment computation used by its non-DOTS counterpart.
for dots,plain in {40:44,41:45,46:42,47:43}.items():
 start=modified[plain].index(b'DXBC');size=struct.unpack_from('<I',modified[plain],start+24)[0]
 modified[dots]=replace(records[dots],modified[plain][start:start+size])
# Native DOTS current-bone variant, sharing source material values.
choice=None
for platform,group in enumerate(bp['progVertex']['m_PlayerSubPrograms']):
 for i,v in enumerate(group):
  keys=[base['m_ParsedForm']['m_KeywordNames'][k] for k in v['m_KeywordIndices']]
  if 'DOTS_INSTANCING_ON' in keys and '_GPU_ANIMATION_PROCEDURAL'in keys:
   if choice is None or len(keys)<choice[0]:choice=(len(keys),platform,i,v)
_,platform,i,v=choice
parameter_data=br[bp['progVertex']['m_ParameterBlobIndices'][platform][i]]
# Unity's native BRG recognizes the canonical metadata buffer name. The paired
# fragment program uses per-volume varyings and has no competing DOTS metadata.
parameter_index=len(modified);modified.append(parameter_data)
source='#define BB_GLOBAL_REGISTER b0\n#define BB_VISIBLE_REGISTER b1\n#define BB_BUILTIN_REGISTER b2\n#define BB_MATERIAL_VALUE_REGISTER b3\n#define BB_MATERIAL_REGISTER b4\n'
source+='\n'.join((root/'tools/MaterialShaders'/n).read_text() for n in ['MaterialFunctions.hlsl','NativeInstanceSkeleton.hlsl','NativeDecalVertex.hlsl'])
program_index=len(modified);modified.append(replace(br[v['m_BlobIndex']],compilecode(source,'BBDecalVertex','vs_5_0')))
for p in target['m_ParsedForm']['m_SubShaders'][0]['m_Passes']:
 if p['m_State']['m_Name']!='DBufferMesh':continue
 original_common=copy.deepcopy(p['progVertex']['m_CommonParameters']);common=copy.deepcopy(bp['progVertex']['m_CommonParameters'])
 source_names={i:n for n,i in bp['m_NameIndices']};names={n:i for n,i in p['m_NameIndices']}
 def remap(obj):
  if isinstance(obj,dict):
   for key,value in list(obj.items()):
    if key=='m_NameIndex':
     name=source_names[value]
     if name not in names:names[name]=max(names.values())+1
     obj[key]=names[name]
    else:remap(value)
  elif isinstance(obj,list):
   for value in obj:remap(value)
 remap(common)
 common['m_ConstantBuffers']=[cb for cb in common['m_ConstantBuffers'] if cb['m_NameIndex']!=names.get('UnityPerMaterial')]
 for cb in original_common['m_ConstantBuffers']:
  if not any(c['m_NameIndex']==cb['m_NameIndex'] for c in common['m_ConstantBuffers']):common['m_ConstantBuffers'].append(cb)
 p['m_NameIndices']=[[n,i] for n,i in sorted(names.items())];p['progVertex']['m_CommonParameters']=common
 for platform,group in enumerate(p['progVertex']['m_PlayerSubPrograms']):
  for i,v in enumerate(group):
   keys=[target['m_ParsedForm']['m_KeywordNames'][k]for k in v['m_KeywordIndices']]
   if 'DOTS_INSTANCING_ON'in keys:v['m_BlobIndex']=program_index;p['progVertex']['m_ParameterBlobIndices'][platform][i]=parameter_index
 for platform,group in enumerate(p['progFragment']['m_PlayerSubPrograms']):
  parameters={v['m_BlobIndex']:p['progFragment']['m_ParameterBlobIndices'][platform][i] for i,v in enumerate(group)}
  dots_parameters={}
  for i,v in enumerate(group):
   mapping={40:44,41:45,46:42,47:43}
   if v['m_BlobIndex']in mapping:
    source_parameter=parameters[mapping[v['m_BlobIndex']]]
    if source_parameter not in dots_parameters:
     # Current projection comes entirely from vertex varyings. Retaining the
     # unused native UnityPerDraw binding makes BRG reject a DOTS fragment.
     raw=without_draw_parameters(modified[source_parameter])
     dots_parameters[source_parameter]=len(modified);modified.append(raw)
    p['progFragment']['m_ParameterBlobIndices'][platform][i]=dots_parameters[source_parameter]
needle=struct.pack('<I',16)+b'UnityPerMaterial'
for sub in target['m_ParsedForm']['m_SubShaders']:sub['m_Passes']=[p for p in sub['m_Passes'] if p['m_State']['m_Name']=='DBufferMesh']
parameter_indices={index for sub in target['m_ParsedForm']['m_SubShaders'] for p in sub['m_Passes']
 for stage in ['progVertex','progFragment'] for group in p[stage]['m_ParameterBlobIndices'] for index in group}
for i in parameter_indices:
 e=modified[i]
 e=bytearray(e);cursor=0
 while (pos:=e.find(needle,cursor))>=0:
  field=pos+len(needle);size=struct.unpack_from('<I',e,field)[0]
  # The later binding entry repeats the buffer name followed by binding kind 1,
  # not its byte size. Only the declaration carries the material block size.
  assert size in (1,208,224,240,256,272,624),('Unexpected source material layout',i,size)
  if size!=1:struct.pack_into('<I',e,field,272)
  cursor=field+4
 modified[i]=bytes(e)
bundle=save(target,modified,'BoneDecal')
package=bytearray(b'BBS1')+struct.pack('<II',2,len(bundle))+bundle+struct.pack('<I',len(dynamic))
for source,entry,profile,code in dynamic.values():
 for text in [source,entry,profile]:
  encoded=text.encode();package+=struct.pack('<I',len(encoded))+encoded
 positions=[];cursor=0
 while(pos:=bundle.find(code,cursor))>=0:positions.append(pos);cursor=pos+len(code)
 assert positions
 package+=struct.pack('<I',len(code))+code+struct.pack('<I',len(positions))+struct.pack('<'+'I'*len(positions),*positions)
(out/'BoneDecal.bbs1').write_bytes(package)
inputs=[Path(__file__),root/'tools/MaterialShaders/NativeDecalTranscription.py',root/'tools/MaterialShaders/NativeDecalVertex.hlsl',root/'tools/MaterialShaders/MaterialFunctions.hlsl',root/'tools/MaterialShaders/NativeInstanceSkeleton.hlsl']
manifest={'schema':1,'program':'BoneDecal','producer':'BuildNativeDecal.py','producerSha256':hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),'packageBytes':len(package),'packageSha256':hashlib.sha256(package).hexdigest(),'sources':{str(p.relative_to(root)).replace('\\','/'):hashlib.sha256(p.read_bytes()).hexdigest() for p in inputs},'nativeInputs':{k:hashlib.sha256(p.read_bytes()).hexdigest() for k,p in [('objectShader',args.object_shader),('decalShader',args.decal_shader),('containerSeed',args.container_seed)]}}
(out/'BoneDecal.manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
print('PASS current-bone vertex and four original fragment variants compile')
