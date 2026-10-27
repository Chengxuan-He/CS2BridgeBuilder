"""Group every measured Draw03 structural domain once; reuse existing unit evidence."""
from pathlib import Path
import ast
import collections
import gzip
import hashlib
import json
import struct
import sys
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays, read_bbn
SOURCE = ROOT / 'artifacts/prepared-materials/Draw03/native-charts'
PROOF = ROOT / 'artifacts/prepared-materials/Draw03/repeat-units-v2'
OUT = PROOF / 'structure-groups'
EPS = 1e-12


def reference(path):
    return {'path': str(path.resolve()), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()}


def save(name, value):
    path = OUT / name
    path.write_text(json.dumps(value, indent=2) + '\n', encoding='utf-8')
    return reference(path)


def run():
    OUT.mkdir(parents=True, exist_ok=True)
    # Reuse the already validated containment implementation without executing
    # its historical top-level audit or overwriting historical evidence.
    module = ast.parse((ROOT/'tools/PreparedMaterials/Draw03/CheckChartCoverage.py').read_text())
    definitions = ast.Module(body=[n for n in module.body if isinstance(n, (ast.ClassDef, ast.FunctionDef))], type_ignores=[])
    context = {'np': np, 'EPS': EPS}
    exec(compile(definitions, 'CheckChartCoverage.py', 'exec'), context)
    Chart = context['Chart']
    meshes = {(m['render'], m['mesh']): m for m in json.loads((SOURCE/'manifest.json').read_bytes())['meshes']}
    catalogue = json.loads((SOURCE/'catalogue.json').read_bytes())
    sources = {s['cid']: s['sha256'] for s in json.loads((PROOF.parent/'rebuild-native-v1/sources.json').read_bytes())['sources']}
    measured = json.loads((PROOF/'candidate-manifest.json').read_bytes())['candidates']
    units = {}
    for name, stage in [('Clean', 'private-clean-trial'), ('Grating', 'grating-single-period')]:
        candidate = next(c for c in measured if c['candidateId'] == 'Draw03/' + ('CleanAsphalt' if name == 'Clean' else name))
        baked = json.loads((PROOF/stage/'report.json').read_bytes())['units'][0]
        units[name] = dict(id=name, sourceDigest=sources['792a855e9d0c5c34d2c41d48b062139d'],
            semanticPattern='Native repeated '+name+' island, retaining the measured original source interval and physical period.',
            bounds=candidate['region']['min']+candidate['region']['max'], physicalPeriod=candidate['physicalSize'],
            evidence=reference(PROOF/'candidate-manifest.json'), mipEvidence=reference(PROOF/stage/'native-check.json'),
            bindingKey=baked['key'], stage=stage)
    domains = {}; charts_by_mesh = {}; inputs = []
    for item in catalogue['resources']:
        mesh = meshes[item['render'], item['mesh']]
        source_digest = sources[mesh['surfaceCid']]
        path = ROOT/'assets/HardcodedNumbers/Draw03'/(item['resource']+'.bin')
        blob = path.read_bytes(); assert hashlib.sha256(blob).hexdigest() == item['sha256']
        ni, _, _, _, _, nd = struct.unpack_from('<6i', blob, 8)
        ints=np.frombuffer(blob, '<i4', ni, 32); ds=np.frombuffer(blob, '<f8', nd, 32+ni*4)
        ndom,ncharts=map(int,ints[:2]); charts=[]
        for c in range(ncharts):
            at=int(ints[2+ndom+c]); rings=[]
            for j in range(int(ints[at])):
                pos,count=map(int,ints[at+1+j*2:at+3+j*2]);rings.append(ds[pos:pos+count*2].reshape(-1,2))
            charts.append(Chart(rings) if rings else None)
        recipe=path.with_name('UvDomain_'+mesh['render'].replace(' ','_')+'_'+str(mesh['mesh'])+'.bin')
        rb=recipe.read_bytes(); ri,_=decode_arrays(*read_bbn(rb))
        assert ri[0]==-2 and ri[1]==ndom
        native_path=ROOT/'artifacts/uv-density/native'/(mesh['render']+'.npz')
        if not native_path.exists():native_path=ROOT/'artifacts/uv-density/native-additional'/(mesh['render']+'.npz')
        assert hashlib.sha256(native_path.read_bytes()).hexdigest()==mesh['sourceNpZSha256']
        native=np.load(native_path); prefix=f"mesh{mesh['mesh']}_"
        uv=native[prefix+'uv0'][:,:2]; faces=native[prefix+'faces']; at=2; mapping=[]
        for di in range(ndom):
            count=abs(int(ri[at+3])); ids=ri[at+4:at+4+count]; at+=4+count
            coord=uv[faces[ids].reshape(-1)]
            chosen=None
            for name,unit in units.items():
                bounds=unit['bounds']
                if source_digest==unit['sourceDigest'] and np.all(coord >= np.asarray(bounds[:2])-EPS) and np.all(coord <= np.asarray(bounds[2:])+EPS):
                    chosen=name;break
            co=int(ints[2+di]); mapping.append([charts[int(c)] for c in ints[co+1:co+1+ints[co]] if charts[int(c)] is not None])
            identity=f"{mesh['render']}|{mesh['mesh']}|{di}"
            domains[identity]=dict(id=identity, sourceDigest=source_digest, receivers=0,
                contained=0,outside=0,unit=chosen,render=mesh['render'],mesh=mesh['mesh'])
        charts_by_mesh[mesh['render'],mesh['mesh']]=mapping
        inputs.extend((reference(path),reference(recipe),reference(native_path)))
    for line in gzip.open(SOURCE/'receivers.json.receivers.jsonl.gz', 'rt'):
        r=json.loads(line); identity=f"{r['name']}|{r['mesh']}|{r['domain']}"; d=domains[identity]
        d['receivers']+=1
        contained=any(c.contains(np.asarray(r['mapped'])) for c in charts_by_mesh[r['name'],r['mesh']][r['domain']])
        d['contained' if contained else 'outside']+=1
    evidence=save('membership-evidence.json',dict(
        scope='All 35 original meshes and LODs; actual changed receivers from symmetric-default-sidewalk delta 0/2/8 only. No proof of arbitrary widths or asymmetric sidewalks.',
        unexercised='Original mapping retained only for unchanged domains in this finite fixture; no blanket future-width classification.',
        sourceInputs=inputs, receiverInput=reference(SOURCE/'receivers.json.receivers.jsonl.gz'),
        domains=list(domains.values())))
    inventory=save('original-domains.json',dict(schema='BridgeBuilder/StructureDomains/1',style='Draw03',
        domains=[{k:d[k] for k in ('id','sourceDigest','receivers')} for d in domains.values()]))
    groups=collections.defaultdict(list)
    for d in domains.values():
        if d['unit']:
            disposition=d['unit']; kind='REPEAT_BASE' if disposition=='Clean' else 'REPEAT_DETAIL'
        elif not d['outside']:
            disposition='contained' if d['receivers'] else 'unchanged-fixture'; kind='ORIGINAL'
        else: disposition='mixed-or-unproven-pattern';kind='NONREPEATING'
        groups[d['sourceDigest'],d['render'],d['mesh'],kind,disposition].append(d['id'])
    rows=[]
    for index,((digest,render,mesh,kind,disposition),ids) in enumerate(sorted(groups.items())):
        row=dict(id=f'group-{index}',kind=kind,sourceDigest=digest,domains=ids,evidence=evidence,render=render,mesh=mesh)
        if kind.startswith('REPEAT_'):row['unit']=disposition
        elif disposition=='contained':row['reason']='All changed receivers in the declared finite fixture remain inside the same original part chart; preserve original native Surface and full mip graph.'
        elif disposition=='unchanged-fixture':row['reason']='No changed receiver in the delta 0/2/8 fixture: preserve the unchanged original native mapping. This does not establish safe mapping at other widths or sidewalk layouts.'
        else:row['reason']='At least one changed receiver exits its original part chart. This atlas domain is outside both evidenced Clean/Grating repeat islands; repeating its triangle or arbitrary crop would copy adjacent authored parts or invent an unevidenced period. Requires a measured genuine pattern shared by this part family, or native nonperiodic representation preserving its finite details; no pixel fallback is supplied.'
        rows.append(row)
    used={r['unit'] for r in rows if 'unit' in r}
    save('manifest.json',dict(schema='BridgeBuilder/StructureUnits/1',style='Draw03',inventory=inventory,
        units=[u for name,u in units.items() if name in used],groups=rows))
    totals=collections.defaultdict(lambda:dict(domains=0,receivers=0,outside=0))
    for r in rows:
        total=totals[r['kind']]
        for identity in r['domains']:
            d=domains[identity];total['domains']+=1;total['receivers']+=d['receivers'];total['outside']+=d['outside']
    summary=dict(meshes=len(catalogue['resources']),domains=len(domains),groups=len(rows),units=list(used),counts=dict(totals))
    save('summary.json',summary);print(json.dumps(summary))


if __name__=='__main__':run()
