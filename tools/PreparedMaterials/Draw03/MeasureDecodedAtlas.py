"""Measure full decoded mip-chain compression without discarding atlas samples."""
import ctypes
import json
import zipfile
from ComposeRoadPeriods import PROOF, ROOT, CIDS, Writer, digest
from InspectWearSources import bc7, GAME


def main():
    output=ROOT/'artifacts/material-remap/Draw03/original-atlas-audit'
    source=json.loads((PROOF/'authoring-texture-scan.json').read_text())
    codec=Writer(GAME,output)
    records=[]
    for cid in CIDS:
        entry=next(r for r in source['found'] if r['cid']==cid)
        with zipfile.ZipFile(entry['path']) as archive: original=archive.read(entry['entry'])
        assert digest(original)==entry['sha256']
        raw=b''.join(p.tobytes() for _,p in bc7(original))
        bound=codec.bound(len(raw),0)
        encoded=ctypes.create_string_buffer(bound)
        size=codec.encode(raw,len(raw),encoded,bound,0,22)
        assert 0<size<=bound
        restored=ctypes.create_string_buffer(len(raw))
        assert codec.decode(encoded,size,restored,len(raw),0)==len(raw)
        assert restored.raw==raw
        row=dict(cid=cid,rawDecodedBytes=len(raw),textureAndCidBytes=size+50,
            originalSha256=digest(original),decodedSha256=digest(raw),lossless=True)
        records.append(row)
        print(json.dumps(row),flush=True)
        (output/'decoded-compression.json').write_text(json.dumps(records,indent=2)+'\n')


if __name__=='__main__':main()
