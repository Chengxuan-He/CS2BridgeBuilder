"""Read-only owner inventory; export history is not proof that an owner still exists."""
import argparse, csv, hashlib, json
from pathlib import Path

STYLES = ['Lift01','Lift02','Lift03','Lift04','Lift05','PedestrianDraw01','PedestrianDraw02']

def inventory(game_data):
    state = game_data / 'ModsData/BridgeBuilder/export-state.tsv'
    rows = list(csv.DictReader(state.read_text(encoding='utf-8-sig').splitlines(), delimiter='\t'))
    result = []
    for style in STYLES:
        matches=[]
        for row in rows:
            fields=row['fingerprint'].split('|')
            if len(fields)<3 or fields[2]!=style: continue
            owner=game_data/'ImportedData'/row['exportName']
            prefabs=sorted(owner.rglob('*.Prefab')) if owner.is_dir() else []
            matches.append(dict(owner=row['exportName'], exportedUtc=row['lastExportUtc'], fingerprint=row['fingerprint'],
                path=str(owner), exists=owner.is_dir(), prefabCount=len(prefabs),
                mechanismPrefabs=[str(p) for p in prefabs if 'Mechanism' in p.name]))
        matches.sort(key=lambda x:x['exportedUtc'], reverse=True)
        present=[r for r in matches if r['exists']]
        result.append(dict(style=style,latestPresent=present[0] if present else None,history=matches,
            status='saved-owner-available-not-yet-audited' if present else 'no-current-saved-owner-evidence'))
    return dict(exportState=str(state),exportStateSha256=hashlib.sha256(state.read_bytes()).hexdigest(),
                scope='Read-only seven-style inventory. No source classification, build attribution, native pose or visual acceptance is inferred from export history.',styles=result)

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--game-data',required=True,type=Path);parser.add_argument('--report',required=True,type=Path)
    args=parser.parse_args(); report=inventory(args.game_data);args.report.parent.mkdir(parents=True,exist_ok=True)
    args.report.write_text(json.dumps(report,indent=2),encoding='utf-8')
    for row in report['styles']: print(row['style'],row['status'],(row['latestPresent'] or {}).get('owner',''))
