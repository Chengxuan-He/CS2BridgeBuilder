"""Measure the original single-render arc bridge; emit immutable width coefficients."""
from pathlib import Path
import argparse, hashlib, json, struct, tempfile, zipfile
import numpy as np
from UvDensityAudit import NativeGeometry

def main():
    a=argparse.ArgumentParser();a.add_argument('game');a.add_argument('output');args=a.parse_args()
    game=Path(args.game);out=Path(args.output);out.mkdir(parents=True,exist_ok=True)
    entry='PedestrianBridgeArc_94f3c5fdebfe4964b9c2d33e236ff9a5.Geometry'
    with zipfile.ZipFile(game/'Content/Game/Blob.cok') as z:
        raw=z.read(entry);cid=z.read(entry+'.cid').decode().strip()
    with tempfile.TemporaryDirectory(dir=out) as temp:
        path=Path(temp)/entry;path.write_bytes(raw)
        m=NativeGeometry(game/'Plugins/x86_64/PipelinePlugin.dll').read(path)[0]
    p=m['positions'];f=m['faces'];assert p.shape==(4818,3) and f.shape==(4960,3)
    # Weld coincident attribute seams offline; all seven transverse beams are separate islands.
    _,inv=np.unique(np.round(p,4),axis=0,return_inverse=True);parents=list(range(int(inv.max())+1))
    def root(i):
        while parents[i]!=i:parents[i]=parents[parents[i]];i=parents[i]
        return i
    for x,y,z in inv[f]:parents[root(y)]=root(x);parents[root(z)]=root(x)
    labels=np.array([root(i) for i in inv]);coeff=np.zeros(len(p),dtype='<f4');scales=np.zeros(len(p),dtype='<f4');parts=[]
    crossing=0
    for label in np.unique(labels):
        ids=np.flatnonzero(labels==label);v=p[ids];lo=v.min(0);hi=v.max(0)
        cross=lo[0]<=0<=hi[0]
        if cross:
            crossing+=1;assert len(ids)==26
            # Each beam keeps its measured own reach, not the full bridge envelope.
            coeff[ids]=v[:,0]/max(abs(lo[0]),abs(hi[0]))
            scales[ids]=1/max(abs(lo[0]),abs(hi[0]))
        else:coeff[ids]=1 if lo[0]>0 else -1
        parts.append(dict(vertices=ids.tolist(),min=lo.tolist(),max=hi.tolist(),crossing=bool(cross)))
    assert crossing==7
    # Vertices split only for attribute seams must receive identical displacement.
    seam_conflicts=0
    for welded in np.unique(inv):
        ids=np.flatnonzero(inv==welded)
        if np.ptp(coeff[ids])>1e-6:seam_conflicts+=1
    assert seam_conflicts==0, seam_conflicts
    body=struct.pack('<Ii6i',0x314e4242,1,0,0,0,0,len(coeff),0)+coeff.tobytes()
    (out/'Types_ArcPedestrian_Coefficients.bin').write_bytes(body+hashlib.sha256(body).digest())
    body=struct.pack('<Ii6i',0x314e4242,1,0,0,0,0,len(scales),0)+scales.tobytes()
    (out/'Types_ArcPedestrian_Frames.bin').write_bytes(body+hashlib.sha256(body).digest())
    results=[]
    for width in (3,8,16,32):
        q=p.copy();q[:,0]=(p[:,0].astype('f8')+coeff.astype('f8')*((width-3)/2)).astype('f4')
        assert np.array_equal(q[:,1:],p[:,1:])
        if width==3:assert np.array_equal(q,p)
        for part in parts:
            if not part['crossing']:
                ids=part['vertices'];assert np.allclose(q[ids,0]-p[ids,0],(width-3)/2*np.sign(p[ids,0]),atol=2e-6)
        results.append(dict(width=width,bounds=[q.min(0).tolist(),q.max(0).tolist()]))
    (out/'ArcPedestrian-measurements.json').write_text(json.dumps(dict(geometryCid=cid,sha256=hashlib.sha256(raw).hexdigest(),vertices=len(p),triangles=len(f),parts=parts,tests=results),indent=2))
    print(cid, 'parts',len(parts),'crossing',crossing,'seam conflicts',seam_conflicts,'PASS identity/YZ/rigid side widths 3,8,16,32')

if __name__=='__main__':main()

