"""Stage exact UV-domain recipes outside formal assets and prove byte reconstruction."""
import argparse
import hashlib
import json
from pathlib import Path
from MovableUvDomainFormat import decode_arrays, encode_arrays, read_bbn, write_bbn

parser=argparse.ArgumentParser(description=__doc__)
parser.add_argument('source',type=Path)
parser.add_argument('output',type=Path)
args=parser.parse_args()
if args.output.exists():raise ValueError('Stage destination already exists')
args.output.mkdir(parents=True)
rows=[]
for path in sorted(args.source.rglob('UvDomain_*.bin')):
    original=path.read_bytes();legacy=decode_arrays(*read_bbn(original))
    candidate=write_bbn(*encode_arrays(*legacy))
    restored=decode_arrays(*read_bbn(candidate))
    if any(a.tobytes()!=b.tobytes() for a,b in zip(legacy,restored)):
        raise ValueError('Reconstruction mismatch: '+str(path))
    if write_bbn(*encode_arrays(*restored))!=candidate:
        raise ValueError('Regeneration mismatch: '+str(path))
    target=args.output/path.relative_to(args.source);target.parent.mkdir(parents=True,exist_ok=True);target.write_bytes(candidate)
    rows.append(dict(path=str(path.relative_to(args.source)),originalBytes=len(original),candidateBytes=len(candidate),originalSHA256=hashlib.sha256(original).hexdigest(),candidateSHA256=hashlib.sha256(candidate).hexdigest(),domains=int(legacy[0][1])))
report=dict(resources=len(rows),domains=sum(r['domains'] for r in rows),originalBytes=sum(r['originalBytes'] for r in rows),candidateBytes=sum(r['candidateBytes'] for r in rows),exactArrays=True,regenerationIdentical=True,files=rows)
(args.output/'verification.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps({k:v for k,v in report.items() if k!='files'}))
