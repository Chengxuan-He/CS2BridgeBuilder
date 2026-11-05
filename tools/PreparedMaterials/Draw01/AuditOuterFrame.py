"""Reproduce the bounded Draw01 outer-frame source audit without modifying production data."""
import hashlib
import json
from pathlib import Path
import numpy as np
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/structure46'
def main():
    recipes=json.loads((OUT/'outer-frame-recipe.json').read_text())
    old=json.loads((OUT/'membership-widen.json').read_text())
    report=[]
    for recipe in recipes:
        path=ROOT/recipe['source']
        assert hashlib.sha256(path.read_bytes()).hexdigest()==recipe['sourceSha256']
        source=np.load(path);faces=source['mesh0_faces'];vertices=source['mesh0_positions']
        chosen=set(recipe['faces']);ids=set(recipe['left']+recipe['right'])
        assert ids==set(faces[sorted(chosen)].ravel())
        assert all(vertices[i,0]<0 for i in recipe['left'])
        assert all(vertices[i,0]>0 for i in recipe['right'])
        row=old['rows'][recipe['lod']]
        assert chosen<=set(row['removed']) and not chosen.intersection(row['restored'])
        assert not ids.intersection(i for g in row['widen'] for i in g['vertices'])
        keys=[tuple(sorted(tuple(float(a) for a in p) for p in vertices[faces[i]])) for i in chosen]
        assert len(keys)==len(set(keys))
        retained=set(range(len(faces)))-set(row['removed'])|set(row['restored'])
        assert not any(ids.intersection(faces[i]) for i in retained)
        report.append(dict(lod=recipe['lod'],restoredFaces=len(chosen),rigidVertices=len(ids),sourceSha256=recipe['sourceSha256']))
    (OUT/'source-audit.json').write_text(json.dumps(dict(status='PASS_SOURCE_NOT_NATIVE_OUTPUT',rows=report),indent=2)+'\n')
    print(report)
if __name__=='__main__':main()
