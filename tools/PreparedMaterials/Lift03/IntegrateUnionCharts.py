"""Stage Lift03's source-verified union charts; --apply updates only its chart BBN."""
import argparse, hashlib, json, struct, sys, zlib
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/PreparedMaterials/Lift03'))
from ExportRecipes import numbers
BASE=ROOT/'artifacts/prepared-materials/Lift03'
OUT=BASE/'exact-union-chart-candidate'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--apply',action='store_true')
    args=parser.parse_args()
    evidence=json.loads((OUT/'source-evidence.json').read_text())
    for relative,digest in evidence['sourceHashes'].items(): assert sha(ROOT/relative)==digest,relative
    baseline=json.loads((BASE/'native-charts/runtime-integration.json').read_text())
    resource=ROOT/baseline['resource'];original=resource.read_bytes()
    assert sha(resource)==baseline['resourceSha256'],'Regenerate original baseline before integration'
    data=numbers(resource);ints=list(data['I32']);coords=list(data['F64'])
    assert ints[0]==1
    streams={(s['render'],s['mesh']):i for i,s in enumerate(baseline['streams'])}
    native={};chart_cache={};set_cache={};modified={}
    for row in json.loads((OUT/'chart-overrides.json').read_text()):
        key=(row['render'],row['mesh']);si=streams[key];domain=row['originalDomain']
        table=ints[2+si];index=table+1+domain
        assert row['style']=='Lift03' and 0<=domain<ints[table] and ints[ints[index]]==0
        assert index not in modified
        if key not in native:
            native[key]=json.loads((BASE/'native-charts'/(row['render'].replace(' ','_')+'_'+str(row['mesh'])+'.json')).read_text())
        chart_ids=native[key]['domains'][domain]['charts']
        verified={a['chart'] for a in evidence['audit'] if (a['render'],a['mesh'])==key and a['status']=='verified'}
        assert chart_ids and all(c in verified for c in chart_ids)
        pointers=[]
        for chart in row['charts']:
            identity=json.dumps(chart,separators=(',',':'))
            if identity not in chart_cache:
                chart_cache[identity]=len(ints);ints.append(len(chart))
                for ring in chart:
                    ints.extend((len(coords),len(ring)));coords.extend(v for p in ring for v in p)
            pointers.append(chart_cache[identity])
        identity=tuple(pointers)
        if identity not in set_cache:
            set_cache[identity]=len(ints);ints.extend((len(pointers),*pointers))
        modified[index]=set_cache[identity];ints[index]=set_cache[identity]
    assert all(v==ints[i] for i,v in enumerate(data['I32']) if i not in modified)
    assert coords[:len(data['F64'])]==list(data['F64'])
    body=b'BBN1'+struct.pack('<7i',1,len(ints),0,0,0,0,len(coords))
    body+=struct.pack('<'+'i'*len(ints),*ints)+struct.pack('<'+'d'*len(coords),*coords)
    raw=body+hashlib.sha256(body).digest()
    target=OUT/resource.name;target.write_bytes(raw)
    reread=numbers(target);assert list(reread['I32'])==ints and list(reread['F64'])==coords
    if args.apply:resource.write_bytes(raw)
    report={'applied':args.apply,'baselineSha256':hashlib.sha256(original).hexdigest(),'candidateSha256':sha(target),'baselineBytes':len(original),'candidateBytes':len(raw),'rawDelta':len(raw)-len(original),'deflate9BaselineBytes':len(zlib.compress(original,9)),'deflate9CandidateBytes':len(zlib.compress(raw,9)),'deflate9Delta':len(zlib.compress(raw,9))-len(zlib.compress(original,9)),'addedUniqueCharts':len(chart_cache),'addedChartSets':len(set_cache),'recoveredDomains':len(modified),'existingChartDataAndOtherDomainsPreserved':True,'typedArrayRoundTrip':True,'textureOrGeometryOrBoneChanges':False,'overrideSha256':sha(OUT/'chart-overrides.json'),'sourceEvidenceSha256':sha(OUT/'source-evidence.json'),'packageLimit':'Deflate estimate only; complete official SDK ZIP remains required.'}
    (OUT/'integration.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report))
if __name__=='__main__':main()
