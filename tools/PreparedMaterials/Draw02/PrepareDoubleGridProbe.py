"""Build matched GPU sampling cases for one/two-period Draw02 native candidates."""
import argparse,json,math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw02/offline-atlas-units'
p=argparse.ArgumentParser();p.add_argument('report',type=Path);p.add_argument('payload_root',type=Path);a=p.parse_args()
unit=json.loads(a.report.read_text())['units'][0]
channels={c['name']:c for c in unit['channels']}
base=json.loads((OUT/'filter-manifest-raw-grid.json').read_text())['pairs']
pairs=list(base);metadata=[]
for source in base:
 name=source['name'].split('/')[1];c=channels[name]
 # Every level stores two periods. Native halving is checked, not independently rounded.
 assert c['height']==1654 and c['mips']==11
 assert c['contentMin'][1]==0 and c['contentMax'][1]==1,'Explicit phase adjustment required for nonzero content origin'
 path=a.payload_root/(c['cid']+'.Texture');b=path.read_bytes()
 assert int.from_bytes(b[4:6],'little')==1654 and b[8]==11
 for label,lo,hi in [('first',0,.5),('second',.5,1),('wrong-uncompensated',0,1)]:
  pair=dict(source);pair['name']='double-'+label+'/'+name
  pair['derived']=str(path.resolve());pair['derivedColor']=str((a.payload_root/(channels['_BaseColorMap']['cid']+'.Texture')).resolve())
  pair['content']=[c['contentMin'][0],lo,c['contentMax'][0],hi];pairs.append(pair)
 metadata.append(dict(channel=name,bytes=len(b),levels=[dict(mip=m,height=max(1,1654>>m),pixelsPerPhysicalPeriod=max(1,1654>>m)/2,densityRatio=(max(1,1654>>m)/2)/(827/2**m)) for m in range(11)]))
# Analytic period/derivative negative control is independent of pixel content.
assert .5*1654==827 and 1*1654!=827
for q in [-8.25,0,.25,1,17.125]:
 assert (q+1)/2-q/2==.5
(OUT/'filter-manifest-double-grid.json').write_text(json.dumps(dict(pairs=pairs),indent=2)+'\n')
(OUT/'double-grid-input-check.json').write_text(json.dumps(dict(channels=metadata,physicalPeriodMetres=4,correctTextureAdvancePerPeriod=.5,negativeUncompensatedAdvance=1,negativePhysicalFrequencyRatio=2,qualityAccepted=False),indent=2)+'\n')
print('Prepared 16 GPU pairs: single plus first/second double periods and uncompensated negative controls. No pixels generated.')
