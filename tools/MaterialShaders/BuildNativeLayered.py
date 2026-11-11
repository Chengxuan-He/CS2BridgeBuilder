"""Author native layered shading with generated current-skeleton vertex adapters.

Inputs are exact extracted native Shader records and a compatible Unity container.
This authoring tool is not a runtime dependency or a game launcher.
"""
from pathlib import Path
import sys, json, struct, ctypes as C, copy, hashlib, re, argparse
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--object-shader', type=Path, required=True)
parser.add_argument('--layered-shader', type=Path, required=True)
parser.add_argument('--container-seed', type=Path, required=True)
parser.add_argument('--output', type=Path, required=True)
parser.add_argument('--unitypy', type=Path)
parser.add_argument('--clip-probe', action='store_true')
args = parser.parse_args()
root = Path(__file__).resolve().parents[2]; out = args.output.resolve()
out.mkdir(parents=True, exist_ok=True)
if args.unitypy: sys.path.insert(0, str(args.unitypy))
import UnityPy
from UnityPy.helpers import CompressionHelper
dll = C.WinDLL('d3dcompiler_47.dll')
compile = dll.D3DCompile
compile.argtypes = [C.c_void_p,C.c_size_t,C.c_char_p,C.c_void_p,C.c_void_p,C.c_char_p,C.c_char_p,C.c_uint,C.c_uint,C.POINTER(C.c_void_p),C.POINTER(C.c_void_p)]
compile.restype = C.c_long
disassemble = dll.D3DDisassemble
disassemble.argtypes = [C.c_void_p,C.c_size_t,C.c_uint,C.c_char_p,C.POINTER(C.c_void_p)]
def data(pointer):
    v = C.cast(pointer,C.POINTER(C.POINTER(C.c_void_p))).contents
    address = C.WINFUNCTYPE(C.c_void_p,C.c_void_p)(v[3])(pointer)
    length = C.WINFUNCTYPE(C.c_size_t,C.c_void_p)(v[4])(pointer)
    result = C.string_at(address,length)
    C.WINFUNCTYPE(C.c_uint,C.c_void_p)(v[2])(pointer)
    return result
def records(shader):
    blob = bytes(shader['compressedBlob'])
    segments = [CompressionHelper.decompress_lz4(blob[o:o+c],n) for o,c,n in zip(shader['offsets'][0],shader['compressedLengths'][0],shader['decompressedLengths'][0])]
    result = []
    for i in range(struct.unpack_from('<i',segments[0])[0]):
        o,n,s = struct.unpack_from('<iii',segments[0],4+i*12)
        result.append(segments[s][o:o+n])
    return result
def rename(data,old,new,optional=False):
    old=old.encode(); new=new.encode(); needle=struct.pack('<i',len(old))+old; count=0
    while (start:=data.find(needle))>=0:
        end=(start+4+len(old)+3)&~3
        data=data[:start]+struct.pack('<i',len(new))+new+bytes((-(start+4+len(new)))%4)+data[end:]
        count+=1
    assert count==2 or (optional and count==0),count
    return data
def replace_program(entry,compiled):
    k=entry.index(b'DXBC'); size=struct.unpack_from('<I',entry,k+24)[0]
    candidates=[pos for pos in range(24,k,4) if pos+4<=k and pos+4+struct.unpack_from('<I',entry,pos)[0]>=k+size and pos+4+struct.unpack_from('<I',entry,pos)[0]<=len(entry) and struct.unpack_from('<I',entry,pos)[0]>size]
    assert len(candidates)==1,candidates
    pos=candidates[0]; end=pos+4+struct.unpack_from('<I',entry,pos)[0]
    program=entry[pos+4:k]+compiled+entry[k+size:end]
    result=entry[:pos]+struct.pack('<I',len(program))+program
    return result+bytes((-len(result))%4)+entry[(end+3)&~3:]
