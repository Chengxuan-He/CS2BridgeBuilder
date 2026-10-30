"""Retire only the proven unreachable legacy Lift05 road alias and unshared textures."""
import hashlib,json,shutil,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];STYLE=ROOT/'assets/Materials/Lift05';OUT=ROOT/'artifacts/material-remap/Lift05/retirement-audit';RETIRED=ROOT/'artifacts/retired/material-remap/Lift05/legacy-road-alias'
def hashbytes(cid):return bytes(int(cid[i:i+2][::-1],16) for i in range(0,len(cid),2))
def main():
 plan=next(x for x in json.loads((ROOT/'artifacts/material-remap/shared/outputs/legacy-alias-cleanup-plan.json').read_text()) if x['style']=='Lift05');candidate=plan['candidateLegacyRoadAliases'];assert len(candidate)==1;entry=candidate[0];descriptor=ROOT/entry['descriptor'];raw=descriptor.read_bytes();surfaces=list(STYLE.rglob('*.Surface'));reverse={};retire=[descriptor,descriptor.with_suffix('.Surface.cid')]
 for dependency in entry['dependencies']:
  cid=dependency['cid'];needle=hashbytes(cid);assert needle in raw,'Native hash encoding mismatch';users=[p for p in surfaces if needle in p.read_bytes()];reverse[cid]=[str(p.relative_to(ROOT)) for p in users]
  if users==[descriptor]:
   texture=ROOT/dependency['path'];retire += [texture,texture.with_suffix('.Texture.cid')]
 # A descriptor's native identity must not be referenced by any remaining style descriptor.
 surfaceNeedle=hashbytes(entry['surfaceCid']);assert all(surfaceNeedle not in p.read_bytes() for p in surfaces if p!=descriptor)
 # The only text alias is its retiring style registration. Other explicit keys remain.
 registration=ROOT/'src/BridgeBuilder/Bridges/Types/Lift05/Lift05RepeatUnits.cs'
 assert entry['key'] in registration.read_text()
 references=[p for p in (ROOT/'src').rglob('*.cs') if entry['key'] in p.read_text(encoding='utf-8-sig')]
 assert references==[registration],references
 retire.append(registration)
 files=[]
 for p in retire:
  relative=p.relative_to(ROOT);destination=RETIRED/relative;assert p.resolve().is_relative_to(ROOT.resolve()) and destination.resolve().is_relative_to(RETIRED.resolve());assert not destination.exists();files.append(dict(path=str(relative),bytes=p.stat().st_size,sha256=hashlib.sha256(p.read_bytes()).hexdigest()))
 report=dict(key=entry['key'],surfaceCid=entry['surfaceCid'],reverseReferences=reverse,files=files,materialBytes=sum(f['bytes'] for f in files if f['path'].startswith('assets')),registrationRemoved=str(registration.relative_to(ROOT)),retainedSharedControl='729893d593df5d52ba911bc1e7eab77b',routeEvidence=['Lift05BridgeGenerator always calls Lift05RoadPreparation.PrepareRenderLevels.', 'PrepareRenderLevels always supplies appearanceForLod Lift05NativeRoadAppearance.ForLod.', 'MovableRoadPreparation replaces both patch and region inputs for every LOD; legacy fallback patches cannot reach Compose.', 'Supported patches use PreparedUnit or original SourceUv and directly bind their Surface; unsupported leaves retain selected appearance.', 'StructuralRepeat PreparedDomains use the separately explicit structural key; other Retile Region keys are retained.'],scope='Only this old road alias is retired. Other Region keys require retained-face/fragment evidence and are not retired by this audit.')
 OUT.mkdir(parents=True,exist_ok=True);(OUT/'legacy-road-alias.json').write_text(json.dumps(report,indent=2)+'\n')
 if '--apply' in sys.argv:
  for p in retire:
   dest=RETIRED/p.relative_to(ROOT);dest.parent.mkdir(parents=True,exist_ok=True);shutil.move(str(p),str(dest));assert hashlib.sha256(dest.read_bytes()).hexdigest()==next(f['sha256'] for f in files if f['path']==str(p.relative_to(ROOT)))
  descriptor.parent.rmdir()
 print('Verified',len(files),'files; material bytes',report['materialBytes'],'applied', '--apply' in sys.argv)
if __name__=='__main__':main()
