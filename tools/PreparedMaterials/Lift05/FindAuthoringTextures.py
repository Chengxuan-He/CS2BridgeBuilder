"""Search every built-in Blob archive for Lift05 complete authoring textures."""
import argparse
import hashlib
import json
from pathlib import Path
import struct
import zipfile


def main():
    p=argparse.ArgumentParser()
    p.add_argument('game',type=Path)
    p.add_argument('evidence',type=Path)
    p.add_argument('output',type=Path)
    a=p.parse_args()
    evidence=json.loads(a.evidence.read_text())
    wanted={x['originalTextureCid'] for x in evidence['channelPayloads'] if x['kind']=='VTTexture'}
    matches=[]; candidates=[]; archives=[]
    for path in sorted((a.game/'Content').rglob('Blob*.cok')):
        with zipfile.ZipFile(path) as z:
            names=set(z.namelist())
            count=0
            for entry in sorted(names):
                if not entry.endswith('.Texture.cid'):continue
                count+=1
                cid=z.read(entry).decode().strip()
                if cid not in wanted and 'liftbridge05' not in entry.lower():continue
                assert entry[:-4] in names
                raw=z.read(entry[:-4]);version,width,height=struct.unpack_from('<3H',raw)
                assert version in (4,5)
                item=dict(cid=cid,archive=str(path),entry=entry[:-4],bytes=len(raw),
                    sha256=hashlib.sha256(raw).hexdigest(),version=version,width=width,height=height,
                    mipCount=raw[8],completeMipCount=max(width,height).bit_length())
                (matches if cid in wanted else candidates).append(item)
            archives.append(dict(path=str(path),textureCidEntries=count,bytes=path.stat().st_size))
    out=dict(style='Lift05',requestedOriginalTextureCids=sorted(wanted),archives=archives,
        matches=matches,nameOnlyCandidates=candidates,
        missing=sorted(wanted-{x['cid'] for x in matches}),
        evidenceSha256=hashlib.sha256(a.evidence.read_bytes()).hexdigest(),
        scope='All Content/**/Blob*.cok; exact original source CIDs, with LiftBridge05 filename candidates reported separately.',
        bakedTextures=0)
    a.output.parent.mkdir(parents=True,exist_ok=True)
    a.output.write_text(json.dumps(out,indent=2)+'\n')
    print(json.dumps(dict(archives=len(archives),textureCidEntries=sum(x['textureCidEntries'] for x in archives),
                         exactMatches=len(matches),nameOnlyCandidates=len(candidates),missing=out['missing'])))


if __name__=='__main__':main()
