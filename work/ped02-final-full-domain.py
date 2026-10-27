exec(open('work/ped02-final-remaining.py').read().split("print('flagReasons'")[0])
from ExportRecipes import numbers
from MovableUvDomainFormat import decode_arrays
allrows={}; precise=collections.Counter(); spans=collections.Counter(); ids=collections.defaultdict(list)
for r in remaining:
    key=(r['render'],r['mesh'])
    if key not in allrows:
        v=numbers(root/'assets/HardcodedNumbers/PedestrianDraw02'/('UvDomain_'+key[0].replace(' ','_')+'_'+str(key[1])+'.bin')); raw,_=decode_arrays(v['I32'],v['F32']);at=2;domains=[]
        for i in range(raw[1]):
            count=abs(int(raw[at+3]));at+=4;domains.append(raw[at:at+count]);at+=count
        allrows[key]=domains
    nr=next(x for x in charts['records'] if (x['name'],x['mesh'])==key);prefix=nr['arrayPrefix'];uv=native[prefix+'_uv'];f=native[prefix+'_faces'];tri=uv[f[allrows[key][r['originalDomain']]]].astype(float); hit=any(overlaps(t,region) for t in tri); reason='full-domain-repeat-overlap' if hit else 'full-domain-no-repeat-overlap';precise[reason]+=1
    did=(r['render'],r['originalDomain'])
    if did not in ids[reason]:ids[reason].append(did)
    if hit:
        fully=all(((t>=region[:2])&(t<=region[2:])).all() for t in tri);spans['contained-repeat' if fully else 'cross-repeat-boundary']+=1
print('FULL DOMAIN',precise,spans);print('IDS',dict(ids))
