"""Check actual Draw01 receiver domains against proposed existing rectangles."""
import collections
import hashlib
import json
from pathlib import Path
import numpy as np

root=Path(__file__).resolve().parents[3]
folder=root/'artifacts/prepared-materials/Draw01'
inventory=json.loads((root/'artifacts/uv-density/structure-tessellated-sources.json').read_text())
units={u['key']:u for u in inventory['units'] if u['style']=='Draw01'}
audit=json.loads((folder/'structure-reuse-audit.json').read_text())
choices={r['triangle']:r['rectangle'] for r in audit['rectangularCoverageCandidates']['candidates']}
def shape(u):return tuple(float(v) for n in ('a','b','c') for v in np.array(u[n],dtype=np.float32))
by_shape=collections.defaultdict(list)
for key in choices:by_shape[shape(units[key])].append(key)
observed=json.loads((folder/'receiver-plans.json.receivers.json').read_text())
records=[]
for o in observed:
    if not o['Triangle']:continue
    for key in by_shape.get(shape(o),[]):
        unit=units[key];rect=units[choices[key]]
        points=np.array(o['points'],dtype=np.float32).astype(float)
        native=np.array([unit[n] for n in ('a','b','c')],dtype=np.float32).astype(float)
        atlas=native[0]+points@(native[1:]-native[0])
        bounds=np.array(rect['bounds'],dtype=np.float32).astype(float);span=bounds[2:]-bounds[:2]
        target=(atlas-bounds[:2])/span
        contained=bool(np.isfinite(target).all() and (target>=0).all() and (target<=1).all())
        folded=not bool((points>=0).all() and (points.sum(axis=1)<=1).all())
        actual=target.astype(np.float32).astype(float)*span+bounds[:2]
        error=0
        for offset in range(0,len(atlas),3):
            expected=(atlas[offset+1:offset+3]-atlas[offset]).T
            rounded=(actual[offset+1:offset+3]-actual[offset]).T
            if np.linalg.det(expected)==0:
                e=0 if np.array_equal(rounded,expected) else float('inf')
            else:e=float(np.sum((rounded@np.linalg.inv(expected)-np.eye(2))**2))
            error=max(error,e)
        eligible=contained and not folded and not o['RepeatOnly'] and error<=1e-8
        outside=np.where(np.any((target<0)|(target>1),axis=1))[0]
        foldindices=np.where(np.any(points<0,axis=1)|(points.sum(axis=1)>1))[0]
        index=int(outside[0] if len(outside) else foldindices[0] if len(foldindices) else 0)
        records.append(dict(key=key,rectangle=rect['key'],render=o['name'],implementation=o['implementation'],
            mesh=o['mesh'],lod=o['lod'],delta=o['delta'],scenario=o['scenario'],left=o['left'],right=o['right'],
            sourceAlreadyReused=o['canUseSource'],repeatOnly=o['RepeatOnly'],needsTriangleFold=folded,
            entireReceiverContained=contained,rectangleGradientSquaredError=error if np.isfinite(error) else None,
            eligibleBeforeNativeFilterCheck=eligible,receiverMin=target.min(axis=0).tolist(),receiverMax=target.max(axis=0).tolist(),
            exampleUnitPoint=points[index].tolist(),exampleAtlasUv=atlas[index].tolist(),exampleRectangleUv=target[index].tolist()))
keys={r['key'] for r in records}
summary=dict(candidateUnits=len(choices),reachedUnits=len(keys),unreached=sorted(set(choices)-keys),observations=len(records),
    receiverFixtureSHA256=hashlib.sha256((root/'artifacts/uv-density/layout-plans.bin').read_bytes()).hexdigest(),
    modSHA256=hashlib.sha256((root/'src/BridgeBuilder/bin/Release/BridgeBuilder.dll').read_bytes()).hexdigest(),
    scenarios=sorted({(r['delta'],r['left'],r['right'],r['scenario']) for r in records}),
    classifications={field:dict(observations=sum(r[field] for r in records),units=len({r['key'] for r in records if r[field]}))
        for field in ('sourceAlreadyReused','repeatOnly','needsTriangleFold','entireReceiverContained','eligibleBeforeNativeFilterCheck')},
    newEligibleObservations=sum(r['eligibleBeforeNativeFilterCheck'] and not r['sourceAlreadyReused'] for r in records),
    unitsWithAnyOutsideReceiver=sorted({r['key'] for r in records if not r['entireReceiverContained']}),
    unitsEligibleForEveryObservedReceiver=sorted(k for k in keys if all(r['eligibleBeforeNativeFilterCheck'] for r in records if r['key']==k)),
    records=records,
    limitations='Actual production plan and UV mapping observations at the listed widths only, not a proof for every width. RepeatOnly maps are not original-atlas coordinates. Source containment ignores native bilinear support and mips; eligible cases still require that test. No asset mutation or texture bake.')
(folder/'receiver-rectangle-audit.json').write_text(json.dumps(summary,indent=2,allow_nan=False)+'\n')
print(json.dumps({k:v for k,v in summary.items() if k not in ('records','unitsWithAnyOutsideReceiver','unreached','unitsEligibleForEveryObservedReceiver')},indent=2))
for kind,predicate in [('outside',lambda r:not r['entireReceiverContained'] and not r['repeatOnly']),('repeatOnly',lambda r:r['repeatOnly']),('eligible',lambda r:r['eligibleBeforeNativeFilterCheck'] and not r['sourceAlreadyReused'])]:
    matches=sorted((r for r in records if predicate(r)),key=lambda r:abs(r['delta']))
    print(kind, json.dumps(matches[0] if matches else None))
