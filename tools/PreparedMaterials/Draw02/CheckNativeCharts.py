"""Validate compact chart schema and real generated receiver containment."""
import collections
import hashlib
import json
from pathlib import Path
import struct
import sys
import numpy as np


def main(repo,python_lib):
    sys.path.insert(0,str(python_lib))
    from shapely.geometry import Polygon
    base=repo/'artifacts/prepared-materials/Draw02/shared-source-migration'
    data=json.loads((base/'native-charts.json').read_text())
    raw=(repo/'assets/HardcodedNumbers/Draw02/Draw02_NativeUvCharts.bin').read_bytes()
    assert raw[:4]==b'BBN1' and hashlib.sha256(raw[:-32]).digest()==raw[-32:]
    version,ni,nu,nl,nul,nf,nd=struct.unpack_from('<7i',raw,4)
    assert version==1 and nu==nl==nul==nf==0
    ints=struct.unpack_from('<'+'i'*ni,raw,32);values=struct.unpack_from('<'+'d'*nd,raw,32+ni*4)
    def read_chart(offset):
        loops=[]
        for n in range(ints[offset]):
            start,count=ints[offset+1+n*2:offset+3+n*2]
            loops.append(tuple(tuple(values[start+2*i:start+2*i+2]) for i in range(count)))
        return tuple(loops)
    checked=0
    for mi,mesh in enumerate(data['meshes']):
        table=ints[mi+1];assert ints[table]==len(mesh['domains'])
        for di,charts in enumerate(mesh['domains']):
            loc=ints[table+1+di];assert ints[loc]==len(charts)
            for n,chart in enumerate(charts):
                decoded=read_chart(ints[loc+1+n]);expected=tuple(tuple(tuple(p) for p in loop) for loop in data['charts'][chart])
                assert decoded==expected
                checked+=1
    shapes=[]
    for loops in data['charts']:
        shape=Polygon()
        for loop in loops:shape=shape.symmetric_difference(Polygon(loop))
        assert shape.is_valid
        shapes.append(shape)
    from ExportNativeCharts import domains
    source_checked=0;source_failed=0;source_degenerate=0
    for mesh in data['meshes']:
        native_path=repo/'artifacts/uv-density/native'/(mesh['render']+'.npz')
        recipe_path=repo/'assets/HardcodedNumbers/Draw02'/('UvDomain_'+mesh['render'].replace(' ','_')+'_'+str(mesh['mesh'])+'.bin')
        with np.load(native_path) as native:
            uv=np.asarray(native[f'mesh{mesh["mesh"]}_uv0'][:,:2],dtype=float)
            faces=native[f'mesh{mesh["mesh"]}_faces']
            for di,members in enumerate(domains(recipe_path)):
                for face in members:
                    poly=Polygon(uv[faces[face]])
                    if not poly.is_valid or poly.area==0:source_degenerate+=1;continue
                    source_checked+=1
                    if not any(shapes[i].covers(poly) for i in mesh['domains'][di]):source_failed+=1
    assert source_failed==0,source_failed
    lookup={(m['render'],m['mesh']):m['domains'] for m in data['meshes']}
    rows=json.loads((base/'chart-plans.json.seams.json').read_text());stats=collections.defaultdict(collections.Counter);examples=[]
    for row in rows:
        q=np.asarray(row['q'],dtype=np.float32).astype(float)
        if row['triangle']:
            a,b,c=[np.asarray(row[k],dtype=np.float32).astype(float) for k in ('a','b','c')]
            mapped=a+q[:,0,None]*(b-a)+q[:,1,None]*(c-a)
        else:
            bounds=np.asarray(row['bounds'],dtype=np.float32).astype(float);mapped=bounds[:2]+q*(bounds[2:]-bounds[:2])
        region=Polygon(mapped.astype(np.float32).astype(float));exact_region=Polygon(mapped)
        chart_ids=lookup[(row['render'],row['mesh'])][row['domain']]
        matched=[i for i in chart_ids if shapes[i].covers(region)]
        exact=any(shapes[i].covers(exact_region) for i in chart_ids)
        counts=stats[(row['render'],row['mesh'],row['delta'])];counts['input']+=1;counts['covered']+=bool(matched);counts['doubleCovered']+=exact
        if matched and len(examples)<4:
            examples.append(dict(input=row,chart=matched[0],originalAtlasUv=mapped.astype(np.float32).tolist()))
    result=dict(schema='BridgeBuilder/Draw02NativeChartCoverage/1',binarySha256=hashlib.sha256(raw).hexdigest(),binaryBytes=len(raw),
        originalFacesChecked=source_checked,originalFacesFailed=source_failed,degenerateOriginalFaces=source_degenerate,bbnExactDomainChartReferences=checked,meshes=len(data['meshes']),domains=sum(len(m['domains']) for m in data['meshes']),
        uniqueCharts=len(data['charts']),receiverTriangles=len(rows),covered=sum(v['covered'] for v in stats.values()),
        doubleCovered=sum(v['doubleCovered'] for v in stats.values()),newTexturePayloads=0,
        rule='One corresponding real chart must cover the complete Float32 atlas triangle, including edges/interior; no bbox/vertex-only/epsilon expansion.',
        rows=[dict(render=k[0],mesh=k[1],delta=k[2],**v) for k,v in stats.items()],examples=examples,
        remaining='Public chart dispatch/native material execution, boundary/mip/normal runtime acceptance, unsupported receivers outside every real chart; old assets retained.')
    (base/'native-chart-coverage.json').write_text(json.dumps(result,indent=2)+'\n')
    print(json.dumps({k:v for k,v in result.items() if k not in ('rows','examples','remaining','rule')}))


if __name__=='__main__':main(Path(sys.argv[1]).resolve(),Path(sys.argv[2]).resolve())
