"""Separate Draw01 source texture identity from sampler identity without baking."""
import collections
import hashlib
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from AuditStructureReuse import bindings

root = Path(__file__).resolve().parents[3]
out = root / 'artifacts/prepared-materials/Draw01/source-sharing'
out.mkdir(parents=True, exist_ok=True)
game = Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content')
inventory_path = root / 'artifacts/uv-density/structure-tessellated-sources.json'
inventory = json.loads(inventory_path.read_text())
units = [u for u in inventory['units'] if u['style'] == 'Draw01']
primary = '97291ab7f7938fc35cc670b15f2bca36'
footprints = {s['cid']: s for s in json.loads((root / 'artifacts/uv-density/structure-draw01-footprint.json').read_text())['sources']}
required = set(u['sourceCid'] for u in units)
required.update(b['cid'] for cid in list(required) for b in footprints[cid]['backing'])
sources = {}
for family in ('Game', 'BridgesAndPorts'):
    for archive in (game / family).glob('*.cok'):
        if not archive.name.startswith(('Blob', 'VT')):
            continue
        with zipfile.ZipFile(archive) as z:
            for entry in z.infolist():
                if not entry.filename.endswith('.cid'):
                    continue
                cid = z.read(entry).decode().strip()
                if cid not in required:
                    continue
                payload = z.getinfo(entry.filename[:-4])
                data = z.read(payload)
                record = dict(cid=cid, archive=str(archive), entry=payload.filename,
                              bytes=len(data), archiveCompressedBytes=payload.compress_size,
                              sha256=hashlib.sha256(data).hexdigest(), builtin=True)
                if cid in sources:
                    assert sources[cid]['sha256'] == record['sha256']
                sources[cid] = record
assert required == sources.keys()

def bitvector(values):
    return [f'{struct.unpack("<I", struct.pack("<f", v))[0]:08x}' for v in values]

def shape(u):
    triangle = u.get('triangle', u.get('Triangle'))
    names = ('a', 'b', 'c') if triangle else ('bounds',)
    return (triangle,) + tuple(v for name in names for v in bitvector(u[name]))

observations = json.loads((root / 'artifacts/prepared-materials/Draw01/receiver-plans.json.receivers.json').read_text())
by_shape = collections.defaultdict(list)
for o in observations:
    by_shape[shape(o)].append(o)
records = []
primary_textures = set()
for u in units:
    assert sources[u['sourceCid']]['sha256'] == u['sourceDigest']
    material = root / 'assets/Materials/Draw01/Structure' / u['key'] / 'Structure.Surface'
    _, channels = bindings(material)
    observed = by_shape[shape(u)]
    if u['sourceCid'] == primary:
        primary_textures.update(channels.values())
    records.append(dict(key=u['key'], sourceMaterial=u['sourceCid'], sourceDigest=u['sourceDigest'],
        textureResources=[b['cid'] for b in footprints[u['sourceCid']]['backing']],
        mode='triangle-periodic-reflection' if u['triangle'] else 'rectangle-periodic',
        float32Bits={name: bitvector(u[name]) for name in ('a', 'b', 'c', 'bounds')},
        originalSampling=u, oldPreparedTextures=channels,
        receiverEvidence=dict(observations=len(observed), directlyReusable=sum(o['canUseSource'] for o in observed),
            requiresLocalSampling=sum(not o['canUseSource'] for o in observed),
            note='Shape-matched prior production observations; not a new current-build or universal-width test.')))

