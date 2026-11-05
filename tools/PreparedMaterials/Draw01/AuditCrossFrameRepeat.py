"""Verify source-coordinate provenance, two-period storage and native panel phase."""
import json
import struct
import numpy as np
from PIL import Image
from StageCleanCap import ROOT,decode,sha

def main():
    out=ROOT/'artifacts/material-remap/Draw01/crossframe47'
    work=out/'repeat'
    part=json.loads((work/'authoring.json').read_text())['parts'][0]
    manifest=json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/manifest.json').read_text())
    from pathlib import Path
    for i,c in enumerate(manifest['units'][0]['channels']):
        path=Path(c['path'].replace('C:\\Users\\admin\\Documents\\Codex\\CS2BridgeBuilder',str(ROOT)))
        raw=path.read_bytes();assert sha(raw)==c['sha256']
        source=np.asarray(Image.frombytes('RGBA',(4096,4096),decode(raw)[:4096*4096],'bcn',(7,'BC7')))
        donor=source[2708:3622,1042:1534]
        authored=np.asarray(Image.open(work/'grating'/f'channel-{i}.png'))
        assert authored.shape==(1828,492,4)
        assert np.array_equal(authored[:914],donor) and np.array_equal(authored[914:],donor)
    phase=[]
    for r in json.loads((out/'native-panel-phase.json').read_text()):
        p=np.array(r['p']);uv=np.array(r['uv'])
        authored=(uv[:,1]*4096-2708)/914
        projected=(-np.abs(p[:,2])+1.9599609375)/4
        error=(projected-authored+.5)%1-.5
        assert abs(error).max()<1e-10
        phase.append(dict(face=r['face'],maxPixelError=float(abs(error).max()*914)))
    owner=[]
    base=ROOT/'artifacts/material-remap/shared/draw01-transverse47'
    for record in json.loads((base/'attributes/index.json').read_text()):
        for binding in record['bindings']:
            if '8bd2a4f36150775e3631026060461089' not in binding['surfaces']:continue
            data=json.loads(Path(record['file']).read_text())
            for slot,surface in enumerate(binding['surfaces']):
                if surface!='8bd2a4f36150775e3631026060461089':continue
                m=data['meshes'][slot]
                owner.append(dict(prefab=binding['prefab'],geometry=record['cid'],slot=slot,triangles=len(m['faces'])))
    result=dict(status='PASS',sourcePanelFaces=phase,sourcePbrDonorExact=True,twoIdenticalSourcePeriods=True,
        textureSize=[492,1828],physicalSize=part['physicalSize'],oldOwnerGratingBindings=owner,
        sourceUvPeriodRows=914,periodMetres=4,limitations=['Native BC7 is lossy; donor equality applies before encoding.','Phase proof covers the 16 recorded full native panel triangles; terminal source position quantization remains reported separately.','Actual owner is candidate47, not post-repair game output.'])
    (out/'source-period-audit.json').write_text(json.dumps(result,indent=2)+'\n')
    with (out/'actual-grating47.bin').open('wb') as stream:
        stream.write(struct.pack('<i',3))
        for cid in ('b044dc478e1f69eac402a851f25ddc9b','d5ef27052929cf510182281183958d85','7de95d499adfe384b3a1bfdf5416631d'):
            m=json.loads((base/'attributes'/(cid+'.attributes.json')).read_text())['meshes'][2]
            stream.write(struct.pack('<i',len(m['faces'])))
            for face in m['faces']:
                stream.write(struct.pack('<i',2))
                for i in face:
                    uv=(m['uv0'][i]+[0,0,0,0])[:4]
                    stream.write(struct.pack('<7f',*m['positions'][i],*uv))
    print('PASS four source PBR donor copies, 16 source panel phases,',len(owner),'actual owner grating bindings')

if __name__=='__main__':main()
