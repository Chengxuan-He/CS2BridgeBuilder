"""Record authored UV0 traversal with verified native shader semantics."""
import hashlib
import json
from pathlib import Path
import runpy
import sys
import zipfile
import struct
from collections import Counter
from PIL import Image
import numpy as np

repo = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(repo / 'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays, read_bbn, write_bbn

base = repo / 'artifacts/prepared-materials/Draw02'
evidence_path = base / 'brace-semantic/source-evidence.json'
evidence = json.loads(evidence_path.read_text())
source_map = json.loads((base / 'structure-units/retile-source-map.json').read_text())
descriptor = base / 'shared-source-migration/source-descriptor-fixtures/8a28ab8d9526e258efbcb81c3bbc936b.Surface'
digest = hashlib.sha256(descriptor.read_bytes()).hexdigest()
assert digest == 'e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597'
parse = runpy.run_path(str(repo / 'tools/PreparedMaterials/Lift05/MapSharedSampling.py'))['surface']
native_surface = parse(descriptor.read_bytes())
assert native_surface['template'] == 1 and native_surface['isVT']
assert native_surface['properties']['vectors'] == {}
assert native_surface['keywords'] == ['_TANGENTSPACE_OCTO']
units = json.loads((base / 'structure-units/manifest.json').read_text())['units']
semantics = repo / 'artifacts/prepared-materials/shared-nontiling/native-semantics'
current_templates = json.loads((semantics / 'templates-current.json').read_text())
assert all(x['verifiedAgainstCurrentResources'] for x in current_templates)
for template in (0, 1, 17):
    data = json.loads((semantics / f'template-{template}.json').read_text())
    assert data['m_Shader']['m_PathID'] == (581 if template == 17 else 571)
    for name, binding in data['m_SavedProperties']['m_TexEnvs']:
        if name in ('_BaseColorMap', '_NormalMap', '_MaskMap', 'DefaultPVTStack'):
            assert binding['m_Scale'] == dict(x=1.0, y=1.0)
            assert binding['m_Offset'] == dict(x=0.0, y=0.0)
archive = Path(sys.argv[1]) if len(sys.argv) > 1 else Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok')
sources = {}
texture_evidence = {}
near_network = json.loads((repo / 'artifacts/prepared-materials/Draw01/non-tiling/classification.json').read_text())['nativeNetworkEvidence']
assert all(m['worldspaceMaskBlueZero'] for m in near_network['channels']['_MaskMap']['mips'])
with zipfile.ZipFile(archive) as package:
    for name in package.namelist():
        if name.startswith('DrawBridge02') and name.endswith('.Surface'):
            raw = package.read(name)
            source_digest = hashlib.sha256(raw).hexdigest()
            sources[source_digest] = dict(entry=name, sha256=source_digest, descriptor=parse(raw))
        if name.startswith('DrawBridge02') and name.endswith('.Texture'):
            raw = package.read(name)
            cid_bytes = package.read(name + '.cid')
            cid = ''.join(f'{b & 15:x}{b >> 4:x}' for b in cid_bytes) if len(cid_bytes) == 16 else cid_bytes.decode().strip()
            version, width, height, depth = struct.unpack_from('<4H', raw)
            mips, fmt, dimension, filtering, wrap, aniso = struct.unpack_from('<6B', raw, 8)
            assert version == 4 and depth == 1 and dimension == 2 and wrap == 0 and filtering == 2
            item = dict(cid=cid, entry=name, sha256=hashlib.sha256(raw).hexdigest(), width=width,
                        height=height, mipCount=mips, format=fmt, nativeWrap=wrap, filter=filtering, aniso=aniso)
            if 'LOD2_MaskMap' in name:
                at = 14; levels = []
                for level in range(mips):
                    w, h = max(1, width >> level), max(1, height >> level)
                    size = ((w + 3) // 4) * ((h + 3) // 4) * 16
                    blocks = raw[at:at + size]; at += size
                    pixels = np.array(Image.frombytes('RGBA', (w, h), blocks, 'bcn', 7))
                    assert not pixels[:, :, :3].any()
                    levels.append(dict(level=level, width=w, height=h, bytes=size, worldspaceMaskBlueZero=True))
                assert at == len(raw)
                item['mips'] = levels
            texture_evidence[cid] = item
assert {r['sourceDigest'] for r in source_map} <= sources.keys()
for name, channel in near_network['channels'].items():
    assert texture_evidence[channel['cid']]['sha256'] == channel['sha256']
for source in sources.values():
    s = source['descriptor']
    if s['template'] != 17: continue
    assert all(cid in texture_evidence for cid in s['textures'].values())
    converted_cid = 'e7e3a33d1499167c8c385ebfb70af78b' if s['textures']['_MaskMap'] == 'd3f52f8478b74e116965f66785d0b966' else '1d6c92d008a952b1db4e1cc6cf7a1160'
    converted_path = repo / 'assets/Materials/Draw02/NativeSources' / converted_cid / 'Object.Surface'
    converted = parse(converted_path.read_bytes())
    assert converted['template'] == 0 and converted['textures'] == s['textures']
    source['equivalentObjectDescriptor'] = dict(cid=converted_cid, sha256=hashlib.sha256(converted_path.read_bytes()).hexdigest())
ints = [1, len(source_map)]
records = []
for entry in source_map:
    render, mesh = entry['render'], entry['mesh']
    recipe = repo / 'assets/HardcodedNumbers/Draw02' / f"UvDomain_{render.replace(' ', '_')}_{mesh}.bin"
    values, _ = decode_arrays(*read_bbn(recipe.read_bytes()))
    members = [m for m in evidence['members'] if m['render'] == render and mesh == 0]
    selected = {f for m in members for f in m['faceIds']}
    native = np.load(repo / 'artifacts/uv-density/native' / (render + '.npz'))
    source = sources[entry['sourceDigest']]['descriptor']
    assert source['properties']['vectors'] == {}
    assert source['keywords'] == ['_TANGENTSPACE_OCTO']
    uv = native[f'mesh{mesh}_uv0'][:, :2]
    faces = native[f'mesh{mesh}_faces']
    window_domains = set()
    window_evidence = None
    if source['template'] == 2:
        template = json.loads((semantics / 'template-2.json').read_text())
        assert template['m_Shader']['m_PathID'] == 550
        wp = semantics.parent / 'windows' / f"Draw02-{render.replace(' ', '_')}-{mesh}.json"
        window_evidence = json.loads(wp.read_text())
        assert np.array_equal(faces, window_evidence['indices'])
        assert np.array_equal(native[f'mesh{mesh}_uv0'], window_evidence['uv0'])
        window_domains = set(window_evidence['sourceLocalDomains'])
    if selected:
        assert entry['sourceDigest'] == digest
        # Each complete independently authored member traverses an interior atlas
        # patch once, using UV0; repeated instances reset to the same mapping.
        # The descriptor has no UV transform or alternate projection override.
        for m in members:
            use = uv[np.unique(faces[m['faceIds']])]
            assert np.isfinite(use).all() and (use > 0).all() and (use < 1).all()
            assert np.array_equal(use.min(0), m['uvMinimum'])
            assert np.array_equal(use.max(0), m['uvMaximum'])
            for unit in units:
                lo, hi = np.array(unit['bounds'][:2]), np.array(unit['bounds'][2:])
                assert np.any(use.max(0) < lo) or np.any(use.min(0) > hi)
    at = 2
    flags = []
    reasons = Counter()
    for domain in range(int(values[1])):
        count = abs(int(values[at + 3])); at += 4
        ids = set(map(int, values[at:at + count])); at += count
        assert not (ids & selected) or ids <= selected, 'Mixed authored membership'
        use = uv[faces[sorted(ids)]].reshape(-1, 2)
        lo, hi = use.min(0), use.max(0)
        # Native 571 samples UV0 directly; 581 samples the UV0 base separately
        # from its worldspace detail. Complete source-domain traversal is tested,
        # not the packed destination chart or a failed coverage result.
        known_shader = source['template'] in (0, 1, 17)
        single_tile = bool(np.isfinite(use).all() and np.all(hi <= np.floor(lo) + 1))
        repeat = any(entry['sourceDigest'] == u['sourceDigest'] and
            np.all(hi >= np.array(u['bounds'][:2])) and np.all(lo <= np.array(u['bounds'][2:])) for u in units)
        positive_network = source['template'] == 17 and hi[1] >= 0
        reason = ('verified-local-window-yzw-color' if domain in window_domains else
                  'window-repeated-pane-traversal' if not known_shader else
                  'native-network-length-mapping' if positive_network else 'repeat-region-overlap' if repeat
                  else 'multiple-tile-traversal' if not single_tile else 'authored-uv0-single-traversal')
        reasons[reason] += 1
        flags.append(int(domain in window_domains or (known_shader and not positive_network and single_tile and not repeat)))
    assert at == len(values)
    ints.extend([len(flags), *flags])
    records.append(dict(**entry, domains=len(flags), nonTilingDomains=sum(flags),
        measuredBraceFaces=len(selected), nativeTemplate=source['template'], classifications=dict(reasons),
        uv0Minimum=uv.min(0).tolist(), uv0Maximum=uv.max(0).tolist(),
        windowEvidence=window_evidence,
        recipeSha256=hashlib.sha256(recipe.read_bytes()).hexdigest(),
        nativeSha256=hashlib.sha256((repo / 'artifacts/uv-density/native' / (render + '.npz')).read_bytes()).hexdigest()))

output = repo / 'assets/HardcodedNumbers/Draw02/Draw02_UvSampling.bin'
output.write_bytes(write_bbn(ints, []))
cases = '\n'.join(f'            ("{r["render"]}", {r["mesh"]}) => {i},' for i, r in enumerate(records))
code = '''// Embedded numerical arrays: Draw02_UvSampling
using System;

namespace BridgeBuilder.Bridges;
// Generated by tools/PreparedMaterials/Draw02/EmitUvSampling.py.
// Only positively measured authored non-tiling mappings are true.
internal static class Draw02UvSampling
{
    private static class Data
    {
        internal static readonly bool[][] Items = Read();
    }

    internal static bool For(string render, int mesh, int domain)
    {
        int index = (render, mesh) switch
        {
CASES
            _ => -1
        };
        if (index < 0) return false;
        var flags = Data.Items[index];
        if ((uint)domain >= (uint)flags.Length)
            throw new InvalidOperationException($"Invalid Draw02 UV domain: {render}/{mesh}/{domain}.");
        return flags[domain];
    }

    private static bool[][] Read()
    {
        var values = HardcodedNumbers.Load("Draw02_UvSampling").I32;
        if (values.Length < 2 || values[0] != 1 || values[1] != MESHCOUNT)
            throw new InvalidOperationException("Invalid Draw02 UV sampling schema.");
        var result = new bool[values[1]][];
        int at = 2;
        for (int mesh = 0; mesh < result.Length; mesh++)
        {
            if (at >= values.Length) throw new InvalidOperationException("Truncated Draw02 UV sampling data.");
            int count = values[at++];
            if (count < 0 || count > values.Length - at)
                throw new InvalidOperationException("Invalid Draw02 UV sampling count.");
            var flags = result[mesh] = new bool[count];
            for (int domain = 0; domain < count; domain++)
            {
                int flag = values[at++];
                if (flag != 0 && flag != 1) throw new InvalidOperationException("Invalid Draw02 UV sampling flag.");
                flags[domain] = flag == 1;
            }
        }
        if (at != values.Length) throw new InvalidOperationException("Trailing Draw02 UV sampling data.");
        return result;
    }
}
'''.replace('CASES', cases).replace('MESHCOUNT', str(len(records)))
(repo / 'src/BridgeBuilder/Bridges/Types/Draw02/Draw02UvSampling.cs').write_text(code, encoding='utf-8')
report = dict(schema=2, scope='All 45 source slots; verified object UV0, negative-V network mapping and local window YZW/COLOR; positive network length, repeat overlaps and repeated window panes remain density-preserving.',
    axes=['U', 'V'], sourceVersion='Cities Skylines II 1.6.2f1',
    membershipSha256=hashlib.sha256(evidence_path.read_bytes()).hexdigest(),
    surfaceSha256=digest, nativeSurface=native_surface, channels=evidence['channels'],
    sourceArchive=str(archive), sources=list(sources.values()), networkTextures=list(texture_evidence.values()),
    nativeNetworkEvidence=near_network,
    nativeTemplates=current_templates, nativeSemanticsSha256=hashlib.sha256((semantics / 'summary.json').read_bytes()).hexdigest(),
    networkWorldspace='Both near/far mask blue channels are exactly zero at every mip; native worldspace contribution is zero. Verified existing object descriptors retain all three texture bindings and Repeat sampling. Negative V and frac(V) are equivalent; positive V stays on native length/density path.',
    meshes=records, resourceSha256=hashlib.sha256(output.read_bytes()).hexdigest(),
    nativeRenderingVerified=False, localization='No user-visible strings or localization paths changed.')
(base / 'brace-semantic/non-tiling-classification.json').write_text(json.dumps(report, indent=2))
print(json.dumps(dict(meshes=len(records), nonTilingDomains=sum(r['nonTilingDomains'] for r in records), bytes=output.stat().st_size)))
