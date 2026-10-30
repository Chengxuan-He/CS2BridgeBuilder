"""Record native finite atlas uses; do not infer non-tiling from failed Retile output."""
import collections
import argparse
import hashlib
import io
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, domains, read_bbn
from AuditStructureReuse import count, text, bindings
from MovableUvDomainFormat import write_bbn
from shapely.geometry import Polygon, MultiPoint, box


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def network_evidence(base, sources):
    # This offline decoder is used only to inspect the original complete mip chain.
    import texture2ddecoder
    native = ROOT / 'artifacts/prepared-materials/shared-nontiling/native-semantics'
    research = ROOT.parent / '2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/shader-programs'
    vertex, fragment = research / '581-106.asm', research / '581-133.asm'
    vertex_text = vertex.read_text()
    assert 'ge r1.w, v3.y, l(0.000000)' in vertex_text
    assert 'frc r4.w, v3.y' in vertex_text
    assert 'movc r0.y, r1.w, r2.y, r4.w' in vertex_text
    template = json.loads((native / 'template-17.json').read_text())
    assert template['m_Shader']['m_PathID'] == 581
    for name, value in template['m_SavedProperties']['m_TexEnvs']:
        if name in ('_BaseColorMap', '_NormalMap', '_MaskMap'):
            assert value['m_Scale'] == dict(x=1.0, y=1.0)
            assert value['m_Offset'] == dict(x=0.0, y=0.0)
    network = [s for s in sources if s['template'] == 17]
    expected = {c['layers'][0]['property']: c['cid'] for c in network[0]['channels']}
    assert set(expected) == {'_BaseColorMap', '_NormalMap', '_MaskMap'}
    assert all({c['layers'][0]['property']: c['cid'] for c in s['channels']} == expected for s in network)
    converted = ROOT / 'assets/Materials/Draw01/NativeSources/1d6c92d008a952b1db4e1cc6cf7a1160/Object.Surface'
    converted_template, converted_bindings = bindings(converted)
    assert converted_template == 0
    assert all(converted_bindings.get(k) == v for k, v in expected.items())
    payloads = {}
    with zipfile.ZipFile(network[0]['archive']) as archive:
        for entry in archive.namelist():
            if not entry.endswith('.Texture.cid'):
                continue
            cid = archive.read(entry).decode().strip()
            if cid not in expected.values():
                continue
            raw = archive.read(entry[:-4])
            version, w, h, depth, mips, fmt, dimension, filtering, wrap, aniso = struct.unpack_from('<4H6B', raw)
            assert (version, depth, dimension, wrap) == (4, 1, 2, 0)
            assert fmt in (108, 109)  # Original BC7 sRGB / linear channels.
            channel = next(k for k, v in expected.items() if v == cid)
            at, levels = 14, []
            for level in range(mips):
                mw, mh = max(1, w >> level), max(1, h >> level)
                size = ((mw + 3) // 4) * ((mh + 3) // 4) * 16
                if channel == '_MaskMap':
                    # Decoder returns BGRA. Native shader581 blends worldspace with B.
                    pixels = np.frombuffer(texture2ddecoder.decode_bc7(raw[at:at+size], mw, mh), np.uint8).reshape(mh, mw, 4)
                    assert not pixels[:, :, 0].any()
                levels.append(dict(level=level, width=mw, height=mh, bytes=size,
                                   worldspaceMaskBlueZero=channel == '_MaskMap'))
                at += size
            assert at == len(raw)
            payloads[channel] = dict(cid=cid, entry=entry[:-4], sha256=hashlib.sha256(raw).hexdigest(),
                                     width=w, height=h, mipCount=mips, nativeWrap=wrap, mips=levels)
    assert set(payloads) == set(expected)
    return dict(templatePath=str((native / 'template-17.json').relative_to(ROOT)),
                templateSha256=sha(native / 'template-17.json'),
                currentTemplateVerificationSha256=sha(native / 'templates-current.json'),
                nativeShader='581-106 vertex lines 546-549, 626-630; 581-133 fragment lines 124-145',
                shaderEvidence=[dict(path=str(p), sha256=sha(p)) for p in (vertex, fragment, native / 'shader-581-parsed.json')],
                mapping='V<0: frac(V), U unchanged; V>=0: native network-length mapping. Only single negative tiles qualify.',
                worldspace='Original mask blue is zero in every source mip; the shader581 worldspace blend is inactive.',
                convertedDescriptorSha256=sha(converted), channels=payloads)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    from FiniteTerminalEvidence import evidence
    terminal = evidence()
    base = ROOT / 'artifacts/prepared-materials/Draw01'
    numbers = ROOT / 'assets/HardcodedNumbers/Draw01'
    sources = json.loads((base / 'rebuild-native-v1/sources.json').read_text())['sources']
    mapping = json.loads((base / 'actual-retile/source-map.json').read_text())
    # Some unchanged slots are absent from the converted-resource inventory.
    # Verify their own CID/bytes, then reuse equal descriptor semantics only.
    by_digest = {s['sha256']: s for s in sources}
    missing = {s['sourceCid']: s for s in mapping if s['sourceCid'] not in {x['cid'] for x in sources}}
    archive_path = sources[0]['archive']
    with zipfile.ZipFile(archive_path) as archive:
        for entry in archive.namelist():
            if not entry.endswith('.Surface.cid'):
                continue
            identity = archive.read(entry).decode().strip()
            if identity not in missing:
                continue
            expected = missing[identity]
            raw = archive.read(entry[:-4])
            digest = hashlib.sha256(raw).hexdigest()
            assert digest == expected['sourceDigest']
            if digest in by_digest:
                sources.append(dict(by_digest[digest], cid=identity, entry=entry[:-4], archive=archive_path))
            elif struct.unpack_from('<Hi?', raw) == (1, 2, False):
                reader = io.BytesIO(raw)
                reader.read(7)
                assert all(count(reader) == 0 for _ in range(4))
                channels = []
                for _ in range(count(reader)):
                    name = text(reader)
                    assert reader.read(1) == b'\xff'
                    channel_cid = ''.join(f'{b & 15:x}{b >> 4:x}' for b in reader.read(16))
                    channels.append(dict(cid=channel_cid, kind='Texture', property=name))
                keywords = [text(reader) for _ in range(count(reader))]
                assert keywords == ['_TANGENTSPACE_OCTO'] and reader.tell() == len(raw)
                sources.append(dict(cid=identity, sha256=digest, entry=entry[:-4], archive=archive_path,
                                    bytes=len(raw), template=2, builtin=True, channels=channels, keywords=keywords))
            elif struct.unpack_from('<Hi?', raw) == (1, 0, True):
                # Native far-LOD atlas: retain its actual VT identities even though
                # no converted sampler previously needed this Surface.
                def cid(data):
                    return ''.join(f'{b & 15:x}{b >> 4:x}' for b in data)
                channels = []
                for layer, name in enumerate(('_BaseColorMap', '_NormalMap', '_MaskMap', '_ControlMask')):
                    identity_vt = cid(raw[83 + layer * 16:99 + layer * 16])
                    if identity_vt != '0' * 32:
                        channels.append(dict(cid=identity_vt, kind='VTTexture', property=name))
                assert raw[283] == 255
                channels.append(dict(cid=cid(raw[284:300]), kind='VTSurface'))
                sources.append(dict(cid=identity, sha256=digest, entry=entry[:-4], archive=archive_path,
                                    bytes=len(raw), template=0, builtin=True, channels=channels))
    verified = {}
    for source in sources:
        with zipfile.ZipFile(source['archive']) as archive:
            raw = archive.read(source['entry'])
            assert hashlib.sha256(raw).hexdigest() == source['sha256']
            assert archive.read(source['entry'] + '.cid').decode().strip() == source['cid']
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
        for name, size, fmt in [('float', 4, '<f'), ('int', 4, '<i'),
                                ('vector', 16, '<4f'), ('color', 16, '<4f')]:
            properties[name] = {}
            for _ in range(count(reader)):
                key = text(reader)
                properties[name][key] = struct.unpack(fmt, reader.read(size))
        assert all(key == '_Snow' for group in properties.values() for key in group)
        verified[source['cid']] = dict(source, properties=properties, nativeVT=vt)
    network = network_evidence(base, sources)
    # Actual registered repeat regions, not arbitrary inferred pixel periodicity.
    repeat_path = numbers / 'Draw01RepeatRegions.bin'
    repeat_values = read_bbn(repeat_path)[5].reshape(-1, 4)
    repeats = [box(*values) for values in repeat_values]
    road_manifest = base / 'genuine-unit-requirements/manifest.json'
    road_regions = [item['region'] for item in json.loads(road_manifest.read_text())['roadCandidates']]
    repeats.extend(box(*region['min'], *region['max']) for region in road_regions)
    correction_path = numbers / 'Types_Draw01_Draw01UndersideUv_cs.bin'
    ci, _, _, _, cf, _ = read_bbn(correction_path)
    records = []
    encoded = [1]
    encoded.append(len(mapping))
    for source in mapping:
        render, mesh = source['render'], source['mesh']
        native_path = ROOT / 'artifacts/uv-density/native' / (render + '.npz')
        recipe_path = numbers / ('UvDomain_' + render.replace(' ', '_') + '_' + str(mesh) + '.bin')
        native = np.load(native_path)
        prefix = f'mesh{mesh}_'
        uv = native[prefix + 'uv0'][:, :2].astype(float)
        faces = native[prefix + 'faces']
        corrected = []
        if mesh == 0 and render in ('DrawBridge01 Mesh', 'DrawBridge01_LOD1 Mesh'):
            offset, start, length = (2, 0, 176) if render == 'DrawBridge01 Mesh' else (178, 352, 158)
            corrected = ci[offset:offset + length].tolist()
            uv[corrected] = cf[start:start + length * 2].reshape(-1, 2)
        triangles = uv[faces]
        src = verified.get(source['sourceCid'])
        if src:
            assert src['sha256'] == source['sourceDigest']
        atlas_is_primary = src and any(c['cid'] == '1011a4b81e3aa59dbb634eccabe35e1a' for c in src['channels'])
        measured = domains(recipe_path)
        window = None
        local_window_domains = set()
        if src and src['template'] == 2:
            evidence = ROOT / 'artifacts/prepared-materials/shared-nontiling'
            report_path = evidence / 'windows' / f'Draw01-{render.replace(" ", "_")}-{mesh}.json'
            report = json.loads(report_path.read_text())
            assert sha(Path(report['geometry'])) == report['geometrySha256']
            template_path = evidence / 'native-semantics/template-2.json'
            assert json.loads(template_path.read_text())['m_Shader']['m_PathID'] == 550
            full_uv = np.asarray(report['uv0'])
            colors = np.asarray(report['colors'])
            assert np.array_equal(faces, report['indices'])
            assert np.array_equal(uv, full_uv[:, :2])
            local = full_uv[faces, 1:3]
            finite = np.all(np.isfinite(full_uv[faces]), axis=(1, 2)) & np.all((local >= 0) & (local <= 1), axis=(1, 2))
            orientation = np.all(full_uv[faces, 3] == full_uv[faces, 3][:, :1], axis=1)
            selection = np.all(colors[faces] == colors[faces][:, :1, :], axis=(1, 2))
            approved_faces = finite & orientation & selection
            local_window_domains = {d['index'] for d in measured if np.all(approved_faces[d['faces']])}
            assert local_window_domains == set(report['sourceLocalDomains'])
            window = dict(report=str(report_path.relative_to(ROOT)), reportSha256=sha(report_path),
                          templateSha256=sha(template_path), sourceGeometrySha256=report['geometrySha256'],
                          nativeShaderEvidence={name: sha(evidence / 'native-semantics' / name)
                                                for name in ('550-152.asm', '550-184.asm')},
                          mapping='Original shader550; finite local UV0.YZ, constant UV0.W orientation and COLOR room selection per source face. Full UV0/COLOR retained.',
                          approvedDomains=sorted(local_window_domains))
        groups = collections.defaultdict(list)
        flags = []
        for domain in measured:
            tri = triangles[domain['faces']]
            if src is None:
                reason = 'unresolved-source-semantics'
            elif not np.isfinite(tri).all():
                reason = 'unresolved-nonfinite-native-uv'
            elif src['template'] == 2:
                reason = ('non-tiling-native-window-local-yz' if domain['index'] in local_window_domains
                          else 'unresolved-window-local-boundary')
            elif src['template'] == 17:
                if np.all(tri[:, :, 1] < 0) and np.ptp(np.floor(tri[:, :, 1])) == 0 and np.all((tri[:, :, 0] >= 0) & (tri[:, :, 0] <= 1)):
                    # Native signed-V encoding identifies finite atlas use. Whole-texture
                    # Repeat in the compatible object descriptor yields the same V+1
                    # samples, derivatives and mips; keep authored raw UVs unchanged.
                    reason = 'non-tiling-native-network-negative-uv0'
                elif np.all(tri[:, :, 1] >= 0):
                    reason = 'density-native-network-length-axis'
                else:
                    reason = 'unresolved-network-mixed-axes'
            elif (np.any(tri < 0) or np.any(tri > 1)) and not (src['nativeVT'] and atlas_is_primary):
                reason = 'unresolved-outside-native-atlas'
            elif render == terminal['render'] and mesh == terminal['mesh'] and domain['index'] in terminal['domains']:
                assert src['cid'] == terminal['sourceSurface'] and src['template'] == terminal['template'] and src['nativeVT']
                # Independently inspected original finite VT terminal faces. Their
                # original negative V is an atlas-clamped edge, not a repeat rate.
                reason = 'non-tiling-native-vt-terminal'
            elif atlas_is_primary and any(
                    (Polygon(t).intersection(region).area > 0 if Polygon(t).area > 0
                     else MultiPoint(t).convex_hull.intersects(region))
                    for t in tri for region in repeats):
                reason = 'density-known-repeat-region-or-overlap'
            else:
                # Template 0/1 UV0 identity; finite native atlas maps, including
                # constant/line maps. Primary native VT clamps outside atlas bounds.
                # Physical/UV topology and face membership
                # are retained in native/domain files, hashed below. No wrap predicate.
                reason = 'non-tiling-native-uv0-atlas'
            flags.append(int(reason.startswith('non-tiling-native-')))
            groups[reason].append(domain['index'])
        encoded.extend([len(flags), *flags])
        records.append(dict(**source, nativeSha256=sha(native_path), domainSha256=sha(recipe_path),
                            domainCount=len(flags), sourceUvAxes=['U', 'V'],
                            uvMin=uv.min(axis=0).tolist(), uvMax=uv.max(axis=0).tolist(),
                            degenerateSourceFaces=int(sum(Polygon(t).area == 0 for t in triangles)),
                            outsideAtlasFaces=int(np.any((triangles < 0) | (triangles > 1), axis=(1, 2)).sum()),
                            correctedVertices=corrected, nativeWindowEvidence=window, classifications=dict(groups)))
    resource = numbers / 'Draw01UvSampling.bin'
    raw = write_bbn(encoded, [])
    resource.write_bytes(raw)
    assert read_bbn(resource)[0].tolist() == encoded
    code = '''// Generated by tools/PreparedMaterials/Draw01/BuildUvSampling.py.
// Embedded numerical arrays: Draw01UvSampling
using System;
namespace BridgeBuilder.Bridges;
internal static class Draw01UvSampling
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
            throw new InvalidOperationException("Draw01 UV sampling domain mismatch.");
        return flags[domain];
    }
    private static class Data
    {
        static Data() { }
        internal static readonly bool[][] Items = Read();
        private static bool[][] Read()
        {
            var values = HardcodedNumbers.Load("Draw01UvSampling").I32;
            if (values.Length < 2 || values[0] != 1 || values[1] != 25)
                throw new InvalidOperationException("Invalid Draw01 UV sampling schema.");
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
                        throw new InvalidOperationException("Invalid Draw01 UV sampling flag.");
                    flags[d] = flag == 1;
                }
            }
            if (at != values.Length)
                throw new InvalidOperationException("Invalid Draw01 UV sampling length.");
            return result;
        }
    }
}
'''
    (ROOT / 'src/BridgeBuilder/Bridges/Types/Draw01/Draw01UvSampling.cs').write_text(code)
    out = args.output or base / 'non-tiling'
    out.mkdir(parents=True, exist_ok=True)
    totals = collections.Counter()
    for record in records:
        totals.update({key: len(ids) for key, ids in record['classifications'].items()})
    report = dict(style='Draw01', schema=1, resourceSha256=sha(resource), resourceBytes=len(raw),
                  nativeTemplateEvidence='source-sharing/report.json#nativeTemplateMappingProperties',
                  nativeNetworkEvidence=network,
                  nativeTerminalEvidence=terminal,
                  repeatRegionsSha256=sha(repeat_path), repeatRegions=repeat_values.tolist(),
                  roadRegionEvidenceSha256=sha(road_manifest), roadRegions=road_regions,
                  undersideCorrectionSha256=sha(correction_path), totals=dict(totals),
                  sources=list(verified.values()), meshes=records,
                  localization='No UI text or localized behavior changed; errors are internal diagnostics.',
                  acceptance='Offline mapping classification only; native rendering and complete owner bytes remain unverified.')
    (out / 'classification.json').write_text(json.dumps(report, indent=2) + '\n')
    print(json.dumps(dict(totals=totals, bytes=len(raw))))


if __name__ == '__main__':
    main()
