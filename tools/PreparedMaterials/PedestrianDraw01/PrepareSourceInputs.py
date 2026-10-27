"""Prepare fresh source-level native input; never read prepared bridge materials.

Files in the private authoring environment are native inputs, not a distributable
material package. Compatible built-in sources must remain references in output.
"""
import argparse
from collections import Counter
import hashlib
import json
from pathlib import Path
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('game_data', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    evidence = args.repository/'artifacts/prepared-materials/PedestrianDraw01/shared-sampling'
    native = json.loads((evidence/'native-bindings.json').read_text())
    sampling = json.loads((evidence/'source-map.json').read_text())
    repeat = json.loads((args.repository/'artifacts/prepared-repeat/PedestrianDraw01/recipes.json').read_text())
    expected = native['nativeDependencyGraph']
    # Explicit new task scope. Never let the input copier become an assets writer.
    allowed = (args.repository/'artifacts/agent-material-env/PedestrianDraw01').resolve()
    destination = args.output.resolve()
    if destination == allowed or allowed not in destination.parents:
        raise ValueError('Output must be a child of the private PedestrianDraw01 environment.')
    if destination.exists():
        raise FileExistsError('Use a new output directory; fresh preparation never overlays old products.')
    archives, lookup = [], {}
    try:
        for family in ('Game', 'BridgesAndPorts'):
            for path in (args.game_data/'Content'/family).glob('*.cok'):
                if not path.name.startswith(('Blob', 'VT')):
                    continue
                archive = zipfile.ZipFile(path)
                archives.append(archive)
                for entry in archive.namelist():
                    if entry.endswith('.cid'):
                        identity = archive.read(entry).decode().strip()
                        if identity in expected:
                            lookup.setdefault(identity, []).append((archive, entry[:-4]))
        payloads = {}
        for identity, node in expected.items():
            matches = lookup[identity]
            data = matches[0][0].read(matches[0][1])
            assert all(archive.read(entry) == data for archive, entry in matches)
            assert len(data) == node['bytes'] and hashlib.sha256(data).hexdigest() == node['sha256']
            payloads[identity] = data
        inputs = destination/'inputs'
        inputs.mkdir(parents=True)
        resources = []
        for identity, data in sorted(payloads.items()):
            node = expected[identity]
            name = identity+'.'+node['kind']
            (inputs/name).write_bytes(data)
            (inputs/(name+'.cid')).write_text(identity, encoding='ascii')
            resources.append(dict(cid=identity, kind=node['kind'], path='inputs/'+name,
                bytes=len(data), sha256=node['sha256'], builtin=True,
                targetPolicy='retain-original-reference', nativeMetadata=node))
        inventory = dict(schema='BridgeBuilder/NativeSourceInputs/1', style='PedestrianDraw01',
            resources=resources, renders=native['nativeRenderBindings'],
            sourceChannelBindings=native['sourceTextureBindings'],
            sampling=sampling['samplers'], fixedRoadSampling=repeat['units'],
            uvChartResource='UvCharts_PedestrianDraw01',
            textureConversionRequired=False,
            descriptorConversions=[],
            unresolved=['Fixed-road regional repeat/cutout requires the shared native sampling interface.',
                'Deformed structural receivers outside reliable charts require a supported native mapping; no per-sampler bake fallback.'],
            outputRequirements=dict(onePayloadPerSource=True, preserveCompleteMips=True,
                preserveNativeNormals=True, originalCompatibleMaterialCreatesNoNewMaterial=True))
        (destination/'source-inputs.json').write_text(json.dumps(inventory, separators=(',', ':'))+'\n', encoding='utf-8')
        manifest = dict(style='PedestrianDraw01', sources=[
            dict(cid=r['cid'], sha256=r['sha256'], template=r['nativeMetadata']['template'])
            for r in resources if r['kind'] == 'Surface'])
        (destination/'sources.json').write_text(json.dumps(manifest, indent=2)+'\n', encoding='utf-8')
        summary = dict(style='PedestrianDraw01', nativeInputs=Counter(node['kind'] for node in expected.values()),
            nativeInputBytes=sum(map(len, payloads.values())), pixelSources=sum(node['kind'] != 'Surface' for node in expected.values()),
            samplerRecords=len(sampling['samplers']), packagedPixelsRequiredForBuiltinReferences=0,
            oldPreparedLibraryRead=False, oldBakerRun=False, outputMaterialFiles=0,
            implementationComplete=False)
        (destination/'source-input-report.json').write_text(json.dumps(summary, indent=2)+'\n')
        print(json.dumps(summary))
    finally:
        for archive in archives:
            archive.close()


if __name__ == '__main__':
    main()
