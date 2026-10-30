"""Check compact carriers against native preregistration rules and source block hashes."""
import argparse, hashlib, io, json, struct
from pathlib import Path


def take(s,fmt):
    return struct.unpack('<'+fmt,s.read(struct.calcsize('<'+fmt)))


def validate(data,checks):
    s=io.BytesIO(data)
    tile,levels,maximum,stacks,groups,total=take(s,'6i')
    assert (tile,levels,maximum,stacks)==(512,3,64,2)
    cats=[]
    for stack in range(stacks):
        for _ in range(take(s,'i')[0]):
            w,h,count,first,tiles,disabled,bias=take(s,'5i?i')
            assert not disabled and w==h
            cats.append(dict(stack=stack,count=count,first=first,tiles=tiles,rows=[]))
    for c in cats:
        for _ in range(c['count']):
            raw=s.read(16);identity=''.join(f'{v&15:x}{v>>4:x}' for v in raw)
            mask,ordinal,duplicates=take(s,'ihi');assert duplicates==0
            indices=[take(s,'ii') for _ in range(levels)]
            c['rows'].append((identity,mask,ordinal,indices))
    ntiles,=take(s,'i');position=s.tell();assert take(s,'q')[0]==position
    header=s.tell();tilebytes=348480;mainbytes=278784
    assert len(data)==header+ntiles*tilebytes
    expected={(x['cid'],x['stack'],x['level'],x['layer'],x['half']):x['sha256'] for x in checks}
    cursor=0;ngroups=0;nr=0;seen=set();used=[bytearray(tilebytes) for _ in range(ntiles)]
    for c in cats:
        assert c['first']==cursor
        nr+=c['count']
        for start in range(0,c['count'],64):
            batch=c['rows'][start:start+64];ngroups+=1
            for i,r in enumerate(batch):assert r[2]==i
            for level in range(3):
                qty=1<<(2*(level+1))
                for begin in range(0,len(batch),qty):
                    group=batch[begin:begin+qty];mask=0
                    for row in group:mask|=row[1]
                    for row in group:assert row[3][level]==(cursor,mask)
                    for layer in range(4):
                        if not mask&(1<<layer):continue
                        raw=data[header+cursor*tilebytes:header+(cursor+1)*tilebytes]
                        for i,(identity,active,ordinal,indices) in enumerate(group):
                            if not active&(1<<layer):continue
                            x=(i&1)|((i>>1)&2)|((i>>2)&4)
                            y=((i>>1)&1)|((i>>2)&2)|((i>>3)&4)
                            for half in range(2):
                                side=64>>level>>half;pitch=132>>half;padding=2>>half
                                base=mainbytes if half else 0;value=bytearray()
                                for row in range(side):
                                    off=base+((padding+y*side+row)*pitch+padding+x*side)*16
                                    assert not any(used[cursor][off:off+side*16])
                                    used[cursor][off:off+side*16]=b'\1'*(side*16)
                                    value.extend(raw[off:off+side*16])
                                key=(identity,c['stack'],level,layer,half)
                                assert hashlib.sha256(value).hexdigest()==expected[key]
                                seen.add(key)
                        assert all(b==0 or used[cursor][i] for i,b in enumerate(raw))
                        cursor+=1
        assert cursor-c['first']==c['tiles']
    assert cursor==ntiles and ngroups==groups and nr==total and seen==set(expected)
    return dict(entries=total,tiles=ntiles,sourceBlocks=len(seen),zeroUnused=True)


def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('report',type=Path);p.add_argument('output',type=Path);a=p.parse_args()
    report=json.loads(a.report.read_text());results=[];cache={}
    for row in report['styles']:
        data=Path(row['path']).read_bytes();sha=hashlib.sha256(data).hexdigest()
        assert sha==row['sha256'] and sha[:32]==row['cid']
        if sha not in cache:cache[sha]=validate(data,row['blockChecks'])
        results.append(dict(style=row['style'],sha256=sha,**cache[sha]))
    a.output.write_text(json.dumps(results,indent=2));print('PASS',len({r['style'] for r in results}),'styles',len(results),'style carriers',len(cache),'unique carriers')


if __name__=='__main__':main()
