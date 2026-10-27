"""Build measured native chart loops and the PedestrianDraw01 runtime adapter."""
import hashlib
import json
from pathlib import Path
import struct
import subprocess
import sys
from collections import defaultdict

from ExportRecipes import numbers
from RecoverNativeBoundaries import recover


def cross(a, b, c):
    return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])


def intersects(a, b, c, d):
    if max(a[0], b[0]) < min(c[0], d[0]) or max(c[0], d[0]) < min(a[0], b[0]):
        return False
    if max(a[1], b[1]) < min(c[1], d[1]) or max(c[1], d[1]) < min(a[1], b[1]):
        return False
    return cross(a, b, c)*cross(a, b, d) <= 0 and cross(c, d, a)*cross(c, d, b) <= 0


def inside(p, loop):
    result = False
    for a, b in zip(loop, loop[1:]+loop[:1]):
        if (a[1] > p[1]) != (b[1] > p[1]):
            if p[0] < (b[0]-a[0])*(p[1]-a[1])/(b[1]-a[1])+a[0]:
                result = not result
    return result


def loops_for(chart):
    edges = [tuple(map(tuple, edge)) for edge in chart['boundary']]
    if not edges or len(set(edges)) != len(edges):
        return None
    graph = defaultdict(list)
    for a, b in edges:
        if a == b:
            return None
        graph[a].append(b)
        graph[b].append(a)
    if any(len(neighbors) != 2 for neighbors in graph.values()):
        return None
    unused = set(graph)
    loops = []
    while unused:
        start = min(unused)
        loop, previous, current = [], None, start
        while True:
            if current not in unused:
                if current != start:
                    return None
                break
            unused.remove(current)
            loop.append(current)
            following = min(graph[current]) if previous is None else next(x for x in graph[current] if x != previous)
            previous, current = current, following
        if len(loop) < 3:
            return None
        loops.append(loop)
    segments = [(li, i, a, b) for li, loop in enumerate(loops)
                for i, (a, b) in enumerate(zip(loop, loop[1:]+loop[:1]))]
    for at, (li, i, a, b) in enumerate(segments):
        for lj, j, c, d in segments[at+1:]:
            if li == lj and ((i-j) % len(loops[li]) in (1, len(loops[li])-1)):
                continue
            if intersects(a, b, c, d):
                return None
    area = 0.0
    for i, loop in enumerate(loops):
        signed = sum(a[0]*b[1]-b[0]*a[1] for a, b in zip(loop, loop[1:]+loop[:1]))*.5
        if signed == 0:
            return None
        depth = sum(inside(loop[0], other) for j, other in enumerate(loops) if i != j)
        area += abs(signed) * (-1 if depth % 2 else 1)
    if area <= 0 or abs(area-chart['summedTriangleArea']) > max(area*1e-10, 1e-15):
        return None
    return loops


