"""Measure native finite UV0 traversals, independently of generated Retile outcomes."""
import collections
import hashlib
import io
import json
import struct
import sys
import zipfile
from pathlib import Path
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
sys.path[:0] = [str(ROOT / 'tools/GeometryMetaprogram'), str(Path(__file__).parent)]
from ExportRecipes import numbers
from Lift03NativeUvCharts import meshes
from MovableUvDomainFormat import decode_arrays, write_bbn

BASE = ROOT / 'artifacts/prepared-materials/Lift03'
NUMBERS = ROOT / 'assets/HardcodedNumbers/Lift03'
OUT = BASE / 'non-tiling'


def sha(data):
    return hashlib.sha256(data).hexdigest()


def string(reader):
    length, shift = 0, 0
    while True:
        b = reader.read(1)[0]
        length |= (b & 127) << shift
        if b < 128:
            return reader.read(length).decode()
        shift += 7


def present(reader):
    value = reader.read(1)[0]
    assert value in (0, 255)
    return value == 255


def count(reader):
    return struct.unpack('<i', reader.read(4))[0] if present(reader) else 0


def cid(data):
    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)


def sources():
    source_path = BASE / 'rebuild-native-v1/input/sources.json'
    template_path = ROOT / 'tools/PrepareLanes/NativeTemplates.json'
    templates = {r['hash']: r for r in json.loads(template_path.read_text())}
    current_path = ROOT / 'artifacts/prepared-materials/shared-nontiling/native-semantics'
    current = {r['hash']: r for r in json.loads((current_path/'templates-current.json').read_text())}
    for template in (0,1):
        assert current[template]['verifiedAgainstCurrentResources']
        measured = json.loads((current_path/f'template-{template}.json').read_text())
        assert measured['m_SavedProperties'] == templates[template]['material']['m_SavedProperties']
    result = {}
    for source in json.loads(source_path.read_text())['sources']:
        with zipfile.ZipFile(source['archive']) as archive:
            raw = archive.read(source['entry'])
            assert sha(raw) == source['sha256']
            assert archive.read(source['entry'] + '.cid').decode().strip() == source['cid']
        reader = io.BytesIO(raw)
        version, template, vt = struct.unpack('<Hi?', reader.read(7))
        assert version == 1
        header = reader.read(276) if vt else None
        surface = cid(reader.read(16)) if vt and present(reader) else None
        properties = {}
        for name, size, fmt in [('float',4,'<f'),('int',4,'<i'),('vector',16,'<4f'),('color',16,'<4f')]:
            properties[name] = {}
            for _ in range(count(reader)):
                key = string(reader)
                properties[name][key] = struct.unpack(fmt, reader.read(size))
        bindings = {}
        for _ in range(count(reader)):
            key = string(reader)
            if present(reader): bindings[key] = cid(reader.read(16))
        keywords = [string(reader) for _ in range(count(reader))]
        assert reader.tell() == len(raw)
        native = templates.get(template)
        safe = native is not None and template in (0, 1) and vt and all(k == '_Snow' for p in properties.values() for k in p)
        safe = safe and set(keywords) <= {'_TANGENTSPACE_OCTO'}
        semantics = None
        if safe:
            saved = native['material']['m_SavedProperties']
            floats, colors = dict(saved['m_Floats']), dict(saved['m_Colors'])
            assert all(floats[k] == 0 for k in ('_UVBase', '_UVDetail', '_UVEmissive'))
            assert colors['_UV0Scale'] == dict(r=1.,g=1.,b=0.,a=0.)
            assert colors['_UV0Offset'] == dict(r=0.,g=0.,b=0.,a=0.)
            assert colors['_UVMappingMask'] == dict(r=1.,g=0.,b=0.,a=0.)
            semantics = dict(uvChannel=0, scale=[1,1], offset=[0,0], axes=['U','V'],
                             shader=native['material']['m_Shader'], nativeVT=True,
                             sourceUvTransform='identity; no local-region frac/fold')
        if template == 2 and not vt and all(not p for p in properties.values()) and set(keywords) <= {'_TANGENTSPACE_OCTO'}:
            assert current[2]['verifiedAgainstCurrentResources']
            window_template = json.loads((current_path/'template-2.json').read_text())
            assert window_template['m_Shader']['m_PathID'] == 550
            semantics = dict(shader=window_template['m_Shader'],nativeVT=False,
                axes=['UV0.Y','UV0.Z'],orientation='UV0.W',selection='COLOR',
                sourceUvTransform='native per-window local coordinates; complete UV0/COLOR retained',
                requiresLocalFaceEvidence=True)
        refs = []
        if header:
            for stack in range(struct.unpack_from('<i', header)[0]):
                for layer in range(4):
                    identity = cid(header[76+stack*136+layer*16:92+stack*136+layer*16])
                    if identity != '0'*32: refs.append(identity)
        result[source['cid']] = dict(**source, properties=properties, keywords=keywords,
            textureBindings=bindings, preprocessedTextures=refs, vtSurface=surface, semantics=semantics)
    return result, dict(path=str(template_path.relative_to(ROOT)), sha256=sha(template_path.read_bytes()),
        currentTemplates={str(k):current[k] for k in (0,1,2)},
        currentEvidenceSha256=sha((current_path/'templates-current.json').read_bytes()),
        nativeSurfaceLoaderSha256=sha((current_path/'SurfaceAsset.cs').read_bytes()))


