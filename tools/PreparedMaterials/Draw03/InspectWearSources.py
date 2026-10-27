"""Read original Draw03 payloads and distinguish a real wear layer from baked color."""
from pathlib import Path
import json
import struct
import zipfile

import numpy as np
from PIL import Image

from PrepareNativeRebuild import digest

ROOT=Path(__file__).resolve().parents[3]
PROOF=ROOT/'artifacts/prepared-materials/Draw03/repeat-units-v2'
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def bc7(data):
    version,w,h,depth,mips,fmt,dimension=struct.unpack_from('<4H3B',data)
    assert version==4 and depth==1 and fmt in (108,109) and dimension==2
    offset=14
    for mip in range(mips):
        width,height=max(1,w>>mip),max(1,h>>mip)
        size=((width+3)//4)*((height+3)//4)*16
        pixels=np.asarray(Image.frombytes('RGBA',(width,height),data[offset:offset+size],'bcn',(7,'BC7')))
        yield mip,pixels
        offset+=size
    assert offset==len(data)


def main():
    scan=json.loads((PROOF/'authoring-texture-scan.json').read_bytes())
    source=json.loads((PROOF.parent/'rebuild-native-v1/native-rebuild.json').read_bytes())
    surface=next(s for s in source['sourceSurfaces'] if s['cid']=='792a855e9d0c5c34d2c41d48b062139d')
    base=next(x for x in scan['found'] if x['cid']==surface['descriptor']['textureProperties']['_BaseColorMap'])
    with zipfile.ZipFile(base['path']) as archive:
        raw=archive.read(base['entry'])
        assert digest(raw)==base['sha256']
        _,pixels=next(bc7(raw))
        control_name=next(n for n in archive.namelist() if n.startswith('DrawBridge03PillarBase_ControlMask_') and n.endswith('.Texture'))
        control_raw=archive.read(control_name)
        control_cid=archive.read(control_name+'.cid').decode().strip()
        control_levels=[dict(mip=m,width=p.shape[1],height=p.shape[0],minimum=p.min((0,1)).tolist(),maximum=p.max((0,1)).tolist()) for m,p in bc7(control_raw)]
        related=[n for n in archive.namelist() if n.startswith('DrawBridge03') and n.endswith('.Texture')]
    candidates=json.loads((PROOF/'candidate-manifest.json').read_bytes())['candidates']
    rows=[]
    for name,low,high in [('LaneWear',818,1194),('BetweenLaneWear',802,834)]:
        region=pixels[low:high,3084:4052,:3]
        # Diagnostic bright-paint count only; this threshold is not a production mask.
        marker=np.all(region>180,axis=2)
        y,x=np.where(marker)
        rows.append(dict(name=name,fullLongitudinalRegion=[3084,low,4052,high],shortRegion=[3084,low,3380,high],
            brightPixelCount=int(marker.sum()),shortBrightPixelCount=int(marker[:,:296].sum()),
            brightBounds=[int(x.min()+3084),int(y.min()+low),int(x.max()+3084),int(y.max()+low)] if len(x) else None,
            exactShortUResets=next(c['boundaryEvidence']['nativeUResets'] for c in candidates if c['candidateId']=='Draw03/'+name),
            fullUSourcePeriodPixels=968,shortCropPixels=296))
    result=dict(style='Draw03',sourceSurface=surface['cid'],sourceDescriptor=surface['descriptor'],
        baseSource=dict(cid=base['cid'],sha256=base['sha256'],archive=base['path'],entry=base['entry']),
        relatedTextureEntries=related,control=dict(cid=control_cid,entry=control_name,sha256=digest(control_raw),levels=control_levels,
            allMipsTransparentZero=all(max(l['maximum'])==0 for l in control_levels),boundToRoad='_ControlMask' in surface['descriptor']['textureProperties']),
        regions=rows,createdMaterialFiles=0,
        conclusion='No independently bound original wear texture/mask or wear parameter. The related ControlMask is zero at all mips. Full original road U repeats include authored white paint; short crops exclude it but have no original full-period evidence.',
        minimumMissingInput='An unpainted full 968-pixel longitudinal source period for the existing lane/gap profile, with matching normal/mask mip chains; alternatively explicit permission to author a repeat unit from the measured 296-pixel unmarked subregion without claiming it is an original complete period.',
        nativeAcceptance='This is source-content inspection using original BC7 bytes; not a new baked asset or game-render acceptance.')
    path=PROOF/'wear-source-audit.json'
    path.write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps(dict(controlCid=control_cid,allControlMipsZero=result['control']['allMipsTransparentZero'],controlMipCount=len(control_levels),regions=rows),indent=2))


if __name__=='__main__':main()
