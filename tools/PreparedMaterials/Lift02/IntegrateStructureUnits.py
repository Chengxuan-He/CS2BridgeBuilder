"""Install the single evidenced Lift02 crossbeam batch and generate its region bindings."""
import hashlib,json,struct
from pathlib import Path
from IntegrateStructures import ROOT,refs,sha

OUT=ROOT/'artifacts/prepared-materials/Lift02/structure-units'
DEST=ROOT/'assets/Materials/Lift02'
manifest=json.loads((OUT/'manifest.json').read_text())
bake=json.loads((OUT/'bake.json').read_text())
native_graph=json.loads((ROOT/'artifacts/prepared-materials/Lift02/rebuild-native-v1/native-graph.json').read_text())
builtin={d['cid'] for s in native_graph['descriptors'] for d in s['dependencies'] if d['scope']=='builtin'}
products={u['id']:u for u in bake['units']}
assert len(products)==3 and bake['producedTextures']==9
checks=[]
for unit in manifest['units']:
    result=products[unit['id']]
    check=json.loads((OUT/(unit['id']+'-native-check.json')).read_text())
    assert check['nativeHeaders'] and check['completeGraph'] and check['mips']==39
    assert check['unitKey']==result['key']
    assert all(c['densityRatioX']==1 and c['densityRatioY']==1 and c['mips']==9 for c in result['channels'])
    levels=[l for l in check['levels'] if l['mip']<9]
    assert max(l['maxLinearError'] for l in levels)<0.005
    checks.append({'unit':unit['id'],'key':result['key'],'nativeMipLevels':27,
                   'pixelCentreMaxError':max(l['maxLinearError'] for l in levels),
                   'atlasBoundaryMaxError':max(l['maxOriginalAtlasBoundaryDifference'] for l in levels),
                   'boundaryDisposition':'Measured existing-atlas pattern differences retained for human observation.'})
source=OUT/'output/Lift02';incoming=[p for p in source.rglob('*') if p.is_file()]
identities={p.read_text().strip():sha(Path(str(p)[:-4]).read_bytes()) for p in DEST.rglob('*.cid')}
for p in incoming:
    dest=DEST/p.relative_to(source)
    assert not dest.exists() or p.read_bytes()==dest.read_bytes(),'Path collision'
    if p.suffix!='.cid':
        identity=Path(str(p)+'.cid').read_text().strip()
        assert identity not in identities or identities[identity]==sha(p.read_bytes()),'CID collision'
for p in incoming:
    dest=DEST/p.relative_to(source);dest.parent.mkdir(parents=True,exist_ok=True)
    if not dest.exists():dest.write_bytes(p.read_bytes())
numbers=[v for u in manifest['units'] for v in u['bounds']]
raw=struct.pack('<8i',0x314e4242,1,0,0,0,0,0,len(numbers))+struct.pack('<'+'d'*len(numbers),*numbers)
raw+=hashlib.sha256(raw).digest()
bbn=ROOT/'assets/HardcodedNumbers/Lift02/Types_Lift02_Lift02StructureRepeatUnits_cs.bin'
bbn.write_bytes(raw)
lines=['using System.Collections.Generic;','namespace BridgeBuilder.Bridges;',
       '// Measured whole indexed crossbeam charts; no receiver-dependent unit bounds.',
       '// Embedded numerical arrays: Types_Lift02_Lift02StructureRepeatUnits_cs',
       'internal static partial class MovableRepeatUnits','{',
       '    static partial void Lift02Regions(string sourceDigest,List<Region> regions)','    {',
       '        if(sourceDigest!="'+manifest['units'][0]['sourceDigest']+'")return;',
       '        var d=Lift02StructureRepeatData.Values.F64;']
for i,unit in enumerate(manifest['units']):
    n=i*4;lines.append('        regions.Add(new Region("'+products[unit['id']]['key']+'",'+','.join('d['+str(j)+']' for j in range(n,n+4))+'));')
lines.extend(['    }','    private static class Lift02StructureRepeatData','    {',
              '        internal static readonly HardcodedNumbers Values=HardcodedNumbers.Load("Types_Lift02_Lift02StructureRepeatUnits_cs");',
              '    }','}'])
(ROOT/'src/BridgeBuilder/Bridges/Types/Lift02/Lift02StructureRepeatUnits.cs').write_text('\n'.join(lines)+'\n')
local={}
for p in DEST.rglob('*.cid'):
    identity=p.read_text().strip();assert identity not in local,'Duplicate payload';local[identity]=Path(str(p)[:-4])
edges=[]
for p in DEST.rglob('*.Surface'):
    for identity in refs(p.read_bytes()):
        assert identity in local or identity in builtin,'Missing dependency'
        edges.append({'surface':p.relative_to(DEST).as_posix(),'cid':identity,'builtin':identity not in local})
files=[{'path':p.relative_to(DEST).as_posix(),'bytes':p.stat().st_size,'sha256':sha(p.read_bytes())} for p in DEST.rglob('*') if p.is_file()]
assert not any(p.is_dir() and not any(p.iterdir()) for p in DEST.rglob('*'))
report={'style':'Lift02','files':files,'totalBytes':sum(f['bytes'] for f in files),'fileCount':len(files),
        'surfaces':len(list(DEST.rglob('*.Surface'))),'textures':len(list(DEST.rglob('*.Texture'))),
        'uniquePayloadCids':len(local),'completeClosure':True,'edges':edges,'checks':checks,
        'newUnits':3,'sharedControlCid':'729893d593df5d52ba911bc1e7eab77b','structureTemplate':1,
        'bbnSha256':sha(raw),'obsoleteFormalFiles':[],
        'cleanupReason':'All previously installed resources remain referenced by existing road/common recipes. Three new units add distinct native source charts; no previous prepared structural payload is superseded.',
        'localization':'No UI text changes; technical source identifiers are intentionally unlocalized.',
        'limits':'Full grouping is finite to halfDelta0/2/8; regions do not establish complete bridge generation. Game visual acceptance and persistence checks remain separate.'}
(OUT/'integration.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:report[k] for k in ['totalBytes','fileCount','surfaces','textures','uniquePayloadCids','completeClosure']}))
