"""Offline network-palette authoring; no pixel baking or runtime compiler.

All native vertex programs and non-appearance passes remain byte-identical.
The bounded transcription changes only base/control sampling in GBuffer/Forward.
"""
from pathlib import Path
import argparse, copy, ctypes as C, hashlib, json, re, struct, sys
from NativePaletteTranscription import transcribe


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--network-shader', type=Path, required=True)
    parser.add_argument('--container-seed', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--unitypy', type=Path)
    parser.add_argument('--limit', type=int, help='Diagnostic compilation limit; never writes a release bundle.')
    options = parser.parse_args()
    if options.unitypy: sys.path.insert(0, str(options.unitypy))
    import UnityPy
    from UnityPy.helpers import CompressionHelper
    output = options.output.resolve(); output.mkdir(parents=True, exist_ok=True)
    shader = json.loads(options.network_shader.read_text())
    blob = bytes(shader['compressedBlob'])
    segments = [CompressionHelper.decompress_lz4(blob[o:o+c], n) for o,c,n in zip(shader['offsets'][0],shader['compressedLengths'][0],shader['decompressedLengths'][0])]
    records = []
    for i in range(struct.unpack_from('<i',segments[0])[0]):
        o,n,s = struct.unpack_from('<iii',segments[0],4+i*12); records.append(segments[s][o:o+n])
    originals = list(records)
    dll = C.WinDLL('d3dcompiler_47.dll')
    compile = dll.D3DCompile
    compile.argtypes=[C.c_void_p,C.c_size_t,C.c_char_p,C.c_void_p,C.c_void_p,C.c_char_p,C.c_char_p,C.c_uint,C.c_uint,C.POINTER(C.c_void_p),C.POINTER(C.c_void_p)];compile.restype=C.c_long
    disassemble = dll.D3DDisassemble
    disassemble.argtypes=[C.c_void_p,C.c_size_t,C.c_uint,C.c_char_p,C.POINTER(C.c_void_p)]
    def data(p):
        if not p:return b''
        vt=C.cast(p,C.POINTER(C.POINTER(C.c_void_p))).contents
        address=C.WINFUNCTYPE(C.c_void_p,C.c_void_p)(vt[3])(p);size=C.WINFUNCTYPE(C.c_size_t,C.c_void_p)(vt[4])(p)
        result=C.string_at(address,size);C.WINFUNCTYPE(C.c_uint,C.c_void_p)(vt[2])(p);return result
    def fields(raw,name):
        name=name.encode();needle=struct.pack('<I',len(name))+name;result=[]
        for m in re.finditer(re.escape(needle),raw):
            at=(m.end()+3)&~3;result.append((at,struct.unpack_from('<3i',raw,at)))
        return result
    def replace(entry,compiled):
        k=entry.index(b'DXBC');size=struct.unpack_from('<I',entry,k+24)[0]
        candidates=[p for p in range(24,k,4) if k+size<=p+4+struct.unpack_from('<I',entry,p)[0]<=len(entry) and struct.unpack_from('<I',entry,p)[0]>size]
        assert len(candidates)==1,candidates
        p=candidates[0];end=p+4+struct.unpack_from('<I',entry,p)[0]
        program=entry[p+4:k]+compiled+entry[k+size:end]
        result=entry[:p]+struct.pack('<I',len(program))+program
        return result+bytes((-len(result))%4)+entry[(end+3)&~3:]
    report=[];modified={};parameter_ids=set();seen=set();fragment_parameters={}
    for sub in shader['m_ParsedForm']['m_SubShaders']:
        for pas in sub['m_Passes']:
            name=pas['m_State']['m_Name']; names=dict(pas['m_NameIndices'])
            for prop in ['_BridgeBuilderPalette0','_BridgeBuilderPalette1','_BridgeBuilderPalette2','_ControlMask']:
                if prop not in names:names[prop]=max(names.values())+1
            pas['m_NameIndices']=[[n,i] for n,i in names.items()]
            for stage in ['progVertex','progFragment']:
                prog=pas[stage];parameter_ids.update(i for group in prog['m_ParameterBlobIndices'] for i in group)
                common=prog['m_CommonParameters']
                for cb in common['m_ConstantBuffers']:
                    if cb['m_NameIndex']==names.get('UnityPerMaterial'):
                        assert cb['m_Size']==1040,cb['m_Size'];cb['m_Size']=1088
                        for i in range(3):cb['m_VectorParams'].append(dict(m_NameIndex=names['_BridgeBuilderPalette'+str(i)],m_Index=1040+16*i,m_ArraySize=0,m_Type=0,m_Dim=4))
                if stage!='progFragment' or name not in ['GBuffer','Forward']:continue
                assemblies={}
                for entry_id in {v['m_BlobIndex'] for group in prog['m_PlayerSubPrograms'] for v in group}:
                    entry=records[entry_id];start=entry.index(b'DXBC');code=entry[start:start+struct.unpack_from('<I',entry,start+24)[0]]
                    pointer=C.c_void_p();assert disassemble(code,len(code),128,None,C.byref(pointer))>=0
                    assemblies[entry_id]=data(pointer).decode().rstrip('\0')
                control_register=max(int(i) for text in assemblies.values() for i in re.findall(r'dcl_resource_[^\n]*\bt(\d+)',text))+1
                for group,variants in enumerate(prog['m_PlayerSubPrograms']):
                    for index,variant in enumerate(variants):
                        entry_id=variant['m_BlobIndex']
                        parameter_id=prog['m_ParameterBlobIndices'][group][index]
                        fragment_parameters[parameter_id]=(fields(records[parameter_id],'_BaseColorMap')[0][1][2],control_register)
                        if entry_id in seen:continue
                        seen.add(entry_id)
                        raw=records[prog['m_ParameterBlobIndices'][group][index]]
                        binding=next(f for _,f in fields(raw,'UnityPerMaterial') if f[0]==1)
                        material=binding[1]
                        base=fields(raw,'_BaseColorMap')[0][1];mask=fields(raw,'_MaskMap')[0][1]
                        entry=records[entry_id];start=entry.index(b'DXBC');code=entry[start:start+struct.unpack_from('<I',entry,start+24)[0]]
                        # Hex immediates retain exact float bits (decimal disassembly
                        # rounds native tiny hash/noise constants to zero).
                        assembly=assemblies[entry_id];(output/f'{name}-{entry_id}.asm').write_text(assembly)
                        base_samples=re.findall(r'sample_\S+\s+(\w+)\.([xyzw]+),.*?\bt'+str(base[1])+r'\.([xyzw]+),',assembly)
                        if base_samples and all(all(sw['xyzw'.index(c)]=='w' for c in dest) for _,dest,sw in base_samples):
                            report.append(dict(passName=name,record=entry_id,unchanged='Native infoview-only fragment samples source alpha, not appearance RGB'))
                            continue
                        source=transcribe(assembly,material,'t'+str(base[1]),'t'+str(mask[1]),control_register)
                        (output/f'{name}-{entry_id}.hlsl').write_text(source)
                        pointer=C.c_void_p();error=C.c_void_p();encoded=source.encode()
                        hr=compile(encoded,len(encoded),f'{name}-{entry_id}'.encode(),None,None,b'BBFragment',b'ps_5_0',1<<15,0,C.byref(pointer),C.byref(error))
                        diagnostic=data(error).decode();(output/f'{name}-{entry_id}.compiler.txt').write_text(diagnostic)
                        if hr<0:raise RuntimeError(diagnostic)
                        compiled=data(pointer);(output/f'{name}-{entry_id}.dxbc').write_bytes(compiled)
                        modified[entry_id]=replace(entry,compiled)
                        report.append(dict(passName=name,record=entry_id,materialRegister=material,baseTexture=base[1],maskTexture=mask[1],controlTexture=control_register,sampler=base[2],originalBytes=len(code),bytes=len(compiled),sha256=hashlib.sha256(compiled).hexdigest()))
                        print('Compiled',name,entry_id,len(compiled),flush=True)
                        if options.limit and len(report)>=options.limit:
                            (output/'partial-report.json').write_text(json.dumps(report,indent=2));return
                common['m_TextureParams'].append(dict(m_NameIndex=names['_ControlMask'],m_Index=control_register,m_SamplerIndex=-1,m_MultiSampled=False,m_Dim=2))
    for index in parameter_ids:
        records[index]=extend_parameters(records[index],fragment_parameters.get(index))
    for index,entry in modified.items():records[index]=entry
    props=shader['m_ParsedForm']['m_PropInfo']['m_Props']
    color=copy.deepcopy(next(p for p in props if p['m_Name']=='_BaseColor'))
    for i in range(3):
        p=copy.deepcopy(color);p['m_Name']=p['m_Description']='_BridgeBuilderPalette'+str(i);p['m_Type']=1;p['m_Flags']=0
        for j in range(4):p['m_DefValue['+str(j)+']']=1.
        props.append(p)
    if not any(p['m_Name']=='_ControlMask' for p in props):
        texture=copy.deepcopy(next(p for p in props if p['m_Name']=='_BaseColorMap'));texture['m_Name']=texture['m_Description']='_ControlMask';texture['m_DefTexture']['m_DefaultName']='black';props.append(texture)
    shader['m_Name']=shader['m_ParsedForm']['m_Name']='BridgeBuilder/Generated/NetworkPalette'
    shader['m_Dependencies']=[];shader['m_ParsedForm']['m_FallbackName']=''
    packed=bytearray(struct.pack('<i',len(records)));position=4+12*len(records)
    for entry in records:packed+=struct.pack('<iii',position,len(entry),0);position+=len(entry)
    packed+=b''.join(records);compressed=CompressionHelper.compress_lz4(bytes(packed))
    shader['compressedBlob']=list(compressed);shader['offsets']=[[0]];shader['compressedLengths']=[[len(compressed)]];shader['decompressedLengths']=[[len(packed)]]
    env=UnityPy.load(str(options.container_seed))
    for obj in env.objects:
        if obj.type.name=='Shader':obj.save_typetree(shader)
    ident='CAB-'+hashlib.sha256(packed).hexdigest()[:32]
    for bundle in env.files.values():
        assert len(bundle.files)==1
        file=next(iter(bundle.files.values()));file.name=ident;bundle.files={ident:file}
    result=next(iter(env.files.values())).save(packer='none');(output/'NetworkPalette.bundle').write_bytes(result)
    root=Path(__file__).resolve().parents[2]
    sources=[Path(__file__),Path(__file__).with_name('NativePaletteTranscription.py'),Path(__file__).with_name('NativeDecalTranscription.py')]
    manifest=dict(schema=2,format='UnityFS',program='NetworkPalette',producer=Path(__file__).name,producerSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),packageBytes=len(result),packageSha256=hashlib.sha256(result).hexdigest(),sources={p.relative_to(root).as_posix():hashlib.sha256(p.read_bytes()).hexdigest() for p in sources},nativeInputs={k:hashlib.sha256(p.read_bytes()).hexdigest() for k,p in [('networkShader',options.network_shader),('containerSeed',options.container_seed)]})
    (output/'NetworkPalette.manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (output/'report.json').write_text(json.dumps(report,indent=2)+'\n')
    assert all(records[v['m_BlobIndex']]==originals[v['m_BlobIndex']] for s in shader['m_ParsedForm']['m_SubShaders'] for p in s['m_Passes'] for group in p['progVertex']['m_PlayerSubPrograms'] for v in group)
    print('PASS',len(report),'native appearance fragments; original vertex payloads unchanged')


def extend_parameters(raw, control=None):
    """Extend concrete native parameter records, not just common reflection metadata."""
    def string_at(position):
        size=struct.unpack_from('<I',raw,position)[0]
        return raw[position+4:position+4+size].decode(),(position+4+size+3)&~3
    def encoded(name):
        value=name.encode();return struct.pack('<I',len(value))+value+bytes((-len(value))%4)
    cursor=8;blocks=[]
    count=struct.unpack_from('<I',raw,4)[0]
    for _ in range(count):
        start=cursor;name,cursor=string_at(cursor)
        size,fields_count=struct.unpack_from('<II',raw,cursor);cursor+=8
        fields_start=cursor
        for _ in range(fields_count):
            _,cursor=string_at(cursor);cursor+=24
        fields_end=cursor
        struct_count=struct.unpack_from('<I',raw,cursor)[0];cursor+=4
        for _ in range(struct_count):
            _,cursor=string_at(cursor)
            _,_,_,members=struct.unpack_from('<4I',raw,cursor);cursor+=16
            for _ in range(members):
                _,cursor=string_at(cursor);cursor+=24
        if name=='UnityPerMaterial':
            assert size==1040,size
            added=b''.join(encoded('_BridgeBuilderPalette'+str(i))+struct.pack('<6I',0,1,4,0,0,1040+16*i) for i in range(3))
            blocks.append(encoded(name)+struct.pack('<II',1088,fields_count+3)+raw[fields_start:fields_end]+added+raw[fields_end:cursor])
        else:blocks.append(raw[start:cursor])
    bindings=raw[cursor:]
    if control is not None:
        sampler,register=control
        count=struct.unpack_from('<I',bindings)[0]
        added=encoded('_ControlMask')+struct.pack('<4i',0,register,sampler,4)
        bindings=struct.pack('<I',count+1)+added+bindings[4:]
    return raw[:8]+b''.join(blocks)+bindings


if __name__ == '__main__':
    main()
