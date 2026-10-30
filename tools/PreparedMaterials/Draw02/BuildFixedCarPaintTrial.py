"""Prepare a paired unpainted/painted full-period native producer trial."""
from pathlib import Path
import copy
import hashlib
import json

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
manifest = json.loads((OUT/'phase-trial-manifest.json').read_text())
base = manifest['units'][0]
base['sourceGradientSampling'] = False
base['id'] = 'Draw02/fixed-car-phase-trilinear-control'
painted = copy.deepcopy(base)
painted['id'] = 'Draw02/fixed-car-phase-trilinear-painted'
evidence = OUT/'paint-composition-recipe.json'
recipe = json.loads(evidence.read_text())
assert base['min'] == recipe['sourceRegion']['min'] and base['max'] == recipe['sourceRegion']['max']
assert base['panels'] == recipe['phasePanels']
assert base['channels'] == [{k:v for k,v in c.items() if k != 'applyPaintComposition'} for c in recipe['channels']]
painted['paintEvidence'] = dict(path=str(evidence), sha256=hashlib.sha256(evidence.read_bytes()).hexdigest())
painted['paintMipRules'] = recipe['sourceMipRules']
manifest['units'] = [base, painted]
(OUT/'paint-trial-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
print('PASS paired full-period units prepared; original trilinear sampling, Control bypass, same phase map.')
