"""Read-only Draw01 routing/owner-budget audit; no pixel conversion."""
import hashlib
import itertools
import json
from pathlib import Path
from ExportRecipes import numbers, sampler_key

ROOT=Path(__file__).resolve().parents[3]
REPORTS=ROOT/'artifacts/prepared-materials/Draw01'
OUT=REPORTS/'owner-unit-budget'


def read(path):return json.loads(path.read_text(encoding='utf-8-sig'))


def main():
    OUT.mkdir(parents=True,exist_ok=True)
    codefiles=[ROOT/'src/BridgeBuilder/Bridges/Types/Draw01'/n for n in
               ['Draw01BridgeGenerator.cs','Draw01ClosedRoad.cs','Draw01RoadPartitions.cs','Draw01FixedBase.cs']]
    codefiles.append(ROOT/'src/BridgeBuilder/Bridges/Common/Movable/MovableSurfaceAtlas.cs')
    values=numbers(ROOT/'assets/HardcodedNumbers/Draw01/Types_Draw01_Draw01RoadPartitions_cs.bin')
    I,F=values['I32'],values['F32']
    candidates=read(REPORTS/'genuine-unit-requirements/manifest.json')['roadCandidates']
    by_region={region:c['candidateId'] for c in candidates for region in c['appearanceRegions']}
    # Exact MaterialRegions branch indices. Motion/section provenance and native
    # mask classification decide which receivers actually occupy these branches.
    rows=[]
    for offset in [I[10],I[11]]:
        for sidewalk in [False,True]:
            if sidewalk:
                branches=[('left',I[0:3],[-F[8],F[9]]),('right',I[3:6],[-F[10],F[11]])]
            else:
                branches=[('left',[I[7],I[6],I[7]],[-F[12],F[13]]),
                          ('right',[I[9],I[8],I[9]],[-F[12],F[13]])]
            for side,regions,stations in branches:
                rows.append(dict(section='sidewalk' if sidewalk else 'carriageway',halfOffset=offset,
                                 side=side,zCuts=stations,regions=[v+offset for v in regions],
                                 units=[by_region[v+offset] for v in regions]))
    recompressed=read(REPORTS/'repeat-units-single-period/lossless-level22/report.json')['channels']
    reports=[read(REPORTS/'repeat-units-single-period/report.json'),
             read(REPORTS/'repeat-units-single-period/grating-descriptor/report.json')]
    units={u['id']:u for r in reports for u in r['units']}
    descriptor_sizes={}
    for name,u in units.items():
        folder='grating-descriptor/Draw01' if name=='metal-grating' else 'candidate-output/Draw01'
        p=REPORTS/'repeat-units-single-period'/folder/'RepeatUnits'/u['key']/'Repeat.Surface'
        descriptor_sizes[name]=p.stat().st_size+Path(str(p)+'.cid').stat().st_size
    groups=[]
    names=sorted(units)
    for count in range(1,len(names)+1):
        for selected in itertools.combinations(names,count):
            textures={r['newCid']:r['newBytes'] for r in recompressed if r['unit'] in selected}
            old={r['oldCid']:r['oldBytes'] for r in recompressed if r['unit'] in selected}
            descriptors=sum(descriptor_sizes[n] for n in selected)
            # Native CID sidecars preserve their 32-byte identities; replacing
            # references with recompressed CIDs changes no descriptor length.
            total=sum(textures.values())+32*len(textures)+descriptors
            groups.append(dict(units=selected,texturePayloads=len(textures),textureBytes=sum(textures.values()),
                               descriptorAndCidBytes=descriptors+32*len(textures),lowerBoundOwnerBytes=total,
                               oldLowerBoundOwnerBytes=sum(old.values())+32*len(old)+descriptors,
                               exceeds10MB=total>10_000_000))
    grating=next(c for c in candidates if c['candidateId']=='metal-grating')
    altkey=sampler_key(grating['sourceDigest'],grating['region']['min'],grating['region']['max'],False)
    result=dict(scope='Source-reachable material groups, not a measurement of an instantiated game owner. '
                      'Counts exclude other owner-specific road/paint/track/structure dependencies and are therefore lower bounds.',
                sourceHashes={str(p.relative_to(ROOT)):hashlib.sha256(p.read_bytes()).hexdigest() for p in codefiles},
                appearanceBranches=rows,groups=groups,
                smallestOverBudget=[g for g in groups if g['exceeds10MB'] and
                    not any(h['exceeds10MB'] and set(h['units'])<set(g['units']) for h in groups)],
                movingAsphaltGratingAlias=dict(requestedCutout=False,requestedKey=altkey,
                    legacyKey=grating['legacyRequestKeys'][0],legacyCutout=True,
                    evidence='MovableSurfaceAtlas pure-asphalt tile initializer leaves RepeatCutout false.'),
                completeOwnerMeasured=False)
    (OUT/'audit.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(groups=groups,smallestOverBudget=result['smallestOverBudget'],movingAsphaltGratingAlias=result['movingAsphaltGratingAlias']),indent=2))


if __name__=='__main__':main()
