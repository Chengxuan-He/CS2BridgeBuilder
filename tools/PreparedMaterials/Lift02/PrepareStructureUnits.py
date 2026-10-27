"""One complete finite-domain grouping and one evidenced crossbeam-unit batch."""
import collections
import hashlib
import json
from pathlib import Path
import struct
import numpy as np
from ExportNativeCharts import ROOT, read_domains
# read_domains validates and decodes both UV-domain schemas; chart BBN is unchanged.

OUT=ROOT/'artifacts/prepared-materials/Lift02/structure-units'
PREVIOUS=ROOT/'artifacts/prepared-materials/Lift02/source-sharing'
NUMBERS=ROOT/'assets/HardcodedNumbers/Lift02'
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def save(name,value):
    p=OUT/name;p.write_text(json.dumps(value,separators=(',',':'))+'\n');return {'path':str(p),'sha256':digest(p)}
def ref(p):return {'path':str(p),'sha256':digest(p)}

def main():
    OUT.mkdir(parents=True,exist_ok=True)
    sources_path=ROOT/'artifacts/prepared-materials/Lift02/source-rebuild/native-source-inventory.json'
    sources=json.loads(sources_path.read_text())
    source_by_cid={s['cid']:s for s in sources['surfaces']}
    source_by_render={(r['render'],i):source_by_cid[cid] for r in sources['renders'] for i,cid in enumerate(r['surfaceCids'])}
    meta=json.loads((PREVIOUS/'native-chart-integration.json').read_text())
    raw=(NUMBERS/'Types_Lift02_Lift02UvCharts_cs.bin').read_bytes()
    assert hashlib.sha256(raw).hexdigest()==meta['resourceSha256']
    _,_,ni,_,_,_,_,nd=struct.unpack('<8i',raw[:32])
    ints=np.frombuffer(raw,dtype='<i4',count=ni,offset=32);ds=np.frombuffer(raw,dtype='<f8',count=nd,offset=32+4*ni)
    selected={25408:'crossbeam-web',25393:'crossbeam-horizontal-flange',25398:'crossbeam-lip'}
    src=source_by_cid['ea923855f34e05d7a517992fe064ddb7']
    routes={};faces={};domains={};chart_sets={};evidence_sources=[]
    for route in meta['routes']:
        k=(route['render'],route['mesh']);routes[k]=route
        p=NUMBERS/('UvDomain_'+k[0].replace(' ','_')+'_'+str(k[1])+'.bin')
        domains[k]=read_domains(p);faces[k]={f:d for d,fs in enumerate(domains[k]) for f in fs}
        chart_sets[k]=[tuple(int(c) for c in ints[ints[route['offset']+d]+1:ints[route['offset']+d]+1+ints[ints[route['offset']+d]]]) for d in range(len(domains[k]))]
        evidence_sources.append(ref(p))
    # Reuse the complete prior public containment result; no repeat GPU probing.
    accepted=json.loads((PREVIOUS/'native-chart-accepted.json').read_text())
    accepted_set={(r['render'],r['mesh'],r['delta'],r['face']) for r in accepted}
    receivers=collections.Counter();pending=collections.Counter();widths=set()
    for line in (PREVIOUS/'actual-receivers.json.receivers.ndjson').open():
        r=json.loads(line);k=(r['name'],r['mesh']);d=faces[k][r['face']];key=(*k,d);widths.add(r['delta']);receivers[key]+=1
        if (*k,r['delta'],r['face']) not in accepted_set:pending[key]+=1
    prior_manifest=json.loads((ROOT/'artifacts/agent-material-env/Lift02/repeat-v2-trial/extracted-source/manifest.json').read_text())['units'][0]
    mipref=ref(ROOT/'artifacts/prepared-materials/Lift02/repeat-units-v2/extracted-mip-verification.json')
    units=[];bake_units=[];unit_evidence=[]
    for chart,name in selected.items():
        assert ints[chart]==1
        offset,n=ints[chart+1:chart+3];loop=ds[offset:offset+n*2].reshape(-1,2);lo=loop.min(0);hi=loop.max(0)
        assert len(loop)==4 and len(set(map(tuple,loop)))==4
        assert set(map(tuple,loop))=={(x,y) for x in (lo[0],hi[0]) for y in (lo[1],hi[1])}
        uses=[];sample=None
        for k in routes:
            if source_by_render[k]['sha256']!=src['sha256']:continue
            matching=[d for d,cs in enumerate(chart_sets[k]) if cs==(chart,)]
            if not matching:continue
            native=ROOT/'artifacts/uv-density/native'/(k[0]+'.npz');mesh=np.load(native)
            ff=mesh[f'mesh{k[1]}_faces'];pp=mesh[f'mesh{k[1]}_positions'];uv=mesh[f'mesh{k[1]}_uv0'][:,:2].astype(float)
            allfaces=[f for d in matching for f in domains[k][d]];centres=np.array([pp[ff[f]].mean(0) for f in allfaces])
            uses.append({'render':k[0],'mesh':k[1],'domains':matching,'faces':allfaces,'native':ref(native),
                         'distinctFaceCentres':len(set(map(tuple,centres))), 'geometryBounds':[centres.min(0).tolist(),centres.max(0).tolist()]})
            if sample is None:
                for f in allfaces:
                    t=uv[ff[f]];p=pp[ff[f]].astype(float)
                    if abs(np.linalg.det(t[1:]-t[0]))<1e-15:continue
                    frame=np.linalg.solve(t[1:]-t[0],p[1:]-p[0]);period=np.linalg.norm(frame,axis=1)*(hi-lo)
                    sample={'render':k[0],'mesh':k[1],'face':f,'positions':p.tolist(),'uv':t.tolist(),'physicalFramePerUv':frame.tolist(),'physicalPeriod':period.tolist()};break
        assert len(uses)>=2 and sum(len(u['faces']) for u in uses)>100 and sample
        pattern={'id':name,'source':src,'chartOffset':chart,'bounds':[*lo,*hi],'originalLoop':loop.tolist(),
                 'semanticPattern':'Original blue painted steel crossbeam '+name+' face, repeatedly authored across the bridge and shared by near/mid meshes; full rectangular indexed chart, not a receiver crop.',
                 'uses':uses,'physicalFrame':sample,'channels':prior_manifest['channels'],'mipEvidence':mipref,
                 'limits':'Coverage is only this measured original chart. No inference to other blue atlas islands or other source Surface CIDs.'}
        eref=save(name+'-evidence.json',pattern);unit_evidence.append(pattern)
        units.append({'id':name,'sourceDigest':src['sha256'],'semanticPattern':pattern['semanticPattern'],'bounds':[*lo,*hi],
                      'physicalPeriod':sample['physicalPeriod'],'evidence':eref,'mipEvidence':mipref})
        b=dict(prior_manifest);b.update(id=name,min=lo.tolist(),max=hi.tolist(),physicalSize=sample['physicalPeriod'],evidence=eref,aliases=[])
        bake_units.append(b)
    bookkeeping={'scope':'All original Lift02 domain resources, all 20 render/material routes. Finite receiver corpus uses halfDelta 0, 2, 8 only; absence of a changed receiver is not proof for other widths.',
                 'halfDeltas':sorted(widths),'sourceInventory':ref(sources_path),'domainResources':evidence_sources,
                 'receiverCorpus':ref(PREVIOUS/'actual-receivers.json.receivers.ndjson'),
                 'nativeContainment':ref(PREVIOUS/'native-chart-coverage.json'),'acceptedReceivers':ref(PREVIOUS/'native-chart-accepted.json')}
    grouping_evidence=save('grouping-evidence.json',bookkeeping)
    inventory=[];grouped=collections.defaultdict(list)
    for k,ds0 in domains.items():
        source=source_by_render[k]
        for d in range(len(ds0)):
            id=f'{k[0]}|{k[1]}|{d}';key=(*k,d);cs=chart_sets[k][d]
            unit=selected.get(cs[0]) if len(cs)==1 and source['sha256']==src['sha256'] else None
            if unit:kind='REPEAT_BASE';reason=unit
            elif not pending[key]:kind='ORIGINAL';reason='Native source mapping remains inside measured indexed chart for all changed sampled receivers; domains with zero receivers retain original UV/material in this finite corpus. Other widths are not certified.'
            else:kind='NONREPEATING';reason='Changed receiver exits its original indexed chart. This source domain contains authored geometry-specific atlas layout; no independently evidenced genuine periodic unit has been established. Preserve the original chart with a supported bounded native mapping; do not expand its bounding rectangle or replace it with another steel/grain region.'
            inventory.append({'id':id,'sourceDigest':source['sha256'],'receivers':receivers[key]})
            grouped[(source['sha256'],kind,reason)].append(id)
    invref=save('original-domains.json',{'schema':'BridgeBuilder/StructureDomains/1','style':'Lift02','domains':inventory})
    groups=[]
    for i,((digest0,kind,reason),members) in enumerate(grouped.items()):
        group={'id':f'{kind.lower()}-{i}','kind':kind,'sourceDigest':digest0,'domains':members,'evidence':grouping_evidence}
        group['unit' if kind.startswith('REPEAT_') else 'reason']=reason;groups.append(group)
    save('manifest.json',{'schema':'BridgeBuilder/StructureUnits/1','style':'Lift02','inventory':invref,'units':units,'groups':groups})
    save('batch.json',{'recipe':'BridgeBuilder/RepeatUnits/2','style':'Lift02','units':bake_units})
    summary={'domains':len(inventory),'routes':len(routes),'receivers':sum(receivers.values()),'units':len(units),
             'groups':[{'kind':g['kind'],'sourceDigest':g['sourceDigest'],'domains':len(g['domains']),'receivers':sum(receivers[tuple(id.rsplit('|',2)[:1])+tuple(map(int,id.rsplit('|',2)[1:]))] for id in g['domains'])} for g in groups]}
    save('grouping-summary.json',summary);print(json.dumps({k:v for k,v in summary.items() if k!='groups'}))

if __name__=='__main__':main()
