"""Author Draw01's explicitly requested transverse asphalt repeat at source density.

The source-coordinate quilt selects original pixels, identically for all PBR channels.
It excludes prototype curb, paint and cap concrete. No receiver-size pixels are used.
"""
import json
from pathlib import Path
import numpy as np
from PIL import Image
from StageCleanCap import ROOT, decode, sha

OUT = ROOT / 'artifacts/material-remap/Draw01/asphalt-repeat34'
OUT.mkdir(parents=True, exist_ok=True)


def main():
    unit = json.loads((ROOT / 'artifacts/material-remap/Draw01/near-units/manifest.json').read_text())['units'][0]
    sources = []
    for channel in unit['channels']:
        raw = Path(channel['path']).read_bytes()
        assert sha(raw) == channel['sha256']
        data = decode(raw)
        sources.append(np.asarray(Image.frombytes('RGBA', (4096, 4096), data[:4096*4096], 'bcn', (7, 'BC7'))).copy())
    # Both candidate strips are pure asphalt. A shortest seam selects source
    # pixels, without crossfading colour or manufacturing a normal-map frame.
    cost = np.zeros((732, 64), dtype=float)
    for source in sources[:3]:
        a = source[2778:3510, 576:640].astype(float)
        b = source[2778:3510, 64:128].astype(float)
        cost += np.mean((a-b)**2, axis=2)
    cost[:, :32] = np.inf
    cost[:, 57:] = np.inf
    values = np.full(64, np.inf); values[48] = cost[0, 48]
    previous = np.zeros((732, 64), dtype=np.int32)
    for y in range(1, 732):
        choices = np.stack([np.r_[np.inf, values[:-1]], values, np.r_[values[1:], np.inf]])
        step = np.argmin(choices, axis=0)
        previous[y] = np.arange(64) + step - 1
        values = choices[step, np.arange(64)] + cost[y]
    seam = np.zeros(732, dtype=np.int32); seam[-1] = 48
    for y in range(731, 0, -1): seam[y-1] = previous[y, seam[y]]
    assert seam.min() >= 32 and seam.max() <= 56 and seam[0] == seam[-1]
    source_x = np.tile(np.arange(128, 640), (732, 1))
    source_y = np.tile(np.arange(2778, 3510)[:, None], (1, 512))
    for y, cut in enumerate(seam): source_x[y, 448+cut:] -= 512
    assert source_x.min() >= 96 and source_x.max() < 633
    np.savez(OUT / 'source-coordinates.npz', x=source_x, y=source_y, seam=seam)
    reports = []
    for index, (channel, source) in enumerate(zip(unit['channels'], sources)):
        pixels = source[source_y, source_x]
        Image.fromarray(pixels).save(OUT / ('source-unit-'+str(index)+'.png'))
        # The derivative is a newly authored periodic unit. Filter that unit,
        # never coarse atlas mips whose footprint includes unrelated grey edges.
        work = pixels.astype(np.float32) / 255
        if index == 0:
            work[:, :, :3] = np.where(work[:, :, :3] <= .04045, work[:, :, :3]/12.92, ((work[:, :, :3]+.055)/1.055)**2.4)
        elif index == 1:
            xy = np.stack((work[:, :, 0]*work[:, :, 3], work[:, :, 1]), axis=2)*2-1
            work[:, :, :3] = np.concatenate((xy, np.sqrt(np.maximum(0, 1-(xy*xy).sum(axis=2)))[:, :, None]), axis=2)
        mip = 0
        with (OUT / ('asphalt-'+str(index)+'.rgba')).open('wb') as stream:
            while True:
                result = work.copy()
                if index == 0:
                    result[:, :, :3] = np.where(result[:, :, :3] <= .0031308, result[:, :, :3]*12.92, 1.055*np.maximum(result[:, :, :3], 0)**(1/2.4)-.055)
                elif index == 1:
                    n = result[:, :, :3]; n /= np.maximum(np.linalg.norm(n, axis=2)[:, :, None], 1e-12)
                    nx = n[:, :, 0]*.5+.5; ny = n[:, :, 1]*.5+.5
                    result[:, :, 0] = 1; result[:, :, 1] = ny
                    result[:, :, 2] = ny; result[:, :, 3] = nx
                stream.write(np.clip(np.rint(result*255), 0, 255).astype('uint8').tobytes())
                h, w = work.shape[:2]
                if w == h == 1: break
                size = (max(1, w//2), max(1, h//2))
                work = np.stack([np.asarray(Image.fromarray(work[:, :, c], mode='F').resize(size, Image.Resampling.BOX)) for c in range(4)], axis=2)
                mip += 1
        reports.append(dict(name=channel['name'], sourceSha256=channel['sha256'], sourcePath=channel['path'], mips=mip+1))
    evidence = dict(status='staged', width=512, height=732, physicalSize=[512*3.36602783203125/769.5625, 4], nativePixelsPerMetre=[769.5625/3.36602783203125, 183], sourceXRange=[int(source_x.min()), int(source_x.max())], sourceYRange=[2778,3509], sourceFaces=[1512,1513], sourceCoordinateSha256=sha((OUT/'source-coordinates.npz').read_bytes()), allChannelsUseIdenticalCoordinates=True, crossfade=False, sourceAtlasUnchanged=True, channels=reports)
    (OUT/'authoring.json').write_text(json.dumps(evidence, indent=2)+'\n')
    print(evidence['physicalSize'])


if __name__ == '__main__': main()
