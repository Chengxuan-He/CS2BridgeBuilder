exec(open('work/ped02-final-full-domain.py').read().split("print('FULL DOMAIN'")[0])
counts=collections.Counter();changes={}
for rec in audit['meshes']:
    nr=next(x for x in charts['records'] if (x['name'],x['mesh'])==(rec['render'],rec['mesh']));prefix=nr['arrayPrefix'];uv=native[prefix+'_uv'];f=native[prefix+'_faces'];v=numbers(root/'assets/HardcodedNumbers/PedestrianDraw02'/('UvDomain_'+rec['render'].replace(' ','_')+'_'+str(rec['mesh'])+'.bin'));raw,_=decode_arrays(v['I32'],v['F32']);at=2;updated=[]
    for di in range(raw[1]):
        n=abs(int(raw[at+3]));at+=4;members=raw[at:at+n];at+=n
        if di in rec['classifications'].get('density-existing-road-repeat-overlap',[]) and not any(overlaps(t,region) for t in uv[f[members]].astype(float)):updated.append(di)
    if updated:changes[(rec['render'],rec['mesh'])]=updated
print('ALL source-domain corrections',sum(len(x) for x in changes.values()),[(k,len(v)) for k,v in changes.items()])
