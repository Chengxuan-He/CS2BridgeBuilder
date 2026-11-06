"""Apply reviewed native control-map substitutions to legacy prepared road descriptors.

Read only the approved running-surface graph. Write to a new staging directory.
Existing final descriptors pass unchanged. Package validation independently checks
the physical consumer list, native graph and every referenced texture digest.
"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess

def digest(data):
    return hashlib.sha256(data).hexdigest()

def token(cid):
    return bytes.fromhex(''.join(cid[i+1] + cid[i] for i in range(0, 32, 2)))

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('source', type=Path)
    parser.add_argument('output', type=Path)
    args = parser.parse_args()
    src, out = args.source.resolve(), args.output.resolve()
    if out.exists() or out.is_relative_to(src) or src.is_relative_to(out):
        raise ValueError('Use a new separate staging directory')
    data = Path(__file__).resolve().parent
    approved = json.loads((data/'RoadTextureConsumers.json').read_text())
    remap = {r['path']: r for r in json.loads((data/'NativeControlRemaps.json').read_text())}
    hashes = json.loads((data/'RoadTexturePayloads.json').read_text())
    def write(rel, payload, cid):
        dest = out/rel
        dest.parent.mkdir(parents=True, exist_ok=True)
        dest.write_bytes(payload)
        Path(str(dest)+'.cid').write_text(cid)
    textures = {}
    for row in approved:
        rel = row['style']+'/'+row['path']
        p = src/rel
        raw = p.read_bytes()
        cid = Path(str(p)+'.cid').read_text().strip()
        if digest(raw) != row['surfaceSha256']:
            change = remap[rel]
            if digest(raw) != change['oldSha'] or cid != change['before']:
                raise ValueError('Unreviewed input: '+str(p))
            for old, new in change['bind'].items():
                if raw.count(token(old)) != 1:
                    raise ValueError('Ambiguous native channel reference: '+str(p))
                raw = raw.replace(token(old),token(new))
            cid = change['after']
        if digest(raw) != row['surfaceSha256']:
            raise ValueError('Unexpected final Surface: '+rel)
        dest = out/rel
        write(rel, raw, cid)
        for ref in row['local'].values():
            style = src/row['style']
            paint = row['path'] == 'Paint/SkinnedPaint.Surface'
            target = Path('Shared/Paint' if paint else row['style']+'/Shared')/(ref+'.Texture')
            options = [src/target, p.parent/(ref+'.Texture'), style/'Shared'/(ref+'.Texture')]
            candidate = next((f for f in options if f.is_file()), None)
            if candidate is None or digest(candidate.read_bytes()) != hashes[ref]:
                raise ValueError('Missing or stale road channel: '+ref)
            if ref not in textures:
                write(target, candidate.read_bytes(),ref)
                textures[ref]=target
            elif textures[ref] != target:
                raise ValueError('Conflicting texture ownership: '+ref)
    for p in (src/'Shared/MidMips').rglob('*.MidMips'):
        target = out/p.relative_to(src)
        target.parent.mkdir(parents=True,exist_ok=True)
        shutil.copyfile(p,target)
        shutil.copyfile(str(p)+'.cid',str(target)+'.cid')
    subprocess.run(['dotnet','run','--project',str(data.parents[1]/'MaterialTexturePolicy'),'--',str(out),str(data/'RoadTextureConsumers.json')],check=True)

if __name__ == '__main__':
    main()
