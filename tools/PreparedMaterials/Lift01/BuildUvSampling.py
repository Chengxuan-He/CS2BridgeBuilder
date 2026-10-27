"""Classify authored Lift01 atlas mappings; never use failed Retile as evidence."""
from collections import Counter
import hashlib
import io
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, FIXTURE, inputs, domains
from ExportRecipes import numbers
from PrepareNativeObjects import count, text, cid
from shapely import polygons, intersection, area, box


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def source_record(source):
    with zipfile.ZipFile(source['archive']) as archive:
        raw = archive.read(source['entry'])
        assert archive.read(source['entry'] + '.cid').decode().strip() == source['cid']
    assert hashlib.sha256(raw).hexdigest() == source['sha256']
    reader = io.BytesIO(raw)
    version, template, vt = struct.unpack('<Hi?', reader.read(7))
    assert version == 1 and template in (0, 1, 2, 33)
    references = {}
    if vt:
        header = reader.read(276)
        for i, channel in enumerate(('BaseColor', 'Normal', 'Mask', 'Control')):
            identity = cid(header[76 + i * 16:92 + i * 16])
            if identity != '0' * 32:
                references[channel] = identity
        marker = reader.read(1)
        assert marker in (b'\0', b'\xff')
        if marker == b'\xff':
            references['VTSurface'] = cid(reader.read(16))
    properties = {}
    for fmt in ('<f', '<i', '<4f', '<4f'):
        for _ in range(count(reader)):
            key = text(reader)
            properties[key] = list(struct.unpack(fmt, reader.read(struct.calcsize(fmt))))
    for _ in range(count(reader)):
        key = text(reader)
        marker = reader.read(1)
        assert marker in (b'\0', b'\xff')
        if marker == b'\xff':
            references[key] = cid(reader.read(16))
    keywords = [text(reader) for _ in range(count(reader))]
    assert reader.tell() == len(raw)
    assert all(k == '_Snow' for k in properties), properties
    return dict(source, properties=properties, references=references, keywords=keywords,
                nativeUv='UV0; no serialized UV scale, offset, worldspace or repeat overrides')


