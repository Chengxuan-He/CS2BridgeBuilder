"""Emit only the authorized 129 Lift05 profiles and their measured phase tree."""
import hashlib
import json
import struct
import sys
import numpy as np
from ReviewStructuralSeams import ROOT, phases
from BuildUvCharts import domains


def main():
    policy=json.loads((ROOT/'artifacts/prepared-materials/Lift05/non-tiling/classification.json').read_text())
    ints=[1,2];floats=[];doubles=[];reports=[]
    for lod in range(2):
        source=np.load(ROOT/f'artifacts/layer-decoupling/Lift05/outputs/native-repeat-mechanism/native-lod{lod}.npz')
        p=source['mesh0_positions'].astype(float);uv=source['mesh0_uv0'].astype(float);f=source['mesh0_indices']
        render='LiftBridge05 Mesh' if lod==0 else 'LiftBridge05_LOD1 Mesh'
        parts=next(m for m in policy['meshes'] if m['render']==render and m['mesh']==0)['mixedProfileEvidence']
        _,evidence=phases(parts,p,uv,f,p)
        by_seed={part['seedFace']:part for part in parts}
        order={item['domain']:i for i,item in enumerate(evidence['tree'])}
        selected={face for part in parts for face in part['faces']}
        selected_domains=[]
        for i,members in enumerate(domains(render,0)):
            overlap=selected.intersection(members)
            if not overlap:continue
            assert overlap==set(members), 'Do not exempt an unapproved source face.'
            selected_domains.append(i)
        ints.extend([len(p),f.size,len(selected_domains),*selected_domains,len(parts)])
        records=[]
        for item in evidence['tree']:
            part=by_seed[item['domain']];ids=f[part['faces']].ravel()
            low=int(ids[np.argmin(p[ids,0])]);high=int(ids[np.argmax(p[ids,0])])
            umin,umax=float(uv[ids,0].min()),float(uv[ids,0].max())
            rate=float((uv[high,0]-uv[low,0])/(umax-umin)/(p[high,0]-p[low,0]))
            parent=order[item['parent']] if item['parent'] is not None else -1
            assert parent<order[item['domain']]
            ints.extend([parent,item.get('anchorVertex',-1),item.get('parentVertex',-1),item.get('vertex',-1)])
            doubles.extend([umin,umax,rate])
            for face in part['faces']:
                records.append((face,order[item['domain']]))
        ints.append(len(records))
        for face,profile in sorted(records):
            ints.extend([face,profile,*map(int,f[face])]);floats.extend(uv[f[face]].ravel().tolist())
        reports.append(dict(lod=lod,profiles=len(parts),retileDomains=len(selected_domains),sourceFaces=len(records),
            domainIds=selected_domains,sourceFaceIds=sorted(selected),tree=evidence['tree']))
    assert sum(r['profiles'] for r in reports)==129
    assert sum(r['retileDomains'] for r in reports)==149
    assert sum(r['sourceFaces'] for r in reports)==237
    raw=b'BBN1'+struct.pack('<7i',1,len(ints),0,0,0,len(floats),len(doubles))
    raw+=struct.pack('<%di'%len(ints),*ints)+struct.pack('<%df'%len(floats),*floats)+struct.pack('<%dd'%len(doubles),*doubles)
    raw+=hashlib.sha256(raw).digest()
    target=ROOT/'assets/HardcodedNumbers/Lift05/Types_Lift05_StructuralWrap.bin'
    if '--check' in sys.argv:
        assert target.read_bytes()==raw, 'Structural wrap metadata is not deterministic.'
    else:
        target.write_bytes(raw)
    report=json.dumps(dict(
        profiles=129,retileDomainRecords=149,sourceFaces=237,bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest(),
        scope='Exactly the authorized source profiles; additional domain records only reflect existing mid-LOD triangle-domain storage, not additional physical parts.',levels=reports),indent=2)+'\n'
    if '--check' not in sys.argv:
        (ROOT/'artifacts/material-remap/Lift05/structural-wrap-metadata.json').write_text(report)
    print('PASS 129 profiles / 149 existing retile domain IDs / 237 exact source faces:',len(raw),'bytes')


if __name__=='__main__':main()
