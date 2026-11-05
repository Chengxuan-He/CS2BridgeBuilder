"""Author a bounded Draw02 moving-walk side-edge repeat from its original PBR."""
from pathlib import Path
import hashlib, json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[3]
# Reuse the reviewed native lossless texture codec and periodic normal mip builder.
# Execute only their declarations/codec setup, never the lane producer's main body.
helper = ROOT / 'tools/PreparedMaterials/Draw02/PrepareLaneUnits.py'
declarations = helper.read_text().split("plan=json.loads(", 1)[0]
scope = {'__file__': str(helper)}
exec(compile(declarations, str(helper), 'exec'), scope)
OUT = ROOT / 'artifacts/material-remap/Draw02/edge55/curb-unit'
OUT.mkdir(parents=True, exist_ok=True)
scope['OUT'] = OUT

source = '8a28ab8d9526e258efbcb81c3bbc936b'
old = json.loads((ROOT / 'artifacts/material-remap/Draw02/offline-atlas-units/producer-report-raw.json').read_text())['units'][0]
original_surface = (ROOT / 'assets/Materials/Draw02/RepeatUnits' / old['key'] / 'Repeat.Surface').read_bytes()
products = []
for unit, minimum, maximum, physical_width, source_faces in [
    ('MovingWalkEdge', [.64111328125,.27392578125], [.64892578125,.475830078125], .14990234375, [3746,3747]),
    ('FixedWalkEdge', [.30322265625,.48876953125], [.3125,.67236328125], .199951171875, [2518,2519,2520,2521])]:
    minimum, maximum = np.array(minimum), np.array(maximum)
    low, high = (minimum * 4096).astype(int), (maximum * 4096).astype(int)
    bindings, channels = {}, {}
    for name in scope['HASHES']:
        original = scope['read'](name)
        pixels = original[low[1]:high[1], low[0]:high[0]].copy()
        levels = scope['mipchain'](pixels, name)
        bindings[name], _ = scope['texture'](levels, name == '_BaseColorMap')
        channels[name] = [dict(level=i, width=p.shape[1], height=p.shape[0],
            sha256=hashlib.sha256(p.tobytes()).hexdigest(),
            seamU=np.abs(p[:, 0].astype(int)-p[:, -1].astype(int)).max(0).tolist(),
            seamV=np.abs(p[0].astype(int)-p[-1].astype(int)).max(0).tolist()) for i, p in enumerate(levels)]
        if name in ('_BaseColorMap','_NormalMap'):
            Image.fromarray(pixels).save(OUT / (unit+name+'.png'))
    surface = original_surface
    for channel in old['channels']:
        original_id = scope['cidbytes'](channel['cid'])
        assert surface.count(original_id) == 1
        surface = surface.replace(original_id, scope['cidbytes'](bindings[channel['name']]))
    cid, _ = scope['write'](surface, '.Surface', 'RepeatUnits/'+unit, 'Repeat.Surface')
    products.append(dict(name=unit, sourceSurface=source, sourceFaces=source_faces, sourceLod=0,
        sourceUvMin=minimum.tolist(), sourceUvMax=maximum.tolist(), physicalSize=[physical_width,4.],
        sourcePixelBounds=[low.tolist(),high.tolist()], surface=cid, bindings=bindings, channels=channels))
report = dict(sourceHashes=scope['HASHES'], helperSha256=hashlib.sha256(helper.read_bytes()).hexdigest(),
    units=products, repeatAxes='UV', classification='Authored road-edge repeats from measured moving-walk closure and fixed-platform curb. U was a finite native cross section; V retains one complete 4 m source region.',
    operations='Exact decoded base texel crop in all four channels. No paint, donor substitution or seam blend. Complete area-filtered periodic mips, linear-light base RGB, decoded normalized AG normal mips, lossless native texture storage.',
    acceptance='Private candidates; native loading, actual curb receiver mapping and visual edge acceptance pending.')
(OUT / 'composition.json').write_text(json.dumps(report, indent=2))
print(json.dumps([dict(name=u['name'],surface=u['surface'],size=u['channels']['_BaseColorMap'][0]) for u in products]))