def main():
    base = ROOT / 'artifacts/prepared-materials/Lift01'
    mapping_path = base / 'structure-semantic/source-map-exact.json'
    mapping = json.loads(mapping_path.read_text())
    graph = json.loads((base / 'rebuild-native-v1/config/source-graph.json').read_text())
    sources = {s['cid']: source_record(s) for s in graph['sources']}
    native_evidence = ROOT / 'artifacts/prepared-materials/shared-nontiling/native-semantics'
    templates = json.loads((native_evidence / 'templates-current.json').read_text())
    assert all(t['verifiedAgainstCurrentResources'] for t in templates)
    assert json.loads((native_evidence / 'template-2.json').read_text())['m_Shader']['m_PathID'] == 550
    assert json.loads((native_evidence / 'template-17.json').read_text())['m_Shader'] == json.loads((native_evidence / 'template-33.json').read_text())['m_Shader']
    for template in (0, 1, 33):
        path = native_evidence / f'template-{template}.json'
        data = json.loads(path.read_text())['m_SavedProperties']
        assert dict(data['m_Floats'])['_UVBase'] == 0
        for name, binding in data['m_TexEnvs']:
            if name in ('_BaseColorMap', '_NormalMap', '_MaskMap'):
                assert binding['m_Scale'] == dict(x=1.0, y=1.0)
                assert binding['m_Offset'] == dict(x=0.0, y=0.0)
        for src in sources.values():
            if src['template'] == template:
                src['templateSha256'] = sha(path)
                src['nativeUv'] = 'UV0 finite atlas; unit source texture scale, zero offset, no source override'
    chart_path = base / 'native-charts/charts.json'
    chart_data = json.loads(chart_path.read_text())
    chart_report = json.loads((base / 'native-charts/report.json').read_text())
    assert chart_report['fixtureSha256'] == sha(FIXTURE)
    chart_streams = {(s['name'], s['mesh']): s for s in chart_data['streams']}
    repeat = (0.57421875, 0.260009765625, 0.6796875, 0.469970703125)
    repeat_shape = box(*repeat)
    records, encoded, totals = [], [1, len(mapping)], Counter()
    for name, mesh, positions, uv, faces in inputs():
        source = next(s for s in mapping if s['render'] == name and s['mesh'] == mesh)
        src = sources[source['sourceCid']]
        assert src['sha256'] == source['sourceDigest']
        measured = domains(name, mesh)
        chart_stream = chart_streams[name, mesh]
        assert len(measured) == len(chart_stream['domains'])
        report = next(s for s in chart_report['streams'] if s['name'] == name and s['mesh'] == mesh)
        for key, array in [('positionsSha256', positions), ('uvSha256', uv), ('indicesSha256', faces)]:
            assert report[key] == hashlib.sha256(array.tobytes()).hexdigest()
        triangles = uv[faces].astype(float)
        window_evidence = None
        if src['template'] == 2:
            window_path = native_evidence.parent / 'windows' / ('Lift01-' + name.replace(' ', '_') + '-' + str(mesh) + '.json')
            window = json.loads(window_path.read_text())
            assert sha(Path(window['geometry'])) == window['geometrySha256']
            assert np.array_equal(np.asarray(window['uv0'], dtype=np.float32)[:, :2], uv)
            assert np.array_equal(window['indices'], faces)
            assert window['sourceLocalDomains'] == list(range(len(measured)))
            assert src['cid'] not in (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01NativeMaterials.cs').read_text()
            window_evidence = dict(path=str(window_path.relative_to(ROOT)), sha256=sha(window_path), geometrySha256=window['geometrySha256'])
        if src['template'] == 33:
            native_path = ROOT / 'artifacts/uv-density/native' / (name + '.npz')
            native = np.load(native_path)[f'mesh{mesh}_uv0']
            assert np.array_equal(native[:, :2], uv) and (native[:, 1] > 0).all()
        valid = np.isfinite(triangles).all(axis=(1, 2)) & (triangles >= 0).all(axis=(1, 2)) & (triangles <= 1).all(axis=(1, 2))
        # The authored grating uses actual repeated region resets. Do not extend
        # non-tiling permission to it, or to a domain touching that region.
        repeated = np.zeros(len(faces), dtype=bool)
        if '_LOD2' not in name and mesh == 0:
            candidate = np.flatnonzero((triangles.max(1) >= repeat[:2]).all(1) & (triangles.min(1) <= repeat[2:]).all(1))
            repeated[candidate] = area(intersection(polygons(triangles[candidate]), repeat_shape)) > 0
        flags, rejected = [], {}
        used_charts = sorted({c for row in chart_stream['domains'] for c in row})
        # Exact charts are linked by coincident physical position AND UV. Thus
        # aliasing between disconnected authored parts cannot create a tile run.
        for d, members in enumerate(measured):
            if src['template'] == 2:
                reason = 'non-tiling-native-window-local'
            elif src['template'] == 33:
                reason = 'density-network-positive-v-length'
            elif not valid[members].all():
                reason = 'unknown-native-uv'
            elif repeated[members].any():
                reason = 'density-repeated-floor-region'
            else:
                # Original finite constant/line maps and overlapping authored
                # triangles remain valid native UV0 mappings. Chart polygon-union
                # failure affects enlarged coverage, not unchanged source UVs.
                reason = 'non-tiling-finite-authored-atlas'
            flag = int(reason.startswith('non-tiling-'))
            flags.append(flag)
            totals[reason] += 1
            if not flag:
                rejected.setdefault(reason, []).append(d)
        encoded.extend([len(flags), *flags])
        path = ROOT / 'assets/HardcodedNumbers/Lift01' / ('UvDomain_' + name.replace(' ', '_') + '_' + str(mesh) + '.bin')
        records.append(dict(**source, domainCount=len(flags), nonTilingDomains=sum(flags), exclusions=rejected,
                            nonTilingAxes=['UV0.y', 'UV0.z'] if src['template'] == 2 else [] if src['template'] == 33 else ['U', 'V'], domainSha256=sha(path),
                            windowEvidence=window_evidence,
                            nativeNetworkV='Positive V multiplied by evaluated network length; full UV retention prohibited' if src['template'] == 33 else None,
                            sourceFaces=len(faces), authoredPartCharts=len(used_charts),
                            conservativeFragmentDomains=sum(any(chart_data['charts'][c].get('conservativeFragment', False) for c in row) for row in chart_stream['domains']),
                            constantOrLineDomains=sum(not row for row in chart_stream['domains']),
                            repeatedRegionFaces=int(repeated.sum()), uvMin=uv.min(0).tolist(), uvMax=uv.max(0).tolist(),
                            positionsSha256=report['positionsSha256'], uvSha256=report['uvSha256'], indicesSha256=report['indicesSha256']))
    assert len(records) == len(mapping) == 23
    raw = b'BBN1' + struct.pack('<7i', 1, len(encoded), 0, 0, 0, 0, 0) + struct.pack('<' + 'i' * len(encoded), *encoded)
    raw += hashlib.sha256(raw).digest()
    resource = ROOT / 'assets/HardcodedNumbers/Lift01/Lift01UvSampling.bin'
    resource.write_bytes(raw)
    assert list(numbers(resource)['I32']) == encoded
    code = '''// Generated by tools/PreparedMaterials/Lift01/BuildUvSampling.py.
// Embedded numerical arrays: Lift01UvSampling
// Embedded numerical arrays: Lift01UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class Lift01UvSampling
{
    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
'''
    code += ''.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},\n' for i, r in enumerate(records))
    code += '''            _ => -1
        };
        if (index < 0) return false;
        var flags = Data.Items[index];
        if ((uint)domain >= (uint)flags.Length)
            throw new InvalidOperationException("Lift01 UV sampling domain mismatch.");
        return flags[domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly bool[][] Items = Read();
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("Lift01UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != 23)
                throw new InvalidOperationException("Invalid Lift01 UV sampling schema.");
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
                        throw new InvalidOperationException("Invalid Lift01 UV sampling flag.");
                    flags[d] = flag == 1;
                }
            }
            if (at != values.Length)
                throw new InvalidOperationException("Invalid Lift01 UV sampling length.");
            return result;
        }
    }
}
'''
    (ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01/Lift01UvSampling.cs').write_text(code)
    out = base / 'non-tiling'
    out.mkdir(exist_ok=True)
    result = dict(style='Lift01', fixtureSha256=sha(FIXTURE), sourceMapSha256=sha(mapping_path),
                  nativeTemplateEvidence=str(native_evidence.relative_to(ROOT)),
                  nativeTemplateInventorySha256=sha(native_evidence / 'templates-current.json'),
                  authoredTopologySha256=sha(chart_path), repeatRegion=list(repeat),
                  repeatEvidence='repeat-units-v2/prototype-evidence.json',
                  topologyBasis='Native physical-position plus UV-connected finite source islands; aliases between disconnected parts are not tiling.',
                  sourceSampling='Object atlas mappings retain source UVs; windows retain full UV0 including local YZ/direction W and COLOR; positive-V network maps retain density path.',
                  networkShaderEvidence=dict(shader=581, vertexBlobs=[106, 107, 108, 109], identicalDxbcSha256='c23313b0e5a3082ecfeaaa1d35c505e34bf38da7933f83004dd6466098425f8b', rule='V >= 0 uses evaluated network length times V; V < 0 uses frac(V). Template33 alpha-test vertex is identical to template17.'),
                  resourceSha256=sha(resource), resourceBytes=len(raw), totals=dict(totals), sources=list(sources.values()), streams=records,
                  localization='No user-visible text; internal diagnostics use the existing localized operation failure path.',
                  pending='Native near/far motion, cold loading and complete generated-owner material footprint remain unverified.')
    (out / 'classification.json').write_text(json.dumps(result, indent=2) + '\n')
    print(json.dumps(dict(totals=totals, bytes=len(raw), resourceSha256=sha(resource))))


if __name__ == '__main__':
    main()
