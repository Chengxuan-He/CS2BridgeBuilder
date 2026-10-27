"""Export Lift01 native source/sampler bindings, without making texture copies."""
import hashlib
import json
from pathlib import Path
import struct

ROOT = Path(__file__).resolve().parents[3]
SOURCE = ROOT/'artifacts/prepared-materials/Lift01/shared-source/source-sampler-map.json'
OUT = ROOT/'artifacts/prepared-materials/Lift01/native-bindings'


def f32(value):
    return struct.unpack('<f', struct.pack('<f', value))[0]


def main():
    source = json.loads(SOURCE.read_text())
    native = json.loads((SOURCE.parent/'native-sources.json').read_text())
    sources = {s['cid']: s for s in native['sources']}
    textures = source['textures']
    for texture in textures:
        # Built-in resources are referenced intact; no per-use setting is claimed
        # to override native resource-global filtering, wrapping or mip behavior.
        texture['binding'] = 'OriginalBuiltInReference'
        texture['nativeSettings'] = 'Retain exact original resource header and complete dependency graph'
    bindings = []
    corners = 0
    for record in source['samplers']:
        sampler = {k: record[k] for k in ['key', 'sourceCid', 'sourceDigest', 'triangle', 'bounds', 'a', 'b', 'c', 'channels']}
        sampler['nativeSurface'] = record['sourceCid']
        sampler['nativeTemplate'] = sources[record['sourceCid']]['template']
        sampler['mapping'] = 'SourceUvWithoutModuloReflectionOrClamping'
        sampler['eligibility'] = 'Original unconditioned coordinates inside recorded source domain with preserved Float32 gradient'
        sampler['outOfDomain'] = 'Explicitly unsupported; do not substitute an adjacent atlas island or a per-sampler baked copy'
        if record['triangle']:
            sampler['sourceOrigin'] = [f32(x) for x in record['a']]
            sampler['sourceAxisU'] = [f32(record['b'][i])-f32(record['a'][i]) for i in range(2)]
            sampler['sourceAxisV'] = [f32(record['c'][i])-f32(record['a'][i]) for i in range(2)]
            for q, expected in [((0, 0), record['a']), ((1, 0), record['b']), ((0, 1), record['c'])]:
                result = [f32(sampler['sourceOrigin'][i] + sampler['sourceAxisU'][i]*q[0] + sampler['sourceAxisV'][i]*q[1]) for i in range(2)]
                assert result == [f32(x) for x in expected]
                corners += 1
        else:
            bounds = [f32(x) for x in record['bounds']]
            sampler['sourceOrigin'] = bounds[:2]
            sampler['sourceAxisU'] = [bounds[2]-bounds[0], 0]
            sampler['sourceAxisV'] = [0, bounds[3]-bounds[1]]
            for q, expected in [((0, 0), bounds[:2]), ((1, 1), bounds[2:])]:
                result = [f32(sampler['sourceOrigin'][i] + sampler['sourceAxisU'][i]*q[0] + sampler['sourceAxisV'][i]*q[1]) for i in range(2)]
                assert result == expected
                corners += 1
        bindings.append(sampler)
    result = {'schema': 'BridgeBuilder/Lift01/NativeSourceSampling/1', 'style': 'Lift01',
              'inventorySha256': source['inventorySha256'],
              'sourceMapSha256': hashlib.sha256(SOURCE.read_bytes()).hexdigest(),
              'textureContent': textures,
              'nativeSurfaces': [{'cid': s['cid'], 'template': s['template'], 'binding': 'OriginalBuiltInReference',
                                  'samplers': s['samplers'], 'requiresObjectTemplateDecision': s['template']==33} for s in sources.values()],
              'samplingRecords': bindings,
              'integration': 'Existing compatible branch only: MovableStructureTiles.Slot.Surface = original; Slot.Original = measured unit; no texture producer. This manifest is not consumed by production.',
              'unsupported': ['Out-of-domain density-preserving UVs', 'RepeatOnly coordinates after integer phase conditioning', 'Unsupported native template conversion', 'Float32 source gradient loss'],
              'cornerMappingChecks': corners, 'newTexturePayloads': 0, 'newMaterialDescriptors': 0,
              'acceptance': {'productionPatchApplied': False, 'previewCreationRegenerationExport': False, 'coldLoadUninstall': False, 'officialSdkPackageUnder50MB': False}}
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT/'native-source-bindings.json').write_text(json.dumps(result, indent=2)+'\n')
    print(json.dumps({'samplingRecords': len(bindings), 'sourceTextures': len(textures), 'cornerChecks': corners, 'newTextures': 0, 'newDescriptors': 0}))


if __name__ == '__main__':
    main()
