"""Create a read-only native continuation probe in the independent resume project."""
import json
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
env=ROOT/'artifacts/material-resume/Lift04/env/unity'
evidence=json.loads((ROOT/'artifacts/material-resume/Lift04/outputs/mixed-axis-source-evidence.json').read_text())
bounds=evidence['exactUncoveredRectanglesPixels']
code=(ROOT/'tools/PreparedMaterials/Lift04/RepeatUnitCheck.Editor.cs').read_text()
code=code.replace('[Serializable] public sealed class Report', '[Serializable] public sealed class Continuation { public string channel; public int mip,region; public float maxSourceDifference,maxRepeatDifference,maxEdgeDifference; }\n [Serializable] public sealed class Report')
code=code.replace('public Level[] levels;', 'public Level[] levels; public Continuation[] continuations;')
code=code.replace('var levels=new List<Level>();','var levels=new List<Level>();var continuations=new List<Continuation>();\n   var coverage=new[]{'+','.join('new Vector4('+','.join(str(v/4096)+'f' for v in b)+')' for b in bounds)+'};')
code=code.replace('levels.Add(level);','''levels.Add(level);
      for(int region=0;region<coverage.Length;region++) {
       program.SetVector("Coverage",coverage[region]);
       continuations.Add(new Continuation{channel=channel.name,mip=mip,region=region,maxSourceDifference=Check(3),maxRepeatDifference=Check(4),maxEdgeDifference=Check(5)});
      }''')
code=code.replace('levels=levels.ToArray()', 'levels=levels.ToArray(),continuations=continuations.ToArray()')
code=code.replace('Debug.Log($"PASS', 'Need(continuations.All(c=>c.maxRepeatDifference<=.0001f),"Native continuation V Repeat phase mismatch");\n   Debug.Log($"PASS')
(env/'Assets/Editor/Lift04RepeatUnitCheck.cs').write_text(code)
compute=(ROOT/'tools/PreparedMaterials/Lift04/RepeatUnitCheck.compute').read_text().replace('float4 Region;', 'float4 Region;\nfloat4 Coverage;')
compute=compute.replace('} else {\n  // Compare', '''} else if(Mode>=3) {
  if(Mode==5) {
   if(p.x==0)q.x=0;
   if(p.x==Extent.x-1)q.x=1;
   if(p.y==0)q.y=0;
   if(p.y==Extent.y-1)q.y=1;
  }
  float2 authored=lerp(Coverage.xy,Coverage.zw,q);
  float2 local=(authored-Region.xy)/Region.zw;
  local.x=local.x*.5+.25;
  // Dyadic coordinates make adding three periods exact in Float32. This
  // isolates native wrap/filter periodicity from coordinate-addition rounding.
  if(Mode==4)local=round(local*256.0)/256.0;
  a=Mode==4 ? Product.SampleLevel(sampler_Product,local+float2(0,3),Mip) : Source.SampleLevel(sampler_Source,authored,Mip);
  b=Product.SampleLevel(sampler_Product,local,Mip);
 } else {
  // Compare''')
(env/'Assets/Lift04RepeatUnitCheck.compute').write_text(compute)
print(env)