base=json.loads(args.object_shader.read_text()); target=json.loads(args.layered_shader.read_text())
bone_property=next(p for p in base['m_ParsedForm']['m_PropInfo']['m_Props'] if p['m_Name']=='colossal_BoneParameters')
if not any(p['m_Name']=='colossal_BoneParameters' for p in target['m_ParsedForm']['m_PropInfo']['m_Props']):
    target['m_ParsedForm']['m_PropInfo']['m_Props'].append(copy.deepcopy(bone_property))
br=records(base); tr=records(target)
dynamic_programs={}
def remember(source,code):
    dynamic_programs.setdefault(hashlib.sha256(code).hexdigest(),(source,code))
    return code
shared='\n'.join((root/'tools/MaterialShaders'/name).read_text(encoding='utf-8-sig') for name in ['MaterialFunctions.hlsl','NativeInstanceSkeleton.hlsl','NativeNetworkVertex.hlsl'])
basepasses={p['m_State']['m_Name']:p for sub in base['m_ParsedForm']['m_SubShaders'] for p in sub['m_Passes']}
report=[]
for sub in target['m_ParsedForm']['m_SubShaders']:
    for tp in sub['m_Passes']:
        name=tp['m_State']['m_Name']; bp=basepasses[name]
        choices=[]
        for platform,array in enumerate(bp['progVertex']['m_PlayerSubPrograms']):
            for i,v in enumerate(array):
                keys=[base['m_ParsedForm']['m_KeywordNames'][k] for k in v['m_KeywordIndices']]
                if 'DOTS_INSTANCING_ON' in keys and '_GPU_ANIMATION_PROCEDURAL' in keys:
                    choices.append((len(keys),platform,i,v))
        _,platform,i,v=min(choices,key=lambda x:x[0])
        entry=br[v['m_BlobIndex']]; k=entry.index(b'DXBC'); code=entry[k:k+struct.unpack_from('<I',entry,k+24)[0]]
        pointer=C.c_void_p(); assert disassemble(code,len(code),0,None,C.byref(pointer))>=0
        asm=data(pointer).decode(); (out/f'567-{name}-progVertex.asm').write_text(asm)
        global_register=re.search(r'cb(\d+)\[44\]',asm)[1]
        builtin_register=re.search(r'cb(\d+)\[0\]\.xxyy',asm)[1]
        material_register=re.search(r'cb(\d+)\[2\]\.w',asm)[1]
        visible_register=re.search(r'cb(\d+)\[r\d+\.[xyzw] \+ 0\]',asm)[1]
        source='#define BB_PACKED_FRAME 1\n'+''.join(f'#define {key} b{value}\n' for key,value in [('BB_GLOBAL_REGISTER',global_register),('BB_VISIBLE_REGISTER',visible_register),('BB_BUILTIN_REGISTER',builtin_register),('BB_MATERIAL_REGISTER',material_register)])
        if name not in ['GBuffer','Forward']: source+='#define BB_COMPACT_VARYINGS 1\n'
        if name=='MotionVectors': source+='#define BB_MOTION_PASS 1\n'
        source+=shared; raw=source.encode(); code=C.c_void_p(); error=C.c_void_p()
        hr=compile(raw,len(raw),name.encode(),None,None,b'BBNetworkVertex',b'vs_5_0',1<<15,0,C.byref(code),C.byref(error))
        diagnostic=data(error).decode() if error else ''
        assert hr>=0,diagnostic
        compiled=remember(source,data(code)); (out/f'generated-{name}.hlsl').write_text(source); (out/f'generated-{name}.dxbc').write_bytes(compiled)
        parameter=bp['progVertex']['m_ParameterBlobIndices'][platform][i]
        parameter_data=rename(br[parameter],'UnityDOTSInstancing_MaterialPropertyMetadata','UnityDOTSInstancing_UserPropertyMetadata')
        parameter_index=len(tr); tr.append(parameter_data)
        program_index=len(tr); tr.append(replace_program(entry,compiled))
        original_common=copy.deepcopy(tp['progVertex']['m_CommonParameters'])
        common=copy.deepcopy(bp['progVertex']['m_CommonParameters'])
        source_names={i:n for n,i in bp['m_NameIndices']}; target_names={n:i for n,i in tp['m_NameIndices']}
        def remap(obj):
            if isinstance(obj,dict):
                for key,value in list(obj.items()):
                    if key=='m_NameIndex':
                        text=source_names[value]
                        if text not in target_names: target_names[text]=max(target_names.values())+1
                        obj[key]=target_names[text]
                    else: remap(value)
            elif isinstance(obj,list):
                for item in obj: remap(item)
        remap(common)
        common['m_ConstantBuffers']=[cb for cb in common['m_ConstantBuffers'] if cb['m_NameIndex']!=target_names.get('UnityPerMaterial')]
        # Preserve the original network material's full-size partial CB declaration;
        # native pixel stages and untouched preview variants still consume it.
        for cb in original_common['m_ConstantBuffers']:
            if not any(c['m_NameIndex']==cb['m_NameIndex'] for c in common['m_ConstantBuffers']):
                common['m_ConstantBuffers'].append(cb)
        tp['m_NameIndices']=[[n,i] for n,i in sorted(target_names.items())]; tp['progVertex']['m_CommonParameters']=common
        changed=0
        for platform,array in enumerate(tp['progVertex']['m_PlayerSubPrograms']):
            for i,v in enumerate(array):
                keys=[target['m_ParsedForm']['m_KeywordNames'][k] for k in v['m_KeywordIndices']]
                if 'DOTS_INSTANCING_ON' not in keys: continue
                # Native network vertex programs decode the packed mesh in every
                # keyword variant. The keyword is not a vertex-layout switch.
                v['m_BlobIndex']=program_index
                tp['progVertex']['m_ParameterBlobIndices'][platform][i]=parameter_index; changed+=1
        report.append({'pass':name,'variants':changed,'bytes':len(compiled),'sha256':hashlib.sha256(compiled).hexdigest(),'registers':[global_register,visible_register,builtin_register,material_register]})
