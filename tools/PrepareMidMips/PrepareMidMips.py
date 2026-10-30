"""Repack existing native BC7 mid-mip blocks for converted Surface identities.

No image decoding, encoding, mip generation, or source asset modification occurs.
The output must pass native registration checks before publishing.
"""
import argparse
from collections import defaultdict
import hashlib
import io
import json
from pathlib import Path
import struct
import zipfile


def digest(b):
    return hashlib.sha256(b).hexdigest()


def cid(b):
    return ''.join(f'{x & 15:x}{x >> 4:x}' for x in b)


def cid_bytes(s):
    return bytes(int(s[i+1]+s[i], 16) for i in range(0,32,2))


def read(s, fmt):
    n=struct.calcsize('<'+fmt)
    b=s.read(n)
    if len(b)!=n: raise ValueError('Truncated native data')
    return struct.unpack('<'+fmt,b)


def vt_header(b):
    if b[:2]!=b'\x01\0': raise ValueError('Unsupported Surface version')
    if not b[6]: return None
    if b[283] not in (0,255): raise ValueError('Invalid VTSurface marker')
    return b[7:300] if b[283] else b[7:284]


def morton(n):
    return (sum(((n>>(2*b))&1)<<b for b in range(3)),
            sum(((n>>(2*b+1))&1)<<b for b in range(3)))


class Sources:
    def __init__(self, game):
        self.zips=[]; self.surfaces={}; self.by_header=defaultdict(list)
        self.textures={}; self.mid=defaultdict(list); self.tile_cache={}
        for p in sorted((game/'Content').glob('*/*.cok')):
            if not p.name.startswith(('Blob','VT','MidMips')):continue
            z=zipfile.ZipFile(p); self.zips.append(z)
            if p.name.startswith('MidMips'):
                for n in z.namelist():
                    if n.endswith('.MidMips'): self.read_mid(z,n)
                continue
            names=set(z.namelist())
            for n in sorted(names):
                if n.endswith(('.Surface','.Material')):
                    if n+'.cid' not in names:continue
                    b=z.read(n); identity=z.read(n+'.cid').decode().strip()
                    if b[:2]!=b'\x01\0':continue
                    h=vt_header(b)
                    if h is None:continue
                    self.surfaces[identity]=(b,str(p),n)
                    self.by_header[h].append(identity)
                elif n.endswith('.VTTexture'):
                    with z.open(n) as f:b=f.read(38)
                    if b[:3]!=b'\x01\0\xff':raise ValueError('Unknown VTTexture header')
                    identity=z.read(n+'.cid').decode().strip()
                    fields=read(io.BytesIO(b[19:]),'5H5Bi')
                    self.textures[identity]=dict(original=cid(b[3:19]),mid=fields[4],width=fields[2],height=fields[3],archive=str(p),entry=n)

    def read_mid(self,z,n):
        info=z.getinfo(n)
        if info.compress_type!=zipfile.ZIP_STORED:raise ValueError('Native MidMips must be stored for bounded seeks')
        with open(z.filename,'rb') as f:
            f.seek(info.header_offset+26); name,extra=read(f,'HH')
            native_offset=info.header_offset+30+name+extra
        with z.open(n) as s:
            tile,levels,maximum,stacks,groups,total=read(s,'6i')
            if (tile,levels,maximum)!=(512,3,64):raise ValueError('Unsupported native MidMips configuration')
            cats=[]
            for stack in range(stacks):
                for _ in range(read(s,'i')[0]):
                    w,h,count,first,tiles,disabled,bias=read(s,'5i?i')
                    cats.append((stack,w,h,count,first,tiles,disabled,bias))
            pending=[]
            for stack,w,h,count,first,tiles,disabled,bias in cats:
                if disabled:continue
                if w!=h:raise ValueError('Unexpected enabled nonsquare category')
                for _ in range(count):
                    identity=cid(s.read(16)); mask,index,duplicates=read(s,'ihi')
                    ids=[identity]+[cid(s.read(16)) for _ in range(duplicates)]
                    indices=[read(s,'ii') for _ in range(levels)]
                    pending.append((ids,dict(stack=stack,width=w,height=h,bias=bias,mask=mask,index=index,indices=indices,archive=z.filename,entry=n)))
            tiles,=read(s,'i'); position=s.tell()
            if read(s,'q')[0]!=position:raise ValueError('Invalid native index alignment')
            for ids,r in pending:
                r.update(start=native_offset+s.tell(),tileCount=tiles)
                for identity in ids:self.mid[identity,r['stack']].append(r)

    def tile(self,r,index):
        if not 0<=index<r['tileCount']:raise ValueError('Native tile out of bounds')
        key=(r['archive'],r['entry'],index)
        if key not in self.tile_cache:
            with open(r['archive'],'rb') as f:
                f.seek(r['start']+index*TILE_BYTES);b=f.read(TILE_BYTES)
            if len(b)!=TILE_BYTES:raise ValueError('Truncated tile')
            self.tile_cache[key]=b
        return self.tile_cache[key]

    def blocks(self,r,level,layer,half):
        first,mask=r['indices'][level]
        if not mask&(1<<layer):raise ValueError('Source layer absent')
        index=first+(mask&((1<<layer)-1)).bit_count()
        raw=self.tile(r,index)
        side=128>>(level+1+half); pitch=132>>half; pad=2>>half
        x,y=morton(r['index']%(4**(level+1)))
        base=MAIN_BYTES if half else 0
        result=b''.join(raw[base+((pad+y*side+row)*pitch+pad+x*side)*16:
                            base+((pad+y*side+row)*pitch+pad+x*side+side)*16] for row in range(side))
        assert len(result)==side*side*16
        return result


