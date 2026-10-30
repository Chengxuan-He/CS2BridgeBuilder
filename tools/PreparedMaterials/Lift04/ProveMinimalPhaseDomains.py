"""Exhaustively fit contiguous authored cells after lifting integer reset phases."""
from pathlib import Path
import json
import numpy as np

ROOT = Path(__file__).resolve().parents[3]
namespace = {}
source_file = Path(__file__).with_name('GroupNearDomains.py')
namespace['__file__'] = str(source_file)
exec(source_file.read_text().split('levels=[]')[0], namespace)
fit, source, original = (namespace[k] for k in ('fit', 'source', 'original'))
results = []
for level in source['levels']:
    faces = {f['face']: f for f in original['lods'][level['lod']]['faces']}
    buckets = {}
    for tile in level['domains']:
        if tile['role'] != 'generated-road':
            continue
        key = (tuple(tile['bones']), tile['positionMin'][0] < -1,
               tuple(tile['tangentDecoded'][0]), int(np.sign(tile['affineXZ'][1][0])))
        buckets.setdefault(key, []).append(tile)
    for key, tiles in buckets.items():
        tiles.sort(key=lambda t: t['positionMin'][2])
        n = len(tiles)
        candidates = {}
        for begin in range(n):
            for end in range(begin + 1, n + 1):
                if end > begin + 1 and tiles[end-2]['positionMax'][2] != tiles[end-1]['positionMin'][2]:
                    break
                candidates[begin, end] = fit(tiles[begin:end], faces)
        cost = [0] + [n + 1] * n
        previous = [None] * (n + 1)
        for end in range(1, n + 1):
            for begin in range(end):
                candidate = candidates.get((begin, end))
                if candidate and candidate['accepted'] and cost[begin] + 1 < cost[end]:
                    cost[end] = cost[begin] + 1
                    previous[end] = begin
        parts = []
        end = n
        while end:
            begin = previous[end]
            assert begin is not None
            parts.append(dict(first=begin, end=end, zmin=tiles[begin]['positionMin'][2],
                              zmax=tiles[end-1]['positionMax'][2], mapping=candidates[begin, end]))
            end = begin
        parts.reverse()
        tail = [n + 1] * n + [0]
        for begin in range(n - 1, -1, -1):
            for end in range(begin + 1, n + 1):
                candidate = candidates.get((begin, end))
                if candidate and candidate['accepted']:
                    tail[begin] = min(tail[begin], 1 + tail[end])
        optimal_edges = [(a, b) for (a, b), v in candidates.items()
                         if v and v['accepted'] and cost[a] + 1 + tail[b] == cost[n]]
        required = []
        for boundary in range(1, n):
            if cost[boundary] + tail[boundary] == cost[n] and not any(
                    a < boundary < b for a, b in optimal_edges):
                required.append(dict(index=boundary, z=tiles[boundary]['positionMin'][2]))
        results.append(dict(lod=level['lod'], key=key, cells=n, minimumDomains=cost[n],
            boundariesRequiredInEveryMinimumFit=required,
            domains=parts, intervalTests=[dict(first=a,end=b,**v) for (a,b),v in candidates.items() if v]))
out = ROOT / 'artifacts/material-remap/Lift04/offline-road-unit/minimum-phase-domains.json'
out.write_text(json.dumps(dict(buckets=results,
    scope='Exact contiguous-cell enumeration within native frame/motion/orientation groups; integer phase offsets removed before fitting. Minimum count does not alone prove uniqueness of each boundary.'), indent=2))
print('Minimum per LOD:', {lod: sum(r['minimumDomains'] for r in results if r['lod']==lod) for lod in (0,1)})