# Isolated preview GBuffer probe: use the native object's non-DOTS draw/bone
# parameters without overwriting the source network pixel material layout.
for sub in target['m_ParsedForm']['m_SubShaders']:
    for tp in sub['m_Passes']:
        name=tp['m_State']['m_Name'];bp=basepasses[name];choices=[]
        for platform,array in enumerate(bp['progVertex']['m_PlayerSubPrograms']):
            for i,v in enumerate(array):
                keys=[base['m_ParsedForm']['m_KeywordNames'][k] for k in v['m_KeywordIndices']]
                if 'DOTS_INSTANCING_ON' not in keys and '_GPU_ANIMATION_PROCEDURAL' in keys:
                    choices.append((len(keys),platform,i,v))
        _,platform,i,v=min(choices,key=lambda x:x[0])
        source='#define BB_NON_DOTS 1\n#define BB_PACKED_FRAME 1\n'
        if name not in ['GBuffer','Forward']:source+='#define BB_COMPACT_VARYINGS 1\n'
        if name=='MotionVectors':source+='\n'.join(['#define BB_MOTION_PASS 1','#define BB_GLOBAL_REGISTER b1','#define BB_DRAW_REGISTER b2','#define BB_MATERIAL_VALUE_REGISTER b3',''])
        source+=shared
        if '--clip-probe' in sys.argv:
            source=source.replace('output.position = bbGlobals[20] * worldPosition.x + bbGlobals[21] * worldPosition.y +\n        bbGlobals[22] * worldPosition.z + bbGlobals[23];','output.position = float4(input.position, 1);')
        raw=source.encode(); code=C.c_void_p(); error=C.c_void_p()
        hr=compile(raw,len(raw),name.encode(),None,None,b'BBNetworkVertex',b'vs_5_0',1<<15,0,C.byref(code),C.byref(error))
        diagnostic=data(error).decode() if error else ''; assert hr>=0,diagnostic
        compiled=remember(source,data(code))
        parameter_data=br[bp['progVertex']['m_ParameterBlobIndices'][platform][i]]
        bone_name=b'colossal_BoneParameters'; bone_start=parameter_data.index(bone_name)
        bone_fields=(bone_start+len(bone_name)+3)&~3
        assert struct.unpack_from('<I',parameter_data,bone_fields+20)[0]==392
        parameter_data=bytearray(parameter_data);struct.pack_into('<I',parameter_data,bone_fields+20,1040)
        parameter_index=len(tr); tr.append(bytes(parameter_data))
        program_index=len(tr); tr.append(replace_program(br[v['m_BlobIndex']],compiled))
        changed=0
        for platform,array in enumerate(tp['progVertex']['m_PlayerSubPrograms']):
            for i,v in enumerate(array):
                keys=[target['m_ParsedForm']['m_KeywordNames'][k] for k in v['m_KeywordIndices']]
                if 'DOTS_INSTANCING_ON' in keys: continue
                v['m_BlobIndex']=program_index
                tp['progVertex']['m_ParameterBlobIndices'][platform][i]=parameter_index; changed+=1
        report.append({'pass':'Preview'+name,'variants':changed,'bytes':len(compiled),'status':'appended native material bone parameter requires actual draw verification'})
