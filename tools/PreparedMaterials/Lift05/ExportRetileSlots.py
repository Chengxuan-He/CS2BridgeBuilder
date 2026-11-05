"""Export actual complete-main-mesh post-Retile append order; no synthetic road slots."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
r=ROOT/'artifacts/material-remap/Lift05/source-face-coverage'
rows=[x.split('\t') for x in (r/'retile-slots.tsv').read_text().splitlines()]
for delta in (0,3,12):
 levels=[]
 for lod in range(3):
  slots=[dict(surface=x[3],role='structure',triangles=int(x[4]),partIds=['Lift05.mechanism-structure']) for x in rows if int(x[0])==lod and float(x[1])==delta]
  assert slots and [int(x[2]) for x in rows if int(x[0])==lod and float(x[1])==delta]==list(range(len(slots)))
  levels.append(dict(roadStart=len(slots),laneStart=len(slots),slots=slots))
 d=dict(style='Lift05',scope=f'Actual complete source-main-mesh Plan/Retile/upload delta={delta}; header-only source CID fixtures; actual native glass appended near/mid; no independent far glass in original source; no support/road append slots included; no database loading claim',provenance=str((r/'native53-retile-glass.log').resolve()),stableSupplementKeys=False,levels=levels)
 (r/f'producer-delta{delta}.json').write_text(json.dumps(d,indent=2)+'\n')
