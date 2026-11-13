"""Independently decode rebuilt UnityFS/serialized Shader/LZ4 program containers."""
import argparse, copy, json, struct, sys
from pathlib import Path
p=argparse.ArgumentParser();p.add_argument('--unitypy',required=True);p.add_argument('--output',default='artifacts/shader-variable');a=p.parse_args()
sys.path.insert(0,a.unitypy)
import UnityPy
from UnityPy.helpers import CompressionHelper

def read(data):
    env=UnityPy.load(data)
    objects={o.type.name:o.read_typetree() for o in env.objects}
    shader=objects['Shader'];blob=bytes(shader['compressedBlob'])
    segments=[CompressionHelper.decompress_lz4(blob[o:o+c],n) for o,c,n in zip(shader['offsets'][0],shader['compressedLengths'][0],shader['decompressedLengths'][0])]
    records=[];end=4+12*struct.unpack_from('<I',segments[0])[0]
    for i in range(struct.unpack_from('<I',segments[0])[0]):
        o,n,s=struct.unpack_from('<iii',segments[0],4+i*12)
        assert o==end and s==0 and o+n<=len(segments[s]);end=o+n
        records.append(segments[s][o:o+n])
    assert end==len(segments[0])
    for name in ['offsets','compressedLengths','decompressedLengths','compressedBlob']:shader.pop(name)
    return objects,records

report=[]
for name in ['BoneDecal','WorldLayeredPbr']:
    package=Path('assets/MaterialPrograms',name+'.bbs1').read_bytes();size=struct.unpack_from('<I',package,8)[0]
    seed=package[12:12+size];original,old=read(seed)
    for file in Path(a.output).glob(name+'-*.bundle'):
        current,records=read(file.read_bytes());assert current==original,'Unrelated shader reflection/pass/asset metadata changed'
        assert len(records)==len(old)
        changed=[]
        for i,(before,after) in enumerate(zip(old,records)):
            if before==after:continue
            k=before.index(b'DXBC');j=after.index(b'DXBC');assert k==j
            oldlen=struct.unpack_from('<I',before,k+24)[0];newlen=struct.unpack_from('<I',after,k+24)[0]
            # Native array prefix is the only pre-DXBC change. Locate its measured owner.
            fields=[p for p in range(24,k,4) if p+4+struct.unpack_from('<I',before,p)[0]>=k+oldlen and p+4+struct.unpack_from('<I',before,p)[0]<=len(before) and struct.unpack_from('<I',before,p)[0]>oldlen]
            assert len(fields)==1
            pos=fields[0];assert before[:pos]==after[:pos] and before[pos+4:k]==after[pos+4:k]
            assert struct.unpack_from('<I',after,pos)[0]-struct.unpack_from('<I',before,pos)[0]==newlen-oldlen
            assert before[k+oldlen:]==after[k+newlen:],'Unrelated program suffix changed'
            changed.append(i)
        report.append({'file':file.name,'bytes':file.stat().st_size,'records':len(old),'changedRecords':changed,'nativeMetadataUnchanged':True})
        print('PASS',file.name,'records',len(old),'changed',len(changed))
Path(a.output,'independent-container-verification.json').write_text(json.dumps(report,indent=2)+'\n')
