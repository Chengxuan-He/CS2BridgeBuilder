"""Record the one measured Lift04 repeating road unit and its four native uses."""
import hashlib
import json
from pathlib import Path
import struct
import numpy as np

from ExportRecipes import source_recipes
from VerifyCoverage import hash_text, read_assets

ROOT=Path(__file__).resolve().parents[3]
GAME=Path('D:/Program Files (x86)/Steam/steamapps/common/Cities Skylines II/Cities2_Data')


def digest(raw):return hashlib.sha256(raw).hexdigest()


def main():
    inventory=json.loads(Path(__file__).with_name('recipes.json').read_text())
    recipe=inventory['units'][0]
    units,evidence=source_recipes(ROOT);unit=units[0]
    assert unit['min']==recipe['min'] and unit['max']==recipe['max']
    original=read_assets(GAME,{recipe['sourceCid']})[recipe['sourceCid']]
    assert digest(original)==recipe['sourceDigest']
    assert original[6]==1 and original[283]==0
    cids=[hash_text(original[83+i*16:99+i*16]) for i in range(3)]
    payloads=read_assets(GAME,set(cids))
    channels=[]
    for name,cid in zip(('_BaseColorMap','_NormalMap','_MaskMap'),cids):
        raw=payloads[cid]
        assert raw[:3]==b'\x01\0\xff' and struct.unpack_from('<H',raw,19)[0]==0
        tile,width,height,mid=struct.unpack_from('<4H',raw,21)
        mips,fmt,filtering,wrap,aniso=raw[29:34]
        tail=struct.unpack_from('<i',raw,34)[0];table=38+tail
        count=struct.unpack_from('<i',raw,table)[0]
        offsets=struct.unpack_from('<'+'i'*count,raw,table+4)
        start=table+4+count*4
        assert tail>0 and count>=width//tile*(height//tile)
        assert all(b>a for a,b in zip((0,)+offsets[:-1],offsets)) and start+offsets[-1]==len(raw)
        assert mips==max(width,height).bit_length()
        levels=[]
        for level in range(mips):
            size=[max(1,width>>level),max(1,height>>level)]
            levels.append(dict(level=level,sourceSize=size,sourceRegionMinPixels=[unit['min'][i]*size[i] for i in range(2)],
                sourceRegionMaxPixels=[unit['max'][i]*size[i] for i in range(2)]))
        channels.append(dict(property=name,cid=cid,sha256=digest(raw),nativeBytes=len(raw),
            kind='VTTexture',width=width,height=height,mipCount=mips,format=fmt,
            filter=filtering,wrap=wrap,aniso=aniso,tileSize=tile,nativeMidMipField=mid,
            mipTailBytes=tail,mipTailSHA256=digest(raw[38:table]),tileBlocks=count,
            fullTilePayloadExtentVerified=True,levels=levels,
            preservation='Use every authored source mip; no mip-zero-only decoding or regeneration.'))
    mesh_path=ROOT/'artifacts/uv-density/native/LiftBridge04 Mesh.npz'
    mesh=np.load(mesh_path);faces=mesh['mesh0_faces'];positions=mesh['mesh0_positions'];uv=mesh['mesh0_uv0'][:,:2]
    pixel_size=(np.array(unit['max'])-unit['min'])*[channels[0]['width'],channels[0]['height']]
    uses=[]
    for alias,pair,length in ((0,(33328,33329),evidence['projectionLengths'][0]),
                              (1,(10581,10582),evidence['projectionLengths'][1]),
                              (2,(28426,28427),evidence['projectionLengths'][2]),
                              (3,(4198,4199),evidence['projectionLengths'][2])):
        ids=np.unique(faces[list(pair)])
        assert np.array_equal(uv[ids].min(0),unit['min']) and np.array_equal(uv[ids].max(0),unit['max'])
        actual=[];areas=[];edges={}
        for face in pair:
            corners=faces[face];p=positions[corners][:,(0,2)].astype(float);q=uv[corners].astype(float)
            gradient=np.linalg.solve(np.column_stack((p,np.ones(3))),q)[:2]
            density=np.linalg.norm(gradient*[channels[0]['width'],channels[0]['height']],axis=1)
            expected=np.array([pixel_size[1]/evidence['lateralLength'],pixel_size[0]/length])
            assert np.max(np.abs(density/expected-1))<.00002
            actual.append(dict(face=face,vertices=corners.tolist(),positions=positions[corners].tolist(),uv=q.tolist(),
                pixelsPerMetreXZ=density.tolist(),sourceGradientXZ=gradient.tolist()))
            a,b,c=q;areas.append(abs(float(np.linalg.det(np.stack((b-a,c-a)))))/2)
            for a,b in zip(q,np.roll(q,-1,axis=0)):
                key=tuple(sorted((tuple(a),tuple(b))));edges[key]=edges.get(key,0)+1
        boundary=[list(map(list,k)) for k,v in edges.items() if v==1]
        assert len(boundary)==4 and np.isclose(sum(areas),np.prod(np.array(unit['max'])-unit['min']),rtol=1e-12,atol=0)
        uses.append(dict(alias=alias,render='LiftBridge04 Mesh',mesh=0,faces=list(pair),
            region='fixed-approach' if alias<2 else 'moving-deck',physicalSizeUV=[length,evidence['lateralLength']],
            densityUV=(pixel_size/[length,evidence['lateralLength']]).tolist(),boundary=boundary,sourceFaces=actual))
    samplers=json.loads((ROOT/'artifacts/prepared-materials/Lift04/shared-sampling/map.json').read_text())['samplers']
    linked=[];unmapped=[]
    for s in samplers:
        exact=s['sourceDigest']==recipe['sourceDigest'] and not s['triangle'] and np.array_equal(np.array(s['bounds'],np.float32),np.array(unit['min']+unit['max'],np.float32))
        (linked if exact else unmapped).append(s['key'])
    data=dict(schema='BridgeBuilder/RepeatUnits/2',style='Lift04',sources=inventory['sources'],
        units=[dict(id='Lift04RoadUnit',sourceCid=recipe['sourceCid'],sourceSHA256=recipe['sourceDigest'],
            sourceDigest=recipe['sourceDigest'],min=unit['min'],max=unit['max'],baseLevelPixelExtent=pixel_size.tolist(),
            physicalSizeUV=uses[0]['physicalSizeUV'],densityUV=uses[0]['densityUV'],physicalScalePolicy='Per-use source sizes/densities are authoritative; pixel unit is shared.',
            cutout=True,rows=False,bands=None,channels=channels,uses=uses,
            oldRepeatKeys=[recipe['key']],oldStructureKeys=linked,
            descriptorPolicy='Road cutout and structural descriptor parameters remain distinct; shared texels do not imply identical material descriptors.',
            unitEvidence='All four original quadrilateral road surfaces map the same measured complete rectangle; no arbitrary triangle/chart creates a unit.')],
        unmatchedStructureKeys=unmapped,unmatchedPolicy='Unimplemented source-level chart mapping; never bake arbitrary triangle/chart payloads.',
        evidence=dict(sourceMeshSHA256=digest(mesh_path.read_bytes()),**evidence),
        outputRequirements=['Preserve fractional source extents at lower mip levels.','Do not rescale 638 base texels to block-aligned 640 texels.',
            'Source payload headers, tails and all tile extents are verified here; actual decoded per-mip texels and native output need the new full-mip producer/validator.'],
        state='SOURCE_UNIT_MEASURED_OUTPUT_NOT_BAKED',localization='No runtime UI text or localization changes.')
    output=Path(__file__).with_name('repeat-units-v2.json');output.write_text(json.dumps(data,indent=2)+'\n')
    proof=ROOT/'artifacts/prepared-materials/Lift04/rebuild-native/repeat-unit-evidence.json'
    proof.write_text(json.dumps(data,indent=2)+'\n')
    # Supply complete, byte-identical native source inputs to the shared decoder.
    # They are temporary authoring inputs, never a packaged builtin texture copy.
    inputs=ROOT/'artifacts/agent-material-env/Lift04/repeat-units-v2/inputs'
    inputs.mkdir(parents=True,exist_ok=True)
    (inputs/'source.Surface').write_bytes(original)
    for cid,raw in payloads.items():(inputs/(cid+'.VTTexture')).write_bytes(raw)
    staged=dict(recipe='BridgeBuilder/RepeatUnits/2',style='Lift04',state='SOURCE_INPUT_READY_FOR_FULL_MIP_VT_EXTRACTION',
        evidence=dict(path=str(proof),sha256=digest(proof.read_bytes())),
        sourceDescriptor=dict(path=str(inputs/'source.Surface'),sha256=digest(original),nativeTemplate=0,virtualTexturing=True),
        units=[dict(id='Lift04RoadUnit',sourceCid=recipe['sourceCid'],sourceDigest=recipe['sourceDigest'],
            min=unit['min'],max=unit['max'],physicalSize=uses[0]['physicalSizeUV'],cutout=True,
            evidence=dict(path=str(proof),sha256=digest(proof.read_bytes())),
            descriptor=dict(path=str(inputs/'source.Surface'),sha256=digest(original)),
            aliases=[recipe['key'],*linked],
            channels=[dict(name=c['property'],cid=c['cid'],sha256=c['sha256'],
                path=str(inputs/(c['cid']+'.VTTexture')),kind='VTTexture',mipCount=c['mipCount']) for c in channels])])
    (inputs.parent/'native-inputs.json').write_text(json.dumps(staged,indent=2)+'\n')
    print(json.dumps(dict(units=1,uses=len(uses),pixels=pixel_size.tolist(),densityUV=[u['densityUV'] for u in uses],
        channels=len(channels),mipCounts=[c['mipCount'] for c in channels],linkedStructureKeys=len(linked),unmappedStructureKeys=len(unmapped),manifest=str(output))))


if __name__=='__main__':main()
