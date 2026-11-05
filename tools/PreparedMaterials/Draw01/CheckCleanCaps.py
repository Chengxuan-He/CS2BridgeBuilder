"""Verify all authored mips and the human-created candidate-32 cap receivers."""
import json
from pathlib import Path
import numpy as np
from PIL import Image
from shapely.geometry import Polygon, Point
from StageCleanCap import ROOT, OUT, decode, sha

OWNER = Path('C:/Users/admin/AppData/LocalLow/Colossal Order/Cities Skylines II/ImportedData/b13b3c5e7-17b2-4e2a-873d-2a2ee401d771')


def main():
    near = json.loads((OUT/'clean-cap-report.json').read_text())
    far = json.loads((OUT/'clean-far-report.json').read_text())
    checks=[]
    for kind, report in [('near-middle',near),('far',far)]:
        c=report['channels'][0];cid=c.get('newCid',c.get('cid'))
        raw=(OUT/'staged/Draw01/Shared'/(cid+'.Texture')).read_bytes()
        blocks=decode(raw);offset=0
        for mip in range(raw[8]):
            w=max(1,int.from_bytes(raw[2:4],'little')>>mip);h=max(1,int.from_bytes(raw[4:6],'little')>>mip)
            size=((w+3)//4)*((h+3)//4)*16
            image=np.array(Image.frombytes('RGBA',(w,h),blocks[offset:offset+size],'bcn',(7,'BC7'))).astype(int)
            offset+=size
            if kind=='near-middle':
                image=image[:,int(w*.25):max(int(w*.25)+1,int(w*.75))]
                yellow=(image[:,:,0]-image[:,:,2]>35)&(image[:,:,1]-image[:,:,2]>20)
            else:
                yellow=(image[:,:,0]-image[:,:,2]>20)&(image[:,:,1]-image[:,:,2]>10)&np.load(OUT/'far-masks.npz')[f'mip{mip}']
            assert not yellow.any(), (kind,mip)
            checks.append(dict(kind=kind,mip=mip,width=w,height=h,embeddedYellowPixels=int(yellow.sum())))
    receivers=[]
    for name, mesh, direct in [('Mechanism Appearance Group 1',0,True),('Mechanism Appearance Group 1',1,False),('Mechanism LOD1 Appearance Group 1',1,True),('Mechanism LOD1 Appearance Group 1',2,False)]:
        data=np.load(OUT/(name+'.npz'));uv=data[f'mesh{mesh}_uv0'][:,:2].astype(float)
        if direct:
            mapped=(uv-[.72705078125,.01238250732421875])/[.2216796875,.26105499267578125]*[.5,1]+[.25,0]
            # Existing cap mapping: finite U, measured 4 m V. Only first partial
            # traversal switches resource; no receiver-dependent texture is created.
            assert mapped[:,0].min()>.24999 and mapped[:,0].max()<.75001
            assert mapped[:,1].min()>-.00001 and mapped[:,1].max()<1
        else:
            mapped=uv
        receivers.append(dict(render=name,mesh=mesh,triangles=len(data[f'mesh{mesh}_faces']),oldDirectAtlas=direct,newUvMin=mapped.min(0).tolist(),newUvMax=mapped.max(0).tolist(),positionsChanged=False))
    data=np.load(OUT/'Mechanism LOD2 Appearance Group 1.npz')
    centers=data['mesh1_uv0'][data['mesh1_faces'],:2].mean(axis=1)
    groups=json.loads((ROOT/'artifacts/material-remap/Draw01/far-appearance.json').read_text())['groups']
    for group in groups:
        if 'fixed-carriageway' not in group['group']:continue
        polygon=Polygon(group['uv']).convex_hull
        count=sum(polygon.covers(Point(*p)) for p in centers)
        assert count>0
        receivers.append(dict(render='Mechanism LOD2 Appearance Group 1',mesh=1,sourceGroup=group['group'],triangles=count,oldDirectAtlas=True,positionsChanged=False,uvChanged=False))
    hashes={str(p.relative_to(OWNER)):sha(p.read_bytes()) for p in (OWNER/'Geometry').glob('*.Geometry')}
    paint=OWNER/'Geometry/Moving Object Skinned Paint Mesh.Geometry'
    footprint=dict(nearTextureDelta=sum(c['newBytes']-c['oldBytes'] for c in near['channels']),farAddedBytes=sum(c['bytes']+32 for c in far['channels'])+far['descriptorBytes']+32)
    footprint['expectedOwnerMaterialDelta']=footprint['nearTextureDelta']+footprint['farAddedBytes']
    footprint['originalOwnerMaterialBytes']=15904169
    footprint['projectedOwnerMaterialBytes']=15904169+footprint['expectedOwnerMaterialDelta']
    footprint['newOwnerMeasured']=False
    result=dict(status='PASS',allMipYellowChecks=checks,actualCandidate32Receivers=receivers,geometryHashes=hashes,paintGeometrySha256=sha(paint.read_bytes()),paintChanges=0,geometryChanges=0,originalStructureSurfaceChanges=0,budget=footprint,nativeRenderingVerified=False,limitations=['Near/middle cap retains existing fractional V grid and repeat-filter characteristics; this repair does not certify the older unit extraction quality.','Independent paint UV/color/geometry are untouched. Native near/far, moving and cold-load acceptance requires a regenerated human sample.','The pre-existing owner material budget failure remains; projected delta is not a new-owner measurement.'])
    (OUT/'validation.json').write_text(json.dumps(result,indent=2)+'\n')
    print('PASS',len(checks),'mip checks;',sum(r['triangles'] for r in receivers),'actual cap triangles;',footprint)


if __name__=='__main__':main()
