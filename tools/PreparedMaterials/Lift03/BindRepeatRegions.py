"""Add native road-template bindings and structural-region bindings without pixel changes."""
import argparse
import hashlib
import json
import shutil
import struct
from pathlib import Path
from IntegrateRepeatUnits import ROOT, cid, digest, string, copy_exact


def hash_bytes(value):
    return bytes(int(value[i+1]+value[i], 16) for i in range(0,32,2))


def road_descriptor(cutout, bindings):
    # Exact native descriptor semantics of PrepareRepeat.Surface; channels are
    # the persisted full-authored-mip products, never rebaked here.
    out=bytearray(struct.pack('<Hi?',1,14 if cutout else 0,False))
    out+=b'\xff'+struct.pack('<i',1)+string('_Snow')+struct.pack('<f',0)
    out+=(b'\xff'+struct.pack('<i',0))*3
    names=('_BaseColorMap','_NormalMap','_MaskMap','_ControlMask')
    out+=b'\xff'+struct.pack('<i',len(names))
    for name in names:
        out+=string(name)+b'\xff'+hash_bytes(bindings[name])
    keywords=('_ALPHATEST_ON','_DISABLE_SSR_TRANSPARENT','_DOUBLESIDED_ON','_TANGENTSPACE_OCTO') if cutout else ('_TANGENTSPACE_OCTO',)
    out+=b'\xff'+struct.pack('<i',len(keywords))
    for key in keywords:
        out+=string(key)
    return bytes(out)


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--publish',action='store_true')
    args=parser.parse_args()
    stage=ROOT/'artifacts/prepared-materials/Lift03/region-integration-v1'
    output=stage/'output/Lift03'
    formal=ROOT/'assets/Materials/Lift03'
    for source in formal.rglob('*'):
        if source.is_file():copy_exact(source,output/source.relative_to(formal))
    bake=json.loads((ROOT/'artifacts/prepared-materials/Lift03/repeat-units-candidate-v2/bake.json').read_text())
    aliases=[]
    structures=[]
    for unit in bake['units']:
        structures.append(dict(unit=unit['id'],bindingKey=unit['key'],sourceTemplate=1))
        bindings={p['name']:p['cid'] for p in unit['channels']}
        bindings['_ControlMask']='729893d593df5d52ba911bc1e7eab77b'
        for index,alias in enumerate(unit['aliases']):
            cutout=not(unit['id']=='Lift03.MainRoad' and index==1)
            raw=road_descriptor(cutout,bindings)
            identity=cid(raw)
            key=digest(string('BridgeBuilder/RepeatUnits/2')+string(identity))
            dest=output/'RepeatUnits'/key/'Repeat.Surface'
            dest.parent.mkdir(parents=True,exist_ok=True)
            if dest.exists():assert dest.read_bytes()==raw
            else:dest.write_bytes(raw);Path(str(dest)+'.cid').write_text(identity)
            aliases.append(dict(samplerKey=alias,bindingKey=key,template=14 if cutout else 0))
    lines=['using System.Collections.Generic;', '', 'namespace BridgeBuilder.Bridges;', '', 'internal static partial class MovableRepeatUnits', '{',
           '    // Embedded numerical arrays: Types_Lift03_Lift03RoadAppearance_cs',
           '    // Reuse its demand-loaded holder; the numeric literals below are array indices.',
           '    // Road templates retain the producer cutout/double-sided semantics.',
           '    // Structural regions instead retain the original single-sided template 1.',
           '    static partial void Lift03(string samplerKey, ref string? unitKey)', '    {', '        switch (samplerKey)', '        {']
    for row in aliases:
        lines += [f'            case "{row["samplerKey"]}":',f'                unitKey = "{row["bindingKey"]}";','                break;']
    lines+=['        }','    }','', '    static partial void Lift03Regions(string sourceDigest, List<Region> regions)', '    {',
            '        if (sourceDigest != "b416f8ead14d3208611b9a86d4b57688ddd1bb87acd0cfce5b541f6340f202f0") return;',
            '        var values = Numeric_Types_Lift03_Lift03RoadAppearance_cs.Values.F32;']
    for row,start in zip(structures,(0,10)):
        lines += [f'        regions.Add(new Region("{row["bindingKey"]}",',
                  f'            values[{start}] / values[{start+1}], values[{start+2}] / values[{start+3}],',
                  f'            values[{start+4}] / values[{start+5}], values[{start+6}] / values[{start+7}]));']
    lines+=['    }','}','']
    generated='\n'.join(lines)
    (stage/'Lift03RepeatUnits.cs').write_text(generated)
    files=[dict(path=str(p.relative_to(output)).replace('\\','/'),sha256=digest(p.read_bytes()),bytes=p.stat().st_size) for p in sorted(output.rglob('*')) if p.is_file()]
    if args.publish:
        graph=json.loads((stage/'graph-validation.json').read_text())
        assert graph['status']=='PASS'
        assert {r['path'].replace('\\','/'):r['sha256'] for r in graph['files']}=={r['path']:r['sha256'] for r in files}
        assert 'PASS native source descriptors:' in (stage/'native-reader.log').read_text()
        code=ROOT/'src/BridgeBuilder/Bridges/Types/Lift03/Lift03RepeatUnits.cs'
        if code.read_text()!=generated:
            backup=ROOT/'artifacts/backups/Lift03/region-binding-v1/Lift03RepeatUnits.cs'
            copy_exact(code,backup)
            code.write_text(generated)
        for source in output.rglob('*'):
            if source.is_file():copy_exact(source,formal/source.relative_to(output))
    report=dict(style='Lift03',roadAliases=aliases,structuralRegions=structures,fileCount=len(files),totalBytes=sum(r['bytes'] for r in files),files=files,
                newTextureFiles=0,rebakedPixels=0,published=args.publish,localization='No user-facing text changed')
    (stage/'integration.json').write_text(json.dumps(report,indent=2)+'\n')
    print(json.dumps({k:v for k,v in report.items() if k!='files'},indent=2))


if __name__=='__main__':main()
