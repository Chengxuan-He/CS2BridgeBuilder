"""Prepare fresh Lift01 source/config inputs without using old generated materials."""
import argparse
import hashlib
import json
from pathlib import Path
import re
import struct
import zipfile

from ExportRecipes import source_recipes, recipe_key
from PrepareNativeObjects import ROOT, GAME


def sha(raw):
    return hashlib.sha256(raw).hexdigest()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    out = args.output.resolve()
    config = out / 'config'
    config.mkdir(parents=True, exist_ok=True)
    references = []
    source_files = ['Lift01SourceRenders.cs', 'Lift01PillarSources.cs', 'Lift01ApproachSources.g.cs']
    for name in source_files:
        path = ROOT / 'src/BridgeBuilder/Bridges/Types/Lift01' / name
        text = path.read_text(encoding='utf-8-sig')
        for render, mesh, material_list in re.findall(r'Create(?:<\w+>)?\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+) \}', text):
            for slot, cid in enumerate(re.findall(r'"([a-f0-9]+)"', material_list)):
                references.append(dict(render=render, meshCid=mesh, slot=slot, sourceCid=cid,
                                       code=name, codeSha256=sha(path.read_bytes())))
    required = {r['sourceCid'] for r in references}
    assert required and len(references) >= 23
    found = {}
    for family in ('Game', 'BridgesAndPorts'):
        for archive_path in (GAME / 'Content' / family).glob('Blob*.cok'):
            with zipfile.ZipFile(archive_path) as archive:
                for entry in archive.namelist():
                    if not entry.endswith('.Surface.cid'):
                        continue
                    cid = archive.read(entry).decode().strip()
                    if cid not in required:
                        continue
                    raw = archive.read(entry[:-4])
                    if cid in found:
                        assert found[cid]['sha256'] == sha(raw)
                        continue
                    destination = out / 'sources' / (cid + '.Surface')
                    destination.parent.mkdir(parents=True, exist_ok=True)
                    destination.write_bytes(raw)
                    version, template, virtual = struct.unpack_from('<Hi?', raw)
                    assert version == 1
                    found[cid] = dict(cid=cid, template=template, vt=virtual, builtin=True,
                                      sha256=sha(raw), bytes=len(raw), archive=str(archive_path),
                                      entry=entry[:-4], inputPath=str(destination),
                                      policy='descriptor-only template conversion' if template == 33 else 'direct original reference')
    assert required == set(found), sorted(required - set(found))
    # These are unchanged measured input records, not generated material products.
    recipes, evidence = source_recipes(ROOT)
    floor = next(r['sourceCid'] for r in references if r['render'] == 'LiftBridge01 Mesh' and r['slot'] == 0)
    old_inventory = json.loads((ROOT / 'tools/PreparedMaterials/Lift01/repeat-recipes.json').read_text())
    source_channels = next(s for s in old_inventory['sources'] if s['cid'] == floor)
    assert source_channels['template'] == found[floor]['template']
    units = [dict(style='Lift01', sourceCid=floor, sourceDigest=found[floor]['sha256'],
                  key=recipe_key(found[floor]['sha256'], r), **r) for r in recipes]
    (config / 'repeat-inventory.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RoadRepeat/1',
        sources=[source_channels], units=units, evidence=evidence), indent=2) + '\n')
    graph = dict(style='Lift01', sources=list(found.values()), usages=references,
                 sourceSurfaceCount=len(found), sourceUsages=len(references),
                 oldGeneratedInputsRead=False, texturePayloadsCopied=0,
                 descriptorConversionSourceCount=sum(s['template'] == 33 for s in found.values()),
                 roadRepeatUnitCount=len(units),
                 roadRepeatRequirement='One measured repeat unit at original 216/215 texels per metre; never per receiver or triangle.',
                 structuralRequirement='Original shared source graph and per-use original UV charts; no old per-triangle products.',
                 sharedBakerPending=['Road native conversion and transparent zero', 'Paint', 'Cable', 'Lane variants', 'Repeat source region conversion'],
                 state='Fresh source/config inputs; not complete production delivery or native rendering proof.')
    (config / 'source-graph.json').write_text(json.dumps(graph, indent=2) + '\n')
    print(json.dumps({k: v for k, v in graph.items() if k not in ('sources', 'usages')}, indent=2))


if __name__ == '__main__':
    main()
