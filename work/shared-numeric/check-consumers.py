import ast,hashlib,json,struct,sys
from pathlib import Path
import numpy as np
root=Path.cwd();sys.path.insert(0,str(root/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import decode_arrays,read_bbn
candidate=root/'artifacts/prepared-materials/shared-numeric-recipe/candidate'
rows=[]
def functions(path,names):
 tree=ast.parse(path.read_text(encoding='utf-8-sig'));nodes=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name in names];env=dict(np=np,struct=struct,hashlib=hashlib,decode_arrays=decode_arrays,read_bbn=read_bbn,ROOT=root)
 exec(compile(ast.Module(nodes,type_ignores=[]),str(path),'exec'),env);return env
for style,filename,fn in [('Draw01','BuildUvCharts.py','domains'),('Draw02','ExportNativeCharts.py','domains'),('Lift02','ExportNativeCharts.py','read_domains')]:
 env=functions(root/'tools/PreparedMaterials'/style/filename,[fn,'read_bbn'] if style=='Draw01' else [fn]);count=0
 for p in (root/'assets/HardcodedNumbers'/style).glob('UvDomain_*.bin'):
  a=env[fn](p);b=env[fn](candidate/style/p.name);assert a==b;(json.dumps(b));count+=1
 rows.append(dict(style=style,resources=count,actualDomainFunction=True,plainJsonTypes=True))
# Lift01 domain reader requires render/mesh rather than a path; redirect only its input numbers() boundary.
path=root/'tools/PreparedMaterials/Lift01/BuildUvCharts.py';env=functions(path,['domains']);count=0
for p in (root/'assets/HardcodedNumbers/Lift01').glob('UvDomain_*.bin'):
 key=p.stem.removeprefix('UvDomain_');name,mesh=key.rsplit('_',1);name=name.replace('_',' ')
 for phase in [False,True]:
  def numbers(_):
   i,f=read_bbn((candidate/'Lift01'/p.name if phase else p).read_bytes());return {'I32':i,'F32':f}
  env['numbers']=numbers;result=env['domains'](name,int(mesh));assert result is not None
  if not phase:old=result
  else:assert old==result;json.dumps(result)
 count+=1
rows.append(dict(style='Lift01',resources=count,actualDomainFunction=True,plainJsonTypes=True))
# Execute both exact Draw03 recipe-read expressions; chart arrays keep their separate schema.
path=root/'tools/PreparedMaterials/Draw03/GenerateUvCharts.py';env=functions(path,['arrays']);count=0
for p in (root/'assets/HardcodedNumbers/Draw03').glob('UvDomain_*.bin'):
 a=decode_arrays(*env['arrays'](p.read_bytes()));b=decode_arrays(*env['arrays']((candidate/'Draw03'/p.name).read_bytes()));assert all(x.tobytes()==y.tobytes() for x,y in zip(a,b))
 c=decode_arrays(*read_bbn((candidate/'Draw03'/p.name).read_bytes()));assert c[0].tobytes()==a[0].tobytes();count+=1
rows.append(dict(style='Draw03',resources=count,actualReaderCompositions=True,exactBits=True))
report=dict(consumers=rows,delegatedCallers={'Draw02':['CheckNativeCharts.py','GroupStructureUnits.py'],'Lift02':['CheckNativeCharts.py','PrepareStructureUnits.py']},lift02CSharp='Both tools use actual MovableUvDomains.Items and build with zero warnings/errors; shared C# test proves all domain records exact.')
(root/'artifacts/prepared-materials/shared-numeric-recipe/remaining-consumers.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(rows))
