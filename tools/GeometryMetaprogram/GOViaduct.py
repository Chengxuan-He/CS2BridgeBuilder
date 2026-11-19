"""Offline GO 224 m source inventory and measured, existing-format numerical .bin recipes."""
from pathlib import Path
import argparse, hashlib, json, struct, tempfile, zipfile
import numpy as np
from UvDensityAudit import NativeGeometry

ENTRIES = [
    'GO-224m-ViaductBridge_07e63aa7306b5cbb2dbf522c814d5e10.Geometry',
    'GO-224m-ViaductBridge_LOD1_06a9f72a46c9ecaf9d287bbeb92dab9a.Geometry',
    'GO-224m-ViaductBridge_LOD2_a6f76703a31e05a2191b3a98095cba46.Geometry',
]

# Each recessed roadway grate includes both its upper sheet and the separate lower rods.
# LOD1/2 represent these assemblies in the planar Y=9.5 road faces removed below.
GRATE_RODS = (tuple(range(48812,49316))+tuple(range(50868,51372)), (), ())
REMOVED = (tuple(range(47924,48070))+tuple(range(49980,50126))+GRATE_RODS[0]+(42,44,204,205),
           (158,160,288,289), ())
CLIPPED_FAR = (14,17,22,25)

def clip_lower(triangle):
    """Keep the far end wall below the full-detail fascia's measured Y=8 seam."""
    polygon=[(triangle[i].astype('f8'),np.eye(3)[i]) for i in range(3)]
    output=[]
    for a,b in zip(polygon,polygon[1:]+polygon[:1]):
        inside=a[0][1]<=8; other=b[0][1]<=8
        if inside:output.append(a)
        if inside!=other:
            t=(8-a[0][1])/(b[0][1]-a[0][1])
            output.append((a[0]+t*(b[0]-a[0]),a[1]+t*(b[1]-a[1])))
    return [np.asarray([output[0][1],output[i][1],output[i+1][1]]) for i in range(1,len(output)-1)]

def write_bbn(path, floats=(), ints=()):
    f=np.asarray(floats,dtype='<f4').ravel();i=np.asarray(ints,dtype='<i4').ravel()
    body=struct.pack('<6i',len(i),0,0,0,len(f),0)+i.tobytes()+f.tobytes()
    path.write_bytes(body+hashlib.sha256(body).digest())

