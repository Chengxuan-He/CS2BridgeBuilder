"""Independent expected blocks from original stored game MidMips archive entries."""
import importlib.util, json, sys, zipfile
from collections import defaultdict
from pathlib import Path

root = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('producer', root/'tools/PrepareMidMips/PrepareMidMips.py')
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
game, output = map(Path, sys.argv[1:3]); output.mkdir(parents=True, exist_ok=True)
s = m.Sources.__new__(m.Sources); s.mid = defaultdict(list); s.tile_cache = {}
archives = sorted((game/'Content').glob('*/MidMips*.cok'))
for path in archives:
    with zipfile.ZipFile(path) as z:
        for name in z.namelist():
            if name.endswith('.MidMips'): s.read_mid(z, name)
# Include high group ordinals and actual native duplicate identities, plus every
# native source used by the independently prepared affected lane inventory.
prepared = json.loads((root/'artifacts/vt-mid-mip-fix/PedestrianDraw02/outputs/prepared-midmips.json').read_text())
selected = {r['sourceCid'] for r in prepared['styles'][0]['records']}
selected.update(cid for (cid, stack), rs in s.mid.items() if any(r['index'] == 63 for r in rs))
high = sorted(selected - {r['sourceCid'] for r in prepared['styles'][0]['records']})
selected -= set(high[6:])
by_entry = defaultdict(list)
for (cid, stack), rs in s.mid.items():
    for r in rs: by_entry[id(r)].append(cid)
duplicates = [ids for ids in by_entry.values() if len(ids) > 1]
for ids in duplicates[:3]: selected.update(ids[:2])
manifest = []
for source in sorted(selected):
    target = m.digest(('original-runtime-alias:'+source).encode())[:32]
    records = []
    for (cid, stack), rs in s.mid.items():
        if cid != source: continue
        r = rs[0]
        blocks = {(level, layer, half): s.blocks(r, level, layer, half)
                  for level in range(3) for layer in range(4) if r['mask'] & (1 << layer) for half in range(2)}
        records.append(dict(cid=target, stack=stack, width=r['width'], height=r['height'], bias=r['bias'], mask=r['mask'], blocks=blocks, source=r))
    data, checks = m.build(records)
    expected = output/(target+'.expected'); expected.write_bytes(data)
    r = records[0]['source']
    with zipfile.ZipFile(r['archive']) as z:
        info = z.getinfo(r['entry'])
        with open(r['archive'], 'rb') as f:
            f.seek(info.header_offset+26); name, extra = m.read(f, 'HH')
        offset = info.header_offset+30+name+extra
    manifest.append(dict(sourceCid=source, targetCid=target, archive=r['archive'], offset=offset, length=info.file_size,
                         expected=str(expected.resolve()), ordinals=[record['source']['index'] for record in records]))
(output/'manifest.json').write_text(json.dumps(manifest, indent=2))
print('PASS generated', len(manifest), 'original source expectations; native duplicate groups', len(duplicates))
