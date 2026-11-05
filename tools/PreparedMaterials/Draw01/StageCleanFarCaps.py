"""Remove measured far paint from same-chart substrate (all roads with --whole-road).

The whole native 512 grid is retained for exact authored coordinates, not stretched
or expanded to receiver size. Unused pixels remain original compressed blocks.
"""
import json
import sys
import numpy as np
from PIL import Image
from shapely.geometry import Polygon, Point
from StageCleanCap import ROOT, OUT, decode, encode, sha, identity, write, cid_bytes
if '--whole-road' in sys.argv:
    import StageCleanCap
    OUT = ROOT/'artifacts/material-remap/Draw01/yellow-line33'
    OUT.mkdir(parents=True,exist_ok=True)
    StageCleanCap.OUT = OUT


def levels(raw):
    data = decode(raw); offset = 0
    for mip in range(raw[8]):
        w = max(1, 512 >> mip); size = ((w+3)//4)**2*16
        yield np.asarray(Image.frombytes('RGBA',(w,w),data[offset:offset+size],'bcn',(7,'BC7'))).copy()
        offset += size


def prepare():
    manifest = json.loads((ROOT/'artifacts/material-remap/shared/draw01-yellow-line32/far-native/manifest.json').read_text())['units'][0]
    channels = [Path(c['path']).read_bytes() for c in manifest['channels']]
    for c, raw in zip(manifest['channels'], channels):
        assert sha(raw) == c['sha256']
    images = [list(levels(raw)) for raw in channels]
    color = images[0][0].astype(int)
    groups = [r for r in json.loads((ROOT/'artifacts/material-remap/Draw01/far-appearance.json').read_text())['groups'] if ('fixed-sidewalk' not in r['group'] if '--whole-road' in sys.argv else 'fixed-carriageway' in r['group'])]
    masks = []
    evidence = []
    for r in groups:
        polygon = Polygon(r['uv']).convex_hull
        xy = np.array([(x,y) for y in range(512) for x in range(512) if polygon.buffer(1/512).covers(Point((x+.5)/512,(y+.5)/512))])
        c = color[xy[:,1],xy[:,0]]
        marked = xy[(c[:,0]-c[:,2]>20)&(c[:,1]-c[:,2]>10)]
        assert len(marked) > 0
        affine = np.array(r['affineXz1ToUv'])
        # Same authored chart and normal frame; translate towards unpainted interior.
        direction = 1 if r['sourceBounds'][1][0] > 0 else -1
        shift = affine[:,0] * direction * 1.5
        masks.append((marked, shift))
        evidence.append(dict(group=r['group'],sourceFaces=r['sourceFaces'],paintPixels=marked.tolist(),donorSourceUvTranslation=shift.tolist(),donorPhysicalTranslation=[direction*1.5,0,0]))
    all_masks = []
    for mip in range(10):
        w = max(1,512 >> mip)
        target = np.zeros((w,w),bool)
        outputs = [images[c][mip].copy() for c in range(3)]
        for marked, shift in masks:
            current = np.zeros((w,w),bool)
            # Include the recorded paint and its current-mip bilinear footprint.
            for x,y in marked:
                mx,my = int(x*w/512),int(y*w/512)
                current[max(0,my-1):min(w,my+2),max(0,mx-1):min(w,mx+2)] = True
            yy,xx = np.where(current & ~target)
            coords = np.column_stack((xx,yy)).astype(float) + shift*w
            low = np.floor(coords).astype(int); fraction = coords-low
            for ch in range(3):
                src = images[ch][mip].astype(float)/255
                if ch == 0:
                    src[:,:,:3] = np.where(src[:,:,:3]<=.04045,src[:,:,:3]/12.92,((src[:,:,:3]+.055)/1.055)**2.4)
                value = np.zeros((len(xx),4))
                for dy in (0,1):
                    for dx in (0,1):
                        weight = (fraction[:,0] if dx else 1-fraction[:,0])*(fraction[:,1] if dy else 1-fraction[:,1])
                        value += src[np.clip(low[:,1]+dy,0,w-1),np.clip(low[:,0]+dx,0,w-1)]*weight[:,None]
                if ch == 0:
                    value[:,:3] = np.where(value[:,:3]<=.0031308,value[:,:3]*12.92,1.055*np.maximum(value[:,:3],0)**(1/2.4)-.055)
                outputs[ch][yy,xx] = np.clip(np.rint(value*255),0,255).astype('uint8')
            target |= current
        all_masks.append(target)
        for ch in range(3):
            with (OUT/f'far-{ch}.rgba').open('wb' if mip==0 else 'ab') as f:
                f.write(outputs[ch].tobytes())
    np.savez(OUT/'far-masks.npz',**{f'mip{i}':m for i,m in enumerate(all_masks)})
    (OUT/'far-cap-source-evidence.json').write_text(json.dumps(dict(source=manifest,groups=evidence,fullNativeGrid=[512,512],mips=10,sourceAtlasUnchanged=True),indent=2)+'\n')


def finish():
    manifest = json.loads((ROOT/'artifacts/material-remap/shared/draw01-yellow-line32/far-native/manifest.json').read_text())['units'][0]
    masks = np.load(OUT/'far-masks.npz'); bindings = {}; report = []
    for ch,c in enumerate(manifest['channels']):
        old = Path(c['path']).read_bytes(); original = decode(old)
        candidate = decode((OUT/f'far-{ch}.Texture').read_bytes())
        assert len(original) == len(candidate)
        merged = bytearray(original); offset = 0; evidence = []
        for mip in range(10):
            w=max(1,512>>mip); b=(w+3)//4
            mask=np.pad(masks[f'mip{mip}'],((0,b*4-w),(0,b*4-w))).reshape(b,4,b,4).any(axis=(1,3)).reshape(-1)
            a=np.frombuffer(original[offset:offset+b*b*16],dtype='u1').reshape(-1,16)
            fresh=np.frombuffer(candidate[offset:offset+b*b*16],dtype='u1').reshape(-1,16)
            new=a.copy();new[mask]=fresh[mask]
            assert np.array_equal(a[~mask],new[~mask])
            merged[offset:offset+b*b*16]=new.tobytes()
            evidence.append(dict(mip=mip,changedFootprintBlocks=int(mask.sum()),outsideMaskBitExact=True))
            offset+=b*b*16
        raw=encode(old,bytes(merged));cid,path=write(raw,'Shared');bindings[c['name']]=cid
        report.append(dict(name=c['name'],cid=cid,bytes=len(raw),sha256=sha(raw),levels=evidence))
    (OUT/'far-bindings.json').write_text(json.dumps(bindings))
    (OUT/'clean-far-report.json').write_text(json.dumps(dict(channels=report,geometryChanges=0,paintMeshChanges=0,sourceAtlasChanges=0),indent=2)+'\n')
    print(bindings)


from pathlib import Path
if __name__ == '__main__':
    finish() if '--finish' in sys.argv else prepare()
