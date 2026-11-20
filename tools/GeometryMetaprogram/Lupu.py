"""Measure Lupu's installed static graph into existing immutable recorded geometry recipes."""
from pathlib import Path
import argparse, hashlib, json, struct, tempfile, zipfile
import numpy as np
from scipy.sparse import coo_matrix
from scipy.sparse.csgraph import connected_components
from UvDensityAudit import NativeGeometry
ENTRIES=['lupudaqiao_fc23faf7dc599cfbe5a086628174ce05.Geometry','lupudaqiaoLOD1_3e29f24695f0889023c88a467bf641b8.Geometry']
def write(path,a,integer=False):
 a=np.asarray(a,dtype='<i4' if integer else '<f4').ravel();h=[0]*6;h[0 if integer else 4]=len(a);b=struct.pack('<6i',*h)+a.tobytes();path.write_bytes(b+hashlib.sha256(b).digest())
def horizontal_section(triangles, height, axis=2):
 points=[]
 for triangle in triangles:
  for i,j in ((0,1),(1,2),(2,0)):
   a,b=triangle[i],triangle[j]
   if (a[axis]-height)*(b[axis]-height)<=0 and a[axis]!=b[axis]:points.append(a+(b-a)*((height-a[axis])/(b[axis]-a[axis])))
 return np.asarray(points)

