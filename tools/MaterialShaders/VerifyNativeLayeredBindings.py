"""Offline verification of native fragment preservation and packed variant consistency."""
import argparse,hashlib,json,struct,sys
from pathlib import Path
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--native',type=Path,required=True);p.add_argument('--bundle',type=Path,required=True);p.add_argument('--unitypy',type=Path);p.add_argument('--output',type=Path,required=True);a=p.parse_args()
if a.unitypy:sys.path.insert(0,str(a.unitypy))
import UnityPy
from UnityPy.helpers import CompressionHelper
source=json.loads(a.native.read_text());target=next(o for o in UnityPy.load(str(a.bundle)).objects if o.type.name=='Shader').read_typetree()
def records(s):
 b=bytes(s['compressedBlob']);segments=[CompressionHelper.decompress_lz4(b[o:o+c],n)for o,c,n in zip(s['offsets'][0],s['compressedLengths'][0],s['decompressedLengths'][0])];out=[]
 for i in range(struct.unpack_from('<i',segments[0])[0]):
  o,n,k=struct.unpack_from('<3i',segments[0],4+i*12);out.append(segments[k][o:o+n])
 return out
def digest(e):
 i=e.index(b'DXBC');return hashlib.sha256(e[i:i+struct.unpack_from('<I',e,i+24)[0]]).hexdigest()
sr,tr=records(source),records(target);passes=lambda s:[p for sub in s['m_ParsedForm']['m_SubShaders']for p in sub['m_Passes']];report=[]
for sp,tp in zip(passes(source),passes(target)):
 name=sp['m_State']['m_Name'];assert name==tp['m_State']['m_Name'];assert json.dumps(sp['m_State'],sort_keys=True)==json.dumps(tp['m_State'],sort_keys=True);fragments=0;vertices=0
 for sa,ta in zip(sp['progFragment']['m_PlayerSubPrograms'],tp['progFragment']['m_PlayerSubPrograms']):
  assert len(sa)==len(ta)
  for sv,tv in zip(sa,ta):
   assert sv['m_KeywordIndices']==tv['m_KeywordIndices'];assert digest(sr[sv['m_BlobIndex']])==digest(tr[tv['m_BlobIndex']]),(name,'native fragment changed');fragments+=1
 for group in tp['progVertex']['m_PlayerSubPrograms']:
  for dots in [False,True]:
   subset=[v for v in group if ('DOTS_INSTANCING_ON'in[target['m_ParsedForm']['m_KeywordNames'][k]for k in v['m_KeywordIndices']])==dots]
   if subset:assert len({digest(tr[v['m_BlobIndex']])for v in subset})==1,(name,dots,'keyword changed packed vertex layout')
   vertices+=len(subset)
 report.append(dict(passName=name,unchangedNativeFragments=fragments,packedVertices=vertices))
a.output.write_text(json.dumps(dict(nativeSha256=hashlib.sha256(a.native.read_bytes()).hexdigest(),bundleSha256=hashlib.sha256(a.bundle.read_bytes()).hexdigest(),passes=report),indent=2)+'\n')
print('PASS',sum(r['unchangedNativeFragments']for r in report),'unchanged native fragment variants;',sum(r['packedVertices']for r in report),'packed vertex variants;',len(report),'unchanged pass states')
