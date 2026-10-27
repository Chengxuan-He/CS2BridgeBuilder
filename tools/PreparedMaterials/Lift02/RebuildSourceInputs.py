"""Build Lift02 source-level inputs directly from original game archives.
No previous prepared output, product store or generated bridge is an input.
"""
from pathlib import Path
from collections import defaultdict
import argparse,hashlib,json,re,struct,zipfile
from ExportSharedSampling import descriptor
from PrepareRecipes import recipes,repeat_key
ROOT=Path(__file__).resolve().parents[3]

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--output',type=Path,default=ROOT/'artifacts/agent-material-env/Lift02/source-rebuild');parser.add_argument('--game',type=Path,default=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data'));args=parser.parse_args()
    inputs=args.output/'inputs';reports=args.output/'reports';inputs.mkdir(parents=True,exist_ok=True);reports.mkdir(parents=True,exist_ok=True)
    references=defaultdict(list);render_records=[]
    for filename in ['Lift02SourceRenders.cs','Lift02PillarSources.cs']:
        text=(ROOT/'src/BridgeBuilder/Bridges/Types/Lift02'/filename).read_text()
        for name,geometry,values in re.findall(r'Create\("([^"]+)", "([a-f0-9]+)", new\[\] \{ ([^}]+)',text):
            surfaces=re.findall(r'"([a-f0-9]{32})"',values);render_records.append(dict(render=name,geometryCid=geometry,surfaceCids=surfaces))
            for slot,cid in enumerate(surfaces):references[cid].append(dict(render=name,mesh=slot))
    archives=[];entries={}
    try:
        for family in ['Game','BridgesAndPorts']:
            for path in (args.game/'Content'/family).glob('*.cok'):
                if not path.name.startswith(('Blob','VT')):continue
                z=zipfile.ZipFile(path);archives.append(z);names=set(z.namelist())
                for name in names:
                    if name.endswith('.cid') and name[:-4] in names:entries[z.read(name).decode().strip()]=(z,name[:-4])
        def record(identity):
            z,name=entries[identity];h=hashlib.sha256()
            with z.open(name) as f:
                while chunk:=f.read(1024*1024):h.update(chunk)
            return dict(cid=identity,archive=z.filename,entry=name,sha256=h.hexdigest(),bytes=z.getinfo(name).file_size,builtin=True)
        surfaces=[];payloads={};native_raw={}
        for identity,uses in sorted(references.items()):
            z,name=entries[identity];raw=z.read(name);native_raw[identity]=raw;template,vt,header,vt_id,bindings,keywords=descriptor(raw)
            graph=[]
            vt_header=None
            if vt_id:
                graph.append(vt_id);va,vn=entries[vt_id]
                with va.open(vn) as stream:vt_header=stream.read(498)
            if header:
                for stack in range(struct.unpack_from('<i',header)[0]):
                    for layer in range(4):
                        a=76+stack*136+layer*16;block=header[a:a+16]
                        cid=''.join(format(b&15,'x')+format(b>>4,'x') for b in block)
                        active=vt_header is not None and vt_header[106+stack*192+layer*48]!=0
                        if cid!='0'*32 and not active:graph.append(cid)
            graph.extend(cid for channel,cid in bindings.items() if not vt or channel not in ['_BaseColorMap','_NormalMap','_MaskMap','_ControlMask']);graph=sorted(set(graph))
            for cid in graph:
                if cid not in payloads:payloads[cid]=record(cid)
            surfaces.append(dict(**record(identity),template=template,virtualTexturing=vt,physicalBackingCids=graph,serializedTextureBindings=bindings,keywords=keywords,uses=uses,action='retainOriginalSurfaceCid'))
        patches,evidence=recipes(ROOT);source_id='ea923855f34e05d7a517992fe064ddb7';digest=hashlib.sha256(native_raw[source_id]).hexdigest()
        for unit in patches:
            unit.update(style='Lift02',sourceCid=source_id,sourceDigest=digest);unit['oldLookupKey']=repeat_key(unit)
            unit['nativeMappingRequired']='region-local repetition using shared full native source, not a resampled texture per region'
        inventory=dict(schema='BridgeBuilder/Lift02NativeSourceRebuild/1',style='Lift02',prototypeRoad=False,
            renders=render_records,surfaces=surfaces,sourcePayloads=list(payloads.values()),fixedEndSampling=patches,samplingEvidence=evidence,
            commonConstantsRequired=['Road.NativeAsphalt descriptor conversion preserving built-in texture CIDs','One transparent zero control identity shared within Lift02','Paint native source/descriptor conversion supplied once by the shared material producer'],
            outputs=dict(originalSurfaceCopies=0,originalTextureCopies=0,bakedTextures=0),
            nativeMipRule='Keep original native Surface/VT references and dependencies. Never substitute mip-zero decoder output.',
            unresolved=['Extended structural receivers require the shared native sampling implementation; chart data remains separate.','Fixed-end local repeat requires verified shared-source native sampling.','Do not publish a source-level manifest alone as a finished material pipeline.'])
        payload=json.dumps(inventory,indent=2).encode();(inputs/'native-source-inventory.json').write_bytes(payload)
        summary=dict(style='Lift02',renders=len(render_records),surfaceUses=sum(map(len,references.values())),sourceSurfaces=len(surfaces),sourcePayloads=len(payloads),sourcePayloadBytes=sum(p['bytes'] for p in payloads.values()),
            templateCounts={str(t):sum(s['template']==t for s in surfaces) for t in sorted({s['template'] for s in surfaces})},
            packagedSourcePayloadBytes=0,preparedOutputFiles=0,oldPreparedLibraryReads=0,newBakes=0,inventorySha256=hashlib.sha256(payload).hexdigest())
        (reports/'source-rebuild.json').write_text(json.dumps(summary,indent=2));print(json.dumps(summary,indent=2))
    finally:
        for archive in archives:archive.close()
if __name__=='__main__':main()
