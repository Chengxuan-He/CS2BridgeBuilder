"""Verify the current Draw01 producer preserves every packaged Repeat dependency."""
import hashlib
import json
from pathlib import Path
from AuditStructureReuse import bindings

root=Path(__file__).resolve().parents[3]
run=root/'artifacts/prepared-materials/Draw01/producer-control-validation'
output=run/'output/Draw01'
official=root/'assets/Materials/Draw01'
report=json.loads((run/'bake.json').read_text())
assert report['bakedTextures']==0 and report['borrowedControls']==0
expected=set()
surfaces=[]
for file in (official/'Repeat').glob('*/Repeat.Surface'):
    relative=file.relative_to(official)
    expected.update((relative,Path(str(relative)+'.cid')))
    template,channels=bindings(file)
    other_template,other_channels=bindings(output/relative)
    assert template==other_template and channels==other_channels
    surfaces.append(dict(key=file.parent.name,template=template,bindings=channels))
    for cid in channels.values():
        expected.update((Path('Shared')/(cid+'.Texture'),Path('Shared')/(cid+'.Texture.cid')))
actual={p.relative_to(output) for p in output.rglob('*') if p.is_file()}
assert actual==expected,(actual-expected,expected-actual)
files=[]
for relative in sorted(expected):
    before=(official/relative).read_bytes();after=(output/relative).read_bytes()
    assert before==after,relative
    files.append(dict(path=relative.as_posix(),bytes=len(after),sha256=hashlib.sha256(after).hexdigest()))
project=root/'artifacts/agent-material-env/Draw01/unity'
pipeline=[]
for path in sorted((project/'Assets/Editor').glob('*.cs')):
    pipeline.append(dict(path=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
path=project/'Assets/RepeatBake.compute'
pipeline.append(dict(path=path.name,sha256=hashlib.sha256(path.read_bytes()).hexdigest()))
result=dict(status='PASS',surfaces=surfaces,files=files,fileCount=len(files),bytes=sum(f['bytes'] for f in files),
    allBytesIdentical=True,bakedTextures=report['bakedTextures'],borrowedControls=report['borrowedControls'],
    createdProducts=report['createdProducts'],reusedProducts=report['reusedProducts'],actualProjectSourceHashes=pipeline,
    compatibilityPipeline=report['pipeline'])
(run/'comparison.json').write_text(json.dumps(result,indent=2)+'\n')
print(json.dumps({k:v for k,v in result.items() if k not in ('files','surfaces','actualProjectSourceHashes','compatibilityPipeline')},indent=2))
