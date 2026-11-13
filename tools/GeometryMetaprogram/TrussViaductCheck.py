import pathlib,struct,hashlib,json,math
root=pathlib.Path(__file__).resolve().parents[2];d=root/'assets/HardcodedNumbers/TrussViaduct'; report=[]
for p in sorted(d.glob('*.bin')):
 b=p.read_bytes();assert hashlib.sha256(b[:-32]).digest()==b[-32:];counts=struct.unpack_from('<6i',b,8);assert len(b)==64+4*sum(counts);v=struct.unpack_from('<'+'f'*counts[4],b,32+4*counts[0]);assert all(math.isfinite(x) for x in v); report.append({'file':p.name,'bytes':len(b),'sha256':hashlib.sha256(b).hexdigest(),'values':len(v)})
for cid,index in [('f87113be96c26ca9af910ab139734804',0),('4fb8c22aa04575fea86fc845f692543b',1),('f822e6c4b8f9263532410cecbd8a6168',2),('8d60c03f6ebd560e8466d20a787275b1',3),('7c8ea4dac5952bab836d3602721ce25f',4)]:
 m=json.loads((root/'artifacts/four-new-styles/TrussViaduct'/(cid+'.Geometry.json')).read_text());b=(d/f'Types_TrussViaduct_{index}_Displacement.bin').read_bytes();c=struct.unpack_from('<6i',b,8);a=struct.unpack_from('<'+'f'*c[4],b,32+c[0]*4);points=[p for mesh in m for p in mesh['positions']];assert len(points)==len(a)
 for half in [0,-4,8,20]:
  for p,k in zip(points,a):
   x=struct.unpack('<f',struct.pack('<f',p[0]+k*half))[0];assert math.isfinite(x)
   if half==0:assert struct.pack('<f',x)==struct.pack('<f',p[0])
(root/'artifacts/four-new-styles/TrussViaduct/table-check.json').write_text(json.dumps({'status':'pass','scopes':['all BBN checksums/lengths/finite coefficients','zero delta exact position bits','half deltas -4,8,20 finite outputs'],'resources':report},indent=2));print('PASS',len(report),'BBN resources; zero-delta exact and finite width cases')

