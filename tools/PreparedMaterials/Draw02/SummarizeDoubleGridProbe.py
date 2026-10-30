"""Summarize matched-footprint actual GPU reads; never set a quality threshold."""
import csv,json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];p=ROOT/'artifacts/material-remap/Draw02/offline-atlas-units'
rows=list(csv.DictReader((p/'native-filter-check-double-grid.tsv').open(),delimiter='\t'));assert len(rows)==1200
index={(x['pair'],x['lod'],x['anisotropy']):x for x in rows};groups=[]
metrics=['maxRGB','maxAlpha','maxNormalVector','alphaMismatch','boundaryRGB','boundaryAlpha','boundaryNormal','boundedInteriorRGB','boundedInteriorAlpha','boundedInteriorNormal']
for channel in ['_BaseColorMap','_ControlMask','_MaskMap','_NormalMap']:
 for period in ['double-first','double-second','double-wrong-uncompensated']:
  matches=[(x,index['unit0/'+channel,x['lod'],x['anisotropy']]) for x in rows if x['pair']==period+'/'+channel]
  summary={}
  for metric in metrics:
   values=[(float(a[metric]),float(b[metric])) for a,b in matches if not metric.startswith('bounded') or int(a['boundedInteriorSamples'])>0]
   summary[metric]=dict(cases=len(values),lower=sum(a<b for a,b in values),equal=sum(a==b for a,b in values),higher=sum(a>b for a,b in values),maximum=max(a for a,b in values))
  groups.append(dict(channel=channel,period=period,metrics=summary))
single=json.loads((p/'producer-report-raw.json').read_text())['units'][0]
files=[p/'output-raw/Draw02/RepeatUnits'/single['key']/'Repeat.Surface']+[p/'output-raw/Draw02/Shared'/(c['cid']+'.Texture') for c in single['channels']]
size=sum(f.stat().st_size+Path(str(f)+'.cid').stat().st_size for f in files)
receipt=json.loads((ROOT/'artifacts/material-remap/shared/two-period-draw02/native-receipt.json').read_text())
report=dict(cases=len(rows),singleBytes=size,doubleBytes=receipt['sizeBytes'],increaseBytes=receipt['sizeBytes']-size,groups=groups,qualityAccepted=False,normalUnits='Euclidean decoded normal vector difference, not degrees.',interior='Two source-footprint exclusion in both axes; no interior at some coarse levels. This conservative mask does not certify all hardware anisotropic tap extents.',limits='Matched GPU sampling measures combined reconstruction/filter error. It does not uniquely separate necessary rounding from every other contributor, nor establish visual acceptance.')
(p/'double-grid-comparison.json').write_text(json.dumps(report,indent=2)+'\n')
text=f'''# Draw02 double-period matched-footprint comparison

The actual native single and double derivatives were sampled on the GPU without rebaking. All 1,200 comparisons completed: four channels, 25 half-step source footprints, three anisotropy ratios, and single/first-double/second-double/uncompensated-negative mappings. Direct texture sampling applies V/2 once; the Surface UV scale is not additionally applied in this test.

Single closure: {size:,} bytes. Double closure: {receipt['sizeBytes']:,} bytes. Increase: {receipt['sizeBytes']-size:,} bytes. These unit closures are not complete bridge or publication budgets.

At isotropic source footprint LOD 1, first-period interior normal vector error is approximately 3.53e-8 with zero alpha classifications changed. The second period has maximum normal vector error 0.315774 and 38 alpha classifications changed; single has 0.302062 and 22. A total mip height of 827 gives 413.5 samples per physical period and differing sample phases between periods. This explains why inspecting only the first copy is insufficient; it is not evidence of an illegal mip chain.

At LOD 2, first/second maximum normal differences are 0.226579/0.220263 versus single 0.218295. At LOD 0, both copies retain the same 0.024676 boundary difference as single. The uncompensated negative control gives normal difference 1.62489 and 2,396 alpha classification changes at LOD 0, confirming sensitivity to the doubled frequency.

The first period's improvement at one level is real but does not establish an overall improvement. No boundary blending was introduced or authorized. Normal differences are vector distances, not angles. Alpha comparisons use the template 1 default cutoff 0.5, but sample classifications are not rendered visual acceptance. All PBR values and per-footprint comparisons are retained in `native-filter-check-double-grid.tsv`; complete metric comparisons are in `double-grid-comparison.json`. Per-level density ratios are in `double-grid-input-check.json`.

Coarse levels with zero bounded-interior samples remain explicit. The narrower integer-grid/two-period authorization makes the chain permitted; it does not automatically accept its measured errors. No production code, packaged resources or installed files changed.
'''
(p/'DoubleGridReview.md').write_text(text)
print(json.dumps({k:v for k,v in report.items() if k!='groups'}))