# Concrete interpolation test: vertex-wise fractional/reflected UVs cannot express
# a fold within an uncut rendered triangle. Preserve a real receiver as evidence.
example = None
for u in units:
    if u['sourceCid'] != primary or not u['triangle']:
        continue
    a,b,c = np.array([u[n] for n in ('a','b','c')],dtype=np.float32).astype(float)
    def fold(q):
        q = q - np.floor(q)
        return np.where((q.sum(axis=-1)>1)[...,None],1-q,q)
    def atlas(q):
        return a+q[...,0,None]*(b-a)+q[...,1,None]*(c-a)
    for o in by_shape[shape(u)]:
        if o['canUseSource'] or o['delta'] != 0:
            continue
        points = np.array(o['points'], dtype=np.float32).astype(float)
        for index in range(0,len(points),3):
            tri = points[index:index+3]
            if len(tri) != 3:
                continue
            center = tri.mean(axis=0)
            expected = atlas(fold(center))
            vertex_only = atlas(fold(tri)).mean(axis=0)
            error = float(np.linalg.norm((expected-vertex_only)*4096))
            if error > 1:
                example = dict(key=u['key'],sourceMaterial=primary,render=o['name'],lod=o['lod'],delta=o['delta'],
                    scenario=o['scenario'],triangleLocalUv=tri.tolist(),barycentric=[1/3]*3,
                    expectedAtlasUv=expected.tolist(),vertexFoldInterpolatedAtlasUv=vertex_only.tolist(),
                    atlasTexelDistance=error,reflectionAtCenter=bool((center-np.floor(center)).sum()>1),
                    note='Analytical mapping discrepancy, not a rendered image or GPU color tolerance.')
                break
        if example:
            break
    if example:
        break
assert example
native_templates=json.loads((root/'tools/PrepareLanes/NativeTemplates.json').read_text())
template=next(t for t in native_templates if t['hash']==1)
properties=template['material']['m_SavedProperties']
mapping_properties={k:[[name,value] for name,value in rows if any(s.lower() in name.lower() for s in ('uv','tile','atlas','repeat','mirror','crop'))] for k,rows in properties.items()}
source_records=[]
for cid in sorted({u['sourceCid'] for u in units}):
    source_records.append(dict(**sources[cid], template=footprints[cid]['template'],
        channels=footprints[cid]['backing'], samplers=sum(u['sourceCid']==cid for u in units)))
data=dict(schema='Draw01/SharedSourceSampling/1',style='Draw01',
    inventorySha256=hashlib.sha256(inventory_path.read_bytes()).hexdigest(),
    sources=source_records, textureResources=[sources[cid] for cid in sorted(required-{u['sourceCid'] for u in units})],
    samplers=records)
(out/'sampling-map.json').write_text(json.dumps(data,indent=2)+'\n')
primary_backing=footprints[primary]['backing']
summary=dict(status='MAPPING_COMPLETE_NATIVE_REPEAT_IMPLEMENTATION_REQUIRED',samplers=len(records),
    sourceMaterials=len(source_records),uniqueSourceTextureResources=len(data['textureResources']),
    primary=dict(cid=primary,samplers=sum(u['sourceCid']==primary for u in units),
        oldTextureFiles=len(primary_textures),oldTextureBytes=sum((root/'assets/Materials/Draw01/Shared'/f'{c}.Texture').stat().st_size for c in primary_textures),
        sourceTextureResources=[sources[b['cid']] for b in primary_backing],
        sourceTextureBytes=sum(sources[b['cid']]['bytes'] for b in primary_backing),
        packagedBytesForBuiltinDirectReferences=0),
    counterexample=example,nativeTemplateMappingProperties=mapping_properties,
    shader=template['material']['m_Shader'],
    producedTextures=0,modifiedAssets=0,deletedAssets=0,
    nativeGap='No demonstrated native atlas-subregion frac+triangular-reflection operation after interpolation, with derivative/normal behavior, in template 1. UV scale/offset or vertex folding alone fails the recorded receiver. Template property inventory is not proof that no native shader can support this.',
    remaining=['Parent native sampler/loader/key interface and verified rendering support.',
        'All-mip/channel boundary comparison using the supported native implementation.',
        'Replacement graph validation, package measurement and only then obsolete payload removal.'])
(out/'report.json').write_text(json.dumps(summary,indent=2)+'\n')
print(json.dumps({k:v for k,v in summary.items() if k not in ('nativeTemplateMappingProperties','counterexample')},indent=2))
