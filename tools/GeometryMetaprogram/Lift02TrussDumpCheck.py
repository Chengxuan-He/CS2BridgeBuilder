"""Verify actual old/new production Plan dumps, independent of runtime membership loops."""
from pathlib import Path
import json,struct,numpy as np,sys
repo=Path(sys.argv[1]);out=repo/'artifacts/material-remap/Lift02/truss69'
b=(repo/'assets/HardcodedNumbers/Lift02/Types_Lift02_CentralTrusses.bin').read_bytes();n=struct.unpack_from('<i',b,8)[0];ng=struct.unpack_from('<i',b,24)[0]
recipe=np.frombuffer(b,'<i4',count=n,offset=32);h=np.frombuffer(b,'<f4',count=ng,offset=32+n*4)
def read(path):return np.frombuffer(path.read_bytes(),'<f4',offset=4).reshape(-1,3)
rows=[]
for lod in range(3):
 name='LiftBridge02'+(f'_LOD{lod}' if lod else '')+' Mesh';source=np.load(repo/'artifacts/uv-density/native'/(name+'.npz'))['mesh0_positions']
 group=np.full(len(source),-1);at=3*ng+int(recipe[:lod*ng].sum())
 for g in range(ng):
  count=recipe[lod*ng+g];group[recipe[at:at+count]]=g;at+=count
 for delta in [-8,-3,0,2,8.5,24]:
  old=read(out/'old68'/f'lod{lod}-delta{delta}.bin');actual=read(out/'new69r3'/f'lod{lod}-delta{delta}.bin')
  assert np.array_equal(actual[:,1:],old[:,1:]);assert np.array_equal(actual[group<0],old[group<0])
  for g in range(ng):
   k=np.float32((h[g]+np.float32(delta))/h[g]);expected=source[group==g,0]*k
   assert np.array_equal(actual[group==g,0].view('<i4'),expected.view('<i4')),(lod,delta,g)
  rows.append(dict(lod=lod,delta=delta,selected=int((group>=0).sum()),allSelectedSourceXBits=True,unchangedOuterAndYZ=True))
(out/'actual-dump-check.json').write_text(json.dumps(rows,indent=2));print('PASS',len(rows),'actual whole-mesh coordinate cases')
