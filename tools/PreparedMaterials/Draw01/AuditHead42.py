"""Read saved native Prefab JSON extensions without modifying the owner."""
import json,re
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
E=ROOT/'artifacts/material-remap/shared/draw01-head42'
P=E/'originals/b08b78b59-b651-4758-be19-004a2226aa51/Prefabs'
OUT=ROOT/'artifacts/material-remap/Draw01/head42'
def read(path):
    text=path.read_text(encoding='utf-8-sig').replace('$fstrref:','')
    text=re.sub(r'\$iref:(\d+)',r'{"ref":\1}',text)
    obj=json.loads(text); ids={}
    def visit(x):
        if isinstance(x,dict):
            if '$id' in x:ids[x['$id']]=x
            for y in x.values():visit(y)
        elif isinstance(x,list):
            for y in x:visit(y)
    visit(obj)
    def resolve(x):
        if isinstance(x,dict):
            if set(x)=={'ref'}:return resolve(ids[x['ref']])
            if '$rcontent' in x:return resolve(x['$rcontent'])
            return {k:resolve(v) for k,v in x.items() if not k.startswith('$')}
        if isinstance(x,list):return [resolve(y) for y in x]
        return x
    return resolve(obj)
bindings=json.loads((E/'actual-bindings.json').read_text(encoding='utf-8-sig'))
bycid={x['cid']:x for x in bindings}; objects={}
for x in bindings:
    f=P/x['prefab']
    if f.exists() and 'NetSectionPrefab' in x['type']:objects[x['cid']]=read(f)
root=read(P/'Bridge.Prefab')
rows=[]
def cid(value):return value.split(':')[-1] if isinstance(value,str) else ''
def pieces(section,path):
    node=objects.get(cid(section),{})
    for i,p in enumerate(node.get('m_Pieces',[])):
        b=bycid.get(cid(p.get('m_Piece')), {})
        rows.append(dict(path=path+'/pieces/'+str(i),piece=b.get('prefab',p.get('m_Piece')),geometry=b.get('geometry'),surfaces=b.get('surfaces'),rootConditions=conditions))
    for i,c in enumerate(node.get('m_SubSections',[])):pieces(c['m_Section'],path+'/subsections/'+str(i))
for i,s in enumerate(root.get('m_Sections',[])):
    conditions={k:s.get(k) for k in ['m_RequireAll','m_RequireAny','m_RequireNone','m_Invert','m_Flip','m_Offset','m_HalfLength']}
    pieces(s['m_Section'],'sections/'+str(i))
OUT.mkdir(exist_ok=True,parents=True)
(OUT/'section-bindings.json').write_text(json.dumps(rows,indent=2)+'\n')
(OUT/'root-sections.json').write_text(json.dumps(root.get('m_Sections',[]),indent=2)+'\n')
print('root sections',len(root.get('m_Sections',[])),'piece occurrences',len(rows))
for r in rows:
    if 'Head Road' in str(r['piece']):print(json.dumps(r))
