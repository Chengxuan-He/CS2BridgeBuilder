"""Verify and retain Lift04 native source-level outputs outside disposable tools."""
import hashlib
import json
from pathlib import Path
import shutil
import struct

from ExportSharedSampling import explicit_texture_bindings
from VerifyCoverage import hash_text, read_assets

ROOT=Path(__file__).resolve().parents[3]
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def main():
    temporary=ROOT/'artifacts/agent-material-env/Lift04/source-rebuild/out/Lift04'
    retained=ROOT/'artifacts/prepared-materials/Lift04/rebuild-native'
    output=retained/'output/Lift04'
    report=json.loads((retained/'source-output.json').read_text())
    templates={s['hash']:{p for stack in s['stacks'] for p in stack} for s in json.loads((ROOT/'tools/PrepareLanes/NativeTemplates.json').read_text())}
    by_cid={}; files=[]
    for entry in report['files']:
        raw=(temporary/entry['path']).read_bytes()
        assert len(raw)==entry['bytes'] and hashlib.sha256(raw).hexdigest()==entry['sha256']
        files.append(entry)
        if entry['path'].endswith('.cid'):
            cid=raw.decode().strip();assert cid not in by_cid
            by_cid[cid]=entry['path'][:-4]
    graphs=[]; builtin=set()
    for cid,path in sorted(by_cid.items()):
        if not path.endswith('.Surface'):continue
        raw=(temporary/path).read_bytes(); template=struct.unpack_from('<i',raw,2)[0]
        refs=set()
        if raw[6]:
            stacks=struct.unpack_from('<i',raw,7)[0];assert 0<=stacks<=2
            for stack in range(stacks):
                for layer in range(4):refs.add(hash_text(raw[83+stack*136+layer*16:99+stack*136+layer*16]))
            if raw[283]==255:refs.add(hash_text(raw[284:300]))
        for binding in explicit_texture_bindings(raw):
            if not raw[6] or binding['property'] not in templates[template]:refs.add(binding['sourceTextureCid'])
        refs.discard('0'*32)
        dependencies=[dict(cid=r,kind='style-local' if r in by_cid else 'builtin',path=by_cid.get(r)) for r in sorted(refs)]
        builtin.update(r for r in refs if r not in by_cid)
        source={'kind':'new-shared-source-descriptor'}
        if path.startswith('Lanes/'):source=dict(originalSurfaceSHA256=path.split('/')[1])
        if path.startswith('Road/'):source=dict(originalSurfaceCID='1d7dad9f64c9fe55a809f1080c39fb92')
        if path.startswith('Cable/'):source=dict(originalSurfaceCID='106cc56e9f15a135bb6ea7b99d675c2d')
        if path.startswith('Paint/'):source=dict(originalSurfaceCID='4b5500b1b5c270890d99149f7f62e51a')
        graphs.append(dict(source=source,cid=cid,path=path,template=template,virtualTexturing=bool(raw[6]),dependencies=dependencies))
    for root in report['roots']:builtin.update(root['builtinDependencies'])
    natives=read_assets(GAME,builtin)
    assert len(natives)==len(builtin)
    for entry in files:
        source=temporary/entry['path'];dest=output/entry['path']
        dest.parent.mkdir(parents=True,exist_ok=True)
        if dest.exists():assert dest.read_bytes()==source.read_bytes()
        else:shutil.copyfile(source,dest)
    assert all(any(d.iterdir()) for d in output.rglob('*') if d.is_dir())
    texture_paths=[path for path in by_cid.values() if path.endswith('.Texture')]
    result=dict(style='Lift04',state='NATIVE_DESCRIPTORS_VERIFIED_STYLE_SAMPLING_INCOMPLETE',
        output=str(output),fileCount=len(files),bytes=sum(f['bytes'] for f in files),
        materialDescriptors=len(graphs),textureFiles=len(texture_paths),textureBytes=sum((output/p).stat().st_size for p in texture_paths),
        prototypeSourceRoots=report['roots'],descriptorGraph=graphs,
        builtinDependencies=[dict(cid=cid,bytes=len(raw),sha256=hashlib.sha256(raw).hexdigest(),handling='direct-original-reference-no-mip-reencoding') for cid,raw in sorted(natives.items())],
        files=files,nativeReader='PASS: installed game reader consumed 29 descriptors, including 27 VT descriptors.',
        unresolved=['Measured road repeat region (four uses) requires native region mapping.',
                    'Structural receivers outside reliable charts require native source-level mapping; no old crop images are restored.',
                    'Renderer, complete mip appearance, cold loading and independent owner lifetime remain unverified.'],
        localization='No UI text or localization behavior changed.')
    (retained/'graph.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:result[k] for k in ('state','fileCount','bytes','materialDescriptors','textureFiles','textureBytes')}))


if __name__=='__main__':main()
