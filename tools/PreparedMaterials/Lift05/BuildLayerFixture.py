"""Export Lift05's actual measured consumer inputs for independent layer tests."""
import hashlib
import json
import struct
import zipfile
from pathlib import Path
import numpy as np
from BuildUvCharts import ROOT, inputs
from ExportRecipes import numbers, recipes, reachable_regions

OUT = ROOT / 'artifacts/layer-decoupling/Lift05/outputs'
OUT.mkdir(parents=True, exist_ok=True)
patch_recipes, recipe_hashes = recipes(ROOT)
reachable, routing = reachable_regions(ROOT)
data = numbers(ROOT / 'assets/HardcodedNumbers/Lift05/Types_Lift05_Lift05RoadAppearance_cs.bin')
f, ints = data['F32'], data['I32']
f32 = np.float32
patches = []
for moving in range(2):
    for half in range(2):
        for side in range(2):
            side_sign, half_sign = f32(-1 if side == 0 else 1), f32(-1 if half == 0 else 1)
            lo, hi = np.asarray(patch_recipes[moving]['min'],dtype='float32'), np.asarray(patch_recipes[moving]['max'],dtype='float32')
            native_min, native_max = f32(f[12+moving]), f32(f[14+moving])
            direction = f32(-side_sign*half_sign) if moving == 0 else side_sign
            gradient = f32(f32(direction*f32(native_max-native_min))/f32(f[16]))
            origin = native_max if moving == 0 and side_sign*half_sign > f[17] else native_min
            u = [float(f32(gradient/f32(hi[0]-lo[0]))), f[18], f[19], float(f32(f32(origin-lo[0])/f32(hi[0]-lo[0])))]
            v = [f[20],f[21],float(f32(-half_sign/f32(f[22]))),f[23]] if moving == 0 else [f[24],f[25],-f[26],f[27]]
            index=moving*4+half*2+side
            patches.append(dict(index=index,moving=bool(moving),half=half,side=side,repeat=True,min=lo.tolist(),max=hi.tolist(),u=u,v=v,
                projectionFrame='Identity roadToObject fixture; runtime multiplies measured vectors by actual transpose TRS.',
                nativeSamplerReachable=index in reachable,
                physicalPeriodV=1/abs(v[2]),
                float32Bits={'u':[struct.pack('<f',x).hex() for x in u],'v':[struct.pack('<f',x).hex() for x in v]}))
source_data=json.loads((ROOT/'artifacts/prepared-materials/Lift05/rebuild-native-v1/inputs/native-inputs.json').read_text())
source=next(x for x in source_data['sources'] if x['cid']=='78fda4223742d922d34b5cc2331eb65f')
with zipfile.ZipFile(source['archive']) as z:raw=z.read(source['entry'])
assert hashlib.sha256(raw).hexdigest()==source['sha256']
(OUT/'native-road-source.Surface').write_bytes(raw)
native=next((p,u,faces) for render,mesh,p,u,faces in inputs() if render=='LiftBridge05 Mesh' and mesh==0)
p,u,faces=native
face_records=[]
for ids,role in [([557,558],'fixed'),([1366,1367],'moving')]:
    face_records.append(dict(role=role,faces=ids,positions=p[faces[ids]].tolist(),uv=u[faces[ids]].tolist()))
old=json.loads((ROOT/'artifacts/prepared-materials/Lift05/repeat-unit-trial-v2/input/manifest.json').read_text())['units'][0]
result=dict(style='Lift05',schema=1,recipeHashes=recipe_hashes,routing=routing,patches=patches,
    prototypeSurface=source,nativeFaceFixtures=face_records,preparedFixedUnit=old,
    selectedRoad='Runtime-selected source; no actual selected-road source fixture is claimed by this style-only fixture.',
    independentLayers=dict(required=True,nativeObjectCapability='UNVERIFIED; existing template 0/14 checks cover only flattened/single-layer inputs',
       sourceBase='OriginalProjection(triangle) with original source texture bindings and wrap',
       sourceWorld='SourceFrame rows X/Z times selected material _WorldspaceUVScale',
       sourceBlend='Selected source authored mask in base projection; preserve independent sampling',
       prototypeReplacement='Disabled for Lift05 moving geometry (PrototypeRoad=false); fixed0..3 use prepared prototype repeat'),
    tolerance=dict(status='Pending common measured per-layer replacement; no threshold adopted',sourceCoordinateStorage='IEEE binary32 measured patch coefficients, original UV source preserved',
                   currentLegacyCutoffTexels=.01),
    acceptance='Source/consumer fixture only, not native independent-layer rendering, motion, cold-load or human acceptance.')
(OUT/'consumer-fixture.json').write_text(json.dumps(result,indent=2)+'\n')
assert len(patches)==8 and sum(x['nativeSamplerReachable'] for x in patches)==4
assert all(patches[i]['u'][0] == -patches[i+1]['u'][0] for i in (0,2,4,6))
print('PASS Lift05: 8 measured patches, 4 reachable fixed samplers, original native Surface hash verified, source face fixtures exported.')
