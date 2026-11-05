"""Install verified Lift03 clean-road products and measured numeric profile."""
from pathlib import Path
import json,sys,shutil
R=Path(__file__).resolve().parents[3];sys.path.insert(0,str(R/'tools/GeometryMetaprogram'))
from MovableUvDomainFormat import write_bbn
report=json.loads((R/'artifacts/material-remap/Lift03/appearance76/prepared.json').read_text())
background,wear=report['units'];b=background['sourceBounds'];w=wear['sourceBounds']
data=[*wear['density'],b[2]-b[0],w[2]-w[0],wear['visualCenterPixel'],w[0],w[3]-w[1],0]
(R/'assets/HardcodedNumbers/Lift03/Types_Lift03_CleanRoadAppearance.bin').write_bytes(write_bbn([1],data))
source=R/'artifacts/material-remap/Lift03/appearance76/Materials/Lift03';target=R/'assets/Materials/Lift03'
for p in source.rglob('*'):
    if p.is_file():
        q=target/p.relative_to(source);q.parent.mkdir(parents=True,exist_ok=True)
        if q.exists():assert q.read_bytes()==p.read_bytes(),q
        else:shutil.copy2(p,q)
print('Installed verified assets',sum(p.stat().st_size for p in source.rglob('*') if p.is_file()))