MAIN_BYTES=528*528
TILE_BYTES=MAIN_BYTES*5//4
PAINT_SOURCE='4b5500b1b5c270890d99149f7f62e51a'


def resolve(sources,path,b,identity):
    h=vt_header(b)
    if h is None:return [],'non-vt'
    if identity in sources.surfaces:return [],'original-native-identity'
    candidates=sources.by_header.get(h,[])
    method='exact-native-vt-header'
    if not candidates and path.as_posix().endswith('Paint/SkinnedPaint.Surface'):
        original=vt_header(sources.surfaces[PAINT_SOURCE][0]); expected=bytearray(277)
        struct.pack_into('<i',expected,0,1)
        expected[4:12]=original[4:12];expected[12:28]=original[12:28];expected[76:92]=original[76:92]
        if bytes(expected)!=h:raise ValueError('Paint source transformation changed')
        candidates=[PAINT_SOURCE];method='verified-paint-layer0-subset'
    stacks,=struct.unpack_from('<i',h)
    records=[]
    for stack in range(stacks):
        offset=4+stack*136
        width,height=struct.unpack_from('<ii',h,offset)
        layers=[cid(h[offset+72+i*16:offset+88+i*16]) for i in range(4)]
        originals=[cid(h[offset+8+i*16:offset+24+i*16]) for i in range(4)]
        active=[i for i in range(4) if originals[i]!='0'*32]
        levels={sources.textures[x]['mid'] for x in layers if x!='0'*32}
        if len(levels)>1:raise ValueError('Mixed VTTexture mid levels')
        if not active or levels=={0}:continue
        if not candidates:raise ValueError('No native source header for '+str(path))
        mask=sum(1<<i for i in active)
        matches=[(c,r) for c in candidates for r in sources.mid.get((c,stack),[]) if (r['width'],r['height'])==(width,height) and r['mask']&mask==mask]
        if not matches:raise ValueError('Missing source MidMips for '+str(path))
        matches.sort(key=lambda x:(x[0],x[1]['archive'],x[1]['entry']))
        source,r=matches[0]
        # Equal header candidates must expose identical selected raw mip blocks.
        blocks={(level,layer,half):sources.blocks(r,level,layer,half) for level in range(3) for layer in active for half in range(2)}
        for _,other in matches[1:]:
            for key,value in blocks.items():
                if sources.blocks(other,*key)!=value:raise ValueError('Ambiguous source header has unequal blocks')
        records.append(dict(cid=identity,sourceCid=source,stack=stack,width=width,height=height,bias=r['bias'],mask=mask,blocks=blocks,source=r,method=method,path=str(path)))
    return records,method if records else 'no-preprocessed-mid-levels'


def build(records):
    categories=defaultdict(list)
    for r in records:categories[r['stack'],r['width'],r['height'],r['bias']].append(r)
    tiles=[]; ordered=[]; groups=0; hashes=[]
    for key,entries in sorted(categories.items()):
        entries.sort(key=lambda r:r['cid']); first=len(tiles)
        for start in range(0,len(entries),64):
            batch=entries[start:start+64];groups+=1
            for i,r in enumerate(batch):r['ordinal']=i;r['outIndices']=[None]*3
            for level in range(3):
                group_size=4**(level+1)
                for chunk_start in range(0,len(batch),group_size):
                    chunk=batch[chunk_start:chunk_start+group_size]
                    mask=0
                    for r in chunk:mask|=r['mask']
                    for r in chunk:r['outIndices'][level]=(len(tiles),mask)
                    for layer in range(4):
                        if not mask&(1<<layer):continue
                        tile=bytearray(TILE_BYTES)
                        for ordinal,r in enumerate(chunk):
                            if not r['mask']&(1<<layer):continue
                            x,y=morton(ordinal)
                            for half in range(2):
                                data=r['blocks'][level,layer,half]
                                side=128>>(level+1+half);pitch=132>>half;pad=2>>half;base=MAIN_BYTES if half else 0
                                for row in range(side):
                                    off=base+((pad+y*side+row)*pitch+pad+x*side)*16
                                    tile[off:off+side*16]=data[row*side*16:(row+1)*side*16]
                                hashes.append(dict(cid=r['cid'],stack=r['stack'],level=level,layer=layer,half=half,sha256=digest(data)))
                        tiles.append(tile)
        ordered.append((key,entries,first,len(tiles)-first))
    stream=io.BytesIO()
    def write(fmt,*values):stream.write(struct.pack('<'+fmt,*values))
    stacks=2
    write('6i',512,3,64,stacks,groups,len(records))
    for stack in range(stacks):
        cats=[c for c in ordered if c[0][0]==stack];write('i',len(cats))
        for (s,w,h,bias),entries,first,count in cats:write('5i?i',w,h,len(entries),first,count,False,bias)
    for key,entries,first,count in ordered:
        for r in entries:
            stream.write(cid_bytes(r['cid']));write('ihi',r['mask'],r['ordinal'],0)
            for first,mask in r['outIndices']:write('ii',first,mask)
    write('i',len(tiles));write('q',stream.tell())
    for tile in tiles:stream.write(tile)
    return stream.getvalue(),hashes


