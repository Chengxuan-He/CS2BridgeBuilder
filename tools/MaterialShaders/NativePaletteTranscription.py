"""Offline, bounded SM5 fragment transcription. Unsupported ISA fails authoring.

Registers retain bit representations. Native signatures, sampling and control flow
are carried through; only the two explicitly identified material samples change.
"""
import re, struct
from NativeDecalTranscription import args


def literal(text):
    values = args(text[2:-1])
    if len(values) == 1: values *= 4
    assert len(values) == 4, values
    bits = [int(x, 0) & 0xffffffff if x.lower().startswith(('0x', '-0x')) or not any(c in x.lower() for c in '.e') else struct.unpack('<I', struct.pack('<f', float(x)))[0] for x in values]
    return 'uint4(' + ','.join(str(x)+'u' for x in bits) + ')'


def value(text, integer=False):
    negative = text.startswith('-')
    text = text[1:] if negative else text
    absolute = text.startswith('|') and text.endswith('|')
    text = text[1:-1] if absolute else text
    if text.startswith('l('):
        result = literal(text)
    else:
        match = re.fullmatch(r'(r\d+|v\d+|(?:cb\d+|icb)\[[^\]]+\])(?:\.([xyzw]+))?', text)
        if not match:
            raise ValueError('Unsupported operand: ' + text)
        base, mask = match.groups()
        base = re.sub(r'\[(r\d+\.[xyzw]) \+ (\d+)\]', r'[int(\1) + \2]', base)
        mask = mask or 'xyzw'
        if len(mask) == 1:
            mask *= 4
        if len(mask) != 4:
            raise ValueError('Unsupported source swizzle: ' + text)
        result = base + '.' + mask
    if absolute:
        result = 'asuint(abs(asfloat(' + result + ')))'
    if negative:
        result = 'asuint(-' + ('asint' if integer else 'asfloat') + '(' + result + '))'
    return result


def signature(assembly, heading):
    block = assembly.split('// ' + heading + ' signature:')[1].split('//\n//\n', 1)[0]
    return [m.groups() for m in re.finditer(r'^//\s+(\w+)\s+(\d+)\s+([xyzw]+)\s+(\d+)\s+(\w+)\s+(float|uint|int)\s*', block, re.M)]


