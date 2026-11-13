import pathlib,json,re,base64,struct,hashlib
repo=pathlib.Path(__file__).resolve().parents[2]; inp=repo/'artifacts/four-new-styles/TrussViaduct'; out=repo/'assets/HardcodedNumbers/TrussViaduct';out.mkdir(parents=True,exist_ok=True)
text=(repo/'src/BridgeBuilder/Bridges/Types/TrussArch01/TrussArch01PierData.g.cs').read_text(); maps=re.findall(r'PortalMap\([^,]+, "([^"]+)", "([^"]+)"\)',text)
ids=['f87113be96c26ca9af910ab139734804','4fb8c22aa04575fea86fc845f692543b','f822e6c4b8f9263532410cecbd8a6168','8d60c03f6ebd560e8466d20a787275b1','7c8ea4dac5952bab836d3602721ce25f','23da94592c16ecf9f6ebbd80594524aa']
for idx,cid in enumerate(ids):
 meshes=json.loads((inp/(cid+'.Geometry.json')).read_text()); d=[];f=[];counts=[]
 for mesh in meshes:
  p=mesh['positions'];counts.append(len(p)); xs=[v[0] for v in p]
  if idx<3:
   bits=base64.b64decode(maps[idx][0]);raw=base64.b64decode(maps[idx][1]);vals=iter(struct.unpack('<'+'f'*(len(raw)//4),raw))
   for i,x in enumerate(xs):
    if bits[i//8]&(1<<(i%8)):
     c=next(vals); d.append(c); f.append(c/x if x else 0)
    else:d.append(1 if x>0 else -1 if x<0 else 0);f.append(0)
  elif idx==3:
   d.extend(1 if x>0 else -1 if x<0 else 0 for x in xs);f.extend([0]*len(xs))
  elif idx==4:
   scale=2/(max(xs)-min(xs));d.extend(x*scale for x in xs);f.extend([scale]*len(xs))
  else:d.extend([0]*len(xs));f.extend([0]*len(xs))
 for label,values in [('Displacement',d),('Frames',f)]:
  b=struct.pack('<8i',0x314e4242,1,len(counts),0,0,0,len(values),0)+struct.pack('<'+'i'*len(counts),*counts)+struct.pack('<'+'f'*len(values),*values);b+=hashlib.sha256(b).digest();(out/f'Types_TrussViaduct_{idx}_{label}.bin').write_bytes(b)
 print(idx,cid,counts)

