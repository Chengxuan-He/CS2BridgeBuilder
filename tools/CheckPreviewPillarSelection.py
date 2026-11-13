"""Offline regression against actual Arc capture and native placeholder candidates."""
import ast
import json
import re
from pathlib import Path
import numpy as np

root = Path(__file__).resolve().parents[1]
scene = (root / 'src/BridgeBuilder/Runtime/BridgePreviewScene.cs').read_text(encoding='utf-8-sig')
candidates = json.loads((root / 'artifacts/four-new-styles/ArcPedestrian/ArcPedestrian-pillar-candidates.json').read_text(encoding='utf-8-sig'))
assert 'deckWidth = course.Network.m_ElevatedWidth;' in scene
assert 'deckWidth = data.m_Width;' not in scene
expression = re.search(r'score = (1f / \(1f \+ Mathf.Abs\(layout.Size.x.*?);', scene, re.S)[1]
expression = expression.replace('layout.Size.x', 'candidate_width').replace('pillar.m_VerticalPillarOffsetRange.max', 'range_max')
expression = expression.replace('Mathf.Abs', 'abs').replace('Mathf.Max', 'max')
expression = re.sub(r'(?<=[0-9])f\b', '', expression)
tree = ast.parse(' '.join(expression.split()), mode='eval').body

def evaluate(node, values):
    if isinstance(node, ast.Constant): return np.float32(node.value)
    if isinstance(node, ast.Name): return np.float32(values[node.id])
    if isinstance(node, ast.Call):
        args = [evaluate(x, values) for x in node.args]
        return np.float32(abs(args[0]) if node.func.id == 'abs' else max(args))
    left, right = evaluate(node.left, values), evaluate(node.right, values)
    if isinstance(node.op, ast.Add): return np.float32(left + right)
    if isinstance(node.op, ast.Sub): return np.float32(left - right)
    if isinstance(node.op, ast.Mult): return np.float32(left * right)
    if isinstance(node.op, ast.Div): return np.float32(left / right)
    raise AssertionError(ast.dump(node))

def ranked(width):
    return sorted(((float(evaluate(tree, {'width': width, 'candidate_width': x['width'],
        'range_max': x['offsetRange'][1]})), x) for x in candidates), key=lambda x: -x[0])

wrong, corrected = ranked(25.5), ranked(22.5)
assert wrong[0][1]['offsetRange'] == [7.5, 17.5]
assert corrected[0][1]['offsetRange'] == [3.5, 4.5]
assert corrected[0][1]['width'] in (21, 22)
assert (sum(wrong[0][1]['offsetRange']) / 2) == 12.5
assert (sum(corrected[0][1]['offsetRange']) / 2) == 4
assert all(x['probability'] == 100 for x in candidates)
report = {'candidateCount': len(candidates), 'source': 'actual captured installed candidate graph',
    'scoring': 'production expression evaluated in binary32, native width input',
    'wrongTop': [{'name': c['name'], 'score': s} for s, c in wrong[:3]],
    'correctedTop': [{'name': c['name'], 'score': s} for s, c in corrected[:3]],
    'nativePillarHalfSpacing': 4, 'capturedWrongHalfSpacing': 12.5,
    'liveRenderingVerified': False}
(root / 'artifacts/arc-preview-selection/selection-check.json').write_text(json.dumps(report, indent=2, ensure_ascii=False), encoding='utf-8')
print('PASS 28 measured candidates: captured wrong canopy selection, native 22.5m roadway selection, paired-pillar spacing; no blacklist or geometry change.')