def main():
    p=argparse.ArgumentParser(description=__doc__)
    for key in ['game-data','materials','output','report']:p.add_argument('--'+key,required=True,type=Path)
    p.add_argument('--per-surface',action='store_true',help='Emit an independently owned cache per converted Surface CID')
    p.add_argument('--shared',action='store_true',help='Store identical immutable caches once under the reserved Shared/MidMips directory')
    p.add_argument('--check',action='store_true',help='Recompute and compare every expected payload without changing material files')
    args=p.parse_args()
    if args.shared and not args.per_surface:p.error('--shared requires --per-surface for owner-granular copies')
    sources=Sources(args.game_data);report={'schema':1,'shared':args.shared,'styles':[]};outputs={}
    for style in sorted(args.materials.iterdir()):
        if not style.is_dir() or style.name=='Shared':continue
        records=[];inventory=[];seen=set()
        for path in sorted(style.rglob('*.Surface')):
            identity=Path(str(path)+'.cid').read_text().strip()
            if identity in seen:continue
            seen.add(identity)
            rs,status=resolve(sources,path,path.read_bytes(),identity)
            records.extend(rs);inventory.append(dict(path=path.relative_to(style).as_posix(),cid=identity,status=status,stacks=len(rs)))
        if not records:continue
        batches=[[r for r in records if r['cid']==c] for c in sorted({r['cid'] for r in records})] if args.per_surface else [records]
        for batch in batches:
            data,checks=build(batch);identity=digest(data)[:32]
            out=args.output/('Shared' if args.shared else style.name)/'MidMips'/identity/'TS512_MB3.MidMips'
            if out in outputs and outputs[out]!=data:raise ValueError('Cache CID collision')
            outputs[out]=data;outputs[Path(str(out)+'.cid')]=identity.encode('ascii')
            rows=[]
            for r in batch:
                rows.append({k:v for k,v in r.items() if k not in ('blocks','outIndices','ordinal')})
            report['styles'].append(dict(style=style.name,cid=identity,bytes=len(data),sha256=digest(data),path=str(out),surfaceInventory=inventory,records=rows,blockChecks=checks))
    # Check the complete expected set before writing anything. Orphans are an
    # explicit regeneration/migration error; never silently register or delete them.
    roots={path.parent.parent for path in outputs}
    if args.shared:roots.add(args.output/'Shared'/'MidMips')
    existing={path for root in roots if root.exists() for path in root.rglob('*') if path.is_file()}
    extra=existing-set(outputs)
    if extra:raise ValueError('Orphan MidMips files: '+', '.join(str(p) for p in sorted(extra)))
    expected_dirs={path.parent for path in outputs}
    orphan_dirs={path for root in roots if root.exists() for path in root.rglob('*') if path.is_dir() and path not in expected_dirs}
    if orphan_dirs:raise ValueError('Orphan MidMips directories: '+', '.join(str(p) for p in sorted(orphan_dirs)))
    for path,data in outputs.items():
        if path.exists() and path.read_bytes()!=data:raise ValueError('Existing MidMips bytes differ: '+str(path))
        if args.check and not path.is_file():raise ValueError('Missing prepared MidMips file: '+str(path))
    if not args.check:
        for path,data in outputs.items():
            if path.exists():continue
            path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    report['payloadCount']=len(outputs)//2
    report['payloadBytes']=sum(len(b) for p,b in outputs.items() if p.suffix=='.MidMips')
    args.report.parent.mkdir(parents=True,exist_ok=True);args.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
    print('PASS', 'check' if args.check else 'prepare',report['payloadCount'],'unique caches',report['payloadBytes'],'payload bytes')


if __name__=='__main__':main()
