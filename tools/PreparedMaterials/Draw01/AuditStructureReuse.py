"""Read-only Draw01 structural sampling and existing-payload reuse audit."""
import argparse
import collections
import hashlib
import io
import json
import struct
from pathlib import Path
import numpy as np


def text(reader):
    count, shift = 0, 0
    while True:
        value = reader.read(1)[0]
        count |= (value & 127) << shift
        if not value & 128:
            return reader.read(count).decode()
        shift += 7


def count(reader):
    marker = reader.read(1)[0]
    if marker == 0:
        return 0
    if marker != 255:
        raise ValueError('Invalid native collection marker')
    return struct.unpack('<i', reader.read(4))[0]


def bindings(path):
    reader = io.BytesIO(path.read_bytes())
    version, template, vt = struct.unpack('<Hi?', reader.read(7))
    assert version == 1 and not vt
    for size in (4, 4, 16, 16):
        for _ in range(count(reader)):
            text(reader)
            reader.read(size)
    result = {}
    for _ in range(count(reader)):
        name = text(reader)
        assert reader.read(1) == b'\xff'
        result[name] = ''.join(f'{b & 15:x}{b >> 4:x}' for b in reader.read(16))
    return template, result


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('report', type=Path)
    args = p.parse_args()
    root = args.repository
    inventory = root/'artifacts/uv-density/structure-tessellated-sources.json'
    units = [u for u in json.loads(inventory.read_text())['units'] if u['style'] == 'Draw01']
    payload = root/'artifacts/prepared-current-structures-canonical/Draw01'
    assert {u['key'] for u in units} == {q.name for q in (payload/'Structure').iterdir()}
    textures = {}
    by_hash = collections.defaultdict(list)
    for path in (payload/'Shared').glob('*.Texture'):
        data = path.read_bytes()
        version, width, height = struct.unpack_from('<HHH', data)
        assert version == 5
        cid = path.with_suffix('.Texture.cid').read_text().strip()
        sha = hashlib.sha256(data).hexdigest()
        textures[cid] = dict(bytes=len(data), sha256=sha, width=width, height=height,
                             path=str(path.relative_to(root)))
        by_hash[sha].append(cid)
    refs = collections.Counter()
    channel_stats = collections.defaultdict(set)
    groups = {name: collections.defaultdict(list) for name in ('sameSampler', 'swapBC', 'anyPermutation', 'sameBounds')}
    details = []
    for unit in units:
        material = payload/'Structure'/unit['key']/'Structure.Surface'
        template, channels = bindings(material)
        for channel, cid in channels.items():
            assert cid in textures
            refs[cid] += 1
            channel_stats[channel].add(cid)
        size = sum(textures[cid]['bytes'] for cid in channels.values())
        details.append(dict(**unit, template=template, textureBytes=size, channels=channels))
        digest = unit['sourceDigest']
        a, b, c = (tuple(unit[n]) for n in ('a', 'b', 'c'))
        if unit['triangle']:
            groups['sameSampler'][(digest, True, a, b, c)].append(unit['key'])
            groups['swapBC'][(digest, a, tuple(sorted((b, c))))].append(unit['key'])
            groups['anyPermutation'][(digest, tuple(sorted((a, b, c))))].append(unit['key'])
            bounds = tuple(min(v[i] for v in (a,b,c)) for i in range(2)) + tuple(max(v[i] for v in (a,b,c)) for i in range(2))
        else:
            bounds = tuple(unit['bounds'])
            groups['sameSampler'][(digest, False, bounds)].append(unit['key'])
        groups['sameBounds'][(digest, bounds)].append(unit['key'])
    detail_by_key = {d['key']: d for d in details}
    grouped = {}
    for name, samples in groups.items():
        repeated = [keys for keys in samples.values() if len(keys) > 1]
        grouped[name] = dict(groups=len(repeated), units=sum(map(len,repeated)),
            groupsDetails=[dict(keys=keys, referencedTextureBytes=sum(detail_by_key[k]['textureBytes'] for k in keys))
                           for keys in sorted(repeated,key=lambda ks:sum(detail_by_key[k]['textureBytes'] for k in ks),reverse=True)])
    duplicates = [ids for ids in by_hash.values() if len(ids)>1]
    # Vertex permutations are integer barycentric transforms, not a tolerance
    # match or guessed common bounding rectangle. Exercise the shader's exact
    # frac/reflection rule, including its normal-map reverse-half condition.
    footprint = json.loads((root/'artifacts/uv-density/structure-draw01-footprint.json').read_text())
    dimensions = {s['cid']: [(l['width'], l['height']) for b in s['backing'] for l in b['layers']]
                  for s in footprint['sources']}
    q = np.r_[np.random.default_rng(4).uniform(-8, 8, (10000, 2)),
              np.array([(x/8,y/8) for x in range(-16,17) for y in range(-16,17)])]
    def sample(t, coordinates):
        phase = coordinates-np.floor(coordinates)
        reverse = phase.sum(axis=1)>1
        phase[reverse] = 1-phase[reverse]
        return t[0]+phase@(t[1:]-t[0]), reverse
    permutations=[]
    replacement={}
    for ids in groups['anyPermutation'].values():
        if len(ids)<2:
            continue
        keeper=min(ids,key=lambda k:detail_by_key[k]['textureBytes'])
        keep=detail_by_key[keeper]
        target=np.array([keep[n] for n in ('a','b','c')])
        for oldkey in ids:
            if oldkey==keeper:
                continue
            old=detail_by_key[oldkey]
            source=np.array([old[n] for n in ('a','b','c')])
            order=[next(i for i in range(3) if np.array_equal(source[i],v)) for v in target]
            weights=np.array([[-1,-1,1],[1,0,0],[0,1,0]])
            affine=weights[order[1:]]
            transformed=q@affine[:,:2].T+affine[:,2]
            before, normal_before=sample(source,q)
            after, normal_after=sample(target,transformed)
            error=float(abs(before-after).max())
            # At exact repeated-domain edges, frac selects opposite endpoints;
            # equality on the open domain is the relevant native texture sample.
            interior=np.all(abs(q-np.round(q))>1e-10,axis=1)&(abs(q.sum(axis=1)-np.round(q.sum(axis=1)))>1e-10)
            inner_error=float(abs(before[interior]-after[interior]).max())
            reverse_equal=bool(np.array_equal(normal_before[interior],normal_after[interior]))
            assert inner_error<1e-10 and reverse_equal
            t=affine[:,:2].astype(float)
            density=[]; relative_density=[]
            for width,height in dimensions[old['sourceCid']]:
                native=(source[1:]-source[0]).T*np.array([width,height])[:,None]
                for cid in keep['channels'].values():
                    tex=textures[cid]
                    generated=np.diag([tex['width'],tex['height']])@t
                    density.append(float(np.linalg.eigvalsh(generated.T@generated-native.T@native).min()))
                    relative_density.append(density[-1]/max(1,float(np.linalg.eigvalsh(native.T@native).max())))
            # Existing canonical crop must retain the source-pixel metric in
            # every direction, not only total area or texture dimensions.
            assert min(relative_density)>-1e-6, (oldkey,keeper,min(relative_density))
            replacement[oldkey]=keeper
            permutations.append(dict(old=oldkey,keeper=keeper,uvAffineRows=affine.tolist(),
                openDomainMaxSourceUvError=inner_error,boundaryMaxSourceUvError=error,
                identicalNormalReversePhase=reverse_equal,minPixelMetricEigenvalue=min(density),
                minRelativePixelMetricDifference=min(relative_density)))
    before_cids={c for d in details for c in d['channels'].values()}
    after_cids={c for d in details if d['key'] not in replacement for c in d['channels'].values()}
    removed_cids=before_cids-after_cids
    rectangles=[d for d in details if not d['triangle']]
    contained=[]
    for unit in details:
        if not unit['triangle']:
            continue
        choices=[r for r in rectangles if r['sourceDigest']==unit['sourceDigest'] and
                 all(r['bounds'][0]<=point[0]<=r['bounds'][2] and r['bounds'][1]<=point[1]<=r['bounds'][3]
                     for point in (unit['a'],unit['b'],unit['c']))]
        if choices:
            keeper=min(choices,key=lambda r:r['textureBytes'])
            contained.append(dict(triangle=unit['key'],rectangle=keeper['key'],textureBytes=unit['textureBytes']))
    contained_keys={u['triangle'] for u in contained}
    remaining_rectangle_cids={c for d in details if d['key'] not in contained_keys for c in d['channels'].values()}
    rectangle_candidate_cids=before_cids-remaining_rectangle_cids
    entries = json.loads((root/'artifacts/structure-package-assessment/package.json').read_text())['entries']
    selected = [e for e in entries if e['FullName'].replace('\\','/').startswith('Materials/Draw01/Shared/')]
    report = dict(style='Draw01', inventory=str(inventory.relative_to(root)), inventorySHA256=hashlib.sha256(inventory.read_bytes()).hexdigest(),
        units=len(units), triangularUnits=sum(u['triangle'] for u in units), rectangularUnits=sum(not u['triangle'] for u in units),
        textureFiles=len(textures), textureBytes=sum(t['bytes'] for t in textures.values()),
        exactDuplicatePayloadGroups=duplicates, exactDuplicateRemovableBytes=sum(sum(textures[c]['bytes'] for c in ids[1:]) for ids in duplicates),
        unreferencedTextures=[c for c in textures if not refs[c]],
        channelSummary={name:dict(uniqueTextures=len(ids),bytes=sum(textures[c]['bytes'] for c in ids)) for name,ids in channel_stats.items()},
        candidateZipSharedBytes=sum(e['CompressedLength'] for e in selected), samplerGroups=grouped,
        permutationReuse=dict(candidates=permutations,removedTextures=len(removed_cids),
            payloadBytes=sum(textures[c]['bytes'] for c in removed_cids),
            zipEntryBytes=sum(e['CompressedLength'] for e in selected if Path(e['FullName']).name.split('.')[0] in removed_cids),
            sourceMetricEvidence='Existing immutable source-CID dimensions from structure-draw01-footprint.json; not its stale sampler/byte totals.'),
        rectangularCoverageCandidates=dict(triangles=len(contained),candidates=sorted(contained,key=lambda x:x['textureBytes'],reverse=True),
            conditionalPayloadBytes=sum(textures[c]['bytes'] for c in rectangle_candidate_cids),
            limit='Source triangle containment only; target receiver coordinates, repeated-domain boundaries and normal phases are not proven. These bytes are not approved savings.'),
        largestUnits=sorted(details,key=lambda d:d['textureBytes'],reverse=True)[:30],
        limits='No bake, geometry change, texture decode or asset mutation. Exact duplicates compare full serialized bytes. Permutations verify source lookup and normal reflection only away from seams; filtering/mip seam equivalence and target Float32 conditioning require integration checks. Rectangle containment alone does not prove equivalence.')
    args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k not in ('samplerGroups','largestUnits','exactDuplicatePayloadGroups','permutationReuse','rectangularCoverageCandidates')},indent=2))
    print(json.dumps({k:v for k,v in report['permutationReuse'].items() if k!='candidates'},indent=2))
    print(json.dumps({k:v for k,v in report['rectangularCoverageCandidates'].items() if k!='candidates'},indent=2))
    print(json.dumps({name:{k:v for k,v in value.items() if k!='groupsDetails'} for name,value in grouped.items()},indent=2))


if __name__ == '__main__':
    main()