tp=next(p for sub in target['m_ParsedForm']['m_SubShaders'] for p in sub['m_Passes'] if p['m_State']['m_Name']=='GBuffer')
# Keep the original pixel offsets unchanged. Extend the one shared material block
# with the new bone parameter; no competing private/partial CB name is introduced.
for sub in target['m_ParsedForm']['m_SubShaders']:
    for p in sub['m_Passes']:
        names={n:i for n,i in p['m_NameIndices']}
        for stage in ['progVertex','progFragment']:
            for cb in p[stage]['m_CommonParameters']['m_ConstantBuffers']:
                if cb['m_NameIndex']==names.get('UnityPerMaterial'):cb['m_Size']=1056
needle=struct.pack('<I',16)+b'UnityPerMaterial'
parameter_indices={index for sub in target['m_ParsedForm']['m_SubShaders'] for p in sub['m_Passes']
    for stage in ['progVertex','progFragment'] for group in p[stage]['m_ParameterBlobIndices'] for index in group}
for index in parameter_indices:
    entry=tr[index]
    entry=bytearray(entry); cursor=0
    while (pos:=entry.find(needle,cursor))>=0:
        field=pos+len(needle);size=struct.unpack_from('<I',entry,field)[0]
        # A later binding entry repeats this name with binding kind 1 rather
        # than a declaration size; its register binding must remain unchanged.
        assert size in (1,624,1040,1056),('Unexpected source material layout',index,size)
        if size!=1:struct.pack_into('<I',entry,field,1056)
        cursor=field+4
    tr[index]=bytes(entry)
target['m_Name']=target['m_ParsedForm']['m_Name']='BridgeBuilder/Generated/LayeredAllDotsIntegration'
# The extracted fallback pointer addresses the source assets file, not this owner
# container. Unsupported APIs fail registration explicitly; do not retain a dangling
# dependency or silently render a fallback-error shader as a successful material.
assert target['m_ParsedForm']['m_FallbackName']=='Hidden/Shader Graph/FallbackError'
assert not target['m_NonModifiableTextures']
target['m_Dependencies']=[]
target['m_ParsedForm']['m_FallbackName']=''
packed=bytearray(struct.pack('<i',len(tr))); pos=4+12*len(tr)
for entry in tr: packed+=struct.pack('<iii',pos,len(entry),0); pos+=len(entry)
packed+=b''.join(tr); compressed=CompressionHelper.compress_lz4(bytes(packed))
target['compressedBlob']=list(compressed); target['offsets']=[[0]]; target['compressedLengths']=[[len(compressed)]]; target['decompressedLengths']=[[len(packed)]]
env=UnityPy.load(str(args.container_seed))
def identify_container():
    # Unity keys loaded serialized files by their internal CAB name, not the
    # public AssetBundle name. Distinct generated programs must coexist.
    name='CAB-'+hashlib.sha256(json.dumps(target,sort_keys=True,separators=(',',':')).encode()).hexdigest()[:32]
    for bundle in env.files.values():
        assert len(bundle.files)==1
        file=next(iter(bundle.files.values()));file.name=name;bundle.files={name:file}
for obj in env.objects:
    if obj.type.name=='Shader': obj.save_typetree(target)
