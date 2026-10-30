"""Keep the original transverse UV boundary and share complete half-width units."""
import argparse
import copy
import hashlib
import json
import struct
from pathlib import Path


def main():
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('full_manifest',type=Path);p.add_argument('output',type=Path);p.add_argument('--frames',type=Path,required=True)
    a=p.parse_args();a.output.mkdir(parents=True,exist_ok=True)
    original=json.loads(a.full_manifest.read_text());units=[];keys={};bindings=[]
    frames=json.loads(a.frames.read_text())
    for region,full in enumerate(original['units']):
        for level in full['originalFaces']:
            sampled=[f for d in frames['domains'] if d['lod']==level['lod'] for f in d['perFace'] if f['face'] in level['faces']]
            assert {tuple(t) for f in sampled for t in f['tangents']}=={(-1.,0.,0.,-1.)}
            assert {tuple(n) for f in sampled for n in f['normals']}=={(0.,1.,0.)}
        for side,band in enumerate(full['bands']):
            low=[min(x,y) for x,y in zip(band['min'],band['max'])]
            high=[max(x,y) for x,y in zip(band['min'],band['max'])]
            identity=tuple(low+high)
            if identity not in keys:
                unit=copy.deepcopy(full);unit.pop('bands');unit['min']=low;unit['max']=high
                unit['physicalSize']=[full['physicalSize'][0]/2,full['physicalSize'][1]]
                unit['id']='half-width-'+str(len(units));unit['mapping']='One complete original half-width walking chart and one evidenced longitudinal cell. Original x=0 boundary retained in geometry; both texture axes sampled without mirroring offline.'
                # Measured identity frame requires no pixel re-encoding. Leaving
                # the optional transform absent preserves native packed RGBA.
                unit['finiteSourceGrid']=True
                unit['normalFrameEvidence']=dict(path=str(a.frames.resolve()),sha256=hashlib.sha256(a.frames.read_bytes()).hexdigest(),
                    sourceNormal=[0.,1.,0.],sourceTangent=[-1.,0.,0.,-1.],targetTangent=[-1.,0.,0.,-1.],matrix=[1.,0.,0.,0.,1.,0.,0.,0.,1.],
                    reason='Runtime retains original negative U and V gradients. The canonical original packed frame matches the resulting target frame; no offline frame rotation.')
                keys[identity]=unit['id'];units.append(unit)
            bindings.append(dict(region=region*2+side,parentRegion=region,side=side,unit=keys[identity],
                originalHalfWidth=full['physicalSize'][0]/2,origin=full['phase']['zOrigin'],period=full['physicalSize'][1],
                unitUOffset=.25 if side==0 else .75,unitUScaleNumerator=-.5,
                unitVScale=-1/full['physicalSize'][1],unitVOffset=1+full['phase']['zOrigin']/full['physicalSize'][1]))
    assert len(units)==3 and len(bindings)==8
    (a.output/'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3',style=original['style'],units=units),indent=2)+'\n')
    (a.output/'bindings.json').write_text(json.dumps(dict(appearanceRegions=bindings,
        geometry='Only the original x=0 transverse UV boundary is retained. No longitudinal repeat cuts are added.',
        sourceManifestSHA256=hashlib.sha256(a.full_manifest.read_bytes()).hexdigest()),indent=2)+'\n')
    print('PASS: three complete half-width units, eight original appearance domains.')


if __name__=='__main__':main()
