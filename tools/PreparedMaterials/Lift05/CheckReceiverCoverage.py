"""Check actual Lift05 plan receivers against only their part-scoped domain charts."""
import collections
import hashlib
import io
import json
from pathlib import Path
import struct
import sys
import numpy as np
from ExportRecipes import numbers

root=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(root/'artifacts/agent-material-env/Lift05/python-deps'))
from shapely.geometry import Polygon, LineString, Point
directory=root/'artifacts/prepared-materials/Lift05/domain-charts'
metadata=json.loads((directory/'charts.json').read_text())
sources={(s['name'],s['mesh']):s['domains'] for s in metadata['streams']}
shapes=[]
for chart in metadata['charts']:
    shape=Polygon()
    for loop in chart['loops']:shape=shape.symmetric_difference(Polygon(loop))
    assert shape.is_valid and not shape.is_empty
    shapes.append(shape)
bbn=numbers(root/'assets/HardcodedNumbers/Lift05/Lift05UvCharts.bin')
data=bbn['I32'];values=bbn['F64'];assert data[:3]==(1,len(metadata['streams']),len(shapes))
for i,chart in enumerate(metadata['charts']):
    at=data[3+len(metadata['streams'])+i];n=data[at];assert n==len(chart['loops'])
    for ring in chart['loops']:
        offset,count=data[at+1:at+3];at+=2
        assert tuple(v for p in ring for v in p)==values[offset:offset+2*count]
for i,stream in enumerate(metadata['streams']):
    at=data[3+i];assert data[at]==len(stream['domains']);at+=1
    for refs in stream['domains']:
        count=data[at];at+=1;assert tuple(refs)==data[at:at+count];at+=count

raw=(directory/'receivers.json.receivers.bin').read_bytes();reader=io.BytesIO(raw)
def unpack(fmt):return struct.unpack('<'+fmt,reader.read(struct.calcsize('<'+fmt)))
def text():
    size=shift=0
    while True:
        b=unpack('B')[0];size|=(b&127)<<shift
        if b<128:break
        shift+=7
    return reader.read(size).decode()
reports=[]
while reader.tell()<len(raw):
    name=text();mesh,delta,count=unpack('ifi');totals=collections.Counter()
    for _ in range(count):
        domain,repeat_only,old_direct,n=unpack('i??i')
        uv=np.asarray(unpack('f'*(n*2))).reshape(-1,2);assert n%3==0 and np.isfinite(uv).all()
        refs=sources[(name,mesh)][domain]
        totals['receiverUnits']+=1;totals['triangles']+=n//3
        totals['oldDirect']+=old_direct
        if not refs:totals['noReliableChart']+=1;continue
        if repeat_only:totals['conditionedRepeatPhase']+=1;continue
        # No approximate boundary expansion. Each complete generated triangle
        # must fit one chart; taking a union across physical parts is forbidden.
        covered=True
        for i in range(0,n,3):
            triangle=Polygon(uv[i:i+3])
            if triangle.area==0:
                unique=np.unique(uv[i:i+3],axis=0)
                triangle=Point(unique[0]) if len(unique)==1 else LineString(unique)
            if not any(shapes[ref].covers(triangle) for ref in refs):covered=False;break
        totals['chartCovered' if covered else 'outsideChart']+=1
        if covered and not old_direct:totals['additionalOriginalCandidates']+=1
    reports.append(dict(name=name,mesh=mesh,delta=delta,**totals))
total=collections.Counter()
for r in reports:
    for k,v in r.items():
        if k not in ('name','mesh','delta'):total[k]+=v
plan=json.loads((directory/'receivers.json').read_text())
assert plan['failures']==plan['packingFailures']==plan['footprintFailures']==0
result=dict(status='PASS',scope='Strict geometric source-chart coverage of actual geometry plan receivers, not filtered native rendering acceptance',
    geometryAssemblySha256=hashlib.sha256((root/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll').read_bytes()).hexdigest(),
    geometryFixtureSha256=hashlib.sha256((root/'artifacts/uv-density/layout-plans.bin').read_bytes()).hexdigest(),
    bbnSha256=hashlib.sha256((root/'assets/HardcodedNumbers/Lift05/Lift05UvCharts.bin').read_bytes()).hexdigest(),
    receiverSha256=hashlib.sha256(raw).hexdigest(),cases=len(reports),summary=dict(total),reports=reports,
    nativeCodec=plan['nativeAttributeCodec'],densityFailures=plan['failures'],nativeRenderAcceptance=False)
(directory/'receiver-coverage.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(dict(cases=len(reports),summary=dict(total),densityFailures=0),indent=2))
