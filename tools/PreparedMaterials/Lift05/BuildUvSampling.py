"""Verify native UV0 atlas traversal and emit Lift05's immutable sampling policy."""
import collections
import hashlib
import io
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, domains, inputs
from ExportRecipes import numbers
from MovableUvDomainFormat import write_bbn
from shapely.geometry import MultiPoint, box

BASE = ROOT / 'artifacts/prepared-materials/Lift05'
NUMBERS = ROOT / 'assets/HardcodedNumbers/Lift05'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def string_length(reader):
    result = shift = 0
    while True:
        value = reader.read(1)[0]
        result |= (value & 127) << shift
        if value < 128:
            return result
        shift += 7


def count(reader):
    marker = reader.read(1)[0]
    if marker == 0:
        return 0
    assert marker == 255
    return struct.unpack('<i', reader.read(4))[0]


def native_properties(raw):
    reader = io.BytesIO(raw)
    version, template, vt = struct.unpack('<Hi?', reader.read(7))
    assert version == 1
    if vt:
        reader.read(276)
        marker = reader.read(1)[0]
        assert marker in (0, 255)
        if marker:
            reader.read(16)
    properties = {}
    for kind, size, fmt in [('floats', 4, '<f'), ('ints', 4, '<i'),
                            ('vectors', 16, '<4f'), ('colors', 16, '<4f')]:
        values = properties[kind] = {}
        for _ in range(count(reader)):
            key = reader.read(string_length(reader)).decode()
            values[key] = list(struct.unpack(fmt, reader.read(size)))
    return template, vt, properties


def occurrences(positions, uv, faces):
    # Physical position AND native UV identify adjacency, so disconnected atlas
    # aliases are not mistaken for one part. Constant and line maps are retained.
    _, vertices = np.unique(np.concatenate((positions, uv), axis=1), axis=0, return_inverse=True)
    parent = list(range(len(faces)))
    def find(i):
        while parent[i] != i:
            parent[i] = parent[parent[i]]
            i = parent[i]
        return i
    edges = {}
    for i, face in enumerate(vertices[faces]):
        for a, b in zip(face, np.roll(face, -1)):
            key = tuple(sorted((int(a), int(b))))
            if key in edges:
                parent[find(i)] = find(edges[key])
            else:
                edges[key] = i
    groups = collections.defaultdict(list)
    for i in range(len(faces)):
        groups[find(i)].append(i)
    return list(groups.values())


