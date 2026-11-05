"""Stage complete native descriptors and payloads for the four-part consumer."""
import json
from StageCleanCap import ROOT,sha,identity,cid_bytes

def main():
    work=ROOT/'artifacts/material-remap/Draw01/crossframe47/repeat'
    target=work/'Materials/Draw01'; records=[]
    for part in json.loads((work/'authoring.json').read_text())['parts']:
        name=part['part']
        if name=='grating':
            key='075ad0b2176777963bb613aa7dc920d51d53a5cbe783beb1aa24bce49cbadc14'
            old=json.loads((ROOT/'artifacts/material-remap/Draw01/near-units/grating-phase2-bake-report.json').read_text())['units'][0]
            bindings={c['name']:c['cid'] for c in old['channels']}
        else:
            key='87b16f614d85671f0ad2e65f113b218460b330780cbb8d6003a089d600139a16'
            bindings=json.loads((ROOT/'artifacts/material-remap/Draw01/asphalt-repeat34/bindings.json').read_text())
        descriptor=(ROOT/'assets/Materials/Draw01/RepeatUnits'/key/'Repeat.Surface').read_bytes(); new={}
        for property,oldcid in bindings.items():
            i=next(j for j,c in enumerate(part['channels']) if c['channel']==property)
            raw=(work/name/f'channel-{i}.Texture').read_bytes();cid=identity(raw)
            path=target/'Shared'/(cid+'.Texture');path.parent.mkdir(parents=True,exist_ok=True)
            path.write_bytes(raw);path.with_suffix('.Texture.cid').write_text(cid)
            token=cid_bytes(oldcid);assert descriptor.count(token)==1
            descriptor=descriptor.replace(token,cid_bytes(cid));new[property]=cid
        key=sha(b'Draw01/PhysicalPartRepeat/1/'+name.encode()+descriptor)
        path=target/'RepeatUnits'/key/'Repeat.Surface';path.parent.mkdir(parents=True,exist_ok=True)
        path.write_bytes(descriptor);path.with_suffix('.Surface.cid').write_text(identity(descriptor))
        records.append(dict(**part,key=key,surface=identity(descriptor),bindings=new))
    (work/'bindings.json').write_text(json.dumps(records,indent=2)+'\n')
    for r in records:print(r['part'],r['key'],r['surface'],r['physicalSize'])

if __name__=='__main__':main()
