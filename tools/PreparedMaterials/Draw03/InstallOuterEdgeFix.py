"""Package the evidenced road-only outer atlas border repair; retain originals."""
from pathlib import Path
import hashlib,json,shutil
ROOT=Path(__file__).resolve().parents[3]
source=ROOT/'artifacts/material-remap/Draw03/full-cross-section-v3/Materials/Draw03'
target=ROOT/'assets/Materials/Draw03'
backup=ROOT/'artifacts/backups/Draw03-outer-edge-v3'
old=json.loads((ROOT/'artifacts/material-remap/Draw03/full-cross-section-v2/composition.json').read_text())['units'][0]
receipt=[]
def preserve(path):
    dest=backup/path.relative_to(target);dest.parent.mkdir(parents=True,exist_ok=True)
    if dest.exists():assert dest.read_bytes()==path.read_bytes()
    else:shutil.copy2(path,dest)
    receipt.append(dict(path=str(path.relative_to(target)),sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
for src in source.rglob('*'):
    if not src.is_file():continue
    dst=target/src.relative_to(source)
    if dst.exists() and dst.read_bytes()!=src.read_bytes():preserve(dst)
    dst.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(src,dst)
descriptors=[p.read_bytes() for p in target.rglob('*.Surface')]
for cid in old['bindings'].values():
    native=bytes(int(cid[i:i+2][::-1],16) for i in range(0,32,2))
    if any(native in d for d in descriptors):continue
    for suffix in ('.Texture','.Texture.cid'):
        path=target/'Shared'/(cid+suffix)
        if path.exists():
            assert path.resolve().is_relative_to(target.resolve())
            preserve(path);path.unlink()
(ROOT/'artifacts/material-remap/Draw03/white-edge/retired-receipt.json').write_text(json.dumps(receipt,indent=2)+'\n')
print('Packaged road-only edge fix; originals preserved:',len(receipt))
