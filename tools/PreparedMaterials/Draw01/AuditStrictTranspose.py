"""Audit only exact A-preserving B/C transposes in current Draw01 samplers."""
import collections
import hashlib
import json
import struct
from pathlib import Path
from AuditStructureReuse import bindings

root=Path(__file__).resolve().parents[3]
inventory=root/'artifacts/uv-density/structure-tessellated-sources.json'
payload=root/'artifacts/prepared-current-structures-canonical/Draw01'
destination=root/'artifacts/prepared-materials/Draw01/strict-transpose-audit'
units=[u for u in json.loads(inventory.read_text())['units'] if u['style']=='Draw01']
triangles=[u for u in units if u['triangle']]
assert {u['key'] for u in units}=={p.name for p in (payload/'Structure').iterdir()}
def bits(values):return struct.pack('<2f',*values)
def key(u):return (u['sourceDigest'],bits(u['a']),bits(u['b']),bits(u['c']))
index=collections.defaultdict(list)
for unit in triangles:index[key(unit)].append(unit)
exact=[]
for unit in triangles:
    source,a,b,c=key(unit)
    if b==c:continue
    for other in index.get((source,a,c,b),[]):
        if unit['key']<other['key']:exact.append((unit,other))
def channel_info(u):
    template,channels=bindings(payload/'Structure'/u['key']/'Structure.Surface')
    result={}
    for name,cid in channels.items():
        path=payload/'Shared'/(cid+'.Texture');raw=path.read_bytes()
        version,w,h,depth,mips,fmt=struct.unpack_from('<HHHHBB',raw)
        assert version==5 and depth==1
        result[name]=dict(cid=cid,bytes=len(raw),format=fmt,mips=[[max(1,w>>i),max(1,h>>i)] for i in range(mips)])
    return template,result
pairs=[]
for old,new in exact:
    oldtemplate,a=channel_info(old);newtemplate,b=channel_info(new)
    pairs.append(dict(first=old['key'],second=new['key'],sourceDigest=old['sourceDigest'],
        sourceAFloat32Bits=bits(old['a']).hex(),uvAffineRows=[[0,1,0],[1,0,0]],
        templatesMatch=oldtemplate==newtemplate,channelNamesMatch=set(a)==set(b),firstChannels=a,secondChannels=b,
        allMipDimensionsTranspose=set(a)==set(b) and all(a[n]['mips']==[list(reversed(s)) for s in b[n]['mips']] for n in a),
        approved=False))
prior=json.loads((root/'artifacts/prepared-materials/Draw01/structure-reuse-audit.json').read_text())
old=prior['permutationReuse']['candidates']
oldmaps=collections.Counter(tuple(v for row in p['uvAffineRows'] for v in row) for p in old)
strictold=[p for p in old if p['uvAffineRows']==[[0,1,0],[1,0,0]]]
result=dict(style='Draw01',inventory=str(inventory.relative_to(root)),inventorySHA256=hashlib.sha256(inventory.read_bytes()).hexdigest(),
    units=len(units),triangles=len(triangles),rectangleUnits=len(units)-len(triangles),
    floatMatching='Bit-exact serialized Float32 pairs; same full sourceDigest; A unchanged; B and C exchanged only. No rounding, epsilon, relabeling or integer shear.',
    orderedSamplerDuplicateGroups=sum(len(v)>1 for v in index.values()),strictTransposePairs=len(pairs),pairs=pairs,
    removableTexturePayloadBytes=0 if not pairs else None,removableTextureSidecarBytes=0 if not pairs else None,
    removablePackageEntryBytes=0 if not pairs else None,
    priorGeneralPermutationPairs=len(old),priorStrictTransposePairs=len(strictold),
    priorMappingCounts=[dict(rows=[list(k[:3]),list(k[3:])],pairs=v) for k,v in oldmaps.items()],
    previousProbeCoversStrictTranspose=False if not strictold else None,
    conclusion='No strict-transpose pair exists in the current Draw01 inventory. The previous arbitrary-permutation GPU rejection does not cover or reject strict transposition.',
    limitations='No new bake or GPU run. An absent pair yields zero potential removal; no synthetic example is presented as real inventory. UV transposition alone does not certify block-compressed pixel or mip-chain transposition.')
destination.mkdir(parents=True,exist_ok=True)
(destination/'audit.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps(result,indent=2))
