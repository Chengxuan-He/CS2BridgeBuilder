"""Export one actual failing receiver per unresolved semantic group, without probing new faces."""
import ast
import gzip
import json
import struct
import numpy as np
from GroupStructureUnits import ROOT,SOURCE,OUT,EPS,reference


def run():
    manifest=json.loads((OUT/'manifest.json').read_bytes())
    membership=json.loads((OUT/'membership-evidence.json').read_bytes())
    by_id={d['id']:d for d in membership['domains']}
    groups=[g for g in manifest['groups'] if g['kind']=='NONREPEATING']
    wanted={d:g for g in groups for d in g['domains'] if by_id[d]['outside']}
    module=ast.parse((ROOT/'tools/PreparedMaterials/Draw03/CheckChartCoverage.py').read_text())
    context={'np':np,'EPS':EPS}
    exec(compile(ast.Module(body=[n for n in module.body if isinstance(n,(ast.ClassDef,ast.FunctionDef))],type_ignores=[]),'CheckChartCoverage.py','exec'),context)
    Chart=context['Chart']; data={}; found={}
    for line in gzip.open(SOURCE/'receivers.json.receivers.jsonl.gz','rt'):
        r=json.loads(line);identity=f"{r['name']}|{r['mesh']}|{r['domain']}"
        if identity not in wanted:continue
        group=wanted[identity]
        if group['id'] in found:continue
        name=f"NativeUvCharts_{r['name'].replace(' ','_')}_{r['mesh']}"
        if name not in data:
            b=(ROOT/'assets/HardcodedNumbers/Draw03'/(name+'.bin')).read_bytes()
            ni,_,_,_,_,nd=struct.unpack_from('<6i',b,8)
            data[name]=(np.frombuffer(b,'<i4',ni,32),np.frombuffer(b,'<f8',nd,32+ni*4))
        ints,ds=data[name];at=int(ints[2+r['domain']]);loops=[];charts=[]
        for c in ints[at+1:at+1+ints[at]]:
            pos=int(ints[2+ints[0]+c]);rings=[]
            for j in range(int(ints[pos])):
                start,count=map(int,ints[pos+1+j*2:pos+3+j*2]);rings.append(ds[start:start+count*2].reshape(-1,2))
            loops.append([ring.tolist() for ring in rings])
            if rings:charts.append(Chart(rings))
        if any(c.contains(np.asarray(r['mapped'])) for c in charts):continue
        found[group['id']]=dict(group=group['id'],sourceDigest=group['sourceDigest'],render=group['render'],mesh=group['mesh'],
            domainCount=len(group['domains']),receiverCount=sum(by_id[d]['receivers'] for d in group['domains']),
            outsideCount=sum(by_id[d]['outside'] for d in group['domains']),receiver=r,originalChartLoops=loops,
            reason=group['reason'])
        if len(found)==len(groups):break
    assert len(found)==len(groups)
    report=dict(style='Draw03',scope=membership['scope'],groups=len(groups),examples=list(found.values()),
        receiverInput=reference(SOURCE/'receivers.json.receivers.jsonl.gz'),
        requestedSharedDecision='Use these actual original loops and receiver coordinates to implement an admissible source-density mapping for finite nonperiodic parts. Grating rectangle coverage does not include these inputs. Do not fall back to runtime constant baking or the deleted per-triangle prepared library.',
        styleComplete=False)
    (OUT/'shared-gap-inputs.json').write_text(json.dumps(report,indent=2)+'\n',encoding='utf-8')
    print(json.dumps({'groups':len(groups),'outsideReceivers':sum(r['outsideCount'] for r in found.values())}))


if __name__=='__main__':run()