def main():
    parser=argparse.ArgumentParser();parser.add_argument('archive');parser.add_argument('game');parser.add_argument('output')
    a=parser.parse_args();out=Path(a.output);out.mkdir(parents=True,exist_ok=True)
    reader=NativeGeometry(Path(a.game)/'Plugins/x86_64/PipelinePlugin.dll')
    records=[]
    with zipfile.ZipFile(a.archive) as archive, tempfile.TemporaryDirectory(dir=out) as tmp:
        for lod,entry in enumerate(ENTRIES):
            raw=archive.read(entry);path=Path(tmp)/entry;path.write_bytes(raw)
            meshes=reader.read(path);assert len(meshes)==1
            mesh=meshes[0];v=mesh['positions'];faces=mesh['faces'];q=v[faces]
            # The authored running sheet is planar at 9.5, spanning the central roadway.
            # Store exact face IDs; no runtime plane/height classification is emitted.
            road=np.all(q[:,:,1]==np.float32(9.5),axis=1)
            assert int(road.sum())==(8,12,6)[lod]
            road[list(REMOVED[lod])]=True
            # Right-handed source frame: longitudinal X -> -Z, transverse Z -> X.
            origin=np.column_stack((v[:,2],v[:,1]-np.float32(9.5),-v[:,0])).astype('<f4')
            z=v[:,2];coeff=np.sign(z).astype('<f4')
            # Roof transverse assemblies attach at the main beams' measured +/-11.199951.
            # The side arches stay rigid. Detailed membership is emitted, never inferred live.
            roof=(v[:,1]>np.float32(15))&(np.abs(z)<=np.float32(11.199951171875))
            # The centre chord/connection plates extend to +/-1.200012; retain their
            # section, and lengthen ties between this station and the side connection.
            inner=np.float32(1.20001220703125);outer=np.float32(11.199951171875)
            coeff[roof]=np.sign(z[roof])*np.maximum(np.abs(z[roof])-inner,0)/(outer-inner)
            # Split only attribute vertices with distinct face differentials. No face
            # is subdivided. Every new vertex retains its exact original channel index.
            source_ids=[];differentials=[];corners=np.zeros(faces.shape,dtype='<i4');lookup={};max_residual=0.
            for fi,face in enumerate(faces):
                if road[fi]:continue
                a=origin[face[1:]].astype('f8')-origin[face[0]];b=coeff[face[1:]].astype('f8')-coeff[face[0]]
                gradient=np.linalg.lstsq(a,b,rcond=None)[0]
                max_residual=max(max_residual,float(np.max(np.abs(a@gradient-b))))
                gradient=gradient.astype('<f4')
                for corner,vertex in enumerate(face):
                    key=(int(vertex),gradient.tobytes())
                    if key not in lookup:
                        lookup[key]=len(source_ids);source_ids.append(int(vertex));differentials.append(gradient)
                    corners[fi,corner]=lookup[key]
            source_ids=np.asarray(source_ids,dtype='<i4');gradients=np.asarray(differentials,dtype='<f4')
            assert max_residual<1e-6,max_residual
            # Authored concrete end caps and foundations: inspected complete source islands,
            # including their upper closures, not a height cut. Source vertex order is frozen.
            support_ranges=(((0,432),(41748,41896)),((184,700),),((0,156),(770,782)))[lod]
            support=np.zeros(len(v),dtype=bool)
            for first,last in support_ranges:support[first:last]=True
            assert np.all(support[faces].all(1)==support[faces].any(1))
            support_faces=support[faces].all(1)
            assert np.all(np.abs(v[support,0])>=82)
            # Complete authored foundation blocks are separate source-index islands.
            foundation=np.zeros(len(v),dtype=bool)
            for first,last in (((41748,41896),),((552,700),),((76,156),))[lod]:foundation[first:last]=True
            assert np.all(foundation[faces].all(1)==foundation[faces].any(1))
            foundation_faces=foundation[faces].all(1)
            part_results=[]
            for part in range(5):
                side_positive=part in (1,3)
                membership=(~support_faces if part==0 else
                    (foundation_faces if part>=3 else support_faces & ~foundation_faces)
                    & (q[:,:,0].mean(1)>0 if side_positive else q[:,:,0].mean(1)<0)) & ~road
                assert membership.any()
                used=np.unique(corners[membership]);remap=np.zeros(len(source_ids),dtype='<i4');remap[used]=np.arange(len(used),dtype='<i4')
                part_corners=np.zeros_like(corners);part_corners[membership]=remap[corners[membership]]
                ids=source_ids[used];positions=origin[ids].copy()
                uv=mesh['uv0'][ids].astype('<f4').copy()
                part_coeff=coeff[ids].copy();part_gradients=gradients[used].copy()
                if lod==2 and part in (1,2):
                    triangles=[];source_faces=[]
                    source_triangles=np.repeat(ids[:,None],3,axis=1).tolist()
                    weights=np.tile([1.,0.,0.],(len(ids),1)).tolist()
                    p_list=positions.tolist();uv_list=uv.tolist();c_list=part_coeff.tolist();g_list=part_gradients.tolist()
                    for fi in np.flatnonzero(membership):
                        if fi not in CLIPPED_FAR:
                            triangles.append(part_corners[fi].tolist());source_faces.append(int(fi));continue
                        for w in clip_lower(q[fi]):
                            indices=[]
                            for row in w:
                                indices.append(len(p_list));source_triangles.append(faces[fi].tolist());weights.append(row.tolist())
                                p_list.append((row@origin[faces[fi]].astype('f8')).tolist())
                                uv_list.append((row@mesh['uv0'][faces[fi]].astype('f8')).tolist())
                                c_list.append(float(row@coeff[faces[fi]].astype('f8')))
                                g_list.append(gradients[corners[fi,0]].tolist())
                            triangles.append(indices);source_faces.append(int(fi))
                    positions=np.asarray(p_list,dtype='<f4');uv=np.asarray(uv_list,dtype='<f4')
                    part_coeff=np.asarray(c_list,dtype='<f4');part_gradients=np.asarray(g_list,dtype='<f4')
                    prefix=f'Types_GOViaduct_{lod}_{part}_'
                    for key,values in [('SourceTriangles',source_triangles),('Triangles',triangles),('SourceFaces',source_faces)]:write_bbn(out/(prefix+key+'.bin'),ints=values)
                    write_bbn(out/(prefix+'Weights.bin'),weights)
                # Native network UV decoding wraps a positive atlas edge 1 to zero.
                # Only network parts use the nearest half-representable interior endpoint.
                if part<3:uv[uv[:,1]==1,1]=np.nextafter(np.float16(1),np.float16(0),dtype=np.float16)
                # Approach pieces use their measured midpoint; rigid foundations use the steel joint.
                if part:positions[:,2]+=(101.5 if part<3 else 91)*(1 if part in (1,3) else -1)
                prefix=f'Types_GOViaduct_{lod}_{part}_'
                write_bbn(out/(prefix+'Origins.bin'),positions)
                write_bbn(out/(prefix+'Displacement.bin'),part_coeff)
                for axis,key in enumerate(('Frames','ShearY','ShearZ')):write_bbn(out/(prefix+key+'.bin'),part_gradients[:,axis])
                write_bbn(out/(prefix+'Uv0.bin'),uv)
                write_bbn(out/(prefix+'Omitted.bin'),ints=np.flatnonzero(~membership))
                write_bbn(out/(prefix+'SourceVertices.bin'),ints=ids)
                write_bbn(out/(prefix+'Corners.bin'),ints=part_corners)
                part_results.append(dict(part=part,sourceFaces=int(membership.sum()),faces=len(triangles) if lod==2 and part in (1,2) else int(membership.sum()),vertices=len(positions),bounds=[positions.min(0).tolist(),positions.max(0).tolist()]))
            assert sum(p['sourceFaces'] for p in part_results)==int((~road).sum())
            records.append(dict(lod=lod,entry=entry,cid=archive.read(entry+'.cid').decode().strip(),
                sha256=hashlib.sha256(raw).hexdigest(),vertices=len(v),triangles=len(faces),
                retainedTriangles=sum(p['faces'] for p in part_results),omittedRoadTriangles=np.flatnonzero(road).tolist(),
                parts=part_results,outputVertices=len(source_ids),frameResidual=max_residual,bounds=[v.min(0).tolist(),v.max(0).tolist()]))
    (out/'GOViaduct-measurements.json').write_text(json.dumps(records,indent=2))
    print(json.dumps(records,indent=2))

if __name__=='__main__':main()