def transcribe(assembly, material_register, base_texture, mask_texture, control_register, inject=True):
    declarations, body, resources = [], [], {}
    inputs, outputs = signature(assembly, 'Input'), signature(assembly, 'Output')
    assembly = re.sub(r'dcl_immediateConstantBuffer\s*\{(.*?)\}\s*\n(?=dcl_)',
                      lambda m: 'BB_ICB ' + '|'.join(re.findall(r'\{([^}]+)\}', m[1])) + '\n', assembly, flags=re.S)
    temps = 0
    hits = [0, 0]; tint_hits = 0
    for line in assembly.splitlines():
        line = line.strip().rstrip('\0')
        if not line or line.startswith('//') or line == 'ps_5_0':
            continue
        if line.startswith('BB_ICB '):
            rows = [literal('l(' + row + ')') for row in line[7:].split('|')]
            declarations.append('static const uint4 icb[] = {' + ','.join(rows) + '};')
            continue
        if line.startswith('dcl_constantbuffer'):
            index, size = map(int, re.search(r'CB(\d+)\[(\d+)\]', line).groups())
            if index == material_register:
                size = max(size, 68)
            declarations.append(f'cbuffer BBNative{index}:register(b{index}) {{ uint4 cb{index}[{size}]; }};')
            continue
        if line.startswith('dcl_resource_texture'):
            kind, dtype, index = re.search(r'dcl_resource_(texture\w+) \((\w+),.* t(\d+)', line).groups()
            resources['t' + index] = (kind, dtype)
            cls = {'texture2d': 'Texture2D', 'texture2darray': 'Texture2DArray', 'texture3d': 'Texture3D', 'texturecubearray': 'TextureCubeArray'}[kind]
            declarations.append(f'{cls}<{dtype}4> t{index}:register(t{index});')
            continue
        if line.startswith('dcl_resource_raw'):
            index = re.search(r't(\d+)', line)[1]
            declarations.append(f'ByteAddressBuffer t{index}:register(t{index});')
            continue
        if line.startswith('dcl_resource_structured'):
            index, stride = map(int, re.search(r't(\d+), (\d+)', line).groups())
            declarations.append(f'struct BBRecord{index} {{uint v[{stride//4}];}}; StructuredBuffer<BBRecord{index}> t{index}:register(t{index});')
            # Use component loads only where the actual instruction consumes them;
            # a four-word read from a one-word native struct is not valid HLSL.
            continue
        if line.startswith('dcl_sampler'):
            index = re.search(r's(\d+)', line)[1]
            typ = 'SamplerComparisonState' if 'comparison' in line else 'SamplerState'
            declarations.append(f'{typ} s{index}:register(s{index});')
            continue
        if line.startswith('dcl_temps'):
            temps = int(line.split()[-1]); continue
        if line.startswith(('dcl_input', 'dcl_output', 'dcl_globalFlags')):
            continue
        match = re.match(r'([a-z0-9_]+(?:\([^)]*\))*)\s*(.*)', line)
        if not match:
            raise ValueError(line)
        opcode, arguments = match.groups(); op = opcode.split('(')[0]
        a = args(arguments) if arguments else []
        if op == 'ret':
            body.append('return BBReturn(' + ','.join('o'+r[3] for r in outputs) + ');'); continue
        if op in ('else', 'endif', 'endloop', 'endswitch', 'default', 'break', 'loop'):
            body.append({'else': '} else {', 'endif': '}', 'endloop': '}', 'endswitch': '}', 'default': 'default:', 'break': 'break;', 'loop': '[loop] while(true) {'}[op]); continue
        if op in ('if_nz', 'if_z', 'breakc_nz', 'breakc_z', 'discard_nz'):
            test = '!=' if op.endswith('nz') else '=='
            suffix = 'break;' if op.startswith('break') else 'discard;' if op.startswith('discard') else '{'
            body.append(f'if (({value(a[0])}).x {test} 0u) {suffix}'); continue
        if op in ('switch', 'case'):
            body.append('switch (('+value(a[0])+').x) {' if op == 'switch' else 'case ('+value(a[0])+').x:'); continue
        destination = a.pop(0)
        saturated = op.endswith('_sat'); op = op[:-4] if saturated else op
        integral = op in ('and','or','xor','not','ishl','ushr','ishr','iadd','imad','imul','udiv','imax','imin','umin','ieq','ine','ige','ilt','ult','uge','utof','itof','firstbit_shi','countbits','bfi')
        def raw(i): return value(a[i], integral)
        def f(i): return 'asfloat(' + raw(i) + ')'
        def signed(i): return 'asint(' + raw(i) + ')'
        def assign(dest, expression):
            if dest == 'null': return
            base, mask = dest.split('.') if '.' in dest else (dest, 'xyzw')
            if saturated: expression = 'asuint(saturate(asfloat(' + expression + ')))'
            body.append(base + '.' + mask + ' = (' + expression + ').' + mask + ';')
        expression = None
        if op == 'mov': expression = raw(0)
        elif op in ('add', 'mul', 'div'): expression = f'asuint({f(0)} '+dict(add='+', mul='*', div='/')[op]+f' {f(1)})'
        elif op == 'mad': expression = f'asuint(mad({f(0)},{f(1)},{f(2)}))'
        elif op in ('min', 'max'): expression = f'asuint({op}({f(0)},{f(1)}))'
        elif op in ('sqrt', 'rsq', 'rcp', 'log', 'exp', 'frc', 'round_ni', 'round_ne', 'round_pi', 'round_z', 'deriv_rtx_coarse', 'deriv_rty_coarse'):
            name = dict(rsq='rsqrt', rcp='rcp', log='log2', exp='exp2', frc='frac', round_ni='floor', round_ne='round', round_pi='ceil', round_z='trunc', deriv_rtx_coarse='ddx_coarse', deriv_rty_coarse='ddy_coarse').get(op, op)
            expression = f'asuint({name}({f(0)}))'
        elif op in ('dp2', 'dp3', 'dp4'):
            sw = 'xyzw'[:int(op[-1])]; expression = f'(asuint(dot(({f(0)}).{sw},({f(1)}).{sw}))).xxxx'
        elif op in ('lt', 'ge', 'eq', 'ne', 'ieq', 'ine', 'ige', 'ilt', 'ult', 'uge'):
            fn = signed if op in ('ige', 'ilt') else raw if op.startswith(('i', 'u')) else f
            cmp = dict(lt='<', ge='>=', eq='==', ne='!=', ieq='==', ine='!=', ige='>=', ilt='<', ult='<', uge='>=')[op]
            expression = f'((uint4)({fn(0)} {cmp} {fn(1)})*0xffffffffu)'
        elif op in ('and', 'or', 'xor', 'ishl', 'ushr', 'ishr', 'iadd', 'imad'):
            operator = dict(and_='&', or_='|', xor='^', ishl='<<', ushr='>>', ishr='>>', iadd='+', imad='*').get(op+'_' if op in ('and','or') else op)
            lhs = signed(0) if op == 'ishr' else raw(0)
            expression = f'asuint({lhs} {operator} {raw(1)}'+(' + '+raw(2) if op == 'imad' else '')+')'
        elif op == 'not': expression = '~' + raw(0)
        elif op in ('imax', 'imin', 'umin'):
            fn = signed if op.startswith('i') else raw
            expression = f'asuint({op[1:]}({fn(0)},{fn(1)}))'
        elif op == 'movc': expression = 'uint4('+','.join(f'({raw(0)}).{c}!=0u?({raw(1)}).{c}:({raw(2)}).{c}' for c in 'xyzw')+')'
        elif op in ('ftou', 'ftoi', 'utof', 'itof'):
            expression = {'ftou': lambda:f'(uint4){f(0)}', 'ftoi':lambda:f'asuint((int4){f(0)})', 'utof':lambda:f'asuint((float4){raw(0)})', 'itof':lambda:f'asuint((float4){signed(0)})'}[op]()
        elif op in ('firstbit_shi', 'countbits'): expression = f'asuint({"BBFirstSigned" if op == "firstbit_shi" else op}({signed(0) if op == "firstbit_shi" else raw(0)}))'
        elif op == 'bfi': expression = f'BBInsert({raw(0)},{raw(1)},{raw(2)},{raw(3)})'
        elif op == 'imul':
            second = a.pop(0)
            if destination != 'null': raise ValueError('High multiply is not used by inspected native programs')
            assign(second, f'({raw(0)} * {raw(1)})'); continue
        elif op == 'udiv':
            second = a.pop(0); assign(destination, f'({raw(0)} / {raw(1)})'); assign(second, f'({raw(0)} % {raw(1)})'); continue
        elif op == 'sincos':
            second = a.pop(0);body.append('{float4 bbAngle='+f(0)+';');assign(destination, 'asuint(sin(bbAngle))');assign(second, 'asuint(cos(bbAngle))');body.append('}');continue
        elif op == 'swapc':
            second = a.pop(0); body.append('{ uint4 bbSwap0='+raw(1)+', bbSwap1='+raw(2)+';')
            assign(destination, 'uint4('+','.join(f'({raw(0)}).{c}?bbSwap1.{c}:bbSwap0.{c}' for c in 'xyzw')+')')
            assign(second, 'uint4('+','.join(f'({raw(0)}).{c}?bbSwap0.{c}:bbSwap1.{c}' for c in 'xyzw')+')'); body.append('}'); continue
        elif op.startswith('ld_raw'):
            resource, sw = a[1].split('.'); expression = f'{resource}.Load4(({raw(0)}).x).{sw}'
        elif op.startswith('ld_structured'):
            resource, sw = a[2].split('.'); mask = destination.split('.')[1] if '.' in destination else 'xyzw'
            expression = 'uint4('+','.join(f'{resource}[({raw(0)}).x].v[({raw(1)}).x/4+{"xyzw".index(sw[k])}]' if 'xyzw'[k] in mask else '0u' for k in range(4))+')'
        elif op.startswith('ld_indexable'):
            resource, sw = a[1].split('.'); kind, dtype = resources[resource]
            dims = 'xyw' if kind == 'texture2d' else 'xyzw'
            expression = f'{resource}.Load(({signed(0)}).{dims})'
            if dtype == 'float': expression = 'asuint('+expression+')'
            expression += '.'+sw
        elif op.startswith(('sample_b', 'sample_d', 'sample_l', 'sample_c_lz')):
            resource, sw = a[1].split('.'); kind, _ = resources[resource]
            dims = {'texture2d':'xy','texture2darray':'xyz','texture3d':'xyz','texturecubearray':'xyzw'}[kind]
            coordinate = f'({f(0)}).{dims}'
            if op.startswith('sample_b'): sample = f'{resource}.SampleBias({a[2]},{coordinate},({f(3)}).x)'
            elif op.startswith('sample_l'): sample = f'{resource}.SampleLevel({a[2]},{coordinate},({f(3)}).x)'
            elif op.startswith('sample_c_lz'): sample = f'{resource}.SampleCmpLevelZero({a[2]},{coordinate},({f(3)}).x).xxxx'
            else: sample = f'{resource}.SampleGrad({a[2]},{coordinate},({f(3)}).xy,({f(4)}).xy)'
            used = destination.split('.')[1] if '.' in destination else 'xyzw'
            alpha_only = resource == base_texture and all(sw['xyzw'.index(c)] == 'w' for c in used)
            if inject and resource in (base_texture, mask_texture) and not alpha_only:
                if not op.startswith('sample_b') or sw != 'xyzw': raise ValueError('Unexpected palette sample: '+line)
                hits[resource == mask_texture] += 1
                sample = f'BBPalette({sample},bbControl.SampleBias({a[2]},{coordinate},({f(3)}).x).rgb,{"true" if resource == mask_texture else "false"})'
            expression = f'asuint({sample}).{sw}'
        else:
            raise ValueError('Unsupported instruction: '+line)
        assign(destination, expression)
        if inject and op == 'mul' and any(re.fullmatch(r'cb'+str(material_register)+r'\[14\]\.[xyzw]{4}', x) for x in a):
            # Native Default saturates the tinted RGB before weather. The native
            # network program has already blended its world layer at this point.
            # Preserve the tint's alpha lane and the later infoview/weather code.
            tint = next(x for x in a if re.fullmatch(r'cb'+str(material_register)+r'\[14\]\.[xyzw]{4}', x))
            reg, written = destination.split('.')
            swizzle = tint.split('.')[-1]
            rgb = ''.join(c for c in written if swizzle['xyzw'.index(c)] in 'xyz')
            if rgb:
                body.append(f'{reg}.{rgb}=asuint(saturate(asfloat({reg}.{rgb})));')
                tint_hits += 1
    if inject and hits != [1, 1]: raise ValueError(('palette sample count', hits))
    if inject and tint_hits != 1: raise ValueError(('base tint stage count', tint_hits))
    if inject:
        declarations.append(f'Texture2D<float4> bbControl:register(t{control_register});')
        declarations.append(f'''float4 BBPalette(float4 value,float3 control,bool mask) {{
float4 a=asfloat(cb{material_register}[65]), b=asfloat(cb{material_register}[66]), c=asfloat(cb{material_register}[67]);
float rg=control.x+control.y, rgb=rg+control.z;
float4 mixed=a+control.y*rg*(b-a);mixed+=control.z*rgb*(c-mixed);
float4 factor=1+min(rgb,1)*(mixed-1);
if(mask) {{ value.a*=factor.a;float3 coat=control*(1-float3(a.a,b.a,c.a));if(dot(coat,coat)!=0)value.g=0; }}
else value.rgb*=factor.rgb;
return value; }}''')
    declarations.append('uint4 BBInsert(uint4 w,uint4 o,uint4 a,uint4 b) {uint4 m=((1u<<w)-1u)<<o;return (a<<o)&m | b&~m;}')
    declarations.append('int4 BBFirstSigned(int4 x) {int4 h=firstbithigh(x);return int4(h.x<0?-1:31-h.x,h.y<0?-1:31-h.y,h.z<0?-1:31-h.z,h.w<0?-1:31-h.w);}')
    input_fields = []
    for name, index, mask, register, system, dtype in inputs:
        interpolation = 'nointerpolation ' if dtype != 'float' else 'noperspective ' if name == 'SV_POSITION' else ''
        input_fields.append(f'{interpolation}{dtype}4 v{register}:{name}{index if name != "SV_POSITION" else ""};')
    declarations.append('struct BBInput {'+''.join(input_fields)+'};')
    declarations.append('struct BBOutput {'+''.join(f'{dtype}4 o{register}:{name}{index};' for name,index,mask,register,system,dtype in outputs)+'};')
    declarations.append('BBOutput BBReturn('+','.join('uint4 o'+r[3] for r in outputs)+'){BBOutput x;'+''.join(f'x.o{r[3]}=asfloat(o{r[3]});' for r in outputs)+'return x;}')
    header = 'BBOutput BBFragment(BBInput input) {\n'
    header += '\n'.join(f'uint4 v{r[3]}='+('asuint(' if r[5]=='float' else '(')+f'input.v{r[3]});' for r in inputs)
    header += '\n'+'\n'.join(f'uint4 r{i}=0;' for i in range(temps))
    header += '\n'+'\n'.join('uint4 o'+r[3]+'=0;' for r in outputs)+'\n'
    return '\n'.join(declarations)+'\n'+header+'\n'.join(body)+'\n}\n'
