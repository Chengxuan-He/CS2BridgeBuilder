"""Emit measured prepared-unit consumer metadata; no baking or deployment."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'artifacts/material-remap/Draw01/near-units'
base=json.loads((OUT/'manifest.json').read_text())['units']
grid=json.loads((OUT/'grid-bake-report.json').read_text())['units']
rows=[]
for domain in range(4):
    if domain==0:
        report=json.loads((OUT/'bounded-paired-report.json').read_text())['units'][0]
    elif domain==1:
        report=json.loads((OUT/'grating-phase2-bake-report.json').read_text())['units'][0]
    else: report=grid[domain]
    source=base[domain]
    channel=report['channels'][0]
    assert all(c['contentMin']==channel['contentMin'] and c['contentMax']==channel['contentMax'] for c in report['channels'])
    rows.append((source,report,channel))
def f(n): return format(n,'.17g')+'f' if '.' in format(n,'.17g') else format(n,'.17g')+'.0f'
arms=[]
for domain,(source,report,channel) in enumerate(rows):
    vals=[*source['min'],*source['max'],*channel['contentMin'],*channel['contentMax']]
    arms.append(f'            {domain} => new Unit("{report["key"]}", '+', '.join(f(v) for v in vals)+'),')
code='''using System;
using BridgeBuilder.Runtime;
using UnityEngine;

namespace BridgeBuilder.Bridges;
// Offline native source-unit candidates. Sampling acceptance is tracked in the
// retained Draw01 reports; loading this graph does not certify its quality.
internal static class Draw01PreparedRoadUnit
{
    private readonly struct Unit
    {
        internal readonly string Key;
        internal readonly Vector2 SourceMin, SourceMax, Min, Max;
        internal Unit(string key,float x0,float y0,float x1,float y1,float u0,float v0,float u1,float v1)
        { Key=key;SourceMin=new Vector2(x0,y0);SourceMax=new Vector2(x1,y1);Min=new Vector2(u0,v0);Max=new Vector2(u1,v1); }
    }
    internal static MovableExistingRoadSurface.Patch Create(int domain,Vector4 u,Vector4 v,Matrix4x4 frame,float sourceUmin,float sourceUmax,Vector4? tangent=null)
    {
        var unit=domain switch
        {
ARMS
            _ => throw new ArgumentOutOfRangeException(nameof(domain))
        };
        // Finite U remains inside the actual producer guard. Continuous V can
        // cross any number of source periods without vertex modulo or cuts.
        float us=(unit.Max.x-unit.Min.x)/(unit.SourceMax.x-unit.SourceMin.x);
        float vs=(unit.Max.y-unit.Min.y)/(unit.SourceMax.y-unit.SourceMin.y);
        u*=us;u.w+=unit.Min.x-unit.SourceMin.x*us;
        v*=vs;v.w+=unit.Min.y-unit.SourceMin.y*vs;
        var min=new Vector2(unit.Min.x+(sourceUmin-unit.SourceMin.x)*us,unit.Min.y);
        var max=new Vector2(unit.Min.x+(sourceUmax-unit.SourceMin.x)*us,unit.Max.y);
        return new MovableExistingRoadSurface.Patch(min,max,frame.transpose*u,frame.transpose*v,
            BridgePreparedMaterials.RepeatUnit("Draw01",unit.Key),preparedUnit:true,repeatV:true,tangent:tangent,projectTangent:!tangent.HasValue).WithFiniteSampling(Vector2.zero,Vector2.one);
    }
}
'''.replace('ARMS','\n'.join(arms))
(ROOT/'src/BridgeBuilder/Bridges/Types/Draw01/Draw01PreparedRoadUnit.cs').write_text(code)
(OUT/'continuous-unit-consumer-metadata.json').write_text(json.dumps({'qualityAccepted':False,'units':[{'domain':i,'source':s['id'],'key':r['key'],'sourceMin':s['min'],'sourceMax':s['max'],'contentMin':c['contentMin'],'contentMax':c['contentMax']} for i,(s,r,c) in enumerate(rows)]},indent=2))
