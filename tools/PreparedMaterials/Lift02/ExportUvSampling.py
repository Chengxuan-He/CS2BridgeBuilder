"""Measure native Lift02 UV traversals and emit independent retained-axis flags.

No receiver failure, repeat-registry absence or texture pixel similarity is input.
The exact native indexed chart, Surface and schema-aware domain records are inputs.
"""
from collections import Counter, defaultdict
import hashlib
import io
import json
from pathlib import Path
import struct
import sys
import zipfile
import numpy as np
from ExportNativeCharts import read_domains
from ExportSharedSampling import read_string, count
from PrepareRecipes import numbers, recipes

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'artifacts/material-remap/Lift02/source-mapping'
NUM = ROOT / 'assets/HardcodedNumbers/Lift02'
RESOURCE = 'Types_Lift02_Lift02UvSampling_cs'
RESEARCH = Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research')


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def ranges(values):
    result = []
    for value in sorted(values):
        if result and value == result[-1][1] + 1:
            result[-1][1] = value
        else:
            result.append([value, value])
    return result


def surface_parameters(source):
    with zipfile.ZipFile(source['archive']) as archive:
        raw = archive.read(source['entry'])
    assert hashlib.sha256(raw).hexdigest() == source['sha256']
    reader = io.BytesIO(raw)
    version, template, vt = struct.unpack('<Hi?', reader.read(7))
    assert version == 1
    vt_header = None
    vt_surface = None
    if vt:
        vt_header = reader.read(276)
        assert struct.unpack_from('<i', vt_header)[0] in (1, 2)
        if reader.read(1)[0]:
            vt_surface = reader.read(16).hex()
    maps = []
    for fmt in ('i', 'f', '4f', '4f'):
        fields = {}
        for _ in range(count(reader)):
            name = read_string(reader)
            fields[name] = struct.unpack('<' + fmt, reader.read(struct.calcsize(fmt)))
        maps.append(fields)
    assert template in (0, 1, 2) and not any(maps[1:])
    assert maps[0] in ({}, {'_Snow': (0,)})
    return dict(template=template, virtualTexturing=vt, parameters=maps,
                explicitUvScaleOffsetOverrides=False,
                vtHeaderSha256=hashlib.sha256(vt_header).hexdigest() if vt else None,
                serializedVtSurfaceReferenceBytes=vt_surface)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    source_path = ROOT / 'artifacts/agent-material-env/Lift02/source-rebuild/inputs/native-source-inventory.json'
    inventory = json.loads(source_path.read_text())
    sources = {s['cid']: s for s in inventory['surfaces']}
    params = {cid: surface_parameters(s) for cid, s in sources.items()}
    shader_evidence = []
    for template in (0, 1):
        path = RESEARCH / f'template-{template}.json'
        data = json.loads(path.read_text())
        assert data['m_Shader']['m_PathID'] == 571
        floats = dict(data['m_SavedProperties']['m_Floats'])
        colors = dict(data['m_SavedProperties']['m_Colors'])
        assert all(floats[k] == 0 for k in ('_UVBase', '_UVDetail', '_UVEmissive'))
        assert colors['_UV0Scale'] == dict(r=1, g=1, b=0, a=0)
        assert colors['_UV0Offset'] == dict(r=0, g=0, b=0, a=0)
        shader_evidence.append(dict(path=str(path), sha256=sha(path), template=template,
                                    uvChannel=0, scale=[1, 1], offset=[0, 0]))
    shader = RESEARCH / 'shader-programs/571-2701.asm'
    assembly = shader.read_text()
    assert 'mad r6.zw, v2.xxxy, cb2[25].xxxy, cb2[25].zzzw' in assembly
    shader_evidence.append(dict(path=str(shader), sha256=sha(shader),
                                evidence='UV0 affine coordinates feed five native channel samples at lines 103-115; no modulo in this sample path.'))
    native = ROOT / 'artifacts/prepared-materials/shared-nontiling/native-semantics'
    for filename, needle in (
        ('VTAtlassingInfo.cs', 'VTAddressMode addressMode = VTAddressMode.Clamp'),
        ('VTDatabase.cs', 'return new VTAtlassingInfo(item, indexInStack);'),
        ('AtlasMaterialsDatabase.cs', 'registeredTextures[materialGuid] = new VTAtlassingInfo(stackGlobalIndex, list[num10]);'),
        ('SurfaceAsset.cs', 'm_VTAtlassingInfos[i] = textureStreamingSystem.ReserveTextureRect(i, biasedSize.x, biasedSize.y);')):
        path = native / filename
        assert needle in path.read_text()
        shader_evidence.append(dict(path=str(path), sha256=sha(path), evidence=needle))
    source_map = json.loads((ROOT / 'artifacts/prepared-materials/Lift02/current-readiness/source-map.json').read_text())
    source_routes = {(r['render'], r['mesh']): r for r in source_map}
    window_domains = {}
    window_evidence = []
    window_root = ROOT / 'artifacts/prepared-materials/shared-nontiling/windows'
    window_template = native / 'template-2.json'
    assert json.loads(window_template.read_text())['m_Shader']['m_PathID'] == 550
    for path in (window_template, native / '550-152.asm', native / '550-184.asm'):
        shader_evidence.append(dict(path=str(path), sha256=sha(path),
                                    evidence='Native Windows template2/shader550; UV0.YZ local interior coordinates, UV0.W orientation, original COLOR selection.'))
    for summary in json.loads((window_root / 'summary.json').read_text()):
        if summary['style'] != 'Lift02':
            continue
        path = window_root / summary['report']
        record = json.loads(path.read_text())
        key = (record['render'], record['mesh'])
        source = source_routes[key]
        assert params[source['sourceCid']]['template'] == 2
        assert sha(Path(record['geometry'])) == record['geometrySha256']
        uv = np.asarray(record['uv0'])
        colors = np.asarray(record['colors'])
        faces = np.asarray(record['indices'])
        assert np.isfinite(uv).all() and np.isfinite(colors).all()
        source_local = (np.all((uv[faces, 1:3] >= 0) & (uv[faces, 1:3] <= 1), axis=(1, 2))
                        & np.all(uv[faces, 3] == uv[faces, 3][:, :1], axis=1)
                        & np.all(colors[faces] == colors[faces][:, :1, :], axis=(1, 2)))
        assert np.flatnonzero(source_local).tolist() == record['sourceLocalFaces']
        domain_path = NUM / ('UvDomain_' + key[0].replace(' ', '_') + '_' + str(key[1]) + '.bin')
        domains = read_domains(domain_path)
        eligible = [i for i, fs in enumerate(domains) if np.all(source_local[fs])]
        assert eligible == record['sourceLocalDomains']
        native_npz = np.load(ROOT / 'artifacts/uv-density/native' / (key[0] + '.npz'))
        assert np.array_equal(native_npz[f'mesh{key[1]}_faces'], faces)
        assert np.array_equal(native_npz[f'mesh{key[1]}_uv0'], uv)
        window_domains[key] = set(eligible)
        window_evidence.append(dict(path=str(path), sha256=sha(path), sourceCid=source['sourceCid'],
                                    sourceDigest=source['sourceDigest'], domains=eligible,
                                    nativeGeometrySha256=record['geometrySha256']))
    meta_path = ROOT / 'artifacts/prepared-materials/Lift02/source-sharing/native-chart-integration.json'
    meta = json.loads(meta_path.read_text())
    chart_path = NUM / 'Types_Lift02_Lift02UvCharts_cs.bin'
    assert sha(chart_path) == meta['resourceSha256']
    arrays = numbers(chart_path)
    ints, doubles = arrays['I32'], arrays['F64']
    repeat_domains = defaultdict(set)
    for name in ('crossbeam-web', 'crossbeam-horizontal-flange', 'crossbeam-lip'):
        evidence = json.loads((ROOT / f'artifacts/prepared-materials/Lift02/structure-units/{name}-evidence.json').read_text())
        for use in evidence['uses']:
            repeat_domains[(use['render'], use['mesh'])].update(use['domains'])
    road, _ = recipes(ROOT)
    road_lo, road_hi = np.array(road[0]['min']), np.array(road[0]['max'])
    chart_bounds = {}

    def finite_chart(offset):
        if offset not in chart_bounds:
            loops = []
            for i in range(ints[offset]):
                start, n = ints[offset + 1 + i * 2:offset + 3 + i * 2]
                loops.append(np.array(doubles[start:start + n * 2]).reshape(-1, 2))
            points = np.concatenate(loops)
            lo, hi = points.min(0), points.max(0)
            # Whole authored indexed island, no texture-boundary traversal.
            chart_bounds[offset] = (bool(np.isfinite(points).all() and np.all(lo >= 0)
                                        and np.all(hi <= 1) and np.all(hi > lo)), lo, hi)
        return chart_bounds[offset]

    payload = [2, len(meta['routes'])]
    records = []
    totals = Counter()
    routes = []
    for route in meta['routes']:
        render, mesh = route['render'], route['mesh']
        key = (render, mesh)
        source = source_routes[key]
        assert source['sourceDigest'] == sources[source['sourceCid']]['sha256']
        native = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        domain_path = NUM / ('UvDomain_' + render.replace(' ', '_') + '_' + str(mesh) + '.bin')
        domain_faces = read_domains(domain_path)
        native_evidence = next(s for s in meta['sources'] if (s['render'], s['mesh']) == key)
        assert sha(native) == native_evidence['nativeSha256']
        data = np.load(native)
        faces = data[f'mesh{mesh}_faces']
        positions = data[f'mesh{mesh}_positions'].astype(np.float64)
        uv = data[f'mesh{mesh}_uv0'][:, :2].astype(np.float64)
        triangles = uv[faces]
        edge1, edge2 = triangles[:, 1] - triangles[:, 0], triangles[:, 2] - triangles[:, 0]
        area = edge1[:, 0] * edge2[:, 1] - edge1[:, 1] * edge2[:, 0]
        groups = defaultdict(list)
        flags = []
        for domain, fs in enumerate(domain_faces):
            chartset = ints[route['offset'] + domain]
            charts = ints[chartset + 1:chartset + 1 + ints[chartset]]
            points = triangles[fs].reshape(-1, 2)
            # Source sampling, not the existence of an old extracted repeat,
            # decides the axes. Separate crossbeams reuse a complete finite island;
            # that does not make one beam tile across its width.
            is_road = source['sourceCid'] == 'ea923855f34e05d7a517992fe064ddb7' and np.all(points >= road_lo) and np.all(points <= road_hi)
            if domain in repeat_domains[key]:
                assert charts and all(finite_chart(c)[0] for c in charts)
                assert np.all(points >= 0) and np.all(points <= 1)
                kind = 'finite-authored-crossbeam-island-uv0'
            elif is_road:
                # Every original face in this domain has U proportional only to
                # longitudinal Z (or constant U on terminal faces). Its finite V
                # cross section must not be repeated when width changes. Check
                # every face in both detailed LODs, including top, bottom and ends.
                p = positions[faces[fs]]
                q = triangles[fs]
                dz = p[:, 1:, 2] - p[:, :1, 2]
                du = q[:, 1:, 0] - q[:, :1, 0]
                assert np.all(dz[:, 0] * du[:, 1] == dz[:, 1] * du[:, 0])
                assert np.all(du[dz == 0] == 0)
                kind = 'longitudinal-u-finite-v-roadbed'
            elif not np.isfinite(points).all():
                raise ValueError('Nonfinite native UV: ' + str((key, domain)))
            elif params[source['sourceCid']]['template'] == 2:
                kind = 'finite-authored-window-local-yz' if domain in window_domains[key] else 'unresolved-window-template'
            elif not np.any(area[fs] != 0):
                kind = 'finite-authored-point-uv0' if np.all(points == points[0]) else 'finite-authored-line-uv0'
            elif not charts:
                kind = 'unresolved-atlas-boundary-traversal'
            elif not all(finite_chart(c)[0] for c in charts):
                kind = 'finite-authored-clamped-island-uv0' if params[source['sourceCid']]['virtualTexturing'] else 'unresolved-atlas-boundary-traversal'
            else:
                kind = 'finite-authored-island-uv0'
            flags.append(3 if kind.startswith('finite-authored-') else
                         2 if kind == 'longitudinal-u-finite-v-roadbed' else 0)
            groups[kind].append(domain)
            totals[kind] += 1
        assert len(flags) == route['domains']
        payload.extend([len(flags), *flags])
        routes.append((render, mesh))
        records.append(dict(**source, nativeSha256=sha(native), domainSha256=sha(domain_path),
                            sourceGeometry=render, materialSlot=mesh,
                            sampling=params[source['sourceCid']],
                            sourcePayloads=sources[source['sourceCid']]['physicalBackingCids'],
                            domains=len(flags), sourceFaces=len(faces),
                            authoredAxes=['UV0.Y', 'UV0.Z'] if params[source['sourceCid']]['template'] == 2 else ['U', 'V'],
                            groups={k: dict(count=len(v), domainRanges=ranges(v)) for k, v in groups.items()}))
        print(render, mesh, dict(Counter({k: len(v) for k, v in groups.items()})), flush=True)
    raw = struct.pack('<II6i', 0x314e4242, 1, len(payload), 0, 0, 0, 0, 0) + np.asarray(payload, dtype='<i4').tobytes()
    raw += hashlib.sha256(raw).digest()
    resource_path = NUM / (RESOURCE + '.bin')
    resource_path.write_bytes(raw)
    switches = '\n'.join(f'            ("{render}", {mesh}) => {i},' for i, (render, mesh) in enumerate(routes))
    code = '''// Generated by ExportUvSampling.py from native source/domain evidence.
// Embedded numerical arrays: Types_Lift02_Lift02UvSampling_cs
using System;
namespace BridgeBuilder.Bridges;
internal static class Lift02UvSampling
{
    internal static bool For(string render, int mesh, int domain) => Axes(render, mesh, domain) == 3;
    internal static int Axes(string render, int mesh, int domain)
    {
        int route = (render, mesh) switch
        {
SWITCHES
            _ => -1
        };
        if (route < 0) return 0;
        var values = Data.Items[route];
        if ((uint)domain >= (uint)values.Length)
            throw new ArgumentOutOfRangeException(nameof(domain), "Unrecorded Lift02 UV domain.");
        return values[domain];
    }
    private static int[][] Read()
    {
        var values = HardcodedNumbers.Load("Types_Lift02_Lift02UvSampling_cs").I32;
        if (values.Length < 2 || values[0] != 2 || values[1] != ROUTECOUNT)
            throw new InvalidOperationException("Invalid Lift02 UV sampling resource.");
        var result = new int[values[1]][];
        int offset = 2;
        for (int route = 0; route < result.Length; route++)
        {
            int count = values[offset++];
            if (count < 0 || count > values.Length - offset)
                throw new InvalidOperationException("Invalid Lift02 UV sampling count.");
            var flags = new int[count];
            for (int domain = 0; domain < count; domain++)
            {
                int value = values[offset++];
                if (value < 0 || value > 3)
                    throw new InvalidOperationException("Invalid Lift02 UV sampling flag.");
                flags[domain] = value;
            }
            result[route] = flags;
        }
        if (offset != values.Length)
            throw new InvalidOperationException("Trailing Lift02 UV sampling values.");
        return result;
    }
    private static class Data
    {
        static Data() { }
        internal static readonly int[][] Items = Read();
    }
}
'''.replace('SWITCHES', switches).replace('ROUTECOUNT', str(len(routes)))
    (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift02/Lift02UvSampling.cs').write_text(code, encoding='utf-8')
    report = dict(schema='BridgeBuilder/Lift02AuthoredUvSampling/2',
                  sourceInventorySha256=sha(source_path), indexedChartsSha256=sha(chart_path),
                  nativeShaderEvidence=shader_evidence,
                  nativeWindowEvidence=window_evidence,
                  resourceSha256=sha(resource_path), resourceBytes=len(raw), totals=dict(totals), routes=records,
                  evidenceRule='Actual source-indexed UV islands retain their original material and finite traversal. Reuse across separate crossbeams is not transverse tiling. Every roadbed-domain source face has longitudinal-only or constant U; V is a finite cross section. Retained-axis bits are U=1 and V=2. Existing longitudinal UV resets remain on original geometry, without new texture-period cuts.',
                  limits=['UV-degenerate domains preserve original point/line mappings; this is not a positive-density pass.', 'Windows retains complete UV0 and COLOR channels; its local YZ mapping is separate from ordinary UV0.XY density.', 'No density claim for allowed stretching; no selected-road or track mapping permission.', 'Native visual, motion and cold-load acceptance remain separate.'])
    (OUT / 'classification.json').write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps(dict(totals=totals, resourceBytes=len(raw))))


if __name__ == '__main__':
    main()
