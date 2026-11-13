"""Compare the production lossless VT conversion with the independent offline decoder."""
import argparse,hashlib,json,sys,tempfile,zipfile
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]/'PrepareRepeatUnits'))
from ExtractVtSources import Decoder,MidCache
p=argparse.ArgumentParser();p.add_argument('archive');p.add_argument('game');p.add_argument('textures');p.add_argument('scratch');a=p.parse_args()
rows=[]
with tempfile.TemporaryDirectory(dir=a.scratch) as tmp,zipfile.ZipFile(a.archive) as source:
 cache=Path(tmp)/'Content/Source';cache.mkdir(parents=True)
 with zipfile.ZipFile(cache/'MidMips.cok','w') as out:
  for n in source.namelist():
   if n.endswith('.MidMips'):out.writestr(n,source.read(n))
 decoder=Decoder(Path(a.game));decoder.mid=MidCache(Path(tmp))
 for n in source.namelist():
  if not n.endswith('.VTSurface'):continue
  raw=source.read(n)
  for stack,layers in ((0,4),(1,1)):
   for layer in range(layers):
    expected,proof=decoder.recover(dict(kind='VTSurface',stack=stack,layer=layer,name='_BaseColorMap'),raw,'')
    path=Path(a.textures)/(Path(n).stem+f'-{stack}-{layer}.Texture')
    actual=path.read_bytes();assert actual==expected,path
    rows.append(dict(name=path.name,bytes=len(actual),sha256=hashlib.sha256(actual).hexdigest(),mips=proof['mips'],format=proof['format'],mipSha256=proof['mipSha256']))
 for archive in decoder.mid.archives:archive.close()
print(json.dumps(rows,indent=2));print(f'PASS {len(rows)} exact independently reconstructed complete texture files; totalBytes={sum(r["bytes"] for r in rows)}')
