"""Inspect the original Lift03 fixed network structure, independently of footings."""
from pathlib import Path
import json,struct,hashlib,zipfile,sys
import numpy as np
ROOT=Path(__file__).resolve().parents[2];sys.path.insert(0,str(ROOT/'tools/GeometryMetaprogram'))
from UvDensityAudit import NativeGeometry
O=ROOT/'artifacts/material-remap/Lift03/appearance76/fixed-source';O.mkdir(parents=True,exist_ok=True)
A=Path('C:/Users/admin/Documents/Codex/2026-10-15/c-users-admin-documents-codex-cs2bridgebuilder/work/bridge-research/all-movable-audit.json');raw=A.read_bytes();assert hashlib.sha256(raw).hexdigest()=='47ef0f1723e8643e74a17751aa480f97260bf6e549aa60ec9f14f8e003b12070'; records=json.loads(raw)['selected']
def cid(v):return ''.join(f'{b&15:x}{b>>4:x}' for b in struct.pack('<4i',*[v[f'm_GuidPart{i}'] for i in range(1,5)]))
game=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data');decoder=NativeGeometry(game/'Plugins/x86_64/PipelinePlugin.dll');result=[]
with zipfile.ZipFile(game/'Content/BridgesAndPorts/Blob.cok') as z:
    names=z.namelist()
    for lod,pid in enumerate(('79486','79487','79488')):
        d=records[pid]['data'];identity=cid(d['m_GeometryAsset']); entry=next(n[:-4] for n in names if n.endswith('.Geometry.cid') and z.read(n).decode().strip()==identity)
        payload=z.read(entry);path=O/f'lod{lod}.Geometry';path.write_bytes(payload);meshes=decoder.read(path);assert len(meshes)==1
        m=meshes[0];np.savez_compressed(O/f'lod{lod}.npz',**m);p=m['positions'];f=m['faces']
        parents=np.arange(len(p));
        def find(x):
            while parents[x]!=x:parents[x]=parents[parents[x]];x=parents[x]
            return x
        for a,b,c in f:
            a,b,c=find(a),find(b),find(c);parents[b]=a;parents[c]=a
        comp=np.array([find(i) for i in range(len(p))]);parts=[]
        for part in np.unique(comp):
            ids=np.flatnonzero(comp==part);faces=np.flatnonzero(comp[f[:,0]]==part)
            parts.append(dict(id=int(part),vertices=len(ids),faces=len(faces),min=p[ids].min(0).tolist(),max=p[ids].max(0).tolist()))
        np.savez_compressed(O/f'lod{lod}-parts.npz',component=comp)
        item=dict(lod=lod,pid=pid,native=d,components={c['name']:records[str(c['m_PathID'])]['data'] for c in d['components']},geometryCid=identity,entry=entry,sha256=hashlib.sha256(payload).hexdigest(),parts=parts)
        (O/f'lod{lod}-parts.json').write_text(json.dumps(item,indent=2)+'\n');result.append(dict(lod=lod,vertices=len(p),faces=len(f),parts=len(parts),bounds=[p.min(0).tolist(),p.max(0).tolist()]))
(O/'summary.json').write_text(json.dumps(result,indent=2)+'\n'); print(result)