def repeated_chart_seams(positions, uv, faces, face_charts, native_charts):
    """Conservatively reject repeated chart traversals meeting at a physical UV seam.

    Separate, disconnected members may reuse a chart. Their shared UVs do not
    establish tiling along either member. Only physically coincident seam edges
    with coplanar consistently oriented faces, a translated reset of the same
    region, and physical UV-continuous component membership qualify. Small
    atlas seam offsets are not a repeat. Near-period resets stay unresolved.
    """
    _, vertex_ids = np.unique(positions, axis=0, return_inverse=True)
    edges = faces[:, [[0,1],[1,2],[2,0]]].reshape(-1,2)
    ids = vertex_ids[edges]
    reverse = ids[:,0] > ids[:,1]
    edges[reverse] = edges[reverse, ::-1]
    ids.sort(axis=1)
    order = np.lexsort((ids[:,1],ids[:,0]))
    sorted_ids = ids[order]
    paired = np.all(sorted_ids[1:] == sorted_ids[:-1],axis=1)
    first, second = order[:-1][paired], order[1:][paired]
    charts = np.repeat(face_charts,3)
    same_chart = (charts[first] >= 0) & (charts[first] == charts[second])
    first, second = first[same_chart], second[same_chart]
    different_uv = np.any(uv[edges[first]] != uv[edges[second]],axis=(1,2))
    p = positions[faces].astype(np.float64)
    normal = np.cross(p[:,1]-p[:,0],p[:,2]-p[:,0])
    a,b = normal[first//3],normal[second//3]
    dot = np.sum(a*b,axis=1)
    lengths = np.sum(a*a,axis=1)*np.sum(b*b,axis=1)
    coplanar = (dot > 0) & (dot*dot >= lengths*(1-1e-12))
    delta = uv[edges[first]].astype(np.float64)-uv[edges[second]].astype(np.float64)
    translated_reset = np.all(delta[:,0] == delta[:,1],axis=1)
    spans = np.asarray([c['bounds'][2:] for c in native_charts])-np.asarray([c['bounds'][:2] for c in native_charts])
    extent = spans[charts[first]]
    full_period = np.any((np.abs(delta[:,0]) == extent) & (extent > 0),axis=1)
    near_period = np.any((np.abs(delta[:,0]) > extent*.5) & (extent > 0),axis=1)
    candidate = different_uv & coplanar & translated_reset & (reverse[first] != reverse[second])
    reset = candidate & full_period
    ambiguous = candidate & near_period & ~full_period
    # The chart table interns equal regions on disconnected members. Never use
    # that alias identity to propagate a repeat decision between those members.
    _, native_vertex = np.unique(np.concatenate((positions,uv),axis=1),axis=0,return_inverse=True)
    native_edges = native_vertex[edges]
    native_edges.sort(axis=1)
    native_order = np.lexsort((native_edges[:,1],native_edges[:,0]))
    sorted_edges = native_edges[native_order]
    continuous = np.all(sorted_edges[1:] == sorted_edges[:-1],axis=1)
    continuous &= charts[native_order[:-1]] == charts[native_order[1:]]
    parent = list(range(len(faces)))
    def root(v):
        while parent[v] != v:
            parent[v] = parent[parent[v]]
            v = parent[v]
        return v
    for a,b in zip(native_order[:-1][continuous]//3,native_order[1:][continuous]//3):
        parent[root(int(a))] = root(int(b))
    roots = np.asarray([root(i) for i in range(len(faces))])
    repeated_roots = set(map(int,roots[np.concatenate((first[reset],second[reset]))//3]))
    ambiguous_roots = set(map(int,roots[np.concatenate((first[ambiguous],second[ambiguous]))//3]))-repeated_roots
    repeated_faces = np.isin(roots,list(repeated_roots))
    ambiguous_faces = np.isin(roots,list(ambiguous_roots))
    examples = {}
    for e,f in zip(first[reset|ambiguous],second[reset|ambiguous]):
        chart = int(charts[e])
        if chart not in examples:
            gradients = []
            for face in (e//3,f//3):
                xyz = positions[faces[face]].astype(float)
                q = uv[faces[face]].astype(float)
                gradients.append(np.linalg.lstsq(xyz[1:]-xyz[0],q[1:]-q[0],rcond=None)[0].T.tolist())
            examples[chart] = dict(chart=chart,faces=[int(e//3),int(f//3)],
                fullChartPeriod=bool(np.any((np.abs(uv[edges[e][0]].astype(float)-uv[edges[f][0]].astype(float)) == spans[chart]) & (spans[chart]>0))),
                chartUvSpan=spans[chart].tolist(),
                sourceUvGradients=gradients,uvTriangles=uv[faces[[e//3,f//3]]].tolist(),
                positions=positions[edges[e]].tolist(),uvBefore=uv[edges[e]].tolist(),uvAfter=uv[edges[f]].tolist(),
                sourceTriangles=positions[faces[[e//3,f//3]]].tolist())
    return repeated_faces, ambiguous_faces, int(np.count_nonzero(reset)), list(examples.values())


def main():
    from shapely import polygons, box, intersects
    verified, template_evidence = sources()
    fixture = ROOT / 'artifacts/uv-density/layout-plans.bin'
    payload = fixture.read_bytes()
    report = json.loads((BASE / 'native-charts/report.json').read_text())
    assert sha(payload) == report['fixtureSha256']
    roads = json.loads((BASE / 'repeat-units-v2/unit-evidence.json').read_text())
    repeat_regions = [box(*u['min'], *u['max']) for u in roads['units']]
    atlas_ids = set(verified[roads['sourceCid']]['preprocessedTextures'])
    encoded, records = [1,len(report['streams'])], []
    for render,lod,mesh,uv,faces,positions in meshes(payload):
        stream = render.replace(' ','_') + '_' + str(mesh)
        native = json.loads((BASE / 'native-charts' / (stream+'.json')).read_text())
        assert sha(uv.tobytes()) == native['nativeUvDigest']
        assert sha(faces.tobytes()) == native['nativeIndicesDigest']
        assert sha(positions.tobytes()) == native['nativePositionDigest']
        recipe = NUMBERS / ('UvDomain_'+stream+'.bin')
        values = numbers(recipe)
        ints, _ = decode_arrays(values['I32'],values['F32'])
        assert ints[1] == len(native['domains'])
        source = verified[native['sourceCid']]
        domain_faces, at = [], 2
        face_charts = np.full(len(faces),-1,dtype=np.int32)
        for domain in native['domains']:
            n = abs(int(ints[at+3])); at += 4
            members = np.asarray(ints[at:at+n],dtype=np.int32); at += n
            domain_faces.append(members)
            assert len(members) == domain['sourceFaceCount']
            if len(domain['charts']) == 1: face_charts[members] = domain['charts'][0]
        assert at == len(ints)
        window_evidence = None
        window_local = set()
        if source['template'] == 2 and source['semantics']:
            window_path = ROOT/'artifacts/prepared-materials/shared-nontiling/windows'/f'Lift03-{render.replace(" ","_")}-{mesh}.json'
            window = json.loads(window_path.read_text())
            assert sha(Path(window['geometry']).read_bytes()) == window['geometrySha256']
            wuv,colors = np.asarray(window['uv0']),np.asarray(window['colors'])
            assert np.array_equal(np.asarray(window['indices']),faces)
            assert np.array_equal(wuv[:,:2],uv)
            wt,wc = wuv[faces],colors[faces]
            local = np.all((wt[:,:,1:3]>=0)&(wt[:,:,1:3]<=1),axis=(1,2))
            local &= np.all(wt[:,:,3]==wt[:,:1,3],axis=1)
            local &= np.all(wc==wc[:,:1,:],axis=(1,2))
            window_local = {i for i,members in enumerate(domain_faces) if np.all(local[members])}
            assert window_local == set(window['sourceLocalDomains'])
            window_evidence = dict(path=str(window_path.relative_to(ROOT)),sha256=sha(window_path.read_bytes()),
                geometrySha256=window['geometrySha256'],sourceLocalDomains=sorted(window_local),
                remainingDomains=sorted(set(range(len(domain_faces)))-window_local))
        repeated, ambiguous, seam_count, reset_examples = repeated_chart_seams(positions,uv,faces,face_charts,native['charts'])
        tri = uv[faces].astype(np.float64)
        invalid = ~np.isfinite(tri).all(axis=(1,2))
        outside = np.any((tri < 0)|(tri > 1),axis=(1,2))
        road_overlap = np.zeros(len(faces),dtype=bool)
        if atlas_ids & set(source['preprocessedTextures']):
            geometry = polygons(tri)
            for region in repeat_regions: road_overlap |= intersects(geometry,region)
        groups, flags = collections.defaultdict(list), []
        for domain,members in zip(native['domains'],domain_faces):
            if source['semantics'] is None:
                reason = 'unknown-native-sampling'
            elif source['template'] == 2:
                reason = 'non-tiling-native-window-local-map' if domain['domain'] in window_local else 'unknown-window-local-domain'
            elif np.any(invalid[members]):
                reason = 'unknown-nonfinite-source-uv'
            elif np.any(outside[members]):
                reason = 'unknown-source-uv-outside-atlas'
            elif np.any(repeated[members]):
                reason = 'density-physical-component-region-repetition'
            elif np.any(ambiguous[members]):
                reason = 'unknown-near-period-source-component-reset'
            else:
                reason = 'non-tiling-native-finite-uv0-traversal'
            flags.append(int(reason.startswith('non-tiling-')))
            groups[reason].append(domain['domain'])
        encoded.extend([len(flags),*flags])
        records.append(dict(render=render,mesh=mesh,lod=lod,stream=stream,geometryCid=native['geometryCid'],
            sourceCid=native['sourceCid'],sourceSha256=source['sha256'],domainCount=len(flags),
            nativeUvDigest=native['nativeUvDigest'],nativePositionDigest=native['nativePositionDigest'],
            nativeIndicesDigest=native['nativeIndicesDigest'],domainSha256=sha(recipe.read_bytes()),
            chartFileSha256=sha((BASE/'native-charts'/(stream+'.json')).read_bytes()),
            chartCount=len(native['charts']),physicalUvResetEdges=seam_count,
            repeatedComponentFaces=np.flatnonzero(repeated).tolist(),ambiguousComponentFaces=np.flatnonzero(ambiguous).tolist(),
            resetExamples=reset_examples,
            sourceUvMin=uv.min(axis=0).tolist(),sourceUvMax=uv.max(axis=0).tolist(),
            sourceUvAxes=source['semantics']['axes'] if source['semantics'] else [],windowEvidence=window_evidence,
            uvCorrections=[],classifications=dict(groups)))
        print(stream,dict(collections.Counter({k:len(v) for k,v in groups.items()})),flush=True)
    assert len(records) == len(report['streams'])
    resource = NUMBERS / 'Lift03UvSampling.bin'
    raw = write_bbn(encoded,[])
    resource.write_bytes(raw)
    assert list(numbers(resource)['I32']) == encoded
    code = '''// Generated by tools/PreparedMaterials/Lift03/BuildUvSampling.py.
// Embedded numerical arrays: Lift03UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class Lift03UvSampling
{
    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
'''
    code += ''.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},\n' for i,r in enumerate(records))
    code += '''            _ => -1
        };
        if (index < 0) return false;
        var flags = Data.Items[index];
        if ((uint)domain >= (uint)flags.Length)
            throw new InvalidOperationException("Lift03 UV sampling domain mismatch.");
        return flags[domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly bool[][] Items = Read();
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("Lift03UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != 20)
                throw new InvalidOperationException("Invalid Lift03 UV sampling schema.");
            var result = new bool[values[1]][];
            int at = 2;
            for (int mesh = 0; mesh < result.Length; mesh++)
            {
                int count = values[at++];
                var flags = result[mesh] = new bool[count];
                for (int d = 0; d < count; d++)
                {
                    int flag = values[at++];
                    if (flag != 0 && flag != 1)
                        throw new InvalidOperationException("Invalid Lift03 UV sampling flag.");
                    flags[d] = flag == 1;
                }
            }
            if (at != values.Length)
                throw new InvalidOperationException("Invalid Lift03 UV sampling length.");
            return result;
        }
    }
}
'''
    (ROOT/'src/BridgeBuilder/Bridges/Types/Lift03/Lift03UvSampling.cs').write_text(code)
    OUT.mkdir(parents=True,exist_ok=True)
    totals=collections.Counter()
    for r in records:
        totals.update({k:len(v) for k,v in r['classifications'].items()})
    evidence=dict(style='Lift03',schema='native-uv-traversal/1',fixtureSha256=sha(payload),
        templateEvidence=template_evidence,sources=verified,streams=records,totals=dict(totals),
        resourceBytes=len(raw),resourceSha256=sha(raw),texturesCreated=0,geometryChanged=False,
        constructed='Raw original UVs; Plan.Construction barycentric interpolation on Fragment source triangle. LOD2 supplement uses LOD1 source stream.',
        interpretation='Finite UV0 atlas traversal, not texture-pixel aperiodicity. Disconnected repeated chart uses do not imply along-part tiling.',
        nativeGameRenderingVerified=False)
    (OUT/'classification.json').write_text(json.dumps(evidence,indent=2)+'\n')
    print(json.dumps(dict(totals=totals,resourceBytes=len(raw),resourceSha256=sha(raw)),indent=2))


if __name__ == '__main__':
    main()
