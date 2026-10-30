"""Prepare one complete source-grid half-floor shared by near/middle physical panels."""
import argparse
import hashlib
import json
import math
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--grid-mip',type=int,default=0)
    args=parser.parse_args()
    assert 0<=args.grid_mip<=10
    source=ROOT/'artifacts/material-remap/Lift01/half-floor/inputs/manifest.json'
    old=json.loads(source.read_text())
    unit=old['units'][0].copy()
    unit.pop('bands')
    unit.update(id='Lift01-complete-half-floor-source-grid',finiteSourceGrid=True,finiteGridMip=args.grid_mip,sourceGradientSampling=True)
    # Content is the same complete native atlas half-floor at both LODs. The
    # runtime original U panel projections differ; no extra pixel variant exists.
    unit['normalTransform']=[1,0,0,0,1,0,0,0,1]
    report=ROOT/'artifacts/material-remap/Lift01/half-floor-domains.json'
    unit['evidence']=dict(path=str(report),sha256=hashlib.sha256(report.read_bytes()).hexdigest())
    source_x=unit['min'][0]*4096;span=(unit['max'][0]-unit['min'][0])*4096
    quantum=2**args.grid_mip;guard=math.ceil(span/2)
    start=math.floor((source_x-guard)/quantum)*quantum
    width=math.ceil((source_x+span+guard-start)/quantum)*quantum
    height=math.ceil((unit['max'][1]-unit['min'][1])*4096)
    mip_count=1+int(math.log2(max(width,height)))
    rgba=bc7=0
    for mip in range(mip_count):
        w,h=max(1,width>>mip),max(1,height>>mip)
        rgba+=w*h*4*3;bc7+=math.ceil(w/4)*math.ceil(h/4)*16*3
    out=ROOT/'artifacts/material-remap/Lift01/half-floor-source-grid'
    out.mkdir(parents=True,exist_ok=True)
    (out/'manifest.json').write_text(json.dumps(dict(recipe='BridgeBuilder/RepeatUnits/3',style='Lift01',units=[unit]),indent=2))
    (out/'budget.json').write_text(json.dumps(dict(gridMip=args.grid_mip,width=width,height=height,mips=mip_count,
        rawRgbaThreeChannelBytes=rgba,bc7ThreeChannelBytes=bc7,actualCompressionNotMeasured=True,
        ownerBudget=10000000,wholePackageBudget=55000000,
        sourceContentSharedAcrossLods=True,perSamplingCopies=0,
        readyForBakeValidation=True,readyForProduction=False,
        scope='Preflight of one whole native half-floor. Descriptor/control/owner dependencies and actual native compression are excluded, so this is not package/owner compliance.'),indent=2))
    print(out/'manifest.json')


if __name__=='__main__':main()
