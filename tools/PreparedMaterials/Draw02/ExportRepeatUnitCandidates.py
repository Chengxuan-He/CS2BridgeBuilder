"""Record measured Draw02 road units and unresolved production calls; never bake."""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from ExportRecipes import f32, numbers


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    p = argparse.ArgumentParser(description=__doc__)
    p.add_argument('repository', type=Path)
    p.add_argument('output', type=Path)
    a = p.parse_args()
    r = a.repository.resolve()
    old_path = r / 'artifacts/prepared-materials/Draw02/repeat-inventory.json'
    old = json.loads(old_path.read_text())
    density_path = r / 'artifacts/prepared-materials/Draw02/source-density.json'
    density = json.loads(density_path.read_text())
    if density['inventorySHA256'] != digest(old_path):
        raise ValueError('Source density evidence does not match the recipe inventory')
    bbn = r / 'assets/HardcodedNumbers/Draw02/Types_Draw02_Draw02RoadAppearance_cs.bin'
    if digest(bbn) != old['evidence']['numericalResourceSHA256']:
        raise ValueError('Measured road recipe changed')
    f = numbers(bbn)['F32']
    mesh_path = r / 'artifacts/uv-density/native/DrawBridge02 Mesh.npz'
    if digest(mesh_path) != density['nativeMeshSHA256']:
        raise ValueError('Native face evidence changed')
    mesh = np.load(mesh_path)
    channel_map_path = r / 'artifacts/prepared-materials/Draw02/shared-source-migration/source-sampler-map.json'
    source_map = json.loads(channel_map_path.read_text())
    source = next(s for s in source_map['sources'] if s['cid'] == old['units'][0]['sourceCid'])
    channels = [c for c in source_map['textureChannels'] if c['identity'] in source['channelIdentities']]
    if len(channels) != 4 or any(c['width'] != 4096 or c['height'] != 4096 for c in channels):
        raise ValueError('Measured Draw02 channel set/dimensions changed')
    gradients = [(f32(f[32]/f[33]),f[22]), (f[25],f[22]),
                 (f32(f[42]/f[43]),f[44]), (f[25],f[51])]
    records, calls = [], []
    for index, old_unit in enumerate(old['units'][:4]):
        unit_id = 'Draw02/' + old_unit['label']
        g = gradients[index]
        extent = np.array(old_unit['max']) - np.array(old_unit['min'])
        regions = old_unit['bands'] or [dict(start=0.,end=1.,min=old_unit['min'],max=old_unit['max'],repeatY=1.)]
        measured_regions = []
        for band_index, band in enumerate(regions):
            span = band['end'] - band['start']
            pixels = (np.array(band['max']) - np.array(band['min'])) * 4096
            measured_regions.append(dict(**band,sourcePixelExtent=pixels.tolist(),
                physicalSize=[span/g[0], 1./(g[1]*band['repeatY'])],
                texelsPerMeter=[pixels[0]*g[0]/span,pixels[1]*g[1]*band['repeatY']]))
        faces=[]
        for row in density['results']:
            if row['unit'] != index: continue
            ids=mesh['mesh0_faces'][row['face']]
            faces.append(dict(face=row['face'],band=row['band'],
                positions=mesh['mesh0_positions'][ids].tolist(),uv0=mesh['mesh0_uv0'][ids].tolist(),
                relativeMetricError=row['relativeMetricError']))
        keys=[u for u in old['units'] if u['label'] == old_unit['label'] or u['label'] == old_unit['label']+'-opaque']
        records.append(dict(id=unit_id,sourceCid=old_unit['sourceCid'],sourceSha256=old_unit['sourceDigest'],
            region=dict(min=old_unit['min'],max=old_unit['max']),bands=old_unit['bands'],bandRows=False,
            physicalSize=[1./g[0],1./g[1]],projectionCyclesPerMeter=list(g),regions=measured_regions,
            prototype=dict(render='DrawBridge02 Mesh',mesh=0,sourceGeometrySha256='1f0ff1a2c0a8318e6537bd347ae064d1bb55bfa48f3cf3c2375601adcd05dd18',
                meshNpzSha256=digest(mesh_path),faces=faces),
            boundaryEvidence=dict(status='not-verified',required='Compare all physical joins and opposite tile edges in every active channel through original mip levels and native filtering; no mip-zero regenerated-chain substitution.'),
            mipEvidence=dict(status='original-native-graph-identified-only',channels=channels,extractionVerified=False),
            authorizedForBake=False,
            reason='Measured original road region and physical cell; complete tileable boundary/mip evidence is still required. The old repeat flag is not sufficient proof.',
            legacyKeys=[dict(key=u['key'],cutout=u['cutout']) for u in keys]))
        for u in keys:
            calls.append(dict(path='Repeat/'+u['key']+'/Repeat.Surface',key=u['key'],unit=unit_id,
                purpose=u['label'],patchIndices=[index*2,index*2+1,index*2+8,index*2+9],
                sourceCall='Draw02RoadAppearance.Patches -> MovableRoadPreparation.Prepare -> MovableSurfaceAtlas.Compose',
                physicallyPresent=(r/'assets/Materials/Draw02/Repeat'/u['key']/'Repeat.Surface').exists(),
                nativeRepeatFlag=index<2,
                note='Fixed patches omit explicit Repeat, but the composer also routes valid prototype projections to Repeat resources.'))
    inv_path=r/'artifacts/uv-density/structure-tessellated-sources.json'
    inv=json.loads(inv_path.read_text())
    structural=[u for u in inv['units'] if u['style']=='Draw02']
    expected={u['key'] for u in source_map['samplers']}
    if {u['key'] for u in structural} != expected:
        raise ValueError('Production structure inventory no longer matches source map')
    structural_calls=[dict(key=u['key'],sourceCid=u['sourceCid'],triangle=u['triangle'],
        path='Structure/'+u['key']+'/Structure.Surface',
        physicallyPresent=(r/'assets/Materials/Draw02/Structure'/u['key']/'Structure.Surface').exists(),
        unit=None,status='no-genuine-repeat-unit-evidence',
        dispatch='MovableStructureTiles.Build: original/converted Surface when CanUseSource; otherwise Structure key lookup') for u in structural]
    summary=dict(roadCandidateUnits=len(records),roadLegacyDescriptorCalls=len(calls),
        roadMissingFiles=sum(not c['physicallyPresent'] for c in calls),
        structuralCatalogueKeys=len(structural_calls),structuralMissingFiles=sum(not c['physicallyPresent'] for c in structural_calls),
        structuralTriangleKeys=sum(c['triangle'] for c in structural_calls),
        bakeApprovedUnits=0,
        caveat='Missing catalogue files enumerate possible prepared fallbacks, not a claim every key executes at every width. CanUseSource is receiver-specific; no finite width sample authorizes dropping other calls.')
    a.output.mkdir(parents=True,exist_ok=True)
    manifest=dict(schema='BridgeBuilder/RepeatUnitCandidates/2',style='Draw02',readyForProduction=False,
        source=source,units=records,summary=summary,
        evidence=dict(roadInventorySha256=digest(old_path),sourceDensitySha256=digest(density_path),
            bbnSha256=digest(bbn),productionStructureInventorySha256=digest(inv_path)))
    (a.output/'units.json').write_text(json.dumps(manifest,indent=2)+'\n')
    (a.output/'calls.json').write_text(json.dumps(dict(style='Draw02',repeat=calls,structure=structural_calls,summary=summary),indent=2)+'\n')
    print(json.dumps(summary))


if __name__=='__main__': main()