identify_container()
for bundle in env.files.values(): (out/'native-layered-all-dots.bundle').write_bytes(bundle.save(packer='none'))
(out/'native-layered-all-dots.json').write_text(json.dumps(report,indent=2)); print(json.dumps(report,indent=2))
# A literal LZ4 segment and uncompressed bundle permit equal-size runtime program
# replacement without a Unity Editor compiler or a private compression dependency.
literal=bytearray([240]);remaining=len(packed)-15
while remaining>=255:literal.append(255);remaining-=255
literal.append(remaining);literal+=packed
target['compressedBlob']=list(literal);target['compressedLengths']=[[len(literal)]]
for obj in env.objects:
    if obj.type.name=='Shader':obj.save_typetree(target)
identify_container()
bundle_bytes=next(iter(env.files.values())).save(packer='none')
package=bytearray(b'BBS1')+struct.pack('<II',1,len(bundle_bytes))+bundle_bytes+struct.pack('<I',len(dynamic_programs))
for source,program in dynamic_programs.values():
    encoded=source.encode();positions=[];cursor=0
    while (pos:=bundle_bytes.find(program,cursor))>=0:positions.append(pos);cursor=pos+len(program)
    assert positions,hashlib.sha256(program).hexdigest()
    package+=struct.pack('<I',len(encoded))+encoded+struct.pack('<I',len(program))+program+struct.pack('<I',len(positions))+struct.pack('<'+'I'*len(positions),*positions)
(out/'native-layered-dynamic.seed').write_bytes(package)
manifest = {
    'schema': 1, 'program': 'WorldLayeredPbr',
    'packageSha256': hashlib.sha256(package).hexdigest(),
    'packageBytes': len(package),
    'producerSha256': hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),
    'sources': {str(path.relative_to(root)).replace('\\','/'): hashlib.sha256(path.read_bytes()).hexdigest()
                for path in [root/'tools/MaterialShaders'/name for name in
                             ['MaterialFunctions.hlsl','NativeInstanceSkeleton.hlsl','NativeNetworkVertex.hlsl']]},
    'nativeInputs': {label: hashlib.sha256(path.read_bytes()).hexdigest() for label,path in
                     [('objectShader',args.object_shader),('layeredShader',args.layered_shader),('containerSeed',args.container_seed)]}
}
(out/'WorldLayeredPbr.manifest.json').write_text(json.dumps(manifest,indent=2)+'\n',encoding='utf-8')
# Diagnostic-only fragment makes vertex/bone binding independently observable.
# It is never used by the native-pixel-program integration bundle above.
raw=b'float4 Main() : SV_Target0 { return float4(1,0.2,0.3,1); }'
code=C.c_void_p(); error=C.c_void_p()
assert compile(raw,len(raw),b'VertexBindingProbe',None,None,b'Main',b'ps_5_0',1<<15,0,C.byref(code),C.byref(error))>=0
compiled=data(code)
fp=tp['progFragment']; first=next(v for array in fp['m_PlayerSubPrograms'] for v in array)
index=len(tr); tr.append(replace_program(tr[first['m_BlobIndex']],compiled))
for array in fp['m_PlayerSubPrograms']:
    for v in array: v['m_BlobIndex']=index
packed=bytearray(struct.pack('<i',len(tr))); pos=4+12*len(tr)
for entry in tr: packed+=struct.pack('<iii',pos,len(entry),0); pos+=len(entry)
packed+=b''.join(tr); compressed=CompressionHelper.compress_lz4(bytes(packed))
target['compressedBlob']=list(compressed); target['offsets']=[[0]]; target['compressedLengths']=[[len(compressed)]]; target['decompressedLengths']=[[len(packed)]]
target['m_Name']=target['m_ParsedForm']['m_Name']='BridgeBuilder/Diagnostic/NativeVertexBinding'
for obj in env.objects:
    if obj.type.name=='Shader': obj.save_typetree(target)
for bundle in env.files.values(): (out/'native-layered-vertex-probe.bundle').write_bytes(bundle.save(packer='none'))
