"""Inspect original packed vertex frames for the native finite-cell source faces."""
from pathlib import Path
import json,zipfile,hashlib
import numpy as np
root=Path(__file__).resolve().parents[3];e=root/'artifacts/layer-decoupling/Draw02/outputs/native-repeat-mechanism';out=root/'artifacts/material-remap/Draw02/near-cells'
source=(e/'audit.py').read_text().split('def analyze')[0]
source=source.replace('(0,4,13)','(0,1,2,4,13)').replace("formats={0:'<f4',1:'<f2',10:'<u4',11:'<i4'}","formats={0:'<f4',1:'<f2',5:'<i2',10:'<u4',11:'<i4'}")
source=source.replace("{0:'positions',4:'uv',13:'bones'}","{0:'positions',1:'normalPacked',2:'tangentPacked',4:'uv',13:'bones'}")
namespace={'__file__':str(e/'audit.py')};exec(source,namespace)
units=json.loads((out/'bake-input.json').read_text())['units'];result=[]
with zipfile.ZipFile('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data/Content/BridgesAndPorts/Blob.cok') as archive:
    for lod in (0,1):
        evidence=json.loads((e/f'lod{lod}-analysis.json').read_text());raw=archive.read(evidence['archiveEntry']);assert hashlib.sha256(raw).hexdigest()==evidence['sha256']
        m=namespace['decode'](raw)[0]
        for unit in (u for u in units if u['id'].startswith(f'lod{lod}-')):
            ids=np.unique(m['indices'][unit['faces']]);normals=np.unique(m['normalPacked'][ids].copy().view('<u4')).tolist()
            tangents=np.unique(m['tangentPacked'][ids].copy().view('<u4')).tolist()
            result.append(dict(unit=unit['id'],vertices=len(ids),normalPacked=[f'{n:08x}'for n in normals],tangentPacked=[f'{n:08x}'for n in tangents]))
            print(result[-1])
(out/'source-frames.json').write_text(json.dumps(result,indent=2)+'\n')