def main():
 ap=argparse.ArgumentParser();ap.add_argument('archive');ap.add_argument('game');ap.add_argument('output');ap.add_argument('evidence');a=ap.parse_args();out=Path(a.output);out.mkdir(parents=True,exist_ok=True);ev=Path(a.evidence);ev.mkdir(parents=True,exist_ok=True)
 reader=NativeGeometry(Path(a.game)/'Plugins/x86_64/PipelinePlugin.dll')
 # One measured source frame: lower inward slab bottom and main arch outer edges.
 roadlo=np.float32(-11.072543144226074);roadhi=np.float32(11.152859687805176);cy=np.float32((roadlo+roadhi)/2);datum=np.float32(37.2265625)
 spanlo=roadlo;spanhi=roadhi;spancy=cy;towerdatum=datum
 # Terminal tower centres and arch-foot centres measured from full source footprints.
 anchors=np.array([0,-270.48046875,270.50390625,-206.033203125,206.060546875],dtype='<f4');cx=np.float32((anchors[1]+anchors[2])/2);length=np.float32(anchors[2]-anchors[1])
 constants=[roadhi-roadlo,np.float32(21.580078125)-np.float32(-21.4541015625),length,datum,cy,cx,np.float32(77.560546875)-datum]+anchors.tolist()+[spanhi-spanlo,spancy,towerdatum,np.float32(34.6201171875)]
 write(out/'Types_Lupu_Parameters.bin',constants)
 records=[];keys=[];braceProfiles=[]
 with zipfile.ZipFile(a.archive) as z,tempfile.TemporaryDirectory(dir=ev) as td:
  for lod,entry in enumerate(ENTRIES):
   raw=z.read(entry);p=Path(td)/entry;p.write_bytes(raw);meshes=reader.read(p);rec={'lod':lod,'entry':entry,'sha256':hashlib.sha256(raw).hexdigest(),'slots':[]}
   for slot,m in enumerate(meshes):
    np.savez(ev/(Path(entry).stem+'_'+str(slot)+'.npz'),**m)
    v=m['positions'];faces=m['faces'];q=v[faces];_,inv=np.unique(v,axis=0,return_inverse=True);g=inv[faces];e=np.concatenate((g[:,[0,1]],g[:,[1,2]],g[:,[2,0]]));_,labels=connected_components(coo_matrix((np.ones(len(e)),(e[:,0],e[:,1])),shape=(inv.max()+1,)*2),directed=False);vl=labels[inv];fl=vl[faces[:,0]]
    ownership=np.zeros(len(faces),dtype='i4');coeff=np.zeros((len(faces),3),dtype='f4');columnshift=np.zeros(len(v),dtype='f4');columnheight=np.zeros(len(v),dtype='f4');supportgroup=np.full(len(v),-1,dtype='i4');columnmoves=[];supportweight=np.zeros(len(v),dtype='f4');balcony=np.zeros(len(v),dtype=bool);hangerShift=np.zeros(len(v),dtype="f4");hangerRise=np.zeros(len(v),dtype="f4");hangers=[];braces=[];braceFaces=set()
    for c in np.unique(fl):
     fi=np.flatnonzero(fl==c);vv=v[np.unique(faces[fi])];lo=vv.min(0);hi=vv.max(0)
     # Independent source columns keep their lower vertices fixed. Only measured
     # cap vertices move vertically; each original face supplies its affine gradient.
     if slot==0 and 160<abs((lo[0]+hi[0])/2)<250 and hi[0]-lo[0]<4 and 34<hi[2]<35 and lo[2]>2:
      vertices=np.unique(faces[fi]);side=1 if lo[1]>cy else -1
      supportgroup[vertices]=len(columnmoves)
      supportweight[vertices]=(v[vertices,2]>=np.float32(34.6201171875)).astype('f4')
      cap=vv[vv[:,2]>=np.float32(34.6201171875)]
      points=sorted({(float(y-cy),float(z-datum)) for _,y,z in cap})
      # Upper convex chain of the authored planar/sloping cap (no mesh cutting).
      upper=[]
      for point in points:
       if upper and point[0]==upper[-1][0]:
        if point[1]<=upper[-1][1]:continue
        upper.pop()
       while len(upper)>1 and ((upper[-1][0]-upper[-2][0])*(point[1]-upper[-1][1])-(upper[-1][1]-upper[-2][1])*(point[0]-upper[-1][0]))>=-1e-10:upper.pop()
       upper.append(point)
      columnmoves.append(dict(contactRange=[float(cap[:,1].min()-cy),float(cap[:,1].max()-cy),side,0.],component=int(c),faces=fi.tolist(),shift=0.,top=float(hi[2]),cap=upper))
     # Integral terminal bearing caps are welded into the main arch shell, not
     # independent column islands. Preserve every non-cap vertex and move the
     # measured cap vertices through their original incident triangles.
     if slot==0 and lo[0]<-260 and hi[0]>260 and hi[2]>70:
      for end in (-1,1):
       capVertices=np.flatnonzero((vl==c)&(v[:,0]*end>260)&(v[:,2]==np.float32(34.6201171875)))
       if not len(capVertices):continue
       capFaces=fi[np.any(np.isin(faces[fi],capVertices),axis=1)]
       affected=np.unique(faces[capFaces]);supportgroup[affected]=len(columnmoves)
       supportweight[capVertices]=1
       cap=v[capVertices];side=1 if lo[1]>cy else -1
       xmin=float(cap[:,1].min()-cy);xmax=float(cap[:,1].max()-cy);y=float(np.float32(34.6201171875)-datum)
       columnmoves.append(dict(role='integral terminal bearing',contactRange=[xmin,xmax,side,0.],component=int(c),end=end,faces=capFaces.tolist(),shift=0.,top=float(np.float32(34.6201171875)),cap=[[xmin,y],[xmax,y]]))
     # The inward lower slab, rails and upper slab form one protruding assembly.
     if slot==0 and (lo[0]>260 or hi[0]<-260) and lo[2]>=37.2265625 and hi[2]<=41.0234375 and np.min(abs(vv[:,1]))<8:
      balcony[np.unique(faces[fi])]=True
     # Measured hanging rods: retain the upper arch attachment, translate the
     # complete lower shoe, and shear the original rod faces between those anchors.
     # Role is defined by its measured anchor domain, never a triangle-count gate:
     # two authored shoes have additional triangulation but the same structural role.
     if slot==0 and abs((lo[0]+hi[0])/2)<160 and hi[0]-lo[0]<3 and lo[2]>33 and hi[2]>40 and (hi[1]<cy or lo[1]>cy):
      vertices=np.unique(faces[fi]);heights=np.sort(np.unique(v[vertices,2]));split=np.argmax(np.diff(heights));lower=heights[split];upper=heights[split+1];side=1 if lo[1]>cy else -1
      weights=np.clip((upper-v[:,2])/(upper-lower),0,1)
      shoe=vertices[weights[vertices]==1]
      # The shoe's original inward wall leans with the rod. Its two inward
      # vertices on each measured end ring define the attachment plane. Remove
      # that small shear so the entire wall contacts the vertical road edge.
      shoePoints=np.unique(v[shoe],axis=0);ordered=shoePoints[np.argsort(shoePoints[:,2])];inner=[]
      for ring in (ordered[:8],ordered[8:]):
       # Extra authored midpoint vertices must not replace either end of the wall.
       xmin,xmax=ring[:,0].min(),ring[:,0].max()
       for end in (ring[ring[:,0]<xmin+(xmax-xmin)*.25],ring[ring[:,0]>xmax-(xmax-xmin)*.25]):
        inner.append(end[np.argmin(side*(end[:,1]-cy))])
      inner=np.asarray(inner)
      design=np.column_stack((inner[:,2],inner[:,0],np.ones(len(inner))))
      wall=np.linalg.lstsq(design,side*(inner[:,1]-cy),rcond=None)[0]
      tilt=-(wall[0]*(v[:,2]-v[shoe,2].min())+wall[1]*(v[:,0]-v[shoe,0].mean()))
      delta=float(roadhi-roadlo)/2-float(np.min(side*(v[shoe,1]-cy)+tilt[shoe]))
      rise=float(datum)-.5-float(v[shoe,2].min())
      hangerShift[vertices]=side*(np.float32(delta)+tilt[vertices])*weights[vertices]
      hangerRise[vertices]=np.float32(rise)*weights[vertices]
      hangers.append(dict(component=int(c),faces=fi.tolist(),sourceVertices=vertices.tolist(),upperVertices=vertices[weights[vertices]==0].tolist(),lowerVertices=shoe.tolist(),lowerJoint=float(lower),upperJoint=float(upper),outwardShift=float(delta),rise=rise,side=side,shoeWall=wall.tolist(),shoeOrigin=[float(v[shoe,2].min()),float(v[shoe,0].mean())],shoeTop=float(v[shoe,2].max()+rise-datum)))
     # Whole terminal tower components, ornaments and windows have independent islands.
     if lo[0]>260:ownership[fi]=2
     elif hi[0]<-260:ownership[fi]=1
     if ownership[fi[0]]:
      assert hi[1]<cy or lo[1]>cy, 'Terminal island crosses the open roadway'
      if lo[1]>cy:ownership[fi]+=2
     coeff[fi]=np.sign(q[fi,:,1]-cy)
     # Every chevron end section follows its side by the full half-width delta.
     # Keep the central joint centred; original triangles bridge the two anchors.
     if slot==0 and lo[1]<cy<hi[1] and hi[2]<datum:
      bounds=np.concatenate((lo,hi)).tolist()
      if lod==0:braceProfiles.append(bounds)
      else:assert bounds in braceProfiles, 'Chevron LOD differs from measured LOD0 assembly'
      center=np.float32((lo[1]+hi[1])/2);span=np.float32(hi[1]-lo[1])
      vertices=np.unique(faces[fi]);distance=abs(v[vertices,1]-center)
      # The source has central joint rings and distinct outer end rings. The
      # largest measured transverse gap separates those authored domains.
      levels=np.sort(np.unique(distance));cut=(levels[np.argmax(np.diff(levels))]+levels[np.argmax(np.diff(levels))+1])/2
      central=vertices[distance<cut];ends=vertices[distance>=cut]
      coeff[fi]=np.where(np.isin(faces[fi],central),0,np.sign(q[fi,:,1]-center))
      hangerShift[central]=cy-center
      braceFaces.update(fi.tolist())
      braces.append(dict(component=int(c),faces=fi.tolist(),center=float(center),span=float(span),bounds=bounds,centralVertices=central.tolist(),endVertices=ends.tolist()))
    # Shallow welded arch-root wedges belong to the continuous arch shell.
    # Separating them rigidly would break their finite shared interface under length adaptation.
    parts=[]
    for part in range(5):
     base=np.column_stack((v[:,1]-(spancy if part==0 else cy),v[:,2]-(datum if part==0 else towerdatum),v[:,0]-cx)).astype('<f4')
     if part==0:
      base[:,0]+=columnshift;base[:,1]+=columnheight
     member=ownership==part;lookup={};ids=[];orig=[];dc=[];grad=[];supportframes=[];corners=np.zeros(faces.shape,dtype='<i4');res=0.
     for fi in np.flatnonzero(member):
      face=faces[fi];points=base[face];d=coeff[fi];mat=points[1:].astype('f8')-points[0];b=d[1:].astype('f8')-d[0];df=np.linalg.lstsq(mat,b,rcond=None)[0];res=max(res,float(np.max(abs(mat@df-b))));df=df.astype('f4');sw=supportweight[face];flatten=sw*(datum-np.float32(.5)-v[face,2])+hangerRise[face];fg=np.linalg.lstsq(mat,flatten[1:].astype('f8')-flatten[0],rcond=None)[0].astype('f4');hs=hangerShift[face];hg=np.linalg.lstsq(mat,hs[1:].astype('f8')-hs[0],rcond=None)[0].astype('f4')
      if fi in braceFaces:
       shifted=points.copy();shifted[:,0]+=hs
       df=np.linalg.lstsq(shifted[1:].astype('f8')-shifted[0],b,rcond=None)[0].astype('f4')
      for j,vi in enumerate(face):
       k=(int(vi),d[j].tobytes(),df.tobytes(),fg.tobytes(),hg.tobytes())
       if k not in lookup:
        lookup[k]=len(ids);ids.append(int(vi));o=base[vi].copy()
        if part:o[2]-=anchors[1+(part-1)%2]-cx
        o[1]+=flatten[j];o[0]+=hs[j]
        if part and balcony[vi]:o[1]-=np.float32(.5)
        orig.append(o);dc.append(d[j]);grad.append(df);supportframes.append(np.concatenate((hg,fg)))
       corners[fi,j]=lookup[k]
     prefix=f'Types_Lupu_{lod}_{slot}_{part}_';data={'Origins':np.asarray(orig).reshape(-1,3),'Displacement':dc,'Frames':np.asarray(grad).reshape(-1,3)[:,0],'ShearY':np.asarray(grad).reshape(-1,3)[:,1],'ShearZ':np.asarray(grad).reshape(-1,3)[:,2],'SourceVertices':ids,'Corners':corners,'Omitted':np.flatnonzero(~member)}
     assert res<1e-5,res
     for kind,values in data.items():
      key=prefix+kind;keys.append(key);write(out/(key+'.bin'),values,kind in ('SourceVertices','Corners','Omitted'))
     if part==0 and slot==0:
      key=f'Types_Lupu_{lod}_{slot}_SupportFrames';keys.append(key);write(out/(key+'.bin'),supportframes)
     parts.append(dict(part=part,faces=int(member.sum()),vertices=len(ids),frameResidual=res))
    assert sum(p['faces'] for p in parts)==len(faces)
    rec['slots'].append(dict(slot=slot,vertices=len(v),faces=len(faces),bounds=[v.min(0).tolist(),v.max(0).tolist()],parts=parts,columnMoves=columnmoves,balconySourceVertices=np.flatnonzero(balcony).tolist(),hangers=hangers,braces=braces))
   records.append(rec)
 report={'sourceMod':151335,'revision':2,'publishedDimensions':None,'referenceMeaning':'Measured terminal-tower centre interval; selected-road outer edges match main arch outer edges at the inward slab bottom datum. Unified source frame; centred chevron joints with fully translated outer end sections, side-part width displacement and column caps and inward lower-slab bottoms meet at the fixed construction plane y=-0.5; all height adaptation is baked offline; no source roadway mesh.','parameters':[float(x) for x in constants],'lods':records}
 (ev/'measurements.json').write_text(json.dumps(report,indent=2));(ev/'resource-keys.json').write_text(json.dumps(keys,indent=2));print(json.dumps(report))
if __name__=='__main__':main()


