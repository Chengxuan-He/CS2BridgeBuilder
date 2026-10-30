"""Export source-evidenced Draw02 layer inputs without baking or changing assets."""
import argparse
import hashlib
import json
from pathlib import Path
import runpy
import struct
import zipfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('output', type=Path)
    parser.add_argument('--archive', type=Path, default=Path(
        'D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/'
        'Cities2_Data/Content/BridgesAndPorts/Blob.cok'))
    args = parser.parse_args()
    repo = Path(__file__).resolve().parents[3]
    read = runpy.run_path(str(Path(__file__).with_name('ExportRecipes.py')))
    decode = runpy.run_path(str(repo / 'tools/PreparedMaterials/Lift05/MapSharedSampling.py'))
    numeric = repo / 'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02RoadAppearance_cs.bin'
    values = read['numbers'](numeric)
    f = values['F32']
    assert len(f) == 56 and len(values['I32']) == 41
    expected = {
        '8a28ab8d9526e258efbcb81c3bbc936b': 'e8c7355d952fdd88921b83fc08e5bc66018457292182aa46bca9d79a896a9597',
        '436d1828c36d1336cf115c85def2c62c': '9d7752868c2c364f68b3cf22e5a53ee7d6fe1b283bc9c69cf03e9316184a692b',
    }
    sources = {}
    with zipfile.ZipFile(args.archive) as archive:
        for entry in archive.namelist():
            if not entry.endswith('.Surface.cid'):
                continue
            raw_id = archive.read(entry)
            identity = decode['cid'](raw_id) if len(raw_id) == 16 else raw_id.decode().strip()
            if identity not in expected:
                continue
            raw = archive.read(entry[:-4])
            digest = hashlib.sha256(raw).hexdigest()
            assert digest == expected[identity]
            native = decode['surface'](raw)
            assert native['template'] == 1 and native['isVT']
            sources[identity] = dict(cid=identity, archiveEntry=entry[:-4], sha256=digest,
                                     bytes=len(raw), descriptor=native)
        assert sources.keys() == expected.keys()
    result = dict(
        schema='BridgeBuilder/Draw02IndependentLayerInputs/1', style='Draw02',
        callChain=['Draw02RoadSampling.PrepareRenderLevels', 'MovableRoadPreparation.Prepare',
                   'MovableSurfaceAtlas.Compose'],
        prototypeRoad=True, roadPrototypeSource='8a28ab8d9526e258efbcb81c3bbc936b',
        roadLods=3, sources=sources,
        numericalResource=dict(path=str(numeric.relative_to(repo)),
            sha256=hashlib.sha256(numeric.read_bytes()).hexdigest(),
            values=values, floatBits=[struct.pack('<f', value).hex() for value in f]),
        projectionFamilies=[
            dict(name=name, minimum=list(f[start:start+2]), maximum=list(f[start+2:start+4]))
            for name, start in [('moving-carriageway',28), ('moving-sidewalk',34),
                                ('fixed-road',38), ('fixed-sidewalk',47)]],
        movingLongitudinalRates=[-f[22], f[23]],
        movingLongitudinalPeriods=[abs(1/f[22]), abs(1/f[23])],
        movingTransverseRate=read['f32'](-f[32]/f[33]),
        sidewalkTransverseRates=[-f[25], f[26]],
        constraints=[
            'Three transverse carriageway bands include grating and solid strips.',
            'Four patch families have sixteen side/leaf projection aliases.',
            'Sidewalk U origins depend independently on actual left/right outer edges.',
            'Selected-road base/mask/worldspace inputs are runtime-selected, not these prototype sources.',
            'Near prototype Surface supplies road appearance across all three road LODs.',
            'Far structural Surface remains separate and must not replace near road appearance.',
            'Object template 1 alone does not prove independent selected-road/prototype layer capability.',
            'No full-road rasterization, native layer implementation or tolerance acceptance is claimed.'
        ])
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(result, indent=2)+'\n', encoding='utf-8')
    print('PASS Draw02 layer source audit: 2 native descriptors; exact BBN arrays; 4 patch families; no asset writes.')


if __name__ == '__main__':
    main()
