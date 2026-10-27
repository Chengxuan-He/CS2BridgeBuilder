"""One complete original-domain inventory and finite semantic disposition for Lift04."""
import collections
import hashlib
import json
from pathlib import Path
import sys

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from Lift04UvChartBindings import read_domains


def reference(path):
    return dict(path=str(path.resolve()),sha256=hashlib.sha256(path.read_bytes()).hexdigest())


def main():
    output=ROOT/'artifacts/prepared-materials/Lift04/structure-inventory'
    output.mkdir(parents=True,exist_ok=True)
    base=ROOT/'artifacts/prepared-materials/Lift04'
    charts_path=base/'shared-sampling/native-charts.json'
    charts=json.loads(charts_path.read_text())
    binding_path=base/'shared-sampling/native-bindings.json'
    bindings=json.loads(binding_path.read_text())
    hashes={s['cid']:s['sourceSHA256'] for s in bindings['surfaces']}
    render_hashes={s['render']:[hashes[c] for c in s['originalSurfaceCids']] for s in bindings['renderBindings']}
    outcome_path=base/'structure-inventory/guarded-profiles/receiver-coverage.json.domains.json'
    outcomes=collections.defaultdict(collections.Counter)
    for row in json.loads(outcome_path.read_text()):
        name,mesh,delta,domain,*_=row['identity'].split('/')
        outcomes[f'{name}|{mesh}|{domain}'][row['kind']]+=row['receivers']
    domains=[];groups=collections.defaultdict(list);details=[]
    for mesh in charts['meshes']:
        name,index=mesh['render'],mesh['mesh'];digest=render_hashes[name][index]
        path=ROOT/'assets/HardcodedNumbers/Lift04'/f'UvDomain_{name.replace(" ","_")}_{index}.bin'
        for number,faces in enumerate(read_domains(path)):
            identity=f'{name}|{index}|{number}';states=outcomes[identity];count=sum(states.values())
            domains.append(dict(id=identity,sourceDigest=digest,receivers=count))
            chart_ids=sorted(set(mesh['faceChart'][face] for face in faces))
            bounds=[mesh['charts'][i]['bounds'] for i in chart_ids if i>=0]
            if states.get('remaining'):
                family='pillarbase' if 'PillarBase' in name else 'pillar' if 'Pillar' in name else 'moving-structure'
                category='NONREPEATING';label=family+'-unproven-original-details'
            elif states.get('beamRegion'):
                category='REPEAT_BASE';label='beam'
            elif states.get('floorRegion'):
                category='REPEAT_BASE';label='floor'
            elif states.get('wideBeamProfile'):
                category='REPEAT_BASE';label='wide-beam-profile'
            elif states.get('outerFlangeProfile'):
                category='REPEAT_BASE';label='outer-flange'
            else:
                category='ORIGINAL';label='native-or-unchanged'
            groups[(category,label,digest)].append(identity)
            details.append(dict(id=identity,sourceDigest=digest,sourceFaces=faces,originalCharts=chart_ids,
                                chartBounds=bounds,receiverStates=dict(states),disposition=label))
    inventory=dict(schema='BridgeBuilder/StructureDomains/1',style='Lift04',domains=domains,
                   scope='All 49004 original structural domains, all eleven meshes/material branches and LODs. Receiver counts cover width deltas -1.25,-0.25,0,2,8,24 only; unchanged domains have zero changed receivers.',
                   sources=[reference(charts_path),reference(binding_path),reference(outcome_path)])
    inventory_path=output/'original-domains.json';inventory_path.write_text(json.dumps(inventory,indent=2)+'\n')
    membership_path=output/'part-membership.json';membership_path.write_text(json.dumps(dict(domains=details),separators=(',',':')))
    beam=json.loads((base/'beam-repeat/source-evidence.json').read_text())
    floor=json.loads((ROOT/'tools/PreparedMaterials/Lift04/repeat-units-v2.json').read_text())['units'][0]
    units=[dict(id='beam',sourceDigest=beam['sourceDigest'],semanticPattern='Original painted beam grain; accepted 404-pixel longitudinal source pattern',
                bounds=beam['min']+beam['max'],physicalPeriod=beam['physicalSizeUV'],
                evidence=reference(base/'beam-repeat/source-evidence.json'),mipEvidence=reference(base/'beam-repeat/candidate/native-check.json')),
           dict(id='floor',sourceDigest=floor['sourceDigest'],semanticPattern='Original authored steel floor surface unit',
                bounds=floor['min']+floor['max'],physicalPeriod=[4,4],
                evidence=reference(ROOT/'tools/PreparedMaterials/Lift04/repeat-units-v2.json'),mipEvidence=reference(base/'rebuild-native/repeat-candidate/native-check.json'))]
    for name in ('wide-beam-profile','outer-flange'):
        proof_path=output/(name+'-evidence.json')
        proof=json.loads(proof_path.read_text())
        units.append(dict(id=name,sourceDigest=proof['sourceDigest'],semanticPattern='Full original cross-profile Normal/Mask retained with measured V-only grain and guarded U boundaries',
                          bounds=proof['unitBounds'],physicalPeriod=proof['physicalFrames'][0]['physicalPeriodUV'],
                          evidence=reference(proof_path),mipEvidence=reference(output/'guarded-profiles/candidate'/('native-check-Lift04-'+name+'.json'))))
    rows=[]
    for (kind,label,digest),members in sorted(groups.items()):
        row=dict(id=label+'-'+digest[:12],kind=kind,sourceDigest=digest,domains=members,evidence=reference(membership_path))
        if kind.startswith('REPEAT'):row['unit']=label
        elif kind=='ORIGINAL':row['reason']='Original native UV-chart mapping passes on all changed receivers of this domain in the declared finite inventory, or the domain has no changed receivers in those widths. No claim for untested future widths.'
        else:row['reason']=('Original '+label+' has density-preserving receivers outside the proven native chart. The original Normal/Mask includes part-specific baked edge, rivet and joint detail; a color-only steel replacement would erase these. No proven common repeated full-channel unit is registered for this class. Minimum implementation: retain original detail by fixed native-mapped boundary geometry plus an evidenced repeating interior, or provide a native sampling representation preserving the original detail without repeating arbitrary triangles. This is an unresolved class, not proof that every included face is inherently nonperiodic.')
        rows.append(row)
    manifest=dict(schema='BridgeBuilder/StructureUnits/1',style='Lift04',inventory=reference(inventory_path),units=units,groups=rows)
    (output/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    constraints=collections.defaultdict(list)
    for detail in details:
        if detail['receiverStates'].get('remaining'):
            constraints[(detail['disposition'],detail['sourceDigest'])].append(detail)
    classes=[dict(group=label,sourceDigest=digest,unresolvedReceivers=sum(d['receiverStates']['remaining'] for d in members),
                  sampledDomains=len(members),examples=sorted(members,key=lambda d:d['receiverStates']['remaining'],reverse=True)[:8])
             for (label,digest),members in sorted(constraints.items())]
    coverage=json.loads((output/'guarded-profiles/receiver-coverage.json').read_text())
    (output/'remaining-constraints.json').write_text(json.dumps(dict(scope='Six-width actual unresolved receivers after guarded single-axis profile integration; not full group receiver counts.',
        counts=coverage['counts'],classes=classes,minimumImplementation='Preserve nonperiodic original Normal/Mask edge/rivet/joint details through bounded native composition or evidenced fixed detail and repeated interior. Do not enable U repeat or erase details to conceal rejected extrapolation.',
        noRuntimeConstantBake=True,fullBridgeGenerationVerified=False),indent=2)+'\n')
    print(json.dumps(dict(domains=len(domains),groups=len(rows),changedReceivers=sum(x['receivers'] for x in domains),manifest=str(output/'manifest.json'))))


if __name__=='__main__':main()
