"""Prepare the authorized Lift04 404-pixel beam pattern without changing its coverage."""
import hashlib
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]


def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    retained=ROOT/'artifacts/prepared-materials/Lift04/beam-repeat'
    inputs=ROOT/'artifacts/agent-material-env/Lift04/beam-repeat/inputs'
    retained.mkdir(parents=True,exist_ok=True);inputs.mkdir(parents=True,exist_ok=True)
    evidence_path=ROOT/'artifacts/prepared-materials/Lift04/structure-counterexample/inspection.json'
    evidence=json.loads(evidence_path.read_text())
    boundary_path=ROOT/'artifacts/shared-material-pipeline/structure-counterexample/strip-boundary.json'
    boundary=json.loads(boundary_path.read_text())
    original=json.loads((ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/extracted-source/manifest.json').read_text())['units'][0]
    assert evidence['originalPixelRectangle']==[2092,2076,2138,3342]
    minimum=[2092/4096,2076/4096];maximum=[2138/4096,2480/4096]
    physical=[evidence['physicalHeightMetres'],evidence['physicalLengthMetres']*404/1266]
    proof=dict(style='Lift04',id='Lift04BeamStrip404',sourceCid=original['sourceCid'],sourceDigest=original['sourceDigest'],
               sourceMeshSHA256=evidence['sourceMeshSha256'],sourceFaces=evidence['originalFaces'],
               coverageMin=[2092/4096,2076/4096],coverageMax=[2138/4096,3342/4096],coveragePixels=[46,1266],
               min=minimum,max=maximum,unitPixels=[46,404],physicalSizeUV=physical,
               densityUV=[46/physical[0],404/physical[1]],template=0,aliases=[],
               inspection=dict(path=str(evidence_path),sha256=sha(evidence_path)),
               nativeBoundaryEvidence=dict(path=str(boundary_path),sha256=sha(boundary_path),measurements=boundary),
               authorization='The source 404-pixel pattern is accepted for one offline repeat unit. Preserve source density and unit interior; boundary variation remains a human visual acceptance item.',
               mapping='Coverage rectangle remains 46x1266; unit sampling rectangle is 46x404. Do not map the old normalized chart directly to the shorter unit.',
               localization='Offline source evidence only; no UI text changes.')
    proof_path=retained/'source-evidence.json';proof_path.write_text(json.dumps(proof,indent=2)+'\n')
    unit=dict(id=proof['id'],sourceCid=original['sourceCid'],sourceDigest=original['sourceDigest'],
              min=minimum,max=maximum,physicalSize=physical,aliases=[],
              evidence=dict(path=str(proof_path),sha256=sha(proof_path)),descriptor=original['descriptor'],channels=original['channels'])
    manifest=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift04',units=[unit])
    (inputs/'manifest.json').write_text(json.dumps(manifest,indent=2)+'\n')
    print(json.dumps(dict(manifest=str(inputs/'manifest.json'),unitPixels=proof['unitPixels'],coveragePixels=proof['coveragePixels'],densityUV=proof['densityUV'])))


if __name__=='__main__':main()