def main():
    source_path = BASE / 'rebuild-native-v1/inputs/native-inputs.json'
    source_data = json.loads(source_path.read_text())
    template_path = ROOT / 'tools/PrepareLanes/NativeTemplates.json'
    template_check = ROOT / 'artifacts/prepared-materials/shared-nontiling/native-semantics/templates-current.json'
    semantics_dir = template_check.parent
    window_template_path = semantics_dir / 'template-2.json'
    assert json.loads(window_template_path.read_text())['m_Shader'] == dict(m_FileID=0, m_PathID=550)
    assert next(t for t in json.loads(template_check.read_text()) if t['hash'] == 2)['verifiedAgainstCurrentResources']
    window_dir = semantics_dir.parent / 'windows'
    clamp_files = [semantics_dir / n for n in ('VTAtlassingInfo.cs', 'VTDatabase.cs', 'SurfaceAsset.cs', 'AtlasMaterialsDatabase.cs', 'VTAddressMode.cs')]
    assert 'VTAddressMode addressMode = VTAddressMode.Clamp' in clamp_files[0].read_text()
    assert 'return new VTAtlassingInfo(item, indexInStack)' in clamp_files[1].read_text()
    assert '(float)vtAtlassingInfo.addressMode' in clamp_files[1].read_text()
    assert 'textureStreamingSystem.ReserveTextureRect(i, biasedSize.x, biasedSize.y)' in clamp_files[2].read_text()
    assert next(t for t in json.loads(template_check.read_text()) if t['hash'] == 0)['verifiedAgainstCurrentResources']
    template = next(t for t in json.loads(template_path.read_text()) if t['hash'] == 0)
    saved = template['material']['m_SavedProperties']
    floats, colors = dict(saved['m_Floats']), dict(saved['m_Colors'])
    assert all(floats[k] == 0 for k in ('_UVBase', '_UVDetail', '_UVEmissive'))
    assert colors['_UV0Scale'] == dict(r=1., g=1., b=0., a=0.)
    assert colors['_UV0Offset'] == dict(r=0., g=0., b=0., a=0.)
    assert colors['_UVMappingMask'] == dict(r=1., g=0., b=0., a=0.)
    sources = {}
    for source in source_data['sources']:
        with zipfile.ZipFile(source['archive']) as archive:
            raw = archive.read(source['entry'])
            assert hashlib.sha256(raw).hexdigest() == source['sha256']
            assert archive.read(source['entry'] + '.cid').decode().strip() == source['cid']
        template_id, vt, properties = native_properties(raw)
        assert template_id == source['template'] and vt == source['native']['isVT']
        assert not any(properties.values())
        assert set(source['native']['keywords']) <= {'_TANGENTSPACE_OCTO'}
        sources[source['cid']] = dict(source, verifiedProperties=properties,
            nativeUv0Identity=template_id == 0, nativeClamp=template_id == 0 and vt,
            semantics='Native template 0 UV0 identity, no Surface override; finite atlas traversal.' if template_id == 0
                else 'Template 2 shader 550; only separately evidenced source-local YZ window domains qualify, retaining full UV0 and COLOR.')
    repeat_path = NUMBERS / 'Types_Lift05_Lift05RepeatRegions_cs.bin'
    repeat_values = np.asarray(numbers(repeat_path)['F64']).reshape(-1, 4)
    repeats = [box(*r) for r in repeat_values]
    # Prototype road patches are separately selected and must never inherit a
    # structural exception, even when their parent structural domain overlaps.
    road_regions = source_data['roadPatches']
    repeats += [box(*r['min'], *r['max']) for r in road_regions]
    bindings = {(b['render'], b['materialSlot']): b for b in source_data['bindings']}
    encoded, records = [1, len(bindings)], []
    for render, mesh, positions, uv, faces in inputs():
        binding = bindings[(render, mesh)]
        source = sources[binding['surfaceCid']]
        native_path = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        native = np.load(native_path)
        assert np.array_equal(native[f'mesh{mesh}_faces'], faces)
        assert np.array_equal(native[f'mesh{mesh}_uv0'][:, :2], uv)
        assert np.array_equal(native[f'mesh{mesh}_positions'][:, :3], positions)
        triangles = uv[faces].astype(float)
        window_ids, window_evidence = set(), None
        if source['template'] == 2:
            assert mesh == 1 and source['cid'] == '24cf998eda84a65dcd7334a7b0b02b90'
            window_path = window_dir / ('Lift05-' + render.replace(' ', '_') + '-1.json')
            window = json.loads(window_path.read_text())
            assert window['render'] == render and window['mesh'] == mesh
            assert sha(Path(window['geometry'])) == window['geometrySha256']
            assert np.array_equal(np.asarray(window['indices']), faces)
            full_uv = np.asarray(window['uv0'])
            assert np.array_equal(full_uv[:, :2], uv)
            assert full_uv.shape[1] == 4 and len(window['colors']) == len(full_uv)
            assert np.isfinite(full_uv).all()
            # Shader 550 uses source-local YZ coordinates. Only its explicitly
            # audited domains qualify; other procedural windows stay unknown.
            window_ids = set(window['sourceLocalDomains'])
            local_faces = set(window['sourceLocalFaces'])
            for d in window_ids:
                member_faces = domains(render, mesh)[d]
                assert set(member_faces) <= local_faces
                yz = full_uv[faces[member_faces], 1:3]
                assert np.all(yz >= 0) and np.all(yz <= 1)
            window_evidence = dict(path=str(window_path.relative_to(ROOT)), sha256=sha(window_path),
                geometrySha256=window['geometrySha256'], sourceLocalDomains=sorted(window_ids),
                uvComponents='Full UV0 XYZW and COLOR retained; source-local mapping axes are UV0 YZ.')
        components = occurrences(positions, uv, faces)
        face_part = {}
        parts = []
        for part, membership in enumerate(components):
            points = triangles[membership]
            finite = bool(np.isfinite(points).all())
            inside = bool(finite and np.all(points >= 0) and np.all(points <= 1))
            parts.append(dict(part=part, faces=membership, finiteAtlasTraversal=finite and (inside or source['nativeClamp']),
                outsideAtlas=not inside, nativeClamp=source['nativeClamp'],
                uvMin=points.min(axis=(0, 1)).tolist(), uvMax=points.max(axis=(0, 1)).tolist()))
            for face in membership:
                face_part[face] = part
        domain_path = NUMBERS / ('UvDomain_' + render.replace(' ', '_') + '_' + str(mesh) + '.bin')
        groups, flags, domain_parts = collections.defaultdict(list), [], []
        degenerate = []
        for d, membership in enumerate(domains(render, mesh)):
            tri = triangles[membership]
            part_ids = sorted({face_part[f] for f in membership})
            domain_parts.append(part_ids)
            if all(MultiPoint(t).convex_hull.area == 0 for t in tri):
                degenerate.append(d)
            if d in window_ids:
                reason = 'non-tiling-native-window-local-yz'
            elif not source['nativeUv0Identity']:
                reason = 'unresolved-native-window-semantics'
            elif not all(parts[p]['finiteAtlasTraversal'] for p in part_ids):
                reason = 'unresolved-outside-native-atlas'
            elif source['sha256'] == '40c466ebabd798b8bca9674788deefa14c10490f9f011f5377c8e48535bb984e' and any(
                    MultiPoint(t).convex_hull.intersects(r) for t in tri for r in repeats):
                reason = 'density-existing-repeat-or-road-overlap'
            else:
                reason = 'non-tiling-native-uv0-atlas'
            flags.append(int(reason in ('non-tiling-native-uv0-atlas', 'non-tiling-native-window-local-yz')))
            groups[reason].append(d)
        encoded.extend([len(flags), *flags])
        records.append(dict(render=render, mesh=mesh, materialSlot=mesh,
            lod=2 if '_LOD2' in render else 1 if '_LOD1' in render else 0,
            sourceCid=source['cid'], sourceDigest=source['sha256'], geometryCid=binding['geometryCid'],
            nativeSha256=sha(native_path), domainSha256=sha(domain_path), domainCount=len(flags),
            nonTilingAxes=['UV0.Y', 'UV0.Z'] if window_evidence else ['U', 'V'],
            windowEvidence=window_evidence, classifications=dict(groups), degenerateDomains=degenerate,
            domainParts=domain_parts, parts=parts))
    assert len(records) == len(bindings)
    resource = NUMBERS / 'Lift05UvSampling.bin'
    resource.write_bytes(write_bbn(encoded, []))
    assert list(numbers(resource)['I32']) == encoded
    code = CLASS_TEMPLATE.replace('STREAMS', ''.join(
        f'            ("{r["render"]}", {r["mesh"]}) => {i},\n' for i, r in enumerate(records)))
    code = code.replace('STREAM_COUNT', str(len(records)))
    (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift05/Lift05UvSampling.cs').write_text(code)
    totals = collections.Counter()
    for record in records:
        totals.update({k: len(v) for k, v in record['classifications'].items()})
    out = BASE / 'non-tiling'
    out.mkdir(exist_ok=True)
    report = dict(style='Lift05', schema=1, totals=dict(totals), sources=list(sources.values()), meshes=records,
        resourceSha256=sha(resource), resourceBytes=resource.stat().st_size,
        nativeInputSha256=sha(source_path), nativeTemplateSha256=sha(template_path), currentTemplateCheckSha256=sha(template_check),
        windowTemplateSha256=sha(window_template_path),
        nativeClampEvidence={str(p.relative_to(ROOT)): sha(p) for p in clamp_files},
        repeatRegionsSha256=sha(repeat_path), repeatRegions=repeat_values.tolist(), roadRegions=road_regions,
        proof='Original native Surface bytes and current native template establish UV0 identity. Position/UV-connected part occurrences use one finite atlas traversal; native VT Clamp preserves finite edge sampling for out-of-atlas coordinates, without normalization or repetition. Constant/line UVs are valid finite mappings. Atlas reuse and repeated painted motifs do not establish UV tiling. Existing repeat and prototype road overlaps remain density paths.',
        interpolation='Shared original-UV path must use construction fragment weights, including degenerate UVs and nonrigid LOD2 domain83 at zero delta.',
        localization='No user-visible text or localized behavior changed; class diagnostics are internal errors.',
        acceptance='Offline classification only. Shared compilation, runtime fragment checks, native appearance and complete owner closure remain separate.')
    (out / 'classification.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(totals=totals, bytes=resource.stat().st_size, sha256=sha(resource))))


CLASS_TEMPLATE = '''// Generated by tools/PreparedMaterials/Lift05/BuildUvSampling.py.
// Embedded numerical arrays: Lift05UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class Lift05UvSampling
{
    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
STREAMS            _ => -1
        };
        if (index < 0) return false;
        var flags = Data.Items[index];
        if ((uint)domain >= (uint)flags.Length)
            throw new InvalidOperationException("Lift05 UV sampling domain mismatch.");
        return flags[domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly bool[][] Items = Read();
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("Lift05UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != STREAM_COUNT)
                throw new InvalidOperationException("Invalid Lift05 UV sampling schema.");
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
                        throw new InvalidOperationException("Invalid Lift05 UV sampling flag.");
                    flags[d] = flag == 1;
                }
            }
            if (at != values.Length)
                throw new InvalidOperationException("Invalid Lift05 UV sampling length.");
            return result;
        }
    }
}
'''


if __name__ == '__main__':
    main()
