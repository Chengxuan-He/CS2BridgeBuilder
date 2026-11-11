"""Bounded offline transcription of the inspected native DBuffer fragment ISA.
Unsupported instructions fail authoring; never substitute an approximate program.
"""
import re, struct

def args(text):
    result=[]; depth=0; start=0
    for i,c in enumerate(text):
        if c=='(': depth+=1
        elif c==')': depth-=1
        elif c==',' and depth==0: result.append(text[start:i].strip());start=i+1
    result.append(text[start:].strip()); return result

def value(text):
    negate=text.startswith('-'); text=text[1:] if negate else text
    absolute=text.startswith('|') and text.endswith('|');text=text[1:-1] if absolute else text
    if text.startswith('l('):
        values=args(text[2:-1]); values=values*4 if len(values)==1 else values
        assert len(values)==4,values
        bits=[]
        for x in values:
            if '.' in x or 'e' in x.lower():n=struct.unpack('<I',struct.pack('<f',float(x)))[0]
            else:n=int(x,0)&0xffffffff
            bits.append(str(n)+'u')
        result='uint4('+','.join(bits)+')'
    else:
        match=re.fullmatch(r'(r\d+|v\d+|cb\d+\[\d+\])(?:\.([xyzw]+))?',text)
        assert match,text
        base,mask=match.groups();mask=mask or 'xyzw'
        if len(mask)==1:mask*=4
        assert len(mask)==4,text
        result=base+'.'+mask
    if absolute:result='asuint(abs(asfloat('+result+')))'
    if negate:result='asuint(-asfloat('+result+'))'
    return result