def main():
    root = Path(sys.argv[1])
    evidence = root/'artifacts/prepared-materials/PedestrianDraw01/shared-sampling'
    source = json.loads((evidence/'native-charts.json').read_text())
    charts = {c['key']: c for c in source['charts']}
    referenced = {key for m in source['meshes'] for d in m['domainCharts'] for key in d['chartKeys']}
    loops = {key: loops_for(charts[key]) for key in sorted(referenced)}
    previous_loops = dict(loops)
    recovered_charts, _ = recover(root, source, loops, loops_for)
    loops.update({key: row['loops'] for key, row in recovered_charts.items()})
    assert all(loops[key] == value for key, value in previous_loops.items() if value)
    assert all(row['retainedArea'] > 0 and row['retainedArea'] == row['originalArea'] for row in recovered_charts.values())
    # A domain containing unreliable boundary data cannot silently omit that area.
    domains = [[d['chartKeys'] if all(loops[k] for k in d['chartKeys']) else []
                for d in m['domainCharts']] for m in source['meshes']]
    used = sorted({key for mesh in domains for domain in mesh for key in domain})
    indices = {key: i for i, key in enumerate(used)}
    ints = [len(domains), len(used)] + [0]*len(used)
    doubles = []
    for mesh in domains:
        ints.append(len(mesh))
        for domain in mesh:
            ints.append(len(domain))
            ints.extend(indices[key] for key in domain)
    for index, key in enumerate(used):
        ints[2+index] = len(ints)
        ints.append(len(loops[key]))
        for loop in loops[key]:
            ints.extend((len(doubles), len(loop)))
            doubles.extend(value for point in loop for value in point)
    raw = b'BBN1'+struct.pack('<7i', 1, len(ints), 0, 0, 0, 0, len(doubles))
    raw += struct.pack('<'+'i'*len(ints), *ints)+struct.pack('<'+'d'*len(doubles), *doubles)
    target = root/'assets/HardcodedNumbers/PedestrianDraw01/UvCharts_PedestrianDraw01.bin'
    target.write_bytes(raw+hashlib.sha256(raw).digest())
    restored = numbers(target)
    assert list(restored['I32']) == ints and list(restored['F64']) == doubles
    cursor = 2+len(used)
    for mesh, expected in zip(source['meshes'], domains):
        assert ints[cursor] == len(mesh['domainCharts'])
        cursor += 1
        for original, keys in zip(mesh['domainCharts'], expected):
            count = ints[cursor]
            cursor += 1
            recovered = [used[i] for i in ints[cursor:cursor+count]]
            cursor += count
            assert recovered == keys
            assert not recovered or recovered == original['chartKeys']
    assert cursor == ints[2] if used else cursor == len(ints)
    keys = [m['render']+'|'+str(m['mesh']) for m in source['meshes']]
    adapter = root/'src/BridgeBuilder/Bridges/Types/PedestrianDraw01/PedestrianDraw01UvCharts.cs'
    key_lines = '\n'.join('            '+json.dumps(key)+',' for key in keys)
    adapter_text = '''using System;
using System.Collections.Generic;

namespace BridgeBuilder.Bridges;

// Generated by BuildUvCharts.py from native mesh/UV adjacency and measured domains.
// Embedded numerical arrays: UvCharts_PedestrianDraw01
// No sampling-key fallback: each lookup is restricted to this source mesh's domain.
internal static class PedestrianDraw01UvCharts
{
    private static readonly Lazy<Dictionary<string, MovableUvChart[][]>> Loaded = new(Load);

    internal static MovableUvChart[] For(string renderName, int meshIndex, int domain)
    {
        if (!Loaded.Value.TryGetValue(renderName + "|" + meshIndex, out var domains)
            || domain < 0 || domain >= domains.Length)
            return Array.Empty<MovableUvChart>();
        return domains[domain];
    }

    private static Dictionary<string, MovableUvChart[][]> Load()
    {
        var data = HardcodedNumbers.Load("UvCharts_PedestrianDraw01");
        var keys = new[]
        {
'''+key_lines+'''
        };
        int meshCount = data.I32[0], chartCount = data.I32[1];
        if (meshCount != keys.Length)
            throw new InvalidOperationException("Invalid PedestrianDraw01 UV chart mesh count.");
        var charts = new MovableUvChart[chartCount];
        for (int i = 0; i < charts.Length; i++)
            charts[i] = MovableUvChart.Read(data.I32, data.F64, data.I32[2 + i]);
        int at = 2 + chartCount;
        var result = new Dictionary<string, MovableUvChart[][]>(StringComparer.Ordinal);
        for (int mesh = 0; mesh < meshCount; mesh++)
        {
            var domains = new MovableUvChart[data.I32[at++]][];
            for (int domain = 0; domain < domains.Length; domain++)
            {
                int count = data.I32[at++];
                var members = count == 0 ? Array.Empty<MovableUvChart>() : new MovableUvChart[count];
                for (int i = 0; i < count; i++) members[i] = charts[data.I32[at++]];
                domains[domain] = members;
            }
            result.Add(keys[mesh], domains);
        }
        return result;
    }
}
'''
    if '--data-only' not in sys.argv:
        adapter.write_text(adapter_text, encoding='utf-8')
    # Extend only empty domain entries using source-valid physical unions.
    # This helper rejects new intersection vertices and verifies each boundary
    # edge against original native triangle edges using exact rational arithmetic.
    # It preserves every existing chart, including the recovered simple loops.
    subprocess.run([sys.executable, str(Path(__file__).with_name('StageExactTriangleCharts.py')),
                    '--repository', str(root), '--native-union'], check=True)
    union_output = root/'artifacts/prepared-materials/PedestrianDraw01/exact-source-union'
    union_report = json.loads((union_output/'stage.json').read_text())
    candidate = (union_output/'UvCharts_PedestrianDraw01.bin').read_bytes()
    assert hashlib.sha256(target.read_bytes()).hexdigest() == union_report['baselineBBNSHA256']
    assert hashlib.sha256(candidate).hexdigest() == union_report['candidateBBNSHA256']
    target.write_bytes(candidate)
    final = numbers(target)
    summary = dict(sourceSHA256=hashlib.sha256((evidence/'native-charts.json').read_bytes()).hexdigest(),
        meshes=len(domains), sourceReferencedCharts=len(referenced), reliableUsedCharts=int(final['I32'][1]),
        totalDomains=sum(len(m) for m in domains), usableDomains=sum(bool(d) for m in domains for d in m)+union_report['candidateDomains'],
        fallbackDomains=union_report['remainingEmptyDomains'], binaryBytes=target.stat().st_size,
        binarySHA256=hashlib.sha256(target.read_bytes()).hexdigest(), repeatInferred=False,
        recoveredCharts=len(recovered_charts), previousValidChartsPreserved=True,
        recoveredPhysicalOccurrences=sum(row['verifiedPhysicalOccurrences'] for row in recovered_charts.values()),
        degenerateOnlyChartsRemainUnusable=True,
        sourceUnionCharts=union_report['newUniqueCharts'], sourceUnionDomains=union_report['candidateDomains'],
        sourceUnionBoundaryEdges=union_report['exactRationalBoundaryEdgesVerified'])
    (evidence/'runtime-chart-build.json').write_text(json.dumps(summary, indent=2)+'\n')
    print(json.dumps(summary))


if __name__ == '__main__':
    main()
