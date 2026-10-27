"""Record Lift04 source texture identities separately from immutable samplers.

This authoring inventory changes no game assets and bakes no texture. A mapping
is not an implementation of a native region/triangle repeat sampler.
"""
import argparse
import hashlib
import json
import re
import struct
from collections import Counter
from pathlib import Path

from VerifyCoverage import hash_text, read_assets


def digest(data):
    return hashlib.sha256(data).hexdigest()


def explicit_texture_bindings(payload):
    """Read native descriptor bindings without changing any original resource."""
    offset = 7
    if payload[6]:
        offset += 276
        if payload[offset] == 255:
            offset += 17
        elif payload[offset] == 0:
            offset += 1
        else:
            raise ValueError('Invalid native Surface presence marker')

    def count():
        nonlocal offset
        present = payload[offset]
        offset += 1
        if present == 0:
            return 0
        if present != 255:
            raise ValueError('Invalid native dictionary marker')
        value = struct.unpack_from('<i', payload, offset)[0]
        offset += 4
        if value < 0:
            raise ValueError('Invalid native dictionary size')
        return value

    def string():
        nonlocal offset
        size = shift = 0
        while True:
            byte = payload[offset]
            offset += 1
            size |= (byte & 127) << shift
            if byte < 128:
                break
            shift += 7
        value = payload[offset:offset + size].decode('utf-8')
        offset += size
        return value

    for size in (4, 4, 16, 16):
        for _ in range(count()):
            string()
            offset += size
    bindings = []
    for _ in range(count()):
        name = string()
        present = payload[offset]
        offset += 1
        if present == 255:
            bindings.append(dict(property=name, sourceTextureCid=hash_text(payload[offset:offset + 16])))
            offset += 16
        elif present != 0:
            raise ValueError('Invalid native texture binding')
    for _ in range(count()):
        string()
    if offset != len(payload):
        raise ValueError('Unconsumed native descriptor bytes')
    return bindings


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', type=Path, default=Path(__file__).resolve().parents[3])
    parser.add_argument('--game-data', type=Path, default=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'))
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    source_path = args.repo / 'artifacts/uv-density/structure-tessellated-sources.json'
    data = json.loads(source_path.read_text())
    units = [u for u in data['units'] if u['style'] == 'Lift04']
    if not units or len({u['key'] for u in units}) != len(units):
        raise ValueError('Missing or duplicate Lift04 sampling records')
    render_path = args.repo / 'src/BridgeBuilder/Bridges/Types/Lift04/Lift04SourceRenders.cs'
    renders = []
    for match in re.finditer(r'Create\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+) \}', render_path.read_text(encoding='utf-8-sig')):
        renders.append(dict(render=match[1], geometryCid=match[2],
                            originalSurfaceCids=re.findall(r'"([a-f0-9]{32})"', match[3])))
    if len(renders) != 9 or any(not r['originalSurfaceCids'] for r in renders):
        raise ValueError('Review changed Lift04 complete part/LOD bindings')
    source_ids = {u['sourceCid'] for u in units}
    render_ids = {cid for r in renders for cid in r['originalSurfaceCids']}
    if not source_ids <= render_ids:
        raise ValueError('Sampler references a source absent from the original render graph')
    sources = read_assets(args.game_data, render_ids)
    descriptions = {s['cid']: s for s in data['sources']}
    bindings, needed = {}, set()
    for cid, payload in sources.items():
        explicit = explicit_texture_bindings(payload)
        if cid not in source_ids:
            # The glass material is not part of the structural sampler list, but
            # remains an original native binding at both detailed LODs.
            if payload[6]:
                raise ValueError('Review changed nonstructural native dependency graph')
            bindings[cid] = explicit
            needed.update(c['sourceTextureCid'] for c in explicit)
            continue
        if struct.unpack_from('<H', payload)[0] != 1 or payload[6] != 1 or payload[283] != 0:
            raise ValueError('Review changed source descriptor or VTSurface backing: ' + cid)
        if any(u['sourceDigest'] != digest(payload) for u in units if u['sourceCid'] == cid):
            raise ValueError('Prototype source digest changed: ' + cid)
        channels = []
        for channel in descriptions[cid]['channels']:
            offset = 83 + channel['stack'] * 136 + channel['layer'] * 16
            texture_cid = hash_text(payload[offset:offset + 16])
            if texture_cid == '0' * 32:
                continue
            channels.append(dict(property=channel['name'], sourceTextureCid=texture_cid))
            needed.add(texture_cid)
        bindings[cid] = channels
    native = read_assets(args.game_data, needed)
    textures = []
    identities = {}
    for cid, payload in native.items():
        version = struct.unpack_from('<H', payload)[0]
        if version == 1 and payload[2] == 255 and struct.unpack_from('<H', payload, 19)[0] == 0:
            _, width, height = struct.unpack_from('<3H', payload, 21)
            mips, graphics_format, kind = payload[29], payload[30], 'VTTexture'
        elif version in (4, 5) and struct.unpack_from('<H', payload, 6)[0] == 1:
            width, height = struct.unpack_from('<2H', payload, 2)
            mips, graphics_format, kind = payload[8], payload[9], 'Texture'
        else:
            raise ValueError('Review native texture encoding: ' + cid)
        roles = sorted({c['property'] for b in bindings.values() for c in b if c['sourceTextureCid'] == cid})
        # The complete native payload includes every mip. Do not include sampler
        # coordinates in this conservative source-content identity.
        key = digest(json.dumps(dict(payloadSHA256=digest(payload), roles=roles,
                                     format=graphics_format, width=width, height=height,
                                     mips=mips), sort_keys=True).encode())
        identities[cid] = key
        textures.append(dict(key=key, sourceTextureCid=cid, nativePayloadSHA256=digest(payload),
                             nativeBytes=len(payload), width=width, height=height,
                             mipCount=mips, format=graphics_format, kind=kind, properties=roles,
                             builtIn=True, directReferenceWhenCompatible=True))
    for binding in bindings.values():
        for channel in binding:
            channel['textureKey'] = identities[channel['sourceTextureCid']]
    records = [dict(u, channels=bindings[u['sourceCid']],
                    operation='triangle-reflect-repeat' if u['triangle'] else 'region-repeat') for u in units]
    physical = list((args.repo / 'assets/Materials/Lift04').rglob('*.Texture'))
    summary = dict(style='Lift04', samplers=len(units), sourceSurfaceCids=len(source_ids),
                   distinctSourceSurfacePayloads=len({digest(sources[cid]) for cid in source_ids}),
                   originalRenderBindings=len(renders), originalSurfaceCids=len(render_ids),
                   nativeTextureCids=len(native), textureContentIdentities=len({t['key'] for t in textures}),
                   nativeTextureBytes=sum(map(len, native.values())),
                   currentStyleTextureFiles=len(physical), currentStyleTextureBytes=sum(p.stat().st_size for p in physical),
                   operations=dict(Counter(r['operation'] for r in records)),
                   status='MAPPING_READY_NATIVE_SAMPLER_UNRESOLVED', bakedTextures=0,
                   localization='No user-visible text or locale behavior changed.')
    report = dict(schema='Lift04/SourceTextureSamplingMap/2', inventorySHA256=digest(source_path.read_bytes()),
                  sourceRenderSHA256=digest(render_path.read_bytes()),
                  summary=summary, textures=textures, samplers=records)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n')
    catalog = dict(schema='Lift04/NativeSurfaceBindings/1', style='Lift04',
                   sourceRenderSHA256=report['sourceRenderSHA256'], renderBindings=renders,
                   surfaces=[dict(cid=cid, sourceSHA256=digest(payload), template=struct.unpack_from('<i', payload, 2)[0],
                                  isVT=bool(payload[6]), mode='OriginalBuiltinReference',
                                  textureBindings=bindings[cid]) for cid, payload in sorted(sources.items())],
                   textures=textures, generatedSurfaceFiles=0, generatedTextureFiles=0,
                   status='SOURCE_BINDINGS_VERIFIED_DENSITY_EXTENSION_PENDING',
                   limits=['This catalog retains original full native dependency references; it is not a custom runtime loader.',
                           'Out-of-region density-preserving sampling remains unresolved; do not alias those outputs to these sources.',
                           'Original native mip bytes are retained by reference; full native loading and per-owner acceptance remain pending.'])
    args.output.with_name('native-bindings.json').write_text(json.dumps(catalog, indent=2) + '\n')
    print(json.dumps(summary, indent=2))


if __name__ == '__main__':
    main()
