"""Set up an isolated offline candidate; never publish or modify shared bakers.

The candidate retains the original half-panel orientation and source triangles.
It is not filtered-edge, normal-frame, native-loading or package acceptance.
"""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
ENV = ROOT / 'artifacts/material-remap/environments/PedestrianDraw02'
OUT = ROOT / 'artifacts/material-remap/PedestrianDraw02'


def main():
    recipe_path = OUT / 'native-panel-recipe.json'
    recipe = json.loads(recipe_path.read_text())
    level = recipe['levels'][0]
    panel = next(p for p in level['panels'] if p['firstFace'] == 8556)
    assert panel['repeatU'] is False and panel['repeatedV']
    # U and V point in the original source orientation (-X, -Z), so this
    # extraction does not introduce a full-width mirrored normal-map frame.
    triangles = [value for t in panel['triangles'] for q, uv in zip(t['unit'], t['sourceUv'])
                 for value in (1-q[0], 1-q[1], *uv)]
    manifest = dict(recipe='BridgeBuilder/RepeatUnits/2', style='PedestrianDraw02', units=[dict(
        id='PedestrianDraw02.MovingNativeHalfPanel', sourceCid=recipe['sourceCid'],
        sourceDigest=recipe['descriptor']['sha256'], descriptor=recipe['descriptor'],
        channels=[dict(c, encoding='BC7') for c in recipe['channels']],
        min=panel['sourceUvMin'], max=panel['sourceUvMax'], physicalSize=panel['physicalSize'],
        evidence=dict(path=str(recipe_path), sha256=hashlib.sha256(recipe_path.read_bytes()).hexdigest()),
        repeatAxes='V', triangles=triangles, aliases=[])])
    (OUT / 'native-panel-candidate-manifest.json').write_text(json.dumps(manifest, indent=2)+'\n')
    editor = ENV / 'unity/Assets/Editor'
    editor.mkdir(parents=True, exist_ok=True)
    source = (ROOT / 'tools/GpuBake/PrepareRepeatUnits.Editor.cs').read_text()
    # Private extension only. Keep a captured input hash to make the exact
    # candidate reproducible when the shared authoring pipeline changes.
    digest = hashlib.sha256(source.encode()).hexdigest()
    replacements = [
        ('public static class PrepareRepeatUnits', 'public static class PrepareNativePanelCandidate'),
        ('public string repeatAxes;', 'public string repeatAxes; public float[] triangles;'),
        ('Assets/RepeatUnit.compute', 'Assets/NativePanelCandidate.compute'),
        ('unit.bands,unit.RepeatU,unit.RepeatV);', 'unit.bands,unit.RepeatU,unit.RepeatV,unit.triangles);'),
        ('bool repeatU=true,bool repeatV=true)', 'bool repeatU=true,bool repeatV=true,float[] triangles=null)'),
        ('int kernel=program.FindKernel("Extract");', '''int kernel=program.FindKernel("Extract");
            if(triangles==null||triangles.Length%12!=0)throw new InvalidDataException("Native panel triangle recipe missing.");
            var points=new Vector4[triangles.Length/4];
            for(int i=0;i<points.Length;i++)points[i]=new Vector4(triangles[i*4],triangles[i*4+1],triangles[i*4+2],triangles[i*4+3]);
            using var panelBuffer=new ComputeBuffer(points.Length,16);panelBuffer.SetData(points);
            program.SetBuffer(kernel,"PanelTriangles",panelBuffer);program.SetInt("PanelTriangleCount",points.Length/3);'''),
    ]
    for old, new in replacements:
        assert source.count(old) == 1, old
        source = source.replace(old, new)
    (editor / 'PrepareNativePanelCandidate.cs').write_text(source)
    for name in ('PreparePaint', 'NativeTextureEncoding'):
        (editor / (name+'.cs')).write_bytes((ROOT / 'tools/GpuBake' / (name+'.Editor.cs')).read_bytes())
    for name in ('MovableTextureSource', 'MovableVtTextureSource'):
        code = (ROOT / 'src/BridgeBuilder/Bridges/Common/Movable' / (name+'.cs')).read_text()
        code = code.replace('namespace BridgeBuilder.Bridges;', 'namespace BridgeBuilder.Bridges {')+'\n}\n'
        code = code.replace('UnityEngine.Object.Destroy(', 'UnityEngine.Object.DestroyImmediate(')
        code = code.replace('makeNoLongerReadable: true', 'makeNoLongerReadable: false')
        code = code.replace('texture.Apply(false,true)', 'texture.Apply(false,false)')
        (editor / (name+'.cs')).write_text(code)
    shader = (ROOT / 'tools/GpuBake/RepeatUnit.compute').read_text()
    shader = shader.replace('int BandCount;', '''int BandCount;
StructuredBuffer<float4> PanelTriangles;
int PanelTriangleCount;
float cross2(float2 a,float2 b){return a.x*b.y-a.y*b.x;}
float2 panelUv(float2 q) {
    float best=1e30;float2 result=0;
    for(int i=0;i<PanelTriangleCount;i++) {
        float4 a=PanelTriangles[i*3],b=PanelTriangles[i*3+1],c=PanelTriangles[i*3+2];
        float det=cross2(b.xy-a.xy,c.xy-a.xy);
        float v=cross2(q-a.xy,c.xy-a.xy)/det;
        float w=cross2(b.xy-a.xy,q-a.xy)/det;
        float3 bary=float3(1-v-w,v,w);
        if(all(bary>=0))return a.zw*bary.x+b.zw*bary.y+c.zw*bary.z;
        // Original micrometre cracks are not welded. Extend the nearest
        // triangle edge only into the uncovered authoring footprint.
        bary=max(bary,0);bary/=bary.x+bary.y+bary.z;
        float2 edge=a.xy*bary.x+b.xy*bary.y+c.xy*bary.z;
        float distance=dot(edge-q,edge-q);
        if(distance<best){best=distance;result=a.zw*bary.x+b.zw*bary.y+c.zw*bary.z;}
    }
    return result;
}''')
    shader = shader.replace('float4 c=Source.SampleLevel(sampler_Source,uv,SourceMip);',
                            'uv=panelUv(q);\n    float4 c=Source.SampleLevel(sampler_Source,uv,SourceMip);')
    (ENV / 'unity/Assets/NativePanelCandidate.compute').write_text(shader)
    settings = ENV / 'unity/ProjectSettings/ProjectSettings.asset'
    settings.write_text(settings.read_text().replace('m_ActiveColorSpace: 0','m_ActiveColorSpace: 1'))
    (OUT / 'native-panel-candidate-pipeline.json').write_text(json.dumps(dict(
        sharedBakerTextSha256=digest, candidateBakerSha256=hashlib.sha256(source.encode()).hexdigest(),
        shaderSha256=hashlib.sha256(shader.encode()).hexdigest(),
        limitations=['Candidate only: filtered mip equivalence and original tangent decoding pending.',
                    'Does not resolve fixed finite patches or integrate runtime routing.']), indent=2)+'\n')
    print('Prepared private candidate authoring project and measured half-panel manifest.')


if __name__ == '__main__':
    main()
