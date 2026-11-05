from pathlib import Path
import json,re
R=Path(__file__).resolve().parents[3];O=R/'artifacts/material-remap/Lift01/coverage54'
plans=json.loads((R/'artifacts/material-remap/Lift01/coverage53.json').read_text(encoding='utf-8-sig'))
levels=[]
for lod in range(3):
 p=next(p for p in plans if p['delta']==0 and p['lod']==lod and p['impl']==('Lift01FarStructure' if lod==2 else 'Lift01DetailedStructure'))
 text=(O/'Lift01RoadTops.evidence.cs').read_text(encoding='utf-8-sig');body=text.split(f'{lod} => new (int, int, int, int)[]')[1].split('}',1)[0]
 road={int(a) for a,b,c,d in re.findall(r'\((\d+), (\d+), (\d+), (\d+)\)',body)}
 groups={'replaced-road-top':road}
 if lod<2:
  for kind,script in [('selected-sleepers','Lift01Sleepers'),('selected-rails-fasteners','Lift01Rails'),('selected-catenary','Lift01Catenary')]:
   body=(O/(script+'.evidence.cs')).read_text(encoding='utf-8-sig').split(f'[] Lod{lod} =')[1].split('};',1)[0]
   groups[kind]={i for a,b in re.findall(r'\((\d+), (\d+)\)',body) for i in range(int(a),int(b)+1)}
 remaining=set(p['removedWithoutFragment'])-set.union(*groups.values())
 if lod<2:assert not remaining
 else:assert len(remaining)==36;groups['replaced-far-tower-proxy']=remaining
 groups['replaced-structural-fragment']=set(p['fragmentParentIds'])
 seen=set()
 for key,ids in groups.items():assert not seen&ids;seen|=ids
 assert len(seen)==p['removed']
 groups['retained-original-structure']=set(range(p['sourceFaces']))-seen
 assert sum(map(len,groups.values()))==p['sourceFaces']
 levels.append(dict(lod=lod,sourceFaces=p['sourceFaces'],dispositions={k:sorted(v) for k,v in groups.items()},counts={k:len(v) for k,v in groups.items()}))
(O/'source-face-disposition.json').write_text(json.dumps(dict(scope='Exact frozen production masks versus independently re-extracted native memberships. Final renderer/animation evidence remains separate.',levels=levels),indent=2))
print([p['counts'] for p in levels])
