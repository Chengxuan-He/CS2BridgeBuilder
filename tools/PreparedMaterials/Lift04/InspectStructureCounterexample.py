"""Inspect only Lift04 face 4's original beam strip for repeat-unit evidence."""
import hashlib
import io
import json
from pathlib import Path
import struct
import numpy as np
from PIL import Image

ROOT=Path(__file__).resolve().parents[3]


def bc7_image(raw):
    version,w,h,depth,mips,fmt,dimension,filtering,wrap,aniso,compression=struct.unpack_from('<4H6Bi',raw)
    assert version==5 and fmt in (108,109) and compression==-1 and depth==1
    size=((w+3)//4)*((h+3)//4)*16
    header=[0]*31
    header[0:7]=[124,0x81007,h,w,size,0,1]
    header[18:21]=[32,4,int.from_bytes(b'DX10','little')]
    header[26]=0x1000
    dds=b'DDS '+struct.pack('<31I',*header)+struct.pack('<5I',98,3,0,1,0)+raw[18:18+size]
    return Image.open(io.BytesIO(dds)).convert('RGBA')


def main():
    evidence=ROOT/'artifacts/prepared-materials/Lift04/structure-counterexample'
    evidence.mkdir(parents=True,exist_ok=True)
    receiver=json.loads((ROOT/'artifacts/shared-material-pipeline/structure-counterexample/receiver.json').read_text())
    assert receiver['face']==4 and receiver['domain']==2 and receiver['mesh']==0 and receiver['delta']==2
    source=ROOT/'artifacts/uv-density/native/LiftBridge04 Mesh.npz'
    mesh=np.load(source);v=mesh['mesh0_positions'];uv=mesh['mesh0_uv0'];faces=mesh['mesh0_faces']
    face=faces[4];strip=np.all(v[faces,2]==v[face[0],2],axis=1)&np.all((v[faces,1]>=v[face,1].min())&(v[faces,1]<=v[face,1].max()),axis=1)
    ids=np.flatnonzero(strip);assert ids.tolist()==[4,5]
    region=uv[np.unique(faces[ids])]
    bounds=np.concatenate((region.min(0),region.max(0)));pixels=np.rint(bounds*4096).astype(int)
    assert np.array_equal(bounds*4096,pixels) and (pixels[2]-pixels[0],pixels[3]-pixels[1])==(46,1266)
    # This coplanar physical strip is exactly one quad. The diagonal shares its
    # endpoint UVs, so it supplies no consecutive unit/reset evidence.
    records=[dict(face=int(f),indices=faces[f].tolist(),positions=v[faces[f]].tolist(),uv=uv[faces[f]].tolist()) for f in ids]
    manifest=json.loads((ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/extracted-source/manifest.json').read_text())
    channel_results=[];strips=[];all_periods=[]
    for ch in manifest['units'][0]['channels']:
        raw=Path(ch['path']).read_bytes();assert hashlib.sha256(raw).hexdigest()==ch['sha256']
        image=bc7_image(raw);crop=image.crop(tuple(pixels));array=np.asarray(crop).astype(np.int16)
        rotated=crop.transpose(Image.Transpose.ROTATE_90);rotated.save(evidence/(ch['name']+'.png'));strips.append(rotated)
        periods=[]
        for shift in range(1,len(array)//2+1):
            differences=np.abs(array[shift:,:,:3]-array[:-shift,:,:3])
            periods.append(dict(shift=shift,mae=float(differences.mean()),max=int(differences.max()),equalComponentFraction=float(np.mean(differences==0))))
        exact=[p['shift'] for p in periods if p['max']==0];all_periods.append(set(exact))
        channel_results.append(dict(name=ch['name'],sourceSha256=ch['sha256'],exactIntegerPeriods=exact,
                                    bestIntegerCandidates=sorted(periods,key=lambda p:p['mae'])[:5],
                                    commonCandidateComparisons=[p for p in periods if p['shift'] in (296,404)],
                                    minEncodedRGBA=array.min(axis=(0,1)).tolist(),maxEncodedRGBA=array.max(axis=(0,1)).tolist(),
                                    interpretation='Encoded-byte RGB comparisons after original BC7 decode; approximate similarity is not proof of authored repetition.'))
    preview=Image.new('RGBA',(1266,46*3),(0,0,0,255))
    for i,img in enumerate(strips):preview.paste(img,(0,i*46))
    preview.save(evidence/'native-strip-channels.png')
    report=dict(style='Lift04',scope='LOD0 mesh0 face4/domain2 only',sourceMeshSha256=hashlib.sha256(source.read_bytes()).hexdigest(),
                originalFaces=records,originalPixelRectangle=pixels.tolist(),sizePixels=[46,1266],
                physicalLengthMetres=float(v[face,0].max()-v[face,0].min()),physicalHeightMetres=float(v[face,1].max()-v[face,1].min()),
                samePhysicalStripFaces=ids.tolist(),internalUvRestarts=0,channels=channel_results,
                commonExactIntegerPeriods=sorted(set.intersection(*all_periods)),
                outcome='BASECOLOR_PERIOD_CANDIDATE_FULL_MATERIAL_UNIT_NOT_PROVEN',
                limits='Finite integer-period check of this original strip, not a global atlas search or proof against every possible subpixel/semantic repetition.',
                conclusion='Two faces cover one continuous original strip without UV restarts. BaseColor has a strong near-exact 404-pixel repetition, while the normal channel has a different best integer candidate (296 pixels) with larger residuals. No shared whole-material period is established. This does not exclude an intended approximate/subpixel pattern, but it does not justify registering an arbitrary rectangle as an authored repeat unit.',
                textureBakes=0,productionFilesChanged=False,localization='Offline diagnostic only; no user-visible text changed.')
    (evidence/'inspection.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:report[k] for k in ('samePhysicalStripFaces','internalUvRestarts','commonExactIntegerPeriods','outcome')}))
    for ch in channel_results:print(ch['name'],ch['bestIntegerCandidates'])


if __name__=='__main__':main()