def transcribe(assembly, current_projection=False, per_volume_area=False):
    declarations=[]; body=[]; resources={}; temps=0
    for line in assembly.splitlines():
        line=line.strip().rstrip('\0')
        if not line or line.startswith('//') or line=='ps_5_0':continue
        if line.startswith('dcl_constantbuffer'):
            index,size=re.search(r'CB(\d+)\[(\d+)\]',line).groups()
            declarations.append(f'cbuffer BBNative{index}:register(b{index}) {{ uint4 cb{index}[{size}]; }};');continue
        if line.startswith('dcl_resource_texture'):
            kind,index=re.search(r'dcl_resource_(texture\w+).* t(\d+)',line).groups()
            resources['t'+index]=kind
            declarations.append(f'{"Texture2DArray" if kind=="texture2darray" else "Texture2D"}<float4> t{index}:register(t{index});');continue
        if line.startswith('dcl_resource_structured'):
            index,stride=map(int,re.search(r't(\d+), (\d+)',line).groups());assert stride%4==0
            resources['t'+str(index)]='structured'
            declarations.append(f'struct BBRecord{index} {{uint v[{stride//4}];}}; StructuredBuffer<BBRecord{index}> t{index}:register(t{index});')
            declarations.append(f'uint4 BBRead{index}(uint i,uint b) {{ b/=4; return uint4(t{index}[i].v[b],t{index}[i].v[b+1],t{index}[i].v[b+2],t{index}[i].v[b+3]); }}');continue
        if line.startswith('dcl_sampler'):
            index=re.search(r's(\d+)',line).group(1);declarations.append(f'SamplerState s{index}:register(s{index});');continue
        if line.startswith('dcl_temps'):temps=int(line.split()[-1]);continue
        if line.startswith(('dcl_input','dcl_output','dcl_globalFlags')):continue
        m=re.match(r'([a-z0-9_]+(?:\([^)]*\))*)\s*(.*)',line);assert m,line
        opcode,arguments=m.groups();op=opcode.split('(')[0]; a=args(arguments) if arguments else []
        if op=='ret':continue
        if op=='if_nz':body.append('if(('+value(a[0])+').x!=0u) {');continue
        if op=='else':body.append('} else {');continue
        if op=='endif':body.append('}');continue
        if op=='discard_nz':body.append('if(('+value(a[0])+').x!=0u) discard;');continue
        destination=a.pop(0);saturated=op.endswith('_sat');op=op[:-4] if saturated else op
        dest,mask=(destination.split('.')+['xyzw'])[:2] if '.' in destination else (destination,'xyzw')
        def raw(i):return value(a[i])
        def f(i):return 'asfloat('+raw(i)+')'
        expression=None
        if op=='mov':expression=raw(0)
        elif op in ('add','mul','div'):expression=f'asuint({f(0)} {dict(add="+",mul="*",div="/")[op]} {f(1)})'
        elif op=='mad':expression=f'asuint(mad({f(0)},{f(1)},{f(2)}))'
        elif op in ('min','max'):expression=f'asuint({op}({f(0)},{f(1)}))'
        elif op in ('sqrt','rsq','log','exp','frc','round_ni','deriv_rtx_coarse','deriv_rty_coarse'):
            name={'rsq':'rsqrt','log':'log2','exp':'exp2','frc':'frac','round_ni':'floor','deriv_rtx_coarse':'ddx_coarse','deriv_rty_coarse':'ddy_coarse'}.get(op,op)
            expression=f'asuint({name}({f(0)}))'
        elif op in ('dp2','dp3','dp4'):
            swizzle='xyzw'[:int(op[-1])];expression=f'(asuint(dot(({f(0)}).{swizzle},({f(1)}).{swizzle}))).xxxx'
        elif op in ('lt','ge','eq','ieq'):
            left,right=(raw(0),raw(1)) if op=='ieq' else (f(0),f(1))
            expression=f'((uint4)({left} {dict(lt="<",ge=">=",eq="==",ieq="==")[op]} {right})*0xffffffffu)'
        elif op=='and':expression=f'({raw(0)} & {raw(1)})'
        elif op=='movc':expression='uint4('+','.join(f'({raw(0)}).{c}!=0u?({raw(1)}).{c}:({raw(2)}).{c}' for c in 'xyzw')+')'
        elif op=='ftou':expression=f'(uint4)({f(0)})'
        elif op=='ftoi':expression=f'asuint((int4)({f(0)}))'
        elif op=='utof':expression=f'asuint((float4)({raw(0)}))'
        elif op=='ubfe':expression=f'BBUbfe({raw(0)},{raw(1)},{raw(2)})'
        elif op.startswith('ld_structured'):
            resource,swizzle=a[2].split('.');expression=f'BBRead{resource[1:]}(({raw(0)}).x,({raw(1)}).x).{swizzle}'
        elif op.startswith('ld_indexable'):
            resource,swizzle=a[1].split('.');coordinate=f'asint({raw(0)})'
            dims='xyzw' if resources[resource]=='texture2darray' else 'xyw'
            expression=f'asuint({resource}.Load(({coordinate}).{dims})).{swizzle}'
        elif op.startswith(('sample_b','sample_d')):
            resource,swizzle=a[1].split('.');sampler=a[2];dims='xyz' if resources[resource]=='texture2darray' else 'xy'
            coordinate=f'({f(0)}).{dims}'
            if op.startswith('sample_b'):sample=f'{resource}.SampleBias({sampler},{coordinate},({f(3)}).x)'
            else:sample=f'{resource}.SampleGrad({sampler},{coordinate},({f(3)}).xy,({f(4)}).xy)'
            expression=f'asuint({sample}).{swizzle}'
        else:raise ValueError('Unsupported instruction: '+line)
        if saturated:expression='asuint(saturate(asfloat('+expression+')))'
        body.append(dest+'.'+mask+' = ('+expression+').'+mask+';')
    header='''uint4 BBUbfe(uint4 width,uint4 offset,uint4 input) { return (input>>offset)&((1u<<width)-1u); }
struct BBFragmentInput {float4 position:SV_POSITION;float4 tangent:INTERP0;float4 uv:INTERP1;float3 world:INTERP2;float3 normal:INTERP3;};
struct BBFragmentOutput {float4 color:SV_Target0;float4 normal:SV_Target1;float4 mask:SV_Target2;float2 weight:SV_Target3;};
BBFragmentOutput BBFragment(BBFragmentInput input) {
uint4 v0=asuint(input.position),v1=asuint(input.tangent),v2=asuint(input.uv),v3=asuint(float4(input.world,0)),v4=asuint(float4(input.normal,0));
uint4 o0=0,o1=0,o2=0,o3=0;
'''
    if current_projection:
        header=header.replace('float3 normal:INTERP3;', 'float3 normal:INTERP3;' + ''.join(f'nointerpolation float4 projection{i}:TEXCOORD{i+8};' for i in range(9)))
        # Replace only object instance matrices. Native material, alpha, distance,
        # texture sampling and PBR instructions remain unchanged.
        header+='uint4 bbProjection[10];\n'
        header+='\n'.join(f'bbProjection[{i}]=asuint(input.projection{i});' for i in range(8))+'\n'
        draw=re.findall(r'dcl_constantbuffer CB(\d+)\[(8|10)\]',assembly)
        assert len(draw)==1,draw
        header+='bbProjection[8]=0;bbProjection[9]=asuint(input.projection8);\n'
        body=[re.sub(r'\bcb'+draw[0][0]+r'\[', 'bbProjection[', line) for line in body]
    if per_volume_area:
        material=re.findall(r'dcl_constantbuffer CB(\d+)\[13\]',assembly)
        assert len(material)==1,material
        body=[re.sub(r'\bcb'+material[0]+r'\[10\]', 'asuint(input.uv)', line) for line in body]
    header+='\n'.join(f'uint4 r{i}=0;' for i in range(temps))+'\n'
    return '\n'.join(declarations)+'\n'+header+'\n'.join(body)+'''
BBFragmentOutput result;result.color=asfloat(o0);result.normal=asfloat(o1);result.mask=asfloat(o2);result.weight=asfloat(o3.xy);return result;
}
'''

if __name__=='__main__':
    import sys
    from pathlib import Path
    Path(sys.argv[2]).write_text(transcribe(Path(sys.argv[1]).read_text()),encoding='utf-8')
