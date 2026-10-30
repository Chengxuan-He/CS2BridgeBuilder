"""Export exact original-source paint masks/donors for the shared offline producer."""
import hashlib
import json
import math
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT/'artifacts/material-remap/Draw02/fixed-car'
phase = json.loads((OUT/'phase-trial-manifest.json').read_text())['units'][0]
mips = []
for mip in range(13):
    scale = 1 << mip
    size = max(1, 4096 >> mip)
    masks = []
    for left, right, donor in ((1344, 1360, min(1640, 1360+scale)),
                               (1904, 1924, max(1640, 1904-scale))):
        rect = [math.floor(left/scale), math.floor(2002/scale),
                math.ceil(right/scale), math.ceil(2754/scale)]
        donor_x = math.floor(donor/scale)
        masks.append(dict(sourceTexelRectHalfOpen=rect,
            sourceUvRect=[v/size for v in rect], donorSourceTexelX=donor_x,
            donorSourceUvX=(donor_x+.5)/size, donorV='Preserve original source V.'))
    mips.append(dict(sourceMip=mip, width=size, height=size, masks=masks))
channels = []
for channel in phase['channels']:
    p = Path(channel['path'])
    assert hashlib.sha256(p.read_bytes()).hexdigest() == channel['sha256']
    channels.append(dict(**channel, applyPaintComposition=channel['name'] != '_ControlMask'))
proof = ROOT/'artifacts/material-remap/Draw02/near-cells/paint-isolation-check.txt'
recipe = dict(schema='Draw02FixedCarPaintComposition/1',
    sourceCid=phase['sourceCid'], sourceDigest=phase['sourceDigest'], descriptor=phase['descriptor'],
    channels=channels, sourceRegion=dict(min=phase['min'], max=phase['max'], basePixels=[602, 752]),
    phasePanels=phase['panels'], repeatAxis='V', finiteAxis='U', periodMetres=4,
    sourceMipRules=mips,
    operation='Inside either measured 2D mask, replace source sampling U with its donor texel-center U and preserve V. BaseColor/Normal/Mask use exactly the same masks and donor coordinates. Control is unchanged.',
    compositionOrder='Map output coordinates through the cyclic phase panels to original source UV. At each contributing integer source mip, test that original UV against that mip\'s two masks; conditionally replace U with that mip\'s donor center, preserve V, and sample the original mip. Blend the contributions using the declared source-LOD rule. Do not reuse the base-mip mask/donor for every level.',
    filteringLimitation='The previous proof is per-output-sample UV gating before original-mip sampling, not a premodified full-atlas texture. Rewriting source texels then SampleGrad can affect samples outside the old mask through filtering footprints; that alternate operation needs its own explicitly measured effective mask and isolation evidence. Full native anisotropic equivalence is not proven by the old integer-mip test.',
    overlapRule='Evaluate A then B, matching the previous verified finite-cell shader. At overlapping coarse-mip masks the donor coordinates coincide.',
    outsideMask='Retain original source sampling exactly; never repair by filling the full unit, generating a new clean crop, or changing unrelated source descriptors.',
    proof=dict(path=str(proof), sha256=hashlib.sha256(proof.read_bytes()).hexdigest(),
        verifiedOutsideMaskChannelTexels=29983320,
        scope='Previous complete-width private finite-cell composition. This proves that tool mask isolation, not the new phased native output or full sampling equivalence.'),
    status='Development composition recipe. Original boundary/internal mip quality counterexamples remain; this is not restoration of unknown pixels or a quality acceptance.')
(OUT/'paint-composition-recipe.json').write_text(json.dumps(recipe, indent=2)+'\n')
print('PASS exact four-channel identities and 13 integer-source-mip mask/donor recipes exported.')
