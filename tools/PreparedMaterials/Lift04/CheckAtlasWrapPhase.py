"""Quantify original wrap-plane displacement separately from UV quantization error."""
import json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
base=ROOT/'artifacts/material-remap/Lift04'
groups=json.loads((base/'near-domain-groups.json').read_text())
audit=json.loads((base/'near-domain-audit.json').read_text())
levels=[]
for level in groups['levels']:
    tiles=audit['levels'][level['lod']]['domains'];records=[]
    for group in level['domains']:
        if not group['repeatLongitudinal']:continue
        members=sorted([t for t in tiles if set(t['faces']).issubset(group['faces'])],key=lambda t:t['positionMin'][2])
        coeff=np.array(group['u'])
        for before,after in zip(members,members[1:]):
            z=before['positionMax'][2];assert z==after['positionMin'][2]
            x=(group['min'][0]+group['max'][0])/2
            actual=coeff[0]*x+coeff[1]*z+coeff[2]
            integer=round(actual)
            reconstructed=(integer-coeff[0]*x-coeff[2])/coeff[1]
            records.append(dict(before=before['faces'],after=after['faces'],originalZ=z,
                projectedInteger=integer,phaseDifference=actual-integer,
                sourcePixelsDifference=abs(actual-integer)*638,
                projectedZ=reconstructed,displacementMetres=reconstructed-z))
    levels.append(dict(lod=level['lod'],checkedLongitudinalSeams=len(records),
        maximumDisplacementMetres=max(abs(r['displacementMetres']) for r in records),
        maximumPhasePixels=max(r['sourcePixelsDifference'] for r in records),seams=records))
(base/'atlas-wrap-phase.json').write_text(json.dumps(dict(levels=levels,
    interpretation='Rejected historical twenty-domain candidate: UV-half rounding equivalence alone does not prove exact atlas boundary footprints. Nonzero seam displacement places an original boundary pixel on a different side for a narrow strip. The original atlas is not a periodic crop; no visual or native acceptance is inferred.'),indent=2))
print([{k:v for k,v in l.items() if k!='seams'} for l in levels])
